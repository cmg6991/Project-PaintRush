using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 규칙에 따라 스폰 포인트별 몬스터 수를 결정하고,
/// 가중치 기반으로 몬스터를 생성합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class RandomMonsterSpawner : MonoBehaviour
{
    [Serializable]
    private sealed class SpawnEntry
    {
        [Tooltip("스폰할 몬스터 프리팹")]
        public MonsterAI prefab;

        [Tooltip("선택 가중치. 값이 클수록 자주 등장합니다.")]
        [Min(0f)]
        public float weight = 1f;

        [Tooltip("이 몬스터가 팔레트 보유 몬스터로 선택될 수 있는지")]
        public bool canBePaletteCarrier = true;
    }

    [Serializable]
    private sealed class StageSpawnRule
    {
        [Min(1)] public int stage = 1;
        [Min(1)] public int minMonstersPerPoint = 1;
        [Min(1)] public int maxMonstersPerPoint = 1;

        public int GetRandomCount()
        {
            int minimum = Mathf.Max(1, minMonstersPerPoint);
            int maximum = Mathf.Max(minimum, maxMonstersPerPoint);

            return UnityEngine.Random.Range(
                minimum,
                maximum + 1);
        }

        public void Sanitize()
        {
            stage = Mathf.Max(1, stage);
            minMonstersPerPoint =
                Mathf.Max(1, minMonstersPerPoint);
            maxMonstersPerPoint =
                Mathf.Max(
                    minMonstersPerPoint,
                    maxMonstersPerPoint);
        }
    }

    [Header("스테이지")]
    [SerializeField, Min(1)]
    private int currentStage = 1;

    [SerializeField]
    private List<StageSpawnRule> stageSpawnRules = new()
    {
        new StageSpawnRule
        {
            stage = 1,
            minMonstersPerPoint = 1,
            maxMonstersPerPoint = 1
        },
        new StageSpawnRule
        {
            stage = 2,
            minMonstersPerPoint = 2,
            maxMonstersPerPoint = 3
        },
        new StageSpawnRule
        {
            stage = 3,
            minMonstersPerPoint = 2,
            maxMonstersPerPoint = 3
        }
    };

    [Header("스폰 설정")]
    [Tooltip("이번 스테이지에서 사용할 스폰 포인트 수입니다.")]
    [SerializeField, Min(0)]
    private int spawnCount = 5;

    [Tooltip("게임 시작 시 한 번만 자동 스폰합니다.")]
    [SerializeField]
    private bool spawnOnStart = true;

    [Tooltip("한 번의 스폰에서 같은 위치를 중복 사용하지 않습니다.")]
    [SerializeField]
    private bool useEachPointOnce = true;

    [Tooltip("한 포인트에서 여러 마리가 생성될 때의 가로 간격입니다.")]
    [SerializeField, Min(0f)]
    private float multiSpawnSpacing = 0.65f;

    [Header("몬스터 후보")]
    [SerializeField]
    private List<SpawnEntry> spawnEntries = new();

    [Header("팔레트 보유 몬스터")]
    [Tooltip("스폰된 몬스터 중 한 마리를 팔레트 보유 몬스터로 지정")]
    [SerializeField]
    private bool assignRandomPaletteCarrier = true;

    [Tooltip("팔레트 보유 몬스터가 죽을 때 드롭할 팔레트 아이템")]
    [SerializeField]
    private GameObject paletteItemPrefab;

    [Header("스폰 위치")]
    [Tooltip("SpawnPoint_01, SpawnPoint_02 등을 자식으로 둔 부모 오브젝트")]
    [SerializeField]
    private Transform spawnPointRoot;

    [Tooltip("체크하면 Spawn Point Root의 자식들을 자동으로 사용합니다.")]
    [SerializeField]
    private bool autoCollectSpawnPoints = true;

    [Tooltip("자동 수집을 끄는 경우 직접 연결할 스폰 포인트")]
    [SerializeField]
    private List<Transform> spawnPoints = new();

    [Header("생성된 몬스터 정리")]
    [Tooltip("생성된 몬스터의 부모. 비워두면 이 오브젝트의 자식으로 생성됩니다.")]
    [SerializeField]
    private Transform spawnedMonsterParent;

    [Header("외부 시스템")]
    [SerializeField]
    private MonsterManager monsterManager;

    private readonly List<MonsterAI> spawnedMonsters = new();
    private readonly List<MonsterAI> paletteCandidates = new();

    private bool hasSpawned;

    public bool HasSpawned => hasSpawned;
    public int CurrentStage => currentStage;
    public IReadOnlyList<MonsterAI> SpawnedMonsters => spawnedMonsters;

    private void Awake()
    {
        spawnedMonsterParent ??= transform;
        ResolveMonsterManager();
    }

    private void Start()
    {
        if (spawnOnStart)
            SpawnStageMonsters();
    }

    private void OnValidate()
    {
        currentStage = Mathf.Max(1, currentStage);
        spawnCount = Mathf.Max(0, spawnCount);
        multiSpawnSpacing = Mathf.Max(0f, multiSpawnSpacing);

        foreach (StageSpawnRule rule in stageSpawnRules)
            rule?.Sanitize();
    }

    public void SpawnStageMonsters()
    {
        if (hasSpawned)
        {
            Debug.LogWarning(
                $"{gameObject.name}: 이미 몬스터를 스폰했습니다.");
            return;
        }

        List<Transform> validSpawnPoints =
            GetValidSpawnPoints();

        if (!ValidateSettings(validSpawnPoints))
            return;

        hasSpawned = true;
        spawnedMonsters.Clear();
        paletteCandidates.Clear();

        if (useEachPointOnce)
            Shuffle(validSpawnPoints);

        int pointCount = ResolveSpawnPointCount(
            validSpawnPoints.Count);

        StageSpawnRule rule = ResolveStageRule();
        int totalSpawned = 0;

        for (int pointIndex = 0;
             pointIndex < pointCount;
             pointIndex++)
        {
            Transform point =
                SelectSpawnPoint(
                    validSpawnPoints,
                    pointIndex);

            int monstersAtPoint =
                rule.GetRandomCount();

            for (int localIndex = 0;
                 localIndex < monstersAtPoint;
                 localIndex++)
            {
                if (TrySpawnMonster(
                        point,
                        localIndex,
                        monstersAtPoint))
                {
                    totalSpawned++;
                }
            }
        }

        AssignRandomPaletteCarrier();

        Debug.Log(
            $"{gameObject.name}: 스테이지 {currentStage}, " +
            $"{pointCount}개 포인트에 총 {totalSpawned}마리 스폰 완료");
    }

    private bool TrySpawnMonster(
        Transform spawnPoint,
        int indexAtPoint,
        int countAtPoint)
    {
        SpawnEntry entry = SelectRandomEntry();

        if (entry?.prefab == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: 선택 가능한 몬스터 프리팹이 없습니다.");
            return false;
        }

        Vector3 spawnPosition =
            CalculateSpawnPosition(
                spawnPoint,
                indexAtPoint,
                countAtPoint);

        MonsterAI monster =
            Instantiate(
                entry.prefab,
                spawnPosition,
                spawnPoint.rotation,
                spawnedMonsterParent);

        monster.SetPaletteCarrier(false);

        spawnedMonsters.Add(monster);

        if (entry.canBePaletteCarrier)
            paletteCandidates.Add(monster);

        ResolveMonsterManager();
        monsterManager?.Register(monster);

        Debug.Log(
            $"{monster.name} 생성 위치: {spawnPoint.name} " +
            $"({spawnPosition.x:F2}, {spawnPosition.y:F2})");

        return true;
    }

    private Vector3 CalculateSpawnPosition(
        Transform point,
        int index,
        int count)
    {
        if (count <= 1 || multiSpawnSpacing <= 0f)
            return point.position;

        float centeredIndex =
            index - (count - 1) * 0.5f;

        return point.position +
               point.right *
               (centeredIndex * multiSpawnSpacing);
    }

    private int ResolveSpawnPointCount(int validPointCount)
    {
        if (!useEachPointOnce)
            return spawnCount;

        int resolved =
            Mathf.Min(spawnCount, validPointCount);

        if (spawnCount > validPointCount)
        {
            Debug.LogWarning(
                $"{gameObject.name}: Spawn Count가 스폰 포인트 수보다 많아 " +
                $"{resolved}개 포인트만 사용합니다.");
        }

        return resolved;
    }

    private StageSpawnRule ResolveStageRule()
    {
        StageSpawnRule exact =
            stageSpawnRules.Find(
                rule =>
                    rule != null &&
                    rule.stage == currentStage);

        if (exact != null)
            return exact;

        StageSpawnRule nearestLower = null;

        foreach (StageSpawnRule rule in stageSpawnRules)
        {
            if (rule == null ||
                rule.stage > currentStage)
            {
                continue;
            }

            if (nearestLower == null ||
                rule.stage > nearestLower.stage)
            {
                nearestLower = rule;
            }
        }

        return nearestLower ??
               new StageSpawnRule
               {
                   stage = currentStage,
                   minMonstersPerPoint = 1,
                   maxMonstersPerPoint = 1
               };
    }

    private void AssignRandomPaletteCarrier()
    {
        if (!assignRandomPaletteCarrier)
            return;

        paletteCandidates.RemoveAll(
            monster =>
                monster == null ||
                monster.IsDead);

        if (paletteCandidates.Count == 0)
        {
            Debug.LogWarning(
                $"{gameObject.name}: 팔레트 보유 몬스터 후보가 없습니다.");
            return;
        }

        MonsterAI selected =
            paletteCandidates[
                UnityEngine.Random.Range(
                    0,
                    paletteCandidates.Count)];

        selected.SetPaletteCarrier(
            true,
            paletteItemPrefab);

        Debug.Log(
            $"[랜덤 스폰] {selected.name}이 팔레트 보유 몬스터로 선택되었습니다.");
    }

    private Transform SelectSpawnPoint(
        List<Transform> validSpawnPoints,
        int spawnIndex)
    {
        if (useEachPointOnce)
            return validSpawnPoints[spawnIndex];

        return validSpawnPoints[
            UnityEngine.Random.Range(
                0,
                validSpawnPoints.Count)];
    }

    private SpawnEntry SelectRandomEntry()
    {
        float totalWeight = GetTotalValidWeight();

        if (totalWeight <= 0f)
            return null;

        float randomValue =
            UnityEngine.Random.Range(
                0f,
                totalWeight);

        float accumulated = 0f;

        foreach (SpawnEntry entry in spawnEntries)
        {
            if (!IsValidEntry(entry))
                continue;

            accumulated += entry.weight;

            if (randomValue <= accumulated)
                return entry;
        }

        return spawnEntries.FindLast(IsValidEntry);
    }

    private bool ValidateSettings(
        List<Transform> validSpawnPoints)
    {
        if (spawnCount <= 0)
        {
            Debug.LogWarning(
                $"{gameObject.name}: Spawn Count가 0입니다.");
            return false;
        }

        if (validSpawnPoints.Count == 0)
        {
            Debug.LogError(
                $"{gameObject.name}: 유효한 Spawn Point가 없습니다.");
            return false;
        }

        if (GetTotalValidWeight() <= 0f)
        {
            Debug.LogError(
                $"{gameObject.name}: 유효한 몬스터 프리팹과 가중치가 없습니다.");
            return false;
        }

        if (assignRandomPaletteCarrier &&
            paletteItemPrefab == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: Palette Item Prefab이 연결되지 않았습니다.");
        }

        return true;
    }

    private List<Transform> GetValidSpawnPoints()
    {
        List<Transform> result = new();

        if (autoCollectSpawnPoints)
        {
            if (spawnPointRoot == null)
            {
                Debug.LogError(
                    $"{gameObject.name}: Spawn Point Root가 연결되지 않았습니다.");
                return result;
            }

            for (int i = 0;
                 i < spawnPointRoot.childCount;
                 i++)
            {
                Transform child =
                    spawnPointRoot.GetChild(i);

                if (child != null &&
                    child.gameObject.activeInHierarchy)
                {
                    result.Add(child);
                }
            }

            return result;
        }

        foreach (Transform point in spawnPoints)
        {
            if (point != null &&
                point.gameObject.activeInHierarchy)
            {
                result.Add(point);
            }
        }

        return result;
    }

    private float GetTotalValidWeight()
    {
        float total = 0f;

        foreach (SpawnEntry entry in spawnEntries)
        {
            if (IsValidEntry(entry))
                total += entry.weight;
        }

        return total;
    }

    private static bool IsValidEntry(
        SpawnEntry entry)
    {
        return entry != null &&
               entry.prefab != null &&
               entry.weight > 0f;
    }

    private void ResolveMonsterManager()
    {
        monsterManager ??=
            MonsterManager.Instance;

        monsterManager ??=
            FindFirstObjectByType<MonsterManager>();
    }

    private static void Shuffle<T>(
        IList<T> list)
    {
        for (int i = list.Count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                UnityEngine.Random.Range(
                    0,
                    i + 1);

            (list[i], list[randomIndex]) =
                (list[randomIndex], list[i]);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;

        foreach (Transform point in GetValidSpawnPoints())
        {
            if (point == null)
                continue;

            Gizmos.DrawWireSphere(
                point.position,
                0.3f);

            Gizmos.DrawLine(
                point.position,
                point.position +
                Vector3.up * 0.75f);
        }
    }
}
