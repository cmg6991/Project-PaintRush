using System;
using System.Collections.Generic;
using UnityEngine;

public class RandomMonsterSpawner : MonoBehaviour
{
    [Serializable]
    private class SpawnEntry
    {
        [Tooltip("스폰할 몬스터 프리팹")]
        public MonsterAI prefab;

        [Tooltip("선택 가중치. 값이 클수록 자주 등장합니다.")]
        [Min(0f)]
        public float weight = 1f;

        [Tooltip("이 몬스터가 팔레트 보유 몬스터로 선택될 수 있는지")]
        public bool canBePaletteCarrier = true;
    }

    [Header("스폰 설정")]
    [SerializeField, Min(0)]
    private int spawnCount = 5;

    [Tooltip("게임 시작 시 한 번만 자동 스폰합니다.")]
    [SerializeField]
    private bool spawnOnStart = true;

    [Tooltip("한 번의 스폰에서 같은 위치를 중복 사용하지 않습니다.")]
    [SerializeField]
    private bool useEachPointOnce = true;

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
    [Tooltip(
        "SpawnPoint_01, SpawnPoint_02 등을 " +
        "자식으로 둔 부모 오브젝트"
    )]
    [SerializeField]
    private Transform spawnPointRoot;

    [Tooltip(
        "체크하면 Spawn Point Root의 " +
        "자식들을 자동으로 사용합니다."
    )]
    [SerializeField]
    private bool autoCollectSpawnPoints = true;

    [Tooltip("자동 수집을 끄는 경우 직접 연결할 스폰 포인트")]
    [SerializeField]
    private List<Transform> spawnPoints = new();

    [Header("생성된 몬스터 정리")]
    [Tooltip(
        "생성된 몬스터를 정리할 부모. " +
        "비워두면 이 오브젝트의 자식으로 생성됩니다."
    )]
    [SerializeField]
    private Transform spawnedMonsterParent;

    [Header("외부 시스템")]
    [SerializeField]
    private MonsterManager monsterManager;

    private readonly List<MonsterAI> spawnedMonsters =
        new List<MonsterAI>();

    private readonly List<MonsterAI> paletteCandidates =
        new List<MonsterAI>();

    private bool hasSpawned;

    public bool HasSpawned => hasSpawned;

    public IReadOnlyList<MonsterAI> SpawnedMonsters =>
        spawnedMonsters;

