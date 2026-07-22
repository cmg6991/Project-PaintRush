using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 씬의 몬스터 등록, 생존 수, 처치 수와
/// 문 개방에 사용하는 처치 가능 몬스터 진행도를 관리합니다.
/// 피라냐는 전체 생존 수에는 포함되지만 문 개방 진행도에서는 제외됩니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MonsterManager : MonoBehaviour
{
    public static MonsterManager Instance { get; private set; }

    private readonly HashSet<MonsterAI> trackedMonsters = new();
    private readonly HashSet<MonsterAI> aliveMonsters = new();

    [Header("런타임 확인")]
    [SerializeField, Min(0)] private int killCount;
    [SerializeField, Min(0)] private int aliveCount;
    [SerializeField, Min(0)] private int totalKillableCount;
    [SerializeField, Min(0)] private int killableKillCount;

    public int KillCount => killCount;
    public int AliveCount => aliveCount;
    public int TotalKillableCount => totalKillableCount;
    public int KillableKillCount => killableKillCount;

    public event Action<int> OnKillCountChanged;
    public event Action<int> OnAliveCountChanged;
    public event Action<int> OnKillableKillCountChanged;
    public event Action OnMonsterProgressChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("MonsterManager가 씬에 두 개 이상 존재합니다.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        MonsterAI[] sceneMonsters =
            FindObjectsByType<MonsterAI>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

        foreach (MonsterAI monster in sceneMonsters)
            Register(monster);
    }

    public bool Register(MonsterAI monster)
    {
        if (monster == null || monster.IsDead)
            return false;

        bool newlyTracked = trackedMonsters.Add(monster);
        bool newlyAlive = aliveMonsters.Add(monster);

        if (!newlyTracked && !newlyAlive)
            return false;

        if (newlyTracked && !monster.IsPiranha)
            totalKillableCount++;

        RefreshAliveCount();
        NotifyProgressChanged();
        return true;
    }

    /// <summary>
    /// 사망이 아닌 비활성화나 제거로 스테이지에서 빠진 몬스터를 등록 해제합니다.
    /// </summary>
    public bool Unregister(MonsterAI monster)
    {
        if (monster == null)
            return false;

        bool removedAlive = aliveMonsters.Remove(monster);
        bool removedTracked = trackedMonsters.Remove(monster);

        if (!removedAlive && !removedTracked)
            return false;

        if (removedTracked && !monster.IsPiranha)
        {
            totalKillableCount =
                Mathf.Max(
                    killableKillCount,
                    totalKillableCount - 1);
        }

        RefreshAliveCount();
        NotifyProgressChanged();
        return true;
    }

    public bool RegisterDeath(MonsterAI monster)
    {
        if (monster == null ||
            !trackedMonsters.Contains(monster) ||
            !aliveMonsters.Remove(monster))
        {
            return false;
        }

        killCount++;

        if (!monster.IsPiranha)
            killableKillCount++;

        RefreshAliveCount();

        OnKillCountChanged?.Invoke(killCount);
        OnKillableKillCountChanged?.Invoke(killableKillCount);
        NotifyProgressChanged();

        Debug.Log(
            $"몬스터 처치 수: {killCount}, " +
            $"생존 몬스터 수: {aliveCount}, " +
            $"문 조건 처치 수: {killableKillCount}/{totalKillableCount}");

        return true;
    }

    public int CalculateRequiredKills(float ratio)
    {
        if (totalKillableCount <= 0)
            return 0;

        return Mathf.CeilToInt(
            totalKillableCount *
            Mathf.Clamp01(ratio));
    }

    public int CalculateRemainingRequiredKills(float ratio)
    {
        return Mathf.Max(
            0,
            CalculateRequiredKills(ratio) -
            killableKillCount);
    }

    public bool IsKillRequirementMet(float ratio)
    {
        return killableKillCount >=
               CalculateRequiredKills(ratio);
    }

    /// <summary>
    /// 팔레트 특수 공격용이 아닙니다.
    /// 다른 광역 기믹이 필요할 때만 사용합니다.
    /// </summary>
    public int DamageAll(
        int damage,
        Color attackColor,
        GameObject attacker,
        bool ignoreElement)
    {
        if (damage <= 0)
            return 0;

        MonsterAI[] targets = GetAliveMonstersSnapshot();
        int attackedCount = 0;

        foreach (MonsterAI monster in targets)
        {
            if (monster == null || monster.IsDead)
                continue;

            monster.TakeDamage(
                damage,
                attackColor,
                attacker,
                ignoreElement);

            attackedCount++;
        }

        return attackedCount;
    }

    public MonsterAI[] GetAliveMonstersSnapshot()
    {
        aliveMonsters.RemoveWhere(
            monster => monster == null || monster.IsDead);

        RefreshAliveCount();

        MonsterAI[] snapshot =
            new MonsterAI[aliveMonsters.Count];

        aliveMonsters.CopyTo(snapshot);
        return snapshot;
    }

    public void ResetKillCount()
    {
        killCount = 0;
        killableKillCount = 0;

        OnKillCountChanged?.Invoke(killCount);
        OnKillableKillCountChanged?.Invoke(killableKillCount);
        NotifyProgressChanged();
    }

    private void RefreshAliveCount()
    {
        int newCount = aliveMonsters.Count;

        if (aliveCount == newCount)
            return;

        aliveCount = newCount;
        OnAliveCountChanged?.Invoke(aliveCount);
    }

    private void NotifyProgressChanged()
    {
        OnMonsterProgressChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
