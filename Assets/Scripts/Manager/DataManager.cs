using System.IO;
using System.Collections.Generic; // List 사용을 위해 추가
using UnityEngine;

[System.Serializable]
public class TileData
{
    public int id;
    public int x;
    public int y;
    public string color;
}

[System.Serializable]
public class MapData
{
    // JsonUtility는 기본 배열([])이나 List<> 모두 지원합니다. 편의상 List로 변경해두면 추가/삭제가 쉽습니다.
    public List<TileData> tiles = new List<TileData>();
}

// Singleton<DataManager>를 상속
public class DataManager : Singleton<DataManager>
{
    // 오직 맵 데이터만 집중 관리하도록 변경
    public MapData CurrentMapData { get; private set; } = new MapData();

    public override void Awake()
    {
        base.Awake(); // 부모 Singleton의 중복 파괴 로직 호출

        // 씬 시작시 맵 데이터 초기화
        if (CurrentMapData == null)
        {
            CurrentMapData = new MapData();
        }
    }

    // [세이브 기능]: 현재 맵 데이터를 JSON 파일로 저장
    public void SaveMapToJson(string fileName)
    {
        // 빌드 후 읽기 전용인 StreamingAssets 대신 에디터 테스트 및 모바일 저장이 자유로운 persistentDataPath를 추천합니다.
        // 만약 반드시 StreamingAssets를 써야 한다면 그대로 두셔도 됩니다.
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        string directoryPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // CurrentMapData 객체를 JSON 문자열로 변환
        string jsonString = JsonUtility.ToJson(CurrentMapData, true);
        File.WriteAllText(filePath, jsonString);
        Debug.Log($"[DataManager] 맵 데이터 세이브 완료: {filePath}");
    }

    // [로드 기능]: JSON 파일을 읽어 CurrentMapData로 복원
    public void LoadMapFromJson(string fileName)
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(filePath))
        {
            string jsonString = File.ReadAllText(filePath);
            CurrentMapData = JsonUtility.FromJson<MapData>(jsonString);
            Debug.Log($"[DataManager] '{fileName}' 맵 로드 완료");
        }
        else
        {
            Debug.LogWarning($"[DataManager] 맵 파일을 찾을 수 없어 빈 맵 데이터로 초기화합니다: {filePath}");
            CurrentMapData = new MapData();
        }
    }
}