using System;
using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Instance { get; private set; }

    private readonly HashSet<MonsterAI> aliveMonsters =
        new HashSet<MonsterAI>();

    [Header("런타임 확인")]
    [SerializeField] private int killCount;
    [SerializeField] private int aliveCount;

    public int KillCount => killCount;
    public int AliveCount => aliveMonsters.Count;

    public event Action<int> OnKillCountChanged;
    public event Action<int> OnAliveCountChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError(
                "MonsterManager가 씬에 두 개 이상 존재합니다."
            );
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        MonsterAI[] sceneMonsters =
            FindObjectsByType<MonsterAI>(
                FindObjectsSortMode.None
            );

        foreach (MonsterAI monster in sceneMonsters)
        {
            Register(monster);
        }

        RefreshAliveCount();
    }

    public bool Register(MonsterAI monster)
    {
        if (monster == null || monster.IsDead)
        {
            return false;
        }

        bool added = aliveMonsters.Add(monster);

        if (added)
        {
            RefreshAliveCount();
        }

        return added;
    }

    public bool Unregister(MonsterAI monster)
    {
        if (monster == null)
        {
            return false;
        }

        bool removed = aliveMonsters.Remove(monster);

        if (removed)
        {
            RefreshAliveCount();
        }

        return removed;
    }

    public bool RegisterDeath(MonsterAI monster)
    {
        if (monster == null)
        {
            return false;
        }

        // 이미 목록에서 제거된 몬스터의 사망은 중복 집계하지 않는다.
        if (!aliveMonsters.Remove(monster))
        {
            return false;
        }

        killCount++;
        RefreshAliveCount();

        OnKillCountChanged?.Invoke(killCount);

        Debug.Log(
            $"몬스터 처치 수: {killCount}, 생존 몬스터 수: {aliveMonsters.Count}"
        );

        return true;
    }

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
                ignoreElement
            );

            attackedCount++;
        }

        return attackedCount;
    }

    public MonsterAI[] GetAliveMonstersSnapshot()
    {
        aliveMonsters.RemoveWhere(
            monster => monster == null || monster.IsDead
        );

        RefreshAliveCount();

        MonsterAI[] snapshot =
            new MonsterAI[aliveMonsters.Count];

        aliveMonsters.CopyTo(snapshot);

        return snapshot;
    }

    public void ResetKillCount()
    {
        killCount = 0;
        OnKillCountChanged?.Invoke(killCount);
    }

    private void RefreshAliveCount()
    {
        aliveCount = aliveMonsters.Count;
        OnAliveCountChanged?.Invoke(aliveCount);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        aliveMonsters.Clear();
    }
}
