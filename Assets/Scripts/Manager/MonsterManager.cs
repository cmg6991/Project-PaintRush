using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 씬에서 살아 있는 몬스터와 처치 수를 관리합니다.
/// </summary>
public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Instance { get; private set; }

    private readonly HashSet<MonsterAI> aliveMonsters = new();

    [Header("런타임 확인")]
    [SerializeField] private int killCount;
    [SerializeField] private int aliveCount;

    public int KillCount => killCount;
    public int AliveCount => aliveCount;

    public event Action<int> OnKillCountChanged;
    public event Action<int> OnAliveCountChanged;

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
                FindObjectsInactive.Exclude);

        foreach (MonsterAI monster in sceneMonsters)
        {
            Register(monster);
        }
    }

    public bool Register(MonsterAI monster)
    {
        if (monster == null || monster.IsDead)
        {
            return false;
        }

        if (!aliveMonsters.Add(monster))
        {
            return false;
        }

        RefreshAliveCount();
        return true;
    }

    public bool Unregister(MonsterAI monster)
    {
        if (monster == null || !aliveMonsters.Remove(monster))
        {
            return false;
        }

        RefreshAliveCount();
        return true;
    }

    public bool RegisterDeath(MonsterAI monster)
    {
        if (monster == null || !aliveMonsters.Remove(monster))
        {
            return false;
        }

        killCount++;

        RefreshAliveCount();
        OnKillCountChanged?.Invoke(killCount);

        Debug.Log(
            $"몬스터 처치 수: {killCount}, " +
            $"생존 몬스터 수: {aliveCount}");

        return true;
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
        {
            return 0;
        }

        MonsterAI[] targets = GetAliveMonstersSnapshot();
        int attackedCount = 0;

        foreach (MonsterAI monster in targets)
        {
            if (monster == null || monster.IsDead)
            {
                continue;
            }

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
        if (killCount == 0)
        {
            return;
        }

        killCount = 0;
        OnKillCountChanged?.Invoke(killCount);
    }

    private void RefreshAliveCount()
    {
        int newCount = aliveMonsters.Count;

        if (aliveCount == newCount)
        {
            return;
        }

        aliveCount = newCount;
        OnAliveCountChanged?.Invoke(aliveCount);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
