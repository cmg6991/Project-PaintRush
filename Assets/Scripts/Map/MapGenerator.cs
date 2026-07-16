using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase[] tilePrefabs;

    public void GenerateMap(MapData mapData)
    {
        if (tilemap == null || mapData == null || mapData.tiles == null)
        {
            Debug.LogError("[MapGenerator] 타일맵 컴포넌트나 데이터가 누락되었습니다.");
            return;
        }

        // 기존에 깔려있던 타일들을 싹 지우고 새로 생성
        tilemap.ClearAllTiles();

        foreach (var tileInfo in mapData.tiles)
        {
            TileBase targetTile = null;

            // 이름이 있다면 이름으로 타일 에셋을 우선 검색
            if (!string.IsNullOrEmpty(tileInfo.name))
            {
                targetTile = FindTileByName(tileInfo.name);
                
                // 디버그 로그 심기
                Debug.Log($"[MapGen 디버그] JSON 이름: {tileInfo.name} ➡️ 매칭된 타일 에셋: {(targetTile != null ? targetTile.name : "Null")}");
            }

            // 이름으로 찾지 못했으나 id가 유효 범위라면 인덱스로 백업 검색
            if (targetTile == null && tileInfo.id >= 0 && tileInfo.id < tilePrefabs.Length)
            {
                targetTile = tilePrefabs[tileInfo.id];
            }

            if (targetTile != null)
            {
                // 소수점 맵이어도 정수형 타일 격자에 칼같이 강제 피팅
                Vector3Int position = new Vector3Int(Mathf.RoundToInt(tileInfo.x), Mathf.RoundToInt(tileInfo.y), 0);
                tilemap.SetTile(position, targetTile);
            }
            else
            {
                Debug.LogWarning($"[MapGenerator] 타일을 찾을 수 없습니다. (Name: {tileInfo.name}, ID: {tileInfo.id})");
            }
        }
        Debug.Log("[MapGenerator] 정수 격자 스냅 매칭 타일맵 배치 완료!");
    }

    // 이름이 일치하는 타일 에셋 검색 헬퍼
    private TileBase FindTileByName(string tileName)
    {
        if (tilePrefabs == null) return null;

        // 비교할 입력 이름을 소문자로 바꾸고, _0, 공백, 언더바, 복사본, 부속 숫자 제거
        string cleanInput = tileName.ToLower().Replace("_0", "").Replace(" ", "").Replace("_", "").Replace("1", "");

        foreach (var tile in tilePrefabs)
        {
            if (tile != null)
            {
                // 등록된 타일 이름 역시 똑같이 걷어내어 비교
                string cleanTileName = tile.name.ToLower().Replace("_0", "").Replace(" ", "").Replace("_", "").Replace("1", "");
                if (cleanTileName == cleanInput)
                {
                    return tile;
                }
            }
        }
        return null;
    }
}