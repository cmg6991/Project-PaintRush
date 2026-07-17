using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    [Header("--- 다중 타일맵 바인딩 ---")]
    public Tilemap solidTilemap;   // 단단한 지형 (Is Trigger: OFF, Layer: Ground)
    public Tilemap ladderTilemap;  // 사다리 지형 (Is Trigger: ON, Tag: Ladder)
    public Tilemap grabTilemap;    // 행거 지형 (Is Trigger: ON, Tag: Grab/Hanger)

    // 하위 호환성 및 기존 단일 참조 보존용 프로퍼티
    public Tilemap tilemap => solidTilemap;

    public List<TileBase> tilePresets; // 사용할 타일 에셋들을 인스펙터에서 등록

    [Header("--- 그리드 스냅 설정 ---")]
    [SerializeField] private float gridUnitSize = 1.28f; // 기준 타일 크기 (스케일 보정용)

    // ID로 타일 에셋을 찾기 위한 딕셔너리
    private Dictionary<int, TileBase> tileIdDictionary;
    // 타일 에셋으로 ID를 찾기 위한 역방향 딕셔너리 (저장용)
    private Dictionary<TileBase, int> tileAssetDictionary;

    void Awake()
    {
        InitTileDictionaries();
    }

    void Start()
    {
        //  씬 내의 3개 타일맵 자동 바인딩 (이름 기반 검색)
        Tilemap[] childTilemaps = FindObjectsByType<Tilemap>(FindObjectsInactive.Include);
        foreach (var map in childTilemaps)
        {
            string nameLower = map.gameObject.name.ToLower();
            if (nameLower.Contains("solid") || nameLower.Contains("ground") || nameLower.Contains("block"))
            {
                solidTilemap = map;
            }
            else if (nameLower.Contains("ladder"))
            {
                ladderTilemap = map;
            }
            else if (nameLower.Contains("grab") || nameLower.Contains("hanger"))
            {
                grabTilemap = map;
            }
        }

        // 백업: 만약 자동 바인딩 후에도 solidTilemap이 없으면 아무 타일맵이나 잡음
        if (solidTilemap == null)
        {
            solidTilemap = FindAnyObjectByType<Tilemap>();
        }

        // 런타임 게임 구동용 맵 로딩 자동 실행 (기존 MapTestLauncher 역할 흡수)
        LoadMap("Stage1");
        Debug.Log("<color=green>[TileManager]</color> 런타임 시작 시 'Stage1' 맵 복원 완료!");

        // 씬 상에 잔존해 있는 임시 스캔용 낱개 오브젝트 자동 소각 (비활성화)
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (GameObject obj in allObjects)
        {
            if (obj != null && obj.name.ToLower().StartsWith("tile_"))
            {
                obj.SetActive(false);
            }
        }
    }

    // 인스펙터 등록 순서(Index)를 기반으로 ID 딕셔너리 세팅
    void InitTileDictionaries()
    {
        tileIdDictionary = new Dictionary<int, TileBase>();
        tileAssetDictionary = new Dictionary<TileBase, int>();

        for (int i = 0; i < tilePresets.Count; i++)
        {
            if (tilePresets[i] != null)
            {
                tileIdDictionary[i] = tilePresets[i];
                tileAssetDictionary[tilePresets[i]] = i;
            }
        }
    }

    // 다중 타일맵들에서 채워진 타일들을 병합 수집하여 JSON 파일로 저장
    public void SaveMap(string fileName)
    {
        MapData mapData = new MapData { tiles = new List<TileData>() };

        // 3개 타일맵 각각에 대해 데이터 수집 (지정된 type 기입)
        CollectTilesFromMap(solidTilemap, "Block", mapData);
        CollectTilesFromMap(ladderTilemap, "Ladder", mapData);
        CollectTilesFromMap(grabTilemap, "Grab", mapData);

        SaveMapData(fileName, mapData);
    }

    // 타일맵별 타일 수집 헬퍼 함수
    private void CollectTilesFromMap(Tilemap targetMap, string defaultType, MapData mapData)
    {
        if (targetMap == null) return;
        BoundsInt bounds = targetMap.cellBounds;

        foreach (var pos in bounds.allPositionsWithin)
        {
            TileBase tile = targetMap.GetTile(pos);
            if (tile != null)
            {
                if (tileAssetDictionary.TryGetValue(tile, out int id))
                {
                    Color tileColor = targetMap.GetColor(pos);
                    string colorHex = "#" + ColorUtility.ToHtmlStringRGBA(tileColor);

                    TileData data = new TileData
                    {
                        id = id,
                        name = tile.name,
                        x = pos.x,
                        y = pos.y,
                        color = colorHex,
                        type = defaultType
                    };
                    mapData.tiles.Add(data);
                }
            }
        }
    }

    // JSON을 읽어오고 각 물리 타일맵에 분기 배치 및 Grid Cell Size 복원
    public void LoadMap(string fileName)
    {
        // Awake가 호출되지 않는 비플레이 모드에서 실행할 경우 딕셔너리를 즉석에서 자동 강제 빌드
        if (tileIdDictionary == null || tileIdDictionary.Count == 0 || tileAssetDictionary == null || tileAssetDictionary.Count == 0)
        {
            InitTileDictionaries();
        }

        // 3개 타일맵 각각에 대해 Grid Cell Size 자동 복원
        SyncGridCellSize(solidTilemap);
        SyncGridCellSize(ladderTilemap);
        SyncGridCellSize(grabTilemap);

        // 파일 로드 수행
        MapData mapData = LoadMapData("Maps/" + fileName);
        if (mapData == null || mapData.tiles == null) return;

        // 기존 타일맵 청소
        if (solidTilemap != null) solidTilemap.ClearAllTiles();
        if (ladderTilemap != null) ladderTilemap.ClearAllTiles();
        if (grabTilemap != null) grabTilemap.ClearAllTiles();

        // 로드된 데이터를 기반으로 물리적 분기 배치 및 색상 적용
        foreach (var data in mapData.tiles)
        {
            Vector3Int position = new Vector3Int(Mathf.RoundToInt(data.x), Mathf.RoundToInt(data.y), 0);

            if (data.type == "Block" || data.type == "Ladder" || data.type == "Grab")
            {
                if (tileIdDictionary.TryGetValue(data.id, out TileBase tile))
                {
                    // 지형 타입(type)에 맞춰 타일맵 분기 선택
                    Tilemap targetMap = solidTilemap;
                    if (data.type == "Ladder")
                    {
                        targetMap = ladderTilemap != null ? ladderTilemap : solidTilemap;
                    }
                    else if (data.type == "Grab")
                    {
                        targetMap = grabTilemap != null ? grabTilemap : solidTilemap;
                    }

                    if (targetMap != null)
                    {
                        targetMap.SetTile(position, tile);

                        // 색상 동기화
                        if (ColorUtility.TryParseHtmlString(data.color, out Color customColor))
                        {
                            targetMap.SetTileFlags(position, TileFlags.None);
                            targetMap.SetColor(position, customColor);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[TileManager] 프리셋 ID {data.id}에 해당하는 타일 에셋이 없습니다.");
                }
            }
            else
            {
                // [기믹 및 장식물 계열: Spike, Decoration, Door, ItemBox, Belt]
                // 런타임에 전용 스크립트 및 충돌 처리를 위해 실제 낱개 게임오브젝트로 직접 복제 스폰
                GameObject prefab = GetGimmickPrefab(data.name);
                if (prefab != null)
                {
                    Vector3 spawnPos = new Vector3(data.x * gridUnitSize, data.y * gridUnitSize, 0f);
                    
                    // 런타임 Instantiate로 안전 스폰
                    GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
                    instance.name = prefab.name;

                    // 색상 복원
                    SpriteRenderer sr = instance.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null && ColorUtility.TryParseHtmlString(data.color, out Color customColor))
                    {
                        sr.color = customColor;
                    }

                    // ColorMinus 컴포넌트 복원 처리 (리플렉션 + 셰이더)
                    ColorMinus colorMinus = instance.GetComponent<ColorMinus>();
                    if (colorMinus != null)
                    {
                        if (data.isColorAbsorbed)
                        {
                            System.Reflection.FieldInfo field = typeof(ColorMinus).GetField("isAbsorbed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (field != null)
                            {
                                field.SetValue(colorMinus, true);
                            }
                            if (sr != null)
                            {
                                sr.color = Color.white;
                                sr.material.SetFloat("_Progress", 1f);
                            }
                        }
                        else if (ColorUtility.TryParseHtmlString(data.originalColorHex, out Color origColor))
                        {
                            if (sr != null)
                            {
                                sr.color = origColor;
                                sr.material.SetColor("_OriginalColor", origColor);
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[TileManager] 기믹 프리팹 '{data.name}'을 MapEditor 프리팹 리스트에서 찾을 수 없습니다.");
                }
            }
        }
        Debug.Log("[TileManager] 하이브리드 지형 조립 및 기믹 스포닝 완료!");
    }

    // 이름 매칭을 통해 MapEditor의 프리팹 리스트에서 기믹 프리팹 찾기
    private GameObject GetGimmickPrefab(string name)
    {
        MapEditor editor = FindAnyObjectByType<MapEditor>();
        if (editor != null && editor.tilePrefabs != null)
        {
            string cleanNameInput = name.ToLower().Replace("_0", "").Replace(" ", "").Replace("_", "").Replace("1", "");
            foreach (var prefab in editor.tilePrefabs)
            {
                if (prefab != null)
                {
                    string cleanPrefabName = prefab.name.ToLower().Replace("_0", "").Replace(" ", "").Replace("_", "").Replace("1", "");
                    if (cleanPrefabName == cleanNameInput)
                    {
                        return prefab;
                    }
                }
            }
        }
        return null;
    }

    // 그리드 크기 자동 동기화 헬퍼
    private void SyncGridCellSize(Tilemap targetMap)
    {
        if (targetMap == null) return;
        Grid grid = targetMap.GetComponentInParent<Grid>();
        if (grid != null)
        {
            grid.cellSize = new Vector3(gridUnitSize, gridUnitSize, 1f);
        }
    }

    // JSON 파일 로컬 저장 헬퍼
    private void SaveMapData(string mapName, MapData mapData)
    {
        string folderPath = Path.Combine(Application.dataPath, "Resources/Maps");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, mapName + ".json");
        string jsonString = JsonUtility.ToJson(mapData, true);
        File.WriteAllText(filePath, jsonString);
        Debug.Log($"[TileManager] 맵 데이터 파일 다이렉트 세이브 완료: {filePath}");
    }

    // JSON 파일 로딩 및 복원 헬퍼
    private MapData LoadMapData(string resourcePath)
    {
        // Resources.Load 
        TextAsset mapTextAsset = Resources.Load<TextAsset>(resourcePath);

        if (mapTextAsset != null)
        {
            string jsonString = mapTextAsset.text;
            return JsonUtility.FromJson<MapData>(jsonString);
        }
        else
        {
            // 에디터 임포트 지연 예외 방어
            string filePath = Path.Combine(Application.dataPath, "Resources", resourcePath + ".json");
            if (File.Exists(filePath))
            {
                string jsonString = File.ReadAllText(filePath);
                return JsonUtility.FromJson<MapData>(jsonString);
            }
            else
            {
                Debug.LogWarning($"[TileManager] Resources 및 로컬 디스크에서 '{resourcePath}' 파일을 찾을 수 없어 빈 맵 데이터를 반환합니다.");
                return new MapData { tiles = new List<TileData>() };
            }
        }
    }
}

// CSPROJ 갱신 누락 방지용 글로벌 데이터 구조 정의
[System.Serializable]
public class TileData
{
    public int id;
    public string name;  // 타일 프리팹/스프라이트 에셋 이름
    public string type;  // 타일 타입
    public int x;        // 타일의 정밀 정규화 격자 x 좌표
    public int y;        // 타일의 정밀 정규화 격자 y 좌표
    public string color; // 타일 색상

    // 물감 충전 타일(ColorMinus)의 흡수 완료 여부 및 원색 저장
    public bool isColorAbsorbed;
    public string originalColorHex;
}

[System.Serializable]
public class MapData
{
    public List<TileData> tiles;
}
