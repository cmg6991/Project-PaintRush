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

    [Header("스폰 위치")]
    [Tooltip("SpawnPoint_01, SpawnPoint_02 ... 를 자식으로 둔 부모 오브젝트")]
    [SerializeField]
    private Transform spawnPointRoot;

    [Tooltip("체크하면 Spawn Point Root의 자식들을 자동으로 사용합니다.")]
    [SerializeField]
    private bool autoCollectSpawnPoints = true;

    [Tooltip("자동 수집을 끄는 경우 직접 연결할 스폰 포인트")]
    [SerializeField]
    private List<Transform> spawnPoints = new();

    [Header("생성된 몬스터 정리")]
    [Tooltip("생성된 몬스터를 정리할 부모. 비워두면 이 오브젝트의 자식으로 생성됩니다.")]
    [SerializeField]
    private Transform spawnedMonsterParent;

    [Header("외부 시스템")]
    [SerializeField]
    private MonsterManager monsterManager;

    private bool hasSpawned;

    public bool HasSpawned => hasSpawned;

    private void Awake()
    {
        if (spawnedMonsterParent == null)
        {
            spawnedMonsterParent = transform;
        }

        if (monsterManager == null)
        {
            monsterManager = MonsterManager.Instance;

            if (monsterManager == null)
            {
                monsterManager =
                    FindFirstObjectByType<MonsterManager>();
            }
        }
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnStageMonsters();
        }
    }

    /// <summary>
    /// 스테이지 시작 시 한 번만 호출하는 스폰 함수입니다.
    /// 이미 스폰한 뒤 다시 호출해도 중복 생성하지 않습니다.
    /// </summary>
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

        if (useEachPointOnce)
        {
            Shuffle(validSpawnPoints);
        }

        int actualSpawnCount = spawnCount;

        if (useEachPointOnce &&
            actualSpawnCount > validSpawnPoints.Count)
        {
            actualSpawnCount = validSpawnPoints.Count;

            Debug.LogWarning(
                $"{gameObject.name}: Spawn Count가 유효한 스폰 포인트 수보다 많아 " +
                $"{actualSpawnCount}마리만 생성합니다."
            );
        }

        int spawnedCount = 0;

        for (int i = 0; i < actualSpawnCount; i++)
        {
            Transform selectedPoint;

            if (useEachPointOnce)
            {
                selectedPoint = validSpawnPoints[i];
            }
            else
            {
                int randomPointIndex =
                    UnityEngine.Random.Range(
                        0,
                        validSpawnPoints.Count
                    );

                selectedPoint =
                    validSpawnPoints[randomPointIndex];
            }

            MonsterAI selectedPrefab =
                SelectRandomPrefab();

            if (selectedPrefab == null)
            {
                Debug.LogWarning(
                    $"{gameObject.name}: 선택 가능한 몬스터 프리팹이 없습니다."
                );
                break;
            }

            MonsterAI spawnedMonster = Instantiate(
                selectedPrefab,
                selectedPoint.position,
                selectedPoint.rotation,
                spawnedMonsterParent
            );

            // 부모의 Scale이나 회전 영향을 최소화하기 위해
            // 월드 좌표를 한 번 더 명시적으로 맞춘다.
            spawnedMonster.transform.SetPositionAndRotation(
                selectedPoint.position,
                selectedPoint.rotation
            );

            if (monsterManager != null)
            {
                monsterManager.Register(spawnedMonster);
            }

            Debug.Log(
                $"{spawnedMonster.name} 생성 위치: {selectedPoint.name} " +
                $"({selectedPoint.position.x:F2}, {selectedPoint.position.y:F2})"
            );

            spawnedCount++;
        }

        Debug.Log(
            $"{gameObject.name}: 스테이지 시작 몬스터 {spawnedCount}마리 스폰 완료"
        );
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
                $"{gameObject.name}: 유효한 몬스터 프리팹과 가중치가 없습니다."
            );
            return false;
        }

        if (validSpawnPoints.Count == 0)
        {
            Debug.LogError(
                $"{gameObject.name}: 유효한 Spawn Point가 없습니다."
            );
            return false;
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
                    $"{gameObject.name}: Spawn Point Root가 연결되지 않았습니다."
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

    private MonsterAI SelectRandomPrefab()
    {
        float totalWeight = GetTotalValidWeight();

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

        foreach (SpawnEntry entry in spawnEntries)
        {
            if (entry == null ||
                entry.prefab == null ||
                entry.weight <= 0f)
            {
                continue;
            }

            accumulatedWeight += entry.weight;

            if (randomValue <= accumulatedWeight)
            {
                return entry.prefab;
            }
        }

        for (int i = spawnEntries.Count - 1;
             i >= 0;
             i--)
        {
            SpawnEntry entry = spawnEntries[i];

            if (entry != null &&
                entry.prefab != null &&
                entry.weight > 0f)
            {
                return entry.prefab;
            }
        }

        return null;
    }

    private float GetTotalValidWeight()
    {
        float totalWeight = 0f;

        foreach (SpawnEntry entry in spawnEntries)
        {
            if (entry == null ||
                entry.prefab == null ||
                entry.weight <= 0f)
            {
                continue;
            }

            totalWeight += entry.weight;
        }

        return totalWeight;
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

            (list[i], list[randomIndex]) =
                (list[randomIndex], list[i]);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;

        List<Transform> points =
            GetValidSpawnPoints();

        foreach (Transform point in points)
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
