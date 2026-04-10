using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  SkillZoneRunner.cs — 지속 효과 영역 실행기 (공용)
//
//  PoisonZone / Blizzard / ArrowRain 이 공유하는 MonoBehaviour.
//  설정된 중심 · 반경 · 지속 시간 동안 매 틱마다:
//    - 범위 내 적에게 직접 피해 (HitEventBufferElement)
//    - 선택적 디버프 1·2 (StatusEffectBufferElement, 틱마다 갱신)
//
//  디버프 갱신 방식:
//    기존에 같은 Stat+Mode+Delta 의 버프가 있으면 Remaining 을 연장한다.
//    없으면 새로 추가. → 영역을 벗어나면 자연히 만료.
//
//  OnDisable 에서 코루틴 정리 → 풀 재사용 시 안전.
// ============================================================

public class SkillZoneRunner : MonoBehaviour
{
    Coroutine _current;

    void OnDisable() { _current = null; }

    // ── 구성 ─────────────────────────────────────────────────

    public struct ZoneConfig
    {
        public float3     Center;
        public float      Radius;
        public float      Duration;
        public float      TickInterval;  // 틱 간격 (초)
        public float      DamagePerTick; // 틱당 직접 피해 (0이면 피해 없음)
        public TeamType   CasterTeam;    // 적 팀 = 반대 팀
        public Entity     CasterEntity;

        // 디버프 1 (선택)
        public bool       HasDebuff1;
        public StatType   Debuff1Stat;
        public float      Debuff1Delta;
        public EffectMode Debuff1Mode;

        // 디버프 2 (선택)
        public bool       HasDebuff2;
        public StatType   Debuff2Stat;
        public float      Debuff2Delta;
        public EffectMode Debuff2Mode;
    }

    // ── 공개 API ─────────────────────────────────────────────

    public void Setup(ZoneConfig config)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(Run(config));
    }

    // ── 내부 ─────────────────────────────────────────────────

    IEnumerator Run(ZoneConfig cfg)
    {
        float elapsed      = 0f;
        float tickInterval = cfg.TickInterval > 0f ? cfg.TickInterval : 0.5f;
        float tickTimer    = 0f;

        while (elapsed < cfg.Duration)
        {
            elapsed   += Time.deltaTime;
            tickTimer += Time.deltaTime;

            if (tickTimer >= tickInterval)
            {
                tickTimer -= tickInterval;
                ApplyTick(cfg, tickInterval);
            }

            yield return null;
        }

        _current = null;

        // 완료 후 컴포넌트 자체 제거 — 동시에 여러 Zone 이 독립 실행 가능하도록
        Destroy(this);
    }

    static void ApplyTick(ZoneConfig cfg, float tickInterval)
    {
        var em = Unity.Entities.World.DefaultGameObjectInjectionWorld?.EntityManager;
        if (em == null) return;

        em.Value.CompleteAllTrackedJobs();

        var query = em.Value.CreateEntityQuery(new EntityQueryDesc
        {
            All  = new ComponentType[] { ComponentType.ReadOnly<UnitIdentityComponent>(),
                                         ComponentType.ReadOnly<LocalTransform>() },
            None = new ComponentType[] { typeof(DeadTag) },
        });

        NativeArray<Entity>         entities   = query.ToEntityArray(Allocator.Temp);
        NativeArray<LocalTransform> transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        float refreshTime = tickInterval * 2f;  // 다음 틱까지 여유 있게 유지

        for (int i = 0; i < entities.Length; i++)
        {
            var id = em.Value.GetComponentData<UnitIdentityComponent>(entities[i]);
            if (id.Team == cfg.CasterTeam) continue;

            float dist = math.distance(
                new float3(transforms[i].Position.x, transforms[i].Position.y, 0f),
                new float3(cfg.Center.x, cfg.Center.y, 0f));
            if (dist > cfg.Radius) continue;

            // 직접 피해
            if (cfg.DamagePerTick > 0f && em.Value.HasBuffer<HitEventBufferElement>(entities[i]))
            {
                em.Value.GetBuffer<HitEventBufferElement>(entities[i]).Add(new HitEventBufferElement
                {
                    Damage         = cfg.DamagePerTick,
                    HitDirection   = float3.zero,
                    AttackerEntity = cfg.CasterEntity,
                });
            }

            // 디버프 적용 (버퍼에 이미 있으면 갱신, 없으면 추가)
            if (em.Value.HasBuffer<StatusEffectBufferElement>(entities[i]))
            {
                var buff = em.Value.GetBuffer<StatusEffectBufferElement>(entities[i]);

                if (cfg.HasDebuff1)
                    RefreshOrAddDebuff(buff, cfg.Debuff1Stat, cfg.Debuff1Delta, cfg.Debuff1Mode, refreshTime);
                if (cfg.HasDebuff2)
                    RefreshOrAddDebuff(buff, cfg.Debuff2Stat, cfg.Debuff2Delta, cfg.Debuff2Mode, refreshTime);
            }
        }

        entities.Dispose();
        transforms.Dispose();
        query.Dispose();
    }

    static void RefreshOrAddDebuff(
        DynamicBuffer<StatusEffectBufferElement> buff,
        StatType stat, float delta, EffectMode mode, float refreshTime)
    {
        for (int j = 0; j < buff.Length; j++)
        {
            var b = buff[j];
            if (b.Stat == stat && b.Mode == mode && math.abs(b.Delta - delta) < 0.001f)
            {
                // 기존 효과 갱신 (남은 시간 연장)
                b.Remaining = math.max(b.Remaining, refreshTime);
                buff[j]     = b;
                return;
            }
        }

        // 새로 추가
        buff.Add(new StatusEffectBufferElement
        {
            Stat      = stat,
            Delta     = delta,
            Mode      = mode,
            Duration  = refreshTime,
            Remaining = refreshTime,
        });
    }
}
