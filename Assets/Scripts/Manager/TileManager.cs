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
            if (tileIdDictionary.TryGetValue(data.id, out TileBase tile))
            {
                Vector3Int position = new Vector3Int(Mathf.RoundToInt(data.x), Mathf.RoundToInt(data.y), 0);

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
        Debug.Log("[TileManager] 다중 물리 타일맵 배치 및 컬러 동기화 완료!");
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
        // Resources.Load 시도
        TextAsset mapTextAsset = Resources.Load<TextAsset>(resourcePath);

        if (mapTextAsset != null)
        {
            string jsonString = mapTextAsset.text;
            return JsonUtility.FromJson<MapData>(jsonString);
        }
        else
        {
            // 백업: 에디터 임포트 지연 예외 방어
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
}

[System.Serializable]
public class MapData
{
    public List<TileData> tiles;
}
