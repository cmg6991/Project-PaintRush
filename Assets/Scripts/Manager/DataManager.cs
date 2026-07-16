using System.IO;
using System.Collections.Generic; // List 사용을 위해 추가
using UnityEngine;

[System.Serializable]
public class TileData
{
    public int id;
    public string name;  // 타일 프리팹/스프라이트 에셋 이름 (예: tile_grass)
    public string type;  // 타일 타입 (Block, Ladder, Hazard 등)
    public int x;        // 타일 위치
    public int y;
    public string color; // 타일 색상
}

[System.Serializable]
public class MapData
{
    public List<TileData> tiles;
}

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public MapData CurrentMapData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CurrentMapData = new MapData { tiles = new List<TileData>() };
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // [세이브 기능]: 씬의 타일 정보를 지정된 JSON 물리 파일로 보존
    public void SaveMapToJson(string mapName)
    {
        string folderPath = Path.Combine(Application.dataPath, "Resources/Maps");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, mapName + ".json");
        string jsonString = JsonUtility.ToJson(CurrentMapData, true);
        File.WriteAllText(filePath, jsonString);
        Debug.Log($"[DataManager] 맵 데이터 세이브 완료: {filePath}");
    }

    // [로드 기능]: 프로젝트 Resources 폴더에서 JSON 맵 텍스트 에셋을 로드하여 복원
    public void LoadMapFromResources(string resourcePath)
    {
        // 1. Resources.Load 시도
        TextAsset mapTextAsset = Resources.Load<TextAsset>(resourcePath);

        if (mapTextAsset != null)
        {
            string jsonString = mapTextAsset.text;
            CurrentMapData = JsonUtility.FromJson<MapData>(jsonString);
            Debug.Log($"[DataManager] Resources '{resourcePath}' 맵 로드 완료");
        }
        else
        {
            // 백업: 에디터 임포트 지연 예외 방어 (실제 하드디스크 경로에서 직접 긁어오기)
            string filePath = Path.Combine(Application.dataPath, "Resources", resourcePath + ".json");
            if (File.Exists(filePath))
            {
                string jsonString = File.ReadAllText(filePath);
                CurrentMapData = JsonUtility.FromJson<MapData>(jsonString);
                Debug.Log($"[DataManager] 에디터 백업 로더로 '{filePath}' 파일 다이렉트 로드 성공!");
            }
            else
            {
                Debug.LogWarning($"[DataManager] Resources 및 로컬 디스크에서 '{resourcePath}' 파일을 찾을 수 없어 빈 맵 데이터로 초기화합니다.");
                CurrentMapData = new MapData { tiles = new List<TileData>() };
            }
        }
    }
}