    private void Awake()
    {
        if (spawnedMonsterParent == null)
        {
            spawnedMonsterParent = transform;
        }

        ResolveMonsterManager();
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnStageMonsters();
        }
    }

    public void SpawnStageMonsters()
    {
        if (hasSpawned)
        {
            Debug.LogWarning(
                $"{gameObject.name}: 이미 몬스터를 스폰했습니다."
            );

            return;
        }

        List<Transform> validSpawnPoints =
            GetValidSpawnPoints();

        if (!ValidateSettings(validSpawnPoints))
        {
            return;
        }

        hasSpawned = true;

        spawnedMonsters.Clear();
        paletteCandidates.Clear();

        if (useEachPointOnce)
        {
            Shuffle(validSpawnPoints);
        }

        int actualSpawnCount = spawnCount;

        if (useEachPointOnce &&
            actualSpawnCount > validSpawnPoints.Count)
        {
            actualSpawnCount =
                validSpawnPoints.Count;

            Debug.LogWarning(
                $"{gameObject.name}: Spawn Count가 " +
                "스폰 포인트 수보다 많아 " +
                $"{actualSpawnCount}마리만 생성합니다."
            );
        }

        int spawnedCount = 0;

        for (int i = 0;
             i < actualSpawnCount;
             i++)
        {
            Transform selectedPoint =
                SelectSpawnPoint(
                    validSpawnPoints,
                    i
                );

            SpawnEntry selectedEntry =
                SelectRandomEntry();

            if (selectedEntry == null ||
                selectedEntry.prefab == null)
            {
                Debug.LogWarning(
                    $"{gameObject.name}: " +
                    "선택 가능한 몬스터 프리팹이 없습니다."
                );

                break;
            }

            MonsterAI spawnedMonster =
                Instantiate(
                    selectedEntry.prefab,
                    selectedPoint.position,
                    selectedPoint.rotation,
                    spawnedMonsterParent
                );

            spawnedMonster
                .transform
                .SetPositionAndRotation(
                    selectedPoint.position,
                    selectedPoint.rotation
                );

            // 프리팹 자체에 Has Palette Item이
            // 켜져 있더라도 우선 일반 몬스터로 초기화
            spawnedMonster.SetPaletteCarrier(
                false
            );

            spawnedMonsters.Add(
                spawnedMonster
            );

            if (selectedEntry.canBePaletteCarrier)
            {
                paletteCandidates.Add(
                    spawnedMonster
                );
            }

            ResolveMonsterManager();

            if (monsterManager != null)
            {
                monsterManager.Register(
                    spawnedMonster
                );
            }

            Debug.Log(
                $"{spawnedMonster.name} 생성 위치: " +
                $"{selectedPoint.name} " +
                $"({selectedPoint.position.x:F2}, " +
                $"{selectedPoint.position.y:F2})"
            );

            spawnedCount++;
        }

        AssignRandomPaletteCarrier();

        Debug.Log(
            $"{gameObject.name}: " +
            $"스테이지 시작 몬스터 " +
            $"{spawnedCount}마리 스폰 완료"
        );
    }

    private void AssignRandomPaletteCarrier()
    {
        if (!assignRandomPaletteCarrier)
        {
            return;
        }

        RemoveInvalidPaletteCandidates();

        if (paletteCandidates.Count == 0)
        {
            Debug.LogWarning(
                $"{gameObject.name}: " +
                "팔레트 보유 몬스터로 지정할 " +
                "후보가 없습니다."
            );

            return;
        }

        int randomIndex =
            UnityEngine.Random.Range(
                0,
                paletteCandidates.Count
            );

        MonsterAI selectedMonster =
            paletteCandidates[randomIndex];

        selectedMonster.SetPaletteCarrier(
            true,
            paletteItemPrefab
        );

        Debug.Log(
            $"[랜덤 스폰] {selectedMonster.name}이 " +
            "팔레트 보유 몬스터로 선택되었습니다."
        );
    }

    private Transform SelectSpawnPoint(
        List<Transform> validSpawnPoints,
        int spawnIndex)
    {
        if (useEachPointOnce)
        {
            return validSpawnPoints[spawnIndex];
        }

        int randomIndex =
            UnityEngine.Random.Range(
                0,
                validSpawnPoints.Count
            );

        return validSpawnPoints[randomIndex];
    }

    private SpawnEntry SelectRandomEntry()
    {
        float totalWeight =
            GetTotalValidWeight();

        if (totalWeight <= 0f)
        {
            return null;
        }

        float randomValue =
            UnityEngine.Random.Range(
                0f,
                totalWeight
            );

        float accumulatedWeight = 0f;

        foreach (SpawnEntry entry
                 in spawnEntries)
        {
            if (!IsValidEntry(entry))
            {
                continue;
            }

            accumulatedWeight +=
                entry.weight;

            if (randomValue <=
                accumulatedWeight)
            {
                return entry;
            }
        }

        for (int i = spawnEntries.Count - 1;
             i >= 0;
             i--)
        {
            SpawnEntry entry =
                spawnEntries[i];

            if (IsValidEntry(entry))
            {
                return entry;
            }
        }

        return null;
    }

    private bool ValidateSettings(
        List<Transform> validSpawnPoints)
    {
        if (spawnCount <= 0)
        {
            Debug.LogWarning(
                $"{gameObject.name}: Spawn Count가 0입니다."
            );

            return false;
        }

        if (GetTotalValidWeight() <= 0f)
        {
            Debug.LogError(
                $"{gameObject.name}: " +
                "유효한 몬스터 프리팹과 " +
                "가중치가 없습니다."
            );

            return false;
        }

        if (validSpawnPoints.Count == 0)
        {
            Debug.LogError(
                $"{gameObject.name}: " +
                "유효한 Spawn Point가 없습니다."
            );

            return false;
        }

        if (assignRandomPaletteCarrier &&
            paletteItemPrefab == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: " +
                "Palette Item Prefab이 " +
                "연결되지 않았습니다."
            );
        }

        return true;
    }

    private List<Transform> GetValidSpawnPoints()
    {
        List<Transform> result =
            new List<Transform>();

        if (autoCollectSpawnPoints)
        {
            if (spawnPointRoot == null)
            {
                Debug.LogError(
                    $"{gameObject.name}: " +
                    "Spawn Point Root가 " +
                    "연결되지 않았습니다."
                );

                return result;
            }

            for (int i = 0;
                 i < spawnPointRoot.childCount;
                 i++)
            {
                Transform child =
                    spawnPointRoot.GetChild(i);

                if (child != null &&
                    child.gameObject
                        .activeInHierarchy)
                {
                    result.Add(child);
                }
            }

            return result;
        }

        foreach (Transform point
                 in spawnPoints)
        {
            if (point != null &&
                point.gameObject
                    .activeInHierarchy)
            {
                result.Add(point);
            }
        }

        return result;
    }

    private void RemoveInvalidPaletteCandidates()
    {
        paletteCandidates.RemoveAll(
            monster =>
                monster == null ||
                monster.IsDead
        );
    }

    private float GetTotalValidWeight()
    {
        float totalWeight = 0f;

        foreach (SpawnEntry entry
                 in spawnEntries)
        {
            if (IsValidEntry(entry))
            {
                totalWeight +=
                    entry.weight;
            }
        }

        return totalWeight;
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
        if (monsterManager != null)
        {
            return;
        }

        monsterManager =
            MonsterManager.Instance;

        if (monsterManager == null)
        {
            monsterManager =
                FindAnyObjectByType
                <MonsterManager>();
        }
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
                    i + 1
                );

            T temporary =
                list[i];

            list[i] =
                list[randomIndex];

            list[randomIndex] =
                temporary;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            Color.magenta;

        List<Transform> points =
            GetValidSpawnPoints();

        foreach (Transform point
                 in points)
        {
            if (point == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(
                point.position,
                0.3f
            );

            Gizmos.DrawLine(
                point.position,
                point.position +
                Vector3.up * 0.75f
            );
        }
    }
}
