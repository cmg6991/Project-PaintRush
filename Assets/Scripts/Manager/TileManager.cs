using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    public Tilemap tilemap; // 유니티 Grid 산하의 Tilemap 컴포넌트 연결
    public List<TileBase> tilePresets; // 사용할 타일 에셋들을 인스펙터에서 등록 

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

    // 현재 타일맵 정보를 DataManager의 CurrentMapData에 채우고 JSON으로 저장 요청
    public void SaveMap(string fileName)
    {
        if (DataManager.Instance == null) return;

        // DataManager 내부 리스트 초기화
        DataManager.Instance.CurrentMapData.tiles.Clear();

        BoundsInt bounds = tilemap.cellBounds;

        foreach (var pos in bounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(pos);
            if (tile != null)
            {
                // 프리셋에 등록된 타일인 경우에만 ID를 추출하여 저장
                if (tileAssetDictionary.TryGetValue(tile, out int id))
                {
                    // 타일의 색상 정보 추출 (기본값은 흰색 #FFFFFF)
                    Color tileColor = tilemap.GetColor(pos);
                    string colorHex = "#" + ColorUtility.ToHtmlStringRGBA(tileColor);

                    TileData data = new TileData
                    {
                        id = id,
                        x = pos.x,
                        y = pos.y,
                        color = colorHex
                    };
                    DataManager.Instance.CurrentMapData.tiles.Add(data);
                }
            }
        }

        // DataManager에게 실질적인 JSON 물리 파일 저장 요청
        DataManager.Instance.SaveMapToJson(fileName);
    }

    // DataManager를 통해 JSON을 읽어오고 화면에 배치
    public void LoadMap(string fileName)
    {
        if (DataManager.Instance == null) return;

        // DataManager에게 파일 로드 요청 -> CurrentMapData에 데이터가 채워짐
        DataManager.Instance.LoadMapFromResources(fileName);

        // 기존 타일맵 청소
        tilemap.ClearAllTiles();

        // 로드된 데이터를 기반으로 타일 재배치 및 색상 적용
        foreach (var data in DataManager.Instance.CurrentMapData.tiles)
        {
            if (tileIdDictionary.TryGetValue(data.id, out TileBase tile))
            {
                // float 좌표를 정수형 Vector3Int에 맞게 반올림 캐스팅하고, 대문자 오타(Int)를 교정합니다.
                Vector3Int position = new Vector3Int(Mathf.RoundToInt(data.x), Mathf.RoundToInt(data.y), 0);

                // 타일 배치
                tilemap.SetTile(position, tile);

                // 헥스코드 문자열을 Color 구조체로 변환하여 적용
                if (ColorUtility.TryParseHtmlString(data.color, out Color customColor))
                {
                    // 타일맵에서 특정 좌표의 타일 색상을 변경하려면 플래그 변경이 선행되어야 정상 적용됩니다.
                    tilemap.SetTileFlags(position, TileFlags.None);
                    tilemap.SetColor(position, customColor);
                }
            }
            else
            {
                Debug.LogWarning($"[TileManager] 프리셋 ID {data.id}에 해당하는 타일 에셋이 없습니다.");
            }
        }
        Debug.Log("[TileManager] 맵 배치 및 컬러 동기화 완료!");
    }
}
