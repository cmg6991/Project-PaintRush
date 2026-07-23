using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 최대 체력은 건드리지 않고,
/// 이동·감지·공격 리듬만 한 에셋에서 조정하는 밸런스 프로필입니다.
/// </summary>
[CreateAssetMenu(
    fileName = "MonsterBalanceProfile",
    menuName = "PaintRush/Monster Balance Profile")]
public sealed class MonsterBalanceProfile : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [SerializeField] private MonsterType type;

        [Header("이동과 감지")]
        [SerializeField, Min(0f)] private float patrolSpeed = 2f;
        [SerializeField, Min(0f)] private float chaseSpeed = 3f;
        [SerializeField, Min(0f)] private float detectRange = 4f;
        [SerializeField, Min(0f)] private float attackRange = 1f;

        [Header("공격")]
        [SerializeField, Min(1)] private int attackDamage = 1;
        [SerializeField, Min(0.05f)] private float attackCooldown = 1.5f;

        [Header("도망")]
        [SerializeField] private bool canRunAway;
        [SerializeField, Min(0)] private int runAwayHp = 1;
        [SerializeField, Min(0f)] private float runAwaySpeed = 4f;
        [SerializeField, Min(0f)] private float runAwayDistance = 6f;
        [SerializeField, Min(0f)] private float runAwayDuration = 2.5f;

        public MonsterType Type => type;
        public float PatrolSpeed => patrolSpeed;
        public float ChaseSpeed => chaseSpeed;
        public float DetectRange => detectRange;
        public float AttackRange => attackRange;
        public int AttackDamage => attackDamage;
        public float AttackCooldown => attackCooldown;
        public bool CanRunAway => canRunAway;
        public int RunAwayHp => runAwayHp;
        public float RunAwaySpeed => runAwaySpeed;
        public float RunAwayDistance => runAwayDistance;
        public float RunAwayDuration => runAwayDuration;

        public Entry(
            MonsterType type,
            float patrolSpeed,
            float chaseSpeed,
            float detectRange,
            float attackRange,
            int attackDamage,
            float attackCooldown,
            bool canRunAway,
            int runAwayHp,
            float runAwaySpeed,
            float runAwayDistance,
            float runAwayDuration)
        {
            this.type = type;
            this.patrolSpeed = patrolSpeed;
            this.chaseSpeed = chaseSpeed;
            this.detectRange = detectRange;
            this.attackRange = attackRange;
            this.attackDamage = attackDamage;
            this.attackCooldown = attackCooldown;
            this.canRunAway = canRunAway;
            this.runAwayHp = runAwayHp;
            this.runAwaySpeed = runAwaySpeed;
            this.runAwayDistance = runAwayDistance;
            this.runAwayDuration = runAwayDuration;
        }

        public void Sanitize()
        {
            patrolSpeed = Mathf.Max(0f, patrolSpeed);
            chaseSpeed = Mathf.Max(0f, chaseSpeed);
            detectRange = Mathf.Max(0f, detectRange);
            attackRange = Mathf.Max(0f, attackRange);
            attackDamage = Mathf.Max(1, attackDamage);
            attackCooldown = Mathf.Max(0.05f, attackCooldown);
            runAwayHp = Mathf.Max(0, runAwayHp);
            runAwaySpeed = Mathf.Max(0f, runAwaySpeed);
            runAwayDistance = Mathf.Max(0f, runAwayDistance);
            runAwayDuration = Mathf.Max(0f, runAwayDuration);
        }
    }

    [SerializeField] private List<Entry> entries = new();

    public IReadOnlyList<Entry> Entries => entries;

    public bool TryGet(
        MonsterType type,
        out Entry entry)
    {
        if (entries == null)
        {
            entry = null;
            return false;
        }

        entry = entries.Find(
            candidate => candidate != null && candidate.Type == type);

        return entry != null;
    }

    private void Reset()
    {
        CreateRecommendedEntries();
    }

    [ContextMenu("권장 밸런스 6종 생성")]
    private void CreateRecommendedEntries()
    {
        entries = new List<Entry>
        {
            new(MonsterType.Slime, 2.4f, 3f, 5f, 4f, 1, 2.2f,
                false, 0, 0f, 0f, 0f),
            new(MonsterType.Snail, 2f, 2.4f, 3.5f, 0.75f, 1, 2.4f,
                false, 0, 0f, 0f, 0f),
            new(MonsterType.Ghost, 3f, 4f, 5f, 1.2f, 1, 1.5f,
                true, 1, 4.5f, 6f, 2.5f),
            new(MonsterType.Piranha, 0f, 0f, 3f, 1.2f, 1, 2f,
                false, 0, 0f, 0f, 0f),
            new(MonsterType.Spider, 4f, 5f, 5.5f, 0.9f, 1, 1.1f,
                true, 1, 5.5f, 6f, 2f),
            new(MonsterType.Frog, 2.5f, 3f, 4.5f, 1.25f, 1, 2f,
                false, 0, 0f, 0f, 0f),
        };

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void OnValidate()
    {
        entries ??= new List<Entry>();

        foreach (Entry entry in entries)
            entry?.Sanitize();
    }
}
