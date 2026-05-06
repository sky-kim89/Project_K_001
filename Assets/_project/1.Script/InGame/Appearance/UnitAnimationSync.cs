using BattleGame.Units;
using Unity.Entities;
using UnityEngine;

// ============================================================
//  UnitAnimationSync.cs
//  ECS UnitStateComponent → Animator 직접 연동 + 피격/사망 연출 컴포넌트.
//
//  ■ 상태 매핑
//    Idle              → bool "Idle"
//    Moving / Chasing  → bool "Run"
//    Attacking (대기)  → bool "Ready"
//    Attacking (발사)  → trigger "Slash"  ← 모든 직업 통일
//    Hit               → trigger "Hit"
//    Dead              → bool "Die"
//
//  ■ 공격 trigger 발동 시점
//    AttackCooldown 증가(= 공격 발생) 감지 → Slash trigger.
//    ApplyState 에서는 trigger 를 발동하지 않아 더블 트리거를 방지.
//
//  ■ 피격 연출
//    Hit 상태 진입 감지 → SpriteRenderer 색 플래시 (빨강 → 흰색)
//
//  ■ 사망 연출 (UnitDeathDespawnSystem 이 TriggerDeath() 를 호출)
//    1. EntityLink.SyncPosition = false → ECS 위치 덮어쓰기 중단
//    2. 현재 바라보는 반대 방향으로 ease-out 이동
//    3. _deathHoldDuration 동안 Die 애니메이션 대기
//    4. PoolController.Despawn() 로 풀 반납
//
//  ■ ECS Job 완료 보장
//    EntityLink 와 동일한 static 프레임 캐시로 1회만 CompleteAllTrackedJobs 호출.
//    DefaultExecutionOrder(10) 으로 EntityLink(0) 보다 늦게 실행해 안전성 확보.
//
//  ■ 스프라이트 방향
//    MovementComponent.Velocity.x 기반 transform.localScale.x 반전.
// ============================================================

[DefaultExecutionOrder(10)]  // EntityLink(0) 이후 실행 보장
[RequireComponent(typeof(EntityLink))]
public class UnitAnimationSync : MonoBehaviour
{
    [SerializeField] Animator _animator;

