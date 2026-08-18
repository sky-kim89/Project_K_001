using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  DeploymentData.cs
//  전투 출전 배치 데이터 — 최대 5개 슬롯.
//
//  슬롯 인덱스 0~4 = AllySpawner SpawnPoints 순서와 대응.
//  빈 슬롯 = "" (빈 문자열).
//
//  배치 규칙:
//    - 같은 유닛은 한 슬롯에만 존재할 수 있다.
//    - Deploy() 호출 시 기존 슬롯에서 먼저 제거 후 새 슬롯에 배치.
//    - 대상 슬롯에 다른 유닛이 있으면 그 유닛은 배치 해제된다.
// ============================================================

public class DeploymentData : ISaveSection
{
    public SaveKey SaveKey => SaveKey.DeploymentData;

    const int SlotCount = 5;

    DeployRawData _raw = new();

    // ── 조회 ─────────────────────────────────────────────────

    public string GetUnitAt(int slot)
        => (slot >= 0 && slot < SlotCount) ? (_raw.Slots[slot] ?? "") : "";

    public int GetSlotOf(string unitName)
    {
        if (string.IsNullOrEmpty(unitName)) return -1;
        for (int i = 0; i < SlotCount; i++)
            if (_raw.Slots[i] == unitName) return i;
        return -1;
    }

    public bool HasAnyDeployed()
    {
        for (int i = 0; i < SlotCount; i++)
            if (!string.IsNullOrEmpty(_raw.Slots[i])) return true;
        return false;
    }

    /// <summary>배치된 유닛 이름 목록 (빈 슬롯 제외, 슬롯 번호 순서 유지).</summary>
    public List<string> GetDeployedUnits()
    {
        var list = new List<string>(SlotCount);
        for (int i = 0; i < SlotCount; i++)
            if (!string.IsNullOrEmpty(_raw.Slots[i]))
                list.Add(_raw.Slots[i]);
        return list;
    }

    // ── 변경 ─────────────────────────────────────────────────

    /// <summary>유닛을 지정 슬롯에 배치. 이미 배치된 슬롯에서 먼저 제거하고, 대상 슬롯의 기존 유닛은 배치 해제.</summary>
    /// <summary>
    /// 배치가 바뀔 때마다 발행된다.
    /// ⚠ 출전 대기 화면이 이 신호로 다시 세워진다 — 없으면 방금 바꾼 편성과
    ///   전장에 서 있는 장수가 어긋난다.
    /// </summary>
    public static event System.Action OnChanged;

    public void Deploy(string unitName, int slot)
    {
        if (slot < 0 || slot >= SlotCount || string.IsNullOrEmpty(unitName)) return;
        for (int i = 0; i < SlotCount; i++)
            if (_raw.Slots[i] == unitName) _raw.Slots[i] = "";
        _raw.Slots[slot] = unitName;
        OnChanged?.Invoke();
    }

    public void UndeploySlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount) return;
        _raw.Slots[slot] = "";
        OnChanged?.Invoke();
    }

    public void Undeploy(string unitName)
    {
        if (string.IsNullOrEmpty(unitName)) return;
        for (int i = 0; i < SlotCount; i++)
            if (_raw.Slots[i] == unitName) _raw.Slots[i] = "";
        OnChanged?.Invoke();
    }

    // ── ISaveSection ─────────────────────────────────────────

    public string Serialize()   => JsonUtility.ToJson(_raw);

    public void Deserialize(string json)
    {
        _raw = JsonUtility.FromJson<DeployRawData>(json) ?? new DeployRawData();
        EnsureSlotCount();
    }

    public void SetDefaults()
    {
        _raw = new DeployRawData();
        EnsureSlotCount();
    }

    void EnsureSlotCount()
    {
        while (_raw.Slots.Count < SlotCount) _raw.Slots.Add("");
        while (_raw.Slots.Count > SlotCount) _raw.Slots.RemoveAt(_raw.Slots.Count - 1);
    }

    // ── 직렬화 전용 내부 클래스 ──────────────────────────────

    [Serializable]
    class DeployRawData
    {
        public List<string> Slots = new List<string> { "", "", "", "", "" };
    }
}
