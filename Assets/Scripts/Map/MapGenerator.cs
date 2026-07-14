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


        foreach (var tileInfo in mapData.tiles)
        {
            if (tileInfo.id >= 0 && tileInfo.id < tilePrefabs.Length)
            {
                Vector3Int position = new Vector3Int(tileInfo.x, tileInfo.y, 0);
                tilemap.SetTile(position, tilePrefabs[tileInfo.id]);
            }
        }
        Debug.Log("[MapGenerator] 타일맵 배치 완료!");
    }
}