    [Header("피격 플래시")]
    [SerializeField] float _hitFlashDuration = 0.18f;
    [SerializeField] Color _hitFlashColor    = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("사망 연출")]
    [Tooltip("뒤로 날아가는 거리 (월드 단위)")]
    [SerializeField] float _deathFlyDistance  = 3.5f;
    [Tooltip("뒤로 날아가는 시간 (초)")]
    [SerializeField] float _deathFlyDuration  = 0.35f;
    [Tooltip("날아간 뒤 Die 애니메이션을 기다리는 추가 시간 (초)")]
    [SerializeField] float _deathHoldDuration = 0.75f;

    EntityLink     _link;
    SpriteRenderer _renderer;

    UnitState _prevState;
    float     _prevCooldown;
    float     _lastFacingX = 1f;
    bool      _isDying;
    Coroutine _hitCoroutine;

    UnitJob   _job;
    bool      _jobCached;

    // EntityLink 와 공유하는 CompleteAllTrackedJobs 프레임 캐시
    static int _lastCompletedFrame = -1;

    static readonly string[] BoolParams =
    {
        "Idle", "Ready", "Walk", "Run", "Crouch", "Crawl",
        "Jump", "Fall", "Land", "Block", "Climb", "Die",
    };

    // ── Unity 생명주기 ────────────────────────────────────────

    void Awake()
    {
        _link = GetComponent<EntityLink>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        _renderer = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable()
    {
        _prevState    = (UnitState)255; // 첫 프레임 강제 갱신
        _prevCooldown = 0f;
        _lastFacingX  = 1f;
        _isDying      = false;
        _hitCoroutine = null;
        _jobCached    = false;

        if (_renderer != null) _renderer.color = Color.white;
    }

    void LateUpdate()
    {
        if (_isDying) return;
        if (_animator == null) return;
        if (_link == null || _link.Entity == Entity.Null) return;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        EntityManager em = world.EntityManager;

        // ── ECS Job 완료 보장 (프레임당 1회) ─────────────────
        if (_lastCompletedFrame != Time.frameCount)
        {
            em.CompleteAllTrackedJobs();
            _lastCompletedFrame = Time.frameCount;
        }

        if (!em.Exists(_link.Entity)) return;

        // ── 현재 상태 읽기 ───────────────────────────────────
        if (!em.HasComponent<UnitStateComponent>(_link.Entity)) return;
        UnitState current = em.GetComponentData<UnitStateComponent>(_link.Entity).Current;

        // ── 공격 감지: 쿨다운 증가 = 공격 발생 ──────────────
        if (em.HasComponent<AttackComponent>(_link.Entity))
        {
            float cooldown = em.GetComponentData<AttackComponent>(_link.Entity).AttackCooldown;

            if (current == UnitState.Attacking && cooldown > _prevCooldown + 0.05f)
            {
                if (!_jobCached)
                {
                    _job = em.HasComponent<BattleGame.Units.UnitJobComponent>(_link.Entity)
                        ? em.GetComponentData<BattleGame.Units.UnitJobComponent>(_link.Entity).Job
                        : UnitJob.Knight;
                    _jobCached = true;
                }
                _animator.SetTrigger(_job == UnitJob.Archer ? "Shot" : "Slash");
            }

            _prevCooldown = cooldown;
        }

        // ── NeedsFlash 확인: 스턴 없는 낮은 데미지(독,존 등)도 플래시 발동 ──
        if (em.HasComponent<HitReactionComponent>(_link.Entity))
        {
            var reaction = em.GetComponentData<HitReactionComponent>(_link.Entity);
            if (reaction.NeedsFlash)
            {
                TriggerHitFlash();
                reaction.NeedsFlash = false;
                em.SetComponentData(_link.Entity, reaction);
            }
        }

        // ── 상태 전환 처리 ───────────────────────────────────
        if (current != _prevState)
        {
            // Hit 상태 진입 → 색 플래시 (스턴 동반 강타 등)
            if (current == UnitState.Hit)
                TriggerHitFlash();

            ApplyState(current);
            _prevState = current;
        }

        // ── 이동 방향 기반 스프라이트 반전 ───────────────────
        if (em.HasComponent<MovementComponent>(_link.Entity))
        {
            float vx = em.GetComponentData<MovementComponent>(_link.Entity).Velocity.x;
            if (Mathf.Abs(vx) > 0.01f)
                _lastFacingX = vx;
        }

        float absX    = Mathf.Abs(transform.localScale.x);
        float targetX = _lastFacingX >= 0f ? absX : -absX;
        if (!Mathf.Approximately(transform.localScale.x, targetX))
        {
            Vector3 s = transform.localScale;
            transform.localScale = new Vector3(targetX, s.y, s.z);
        }
    }

    // ── 공개 API (UnitDeathDespawnSystem 에서 호출) ───────────

    /// <summary>
    /// 사망 연출을 시작한다. 연출 완료 후 자동으로 PoolController.Despawn() 을 호출한다.
    /// </summary>
    public void TriggerDeath()
    {
        if (_isDying) return;
        _isDying = true;

        if (_hitCoroutine != null)
        {
            StopCoroutine(_hitCoroutine);
            _hitCoroutine = null;
        }

        if (_renderer != null) _renderer.color = Color.white;

        // EntityLink 의 ECS→Transform 위치 동기화를 중단해 코루틴이 자유롭게 이동
        if (_link != null) _link.SyncPosition = false;

        StartCoroutine(DeathSequence());
    }

    // ── 상태 → Animator ──────────────────────────────────────

    void ApplyState(UnitState state)
    {
        switch (state)
        {
            case UnitState.Idle:
                SetBool("Idle");
                break;

            case UnitState.Moving:
            case UnitState.Chasing:
                SetBool("Run");
                break;

            case UnitState.Attacking:
                // 공격 사이 대기 자세. trigger 는 쿨다운 모니터링에서만 발동.
                SetBool("Ready");
                break;

            case UnitState.Hit:
                _animator.SetTrigger("Hit");
                break;

            case UnitState.Dead:
                SetBool("Die");
                break;
        }
    }

    // ── 헬퍼 ─────────────────────────────────────────────────

    /// <summary>
    /// 피격 플래시를 시작한다.
    /// 스턴 여부와 무관하게 데미지를 받으면 호출된다.
    /// 이미 플래시 중이라면 처음부터 다시 시작해 다중 틱 데미지도 정상 표시.
    /// </summary>
    void TriggerHitFlash()
    {
        if (_hitCoroutine != null) StopCoroutine(_hitCoroutine);
        if (_renderer != null)    _renderer.color = Color.white;
        _hitCoroutine = StartCoroutine(HitFlashRoutine());
    }

    // ── 코루틴 ───────────────────────────────────────────────

    System.Collections.IEnumerator HitFlashRoutine()
    {
        if (_renderer == null) yield break;

        _renderer.color = _hitFlashColor;

        float t = 0f;
        while (t < _hitFlashDuration)
        {
            t += Time.deltaTime;
            _renderer.color = Color.Lerp(_hitFlashColor, Color.white, t / _hitFlashDuration);
            yield return null;
        }

        _renderer.color = Color.white;
        _hitCoroutine   = null;
    }

    System.Collections.IEnumerator DeathSequence()
    {
        // 현재 바라보는 방향의 반대로 날아감 (scale.x 로 방향 저장됨)
        float facingSign = transform.localScale.x >= 0f ? 1f : -1f;
        float flyDirX    = -facingSign;

        Vector3 startPos = transform.position;
        Vector3 endPos   = startPos + new Vector3(flyDirX * _deathFlyDistance, 0f, 0f);

        float elapsed = 0f;
        while (elapsed < _deathFlyDuration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / _deathFlyDuration;
            float et = 1f - (1f - t) * (1f - t);   // ease-out quad
            transform.position = Vector3.Lerp(startPos, endPos, et);
            yield return null;
        }
        transform.position = endPos;

        // Die 애니메이션이 끝날 때까지 대기
        yield return new WaitForSeconds(_deathHoldDuration);

        PoolController.Instance?.Despawn(gameObject);
    }

    // ── 헬퍼 ─────────────────────────────────────────────────

    void SetBool(string param)
    {
        ClearBools();
        _animator.SetBool(param, true);
    }

    void ClearBools()
    {
        foreach (string p in BoolParams)
            _animator.SetBool(p, false);
    }
}
