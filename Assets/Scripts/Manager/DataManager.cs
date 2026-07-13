using System.IO;
using UnityEngine;

[System.Serializable]
public class TileData { public int id; public int x; public int y; }

[System.Serializable]
public class MapData { public TileData[] tiles; }

// Singleton<DataManager>를 상속
public class DataManager : Singleton<DataManager>
{
    public MapData LoadedMapData { get; private set; }

    // 싱글톤 유지 방법
    public override void Awake()
    {
        base.Awake();
        // 추가적인 초기화 코드들...
    }

    public void LoadMapFromJson(string fileName)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        if (File.Exists(filePath))
        {
            string jsonString = File.ReadAllText(filePath);
            LoadedMapData = JsonUtility.FromJson<MapData>(jsonString);
            Debug.Log($"[DataManager] '{fileName}' 로드 완료");
        }
        else
        {
            Debug.LogError($"[DataManager] 파일을 찾을 수 없습니다: {filePath}");
        }
    }
}
