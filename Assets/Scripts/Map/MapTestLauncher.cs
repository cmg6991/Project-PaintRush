using UnityEngine;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapTestLauncher : MonoBehaviour
{
    [Header("--- Target Config ---")]
    [SerializeField] private string targetMapName = "Maps/Stage1"; // 불러올 맵 이름

    private void Start()
    {
        // DataManager에게 Resources 폴더 안의 JSON 파일 로드 요청
        DataManager.Instance.LoadMapFromResources(targetMapName);

        // 씬 안에 배치되어 있는 MapGenerator 컴포넌트 찾기
        MapGenerator generator = FindAnyObjectByType<MapGenerator>();

        if (generator != null)
        {
            // 로드 완료된 데이터를 기반으로 타일맵 생성 구동
            generator.GenerateMap(DataManager.Instance.CurrentMapData);
            Debug.Log($"[MapTestLauncher] '{targetMapName}' 맵 로딩 및 배치 성공!");
        }
        else
        {
            Debug.LogError("[MapTestLauncher] 씬 내에서 MapGenerator를 찾을 수 없습니다.");
        }
    }

    // 인스펙터의 컴포넌트 이름 우클릭 메뉴로 실행 가능한 씬
    [ContextMenu("Scan Scene to JSON (Stage1)")]
    public void ScanSceneToStage1()
    {
        // 데이터 초기화
        DataManager.Instance.CurrentMapData.tiles.Clear();

        // 씬 내의 "tile_"로 시작하는 모든 일반 오브젝트 탐색
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        List<TileData> scannedTiles = new List<TileData>();

        MapGenerator generator = FindObjectOfType<MapGenerator>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().StartsWith("tile_"))
            {
                TileData data = new TileData();

                // 이름 정제: "tile_brick_0 (12)" -> "tile_brick_0"
                string cleanName = obj.name;
                int index = cleanName.IndexOf(" (");
                if (index > 0)
                {
                    cleanName = cleanName.Substring(0, index);
                }

                data.name = cleanName;
                data.x = Mathf.RoundToInt(obj.transform.position.x);
                data.y = Mathf.RoundToInt(obj.transform.position.y);

                //  ID 자동 분석 연산
                int matchedId = 0;
                if (generator != null && generator.tilePrefabs != null)
                {
                    string cleanInput = cleanName.ToLower().Replace("_0", "").Replace(" ", "").Replace("_", "").Replace("1", "");
                    for (int i = 0; i < generator.tilePrefabs.Length; i++)
                    {
                        if (generator.tilePrefabs[i] != null)
                        {
                            string cleanTileName = generator.tilePrefabs[i].name.ToLower().Replace("_0", "").Replace(" ", "").Replace("_", "").Replace("1", "");
                            if (cleanTileName == cleanInput)
                            {
                                matchedId = i;
                                break;
                            }
                        }
                    }
                }
                data.id = matchedId;

                // Color 자동 검출 연산
                SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    Color col = sr.color;
                    if (col == Color.red) data.color = "Red";
                    else if (col == Color.green) data.color = "Green";
                    else if (col == Color.blue) data.color = "Blue";
                    else if (col == Color.yellow) data.color = "Yellow";
                    else data.color = "White";
                }
                else
                {
                    data.color = "White";
                }

                // Type 정밀 세분화 연산
                string nameLower = cleanName.ToLower();
                if (nameLower.Contains("ladder")) data.type = "Ladder";
                else if (nameLower.Contains("spike")) data.type = "Hazard";
                else if (nameLower.Contains("castle")) data.type = "Castle";
                else if (nameLower.Contains("crate")) data.type = "Crate";
                else if (nameLower.Contains("door")) data.type = "Door";
                else if (nameLower.Contains("bridge")) data.type = "Bridge";
                else if (nameLower.Contains("cog")) data.type = "Cog";
                else if (nameLower.Contains("grab")) data.type = "Grab";
                else if (nameLower.Contains("grass")) data.type = "Grass";
                else data.type = "Block";

                scannedTiles.Add(data);
            }
        }

        DataManager.Instance.CurrentMapData.tiles = scannedTiles;

        // Resources/Maps 폴더에 Stage1.json 파일로 다이렉트 저장
#if UNITY_EDITOR
        string folderPath = Path.Combine(Application.dataPath, "Resources/Maps");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, "Stage1.json");
        string jsonString = JsonUtility.ToJson(DataManager.Instance.CurrentMapData, true);
        File.WriteAllText(filePath, jsonString);

        // 유니티 프로젝트 에셋 데이터베이스 새로고침 (즉시 탐색기에 보임)
        AssetDatabase.Refresh();
        Debug.Log($"<color=green>[스캐너 완료]</color> 씬의 타일 오브젝트 {scannedTiles.Count}개를 성공적으로 스캔하여 '{filePath}' 저장 및 갱신 완료!");
#endif
    }
}
