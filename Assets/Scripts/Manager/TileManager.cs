using System.Collections.Generic;
using System.IO;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    [Header("--- 다중 타일맵 바인딩 ---")]
    public Tilemap solidTilemap;        // "Block" 지형 (Is Trigger: OFF, Layer: Ground)
    public Tilemap ladderTilemap;       // "Ladder" 지형 (Is Trigger: ON, Tag: Ladder)
    public Tilemap grabTilemap;         // "Grab" 지형 (Is Trigger: ON, Tag: Grab/Hanger)
    public Tilemap spikeTilemap;        // "Spike" 함정류 타일맵
    public Tilemap doorTilemap;         // "Door" 문류 타일맵
    public Tilemap itemBoxTilemap;      // "ItemBox" 상자류 타일맵
    public Tilemap beltTilemap;         // "Belt" 컨베이어벨트류 타일맵
    public Tilemap decorationTilemap;   // "Decoration" 장식물류 타일맵

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
        // 씬 내의 모든 타일맵 자동 바인딩 (이름 기반 검색 확장)
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
            else if (nameLower.Contains("spike") || nameLower.Contains("trap"))
            {
                spikeTilemap = map;
            }
            else if (nameLower.Contains("door"))
            {
                doorTilemap = map;
            }
            else if (nameLower.Contains("item") || nameLower.Contains("box") || nameLower.Contains("chest") || nameLower.Contains("crate"))
            {
                itemBoxTilemap = map;
            }
            else if (nameLower.Contains("belt") || nameLower.Contains("conveyor"))
            {
                beltTilemap = map;
            }
            else if (nameLower.Contains("deco") || nameLower.Contains("flag") || nameLower.Contains("prop"))
            {
                decorationTilemap = map;
            }
        }

        // 백업: 만약 자동 바인딩 후에도 solidTilemap이 없으면 아무 타일맵이나 잡음
        if (solidTilemap == null)
        {
            solidTilemap = FindAnyObjectByType<Tilemap>();
        }

        // 런타임 게임 구동용 맵 로딩 자동 실행
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

        // 8개 모든 세분화 타일맵 각각에 대해 데이터 수집 (지정된 고유 type 기입)
        CollectTilesFromMap(solidTilemap, "Block", mapData);
        CollectTilesFromMap(ladderTilemap, "Ladder", mapData);
        CollectTilesFromMap(grabTilemap, "Grab", mapData);
        CollectTilesFromMap(spikeTilemap, "Spike", mapData);
        CollectTilesFromMap(doorTilemap, "Door", mapData);
        CollectTilesFromMap(itemBoxTilemap, "ItemBox", mapData);
        CollectTilesFromMap(beltTilemap, "Belt", mapData);
        CollectTilesFromMap(decorationTilemap, "Decoration", mapData);

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

    // JSON을 읽어오고 8가지 타입 분류에 맞춰 완벽하게 전용 타일맵에 매칭 배치
    public void LoadMap(string fileName)
    {
        // Awake가 호출되지 않는 비플레이 모드에서 실행할 경우 딕셔너리를 즉석에서 자동 강제 빌드
        if (tileIdDictionary == null || tileIdDictionary.Count == 0 || tileAssetDictionary == null || tileAssetDictionary.Count == 0)
        {
            InitTileDictionaries();
        }

        // 모든 타일맵의 그리드 크기 동기화 및 기존 데이터 청소
        Tilemap[] allMaps = new Tilemap[] { solidTilemap, ladderTilemap, grabTilemap, spikeTilemap, doorTilemap, itemBoxTilemap, beltTilemap, decorationTilemap };
        foreach (var map in allMaps)
        {
            if (map != null)
            {
                SyncGridCellSize(map);
                map.ClearAllTiles();
            }
        }

        // 파일 로드 수행
        MapData mapData = LoadMapData("Maps/" + fileName);
        if (mapData == null || mapData.tiles == null) return;

        // 로드된 데이터를 기반으로 물리적 분기 배치 및 색상 적용
        foreach (var data in mapData.tiles)
        {
            // 타일맵용 정수 좌표
            Vector3Int tilePosition = new Vector3Int(Mathf.RoundToInt(data.x), Mathf.RoundToInt(data.y), 0);

            // [안전장치] Fuzzy Match 이름 매칭을 통해 tilePresets에서 정확한 타일 에셋 검색
            TileBase tile = tilePresets.Find(t => t != null && CleanTileName(t.name) == CleanTileName(data.name));
            if (tile == null)
            {
                tileIdDictionary.TryGetValue(data.id, out tile);      // 이름 실패 시 백업 조회
            }

            if (tile != null)
            {
                // data.type에 따라 명확하게 분기 처리 (가독성과 유지보수성 극대화)
                switch (data.type)
                {
                    // ==========================================
                    // 타일맵 드로잉 계열 (물리 및 단순 지형 역할)
                    // ==========================================
                    case "Block":
                        PlaceTileInMap(solidTilemap, tilePosition, tile, data.color);
                        break;

                    case "Ladder":
                        PlaceTileInMap(ladderTilemap ?? solidTilemap, tilePosition, tile, data.color);
                        break;

                    case "Grab":
                        PlaceTileInMap(grabTilemap ?? solidTilemap, tilePosition, tile, data.color);
                        break;

                    // ==========================================
                    // 런타임 프리팹 스폰 계열 (독립 기믹 작동)
                    // ==========================================
                    case "Spike":
                    case "Door":
                    case "ItemBox":
                    case "Belt":
                    case "Decoration":
                        SpawnGimmickObject(data, tilePosition);
                        break;

                    default:
                        // 예외 방어용 백업
                        PlaceTileInMap(solidTilemap, tilePosition, tile, data.color);
                        break;
                }
            }
            else
            {
                // 기믹 오브젝트인 경우, 타일 에셋(TileBase)이 등록되어 있지 않아도 프리팹만 있으면 바로 생성하도록 예외 방어 처리
                if (data.type == "Spike" || data.type == "Door" || data.type == "ItemBox" || data.type == "Belt" || data.type == "Decoration")
                {
                    SpawnGimmickObject(data, tilePosition);
                }
                else
                {
                    Debug.LogWarning($"[TileManager] 프리셋 리스트에서 '{data.name}'(ID: {data.id}) 타일 에셋을 찾을 수 없습니다.");
                }
            }
        }
        Debug.Log("<color=green>[TileManager]</color> 8종류 타입별 전용 타일맵 및 기믹 프리팹 완벽 복원 완료!");
    }

    // 타일맵에 타일을 칠하고 색상을 복원하는 헬퍼 함수
    private void PlaceTileInMap(Tilemap targetMap, Vector3Int position, TileBase tile, string colorHex)
    {
        if (targetMap == null) return;
        targetMap.SetTile(position, tile);

        if (ColorUtility.TryParseHtmlString(colorHex, out Color customColor))
        {
            targetMap.SetTileFlags(position, TileFlags.None);
            targetMap.SetColor(position, customColor);
        }
    }

    // 기믹 프리팹을 런타임에 낱개 오브젝트로 스폰하는 헬퍼 함수
    private void SpawnGimmickObject(TileData data, Vector3Int position)
    {
        GameObject prefab = GetGimmickPrefab(data.name);
        if (prefab != null)
        {
            Vector3 spawnPos = new Vector3(data.x * gridUnitSize, data.y * gridUnitSize, 0f);
            GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
            instance.name = prefab.name;

            // 색상 복원
            SpriteRenderer sr = instance.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && ColorUtility.TryParseHtmlString(data.color, out Color customColor))
            {
                sr.color = customColor;
            }

            // ColorMinus 색상 흡수 기믹 정보 복원
            ColorMinus colorMinus = instance.GetComponent<ColorMinus>();
            if (colorMinus != null)
            {
                if (data.isColorAbsorbed)
                {
                    System.Reflection.FieldInfo field = typeof(ColorMinus).GetField("isAbsorbed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null) field.SetValue(colorMinus, true);
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
            Debug.LogWarning($"[TileManager] 기믹 프리팹 '{data.name}'을 프리팹 리스트에서 찾을 수 없습니다.");
        }
    }

    // 복사본 숫자, 공백, 언더바, _0 접미사 등을 도려내는 Fuzzy Match 이름 정제 함수
    private string CleanTileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        
        // 괄호 복사본 접미사 정리: "tile_brick_0 (1)" -> "tile_brick_0"
        int index = name.IndexOf(" (");
        if (index > 0)
        {
            name = name.Substring(0, index);
        }

        // 소문자 변환 후 불필요한 노이즈 도려냄
        return name.ToLower()
                   .Replace("_0", "")
                   .Replace(" ", "")
                   .Replace("_", "")
                   .Replace("1", "")
                   .Replace("2", "")
                   .Replace("3", "")
                   .Replace("4", "")
                   .Replace("5", "")
                   .Replace("6", "")
                   .Replace("7", "")
                   .Replace("8", "")
                   .Replace("9", "");
    }

    // 이름 매칭을 통해 MapEditor의 프리팹 리스트에서 기믹 프리팹 찾기
    private GameObject GetGimmickPrefab(string name)
    {
        MapEditor editor = FindAnyObjectByType<MapEditor>();
        if (editor != null && editor.tilePrefabs != null)
        {
            string cleanNameInput = CleanTileName(name);
            foreach (var prefab in editor.tilePrefabs)
            {
                if (prefab != null)
                {
                    string cleanPrefabName = CleanTileName(prefab.name);
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
        TextAsset mapTextAsset = Resources.Load<TextAsset>(resourcePath);

        if (mapTextAsset != null)
        {
            string jsonString = mapTextAsset.text;
            return JsonUtility.FromJson<MapData>(jsonString);
        }
        else
        {
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
    public string name;            // 타일 프리팹/스프라이트 에셋 이름
    public string type;            // 타일 타입
    public float x;                // 격자 x 좌표
    public float y;                // 격자 y 좌표
    public string color;           // 타일 색상
    public bool isColorAbsorbed;   // 물감 흡수 여부
    public string originalColorHex;// 흡수 전 원래 색상
}

[System.Serializable]
public class MapData
{
    public List<TileData> tiles;
}