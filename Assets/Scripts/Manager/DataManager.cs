using System.IO;
using UnityEditor.Overlays;
using UnityEngine;

[System.Serializable]
public class TileData { public int id; public int x; public int y; public string color; }

[System.Serializable]
public class MapData { public TileData[] tiles; }

// Singleton<DataManager>를 상속
public class DataManager : Singleton<DataManager>
{
    public SaveData CurrentSaveData { get; private set; } = new SaveData();

    // 싱글톤 유지 방법
    public override void Awake()
    {
        base.Awake();       // 부모 Singleton의 중복 파괴 로직 호출
        
        // 씬 시작시 데이터 초기화
        if (CurrentSaveData == null)
        {
            CurrentSaveData = new SaveData();
        }
    }

    // [세이브 기능]: CurrentSaveData를 JSON 파일로 디스크에 물리적 저장
    public void SaveGameToJson(string fileName)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        
        // 디렉토리가 없으면 생성 (StreamingAssets)
        string directoryPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string jsonString = JsonUtility.ToJson(CurrentSaveData, true);
        File.WriteAllText(filePath, jsonString);
        Debug.Log($"[DataManager] 세이브 완료: {filePath}");
    }

    // [로드 기능]: JSON 파일을 읽어 CurrentSaveData로 복원
    public void LoadGameFromJson(string fileName)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        if (File.Exists(filePath))
        {
            string jsonString = File.ReadAllText(filePath);
            CurrentSaveData = JsonUtility.FromJson<SaveData>(jsonString);
            Debug.Log($"[DataManager] '{fileName}' 로드 완료");
        }
        else
        {
            Debug.LogWarning($"[DataManager] 세이브 파일을 찾을 수 없어 기본 데이터로 초기화합니다: {filePath}");
            CurrentSaveData = new SaveData();
        }
    }
}
