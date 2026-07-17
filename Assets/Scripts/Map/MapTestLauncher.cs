using UnityEngine;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapTestLauncher : MonoBehaviour
{
    [Header("--- Target Config ---")]
    [SerializeField] private string targetMapName = "Stage1";   // 불러올 맵 이름 (Maps/ 폴더 기준 파일명)
    [SerializeField] private float gridUnitSize = 1.28f;        // 기준 그리드 유닛 크기 (사이사이 보간 작업)

    private void Start()
    {
        // 씬 내의 임시 배치용 "tile_" 껍데기 오브젝트들을 탐색하여 자동 비활성화 (투명 벽 충돌 차단)
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().StartsWith("tile_"))
            {
                obj.SetActive(false);
            }
        }

        // 씬 안에 배치되어 있는 TileManager 컴포넌트 찾기
        TileManager tileManager = FindAnyObjectByType<TileManager>();

        if (tileManager != null)
        {
            // 로드 완료된 데이터를 기반으로 타일맵 생성 구동
            tileManager.LoadMap(targetMapName);
            Debug.Log($"[MapTestLauncher] '{targetMapName}' 맵 로딩 성공!");
        }
        else
        {
            Debug.LogError("[MapTestLauncher] 씬 내에서 TileManager를 찾을 수 없습니다.");
        }
    }

    // 인스펙터의 컴포넌트 이름 우클릭 메뉴로 실행 가능한 씬
    [ContextMenu("Scan Scene to JSON (Stage1)")]
    public void ScanSceneToStage1()
    {
        // 씬 내의 "tile_"로 시작하는 모든 일반 오브젝트 탐색
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
        List<TileData> scannedTiles = new List<TileData>();

        TileManager tileManager = FindAnyObjectByType<TileManager>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().StartsWith("tile_"))
            {
                TileData data = new TileData();

                // 이름 Parse "tile_brick_0 (12)" -> "tile_brick_0" (유지 보수)
                string cleanName = obj.name;
                int index = cleanName.IndexOf(" (");
                if (index > 0)
                {
                    cleanName = cleanName.Substring(0, index);
                }

                data.name = cleanName;

                // 그리드 유닛 정규화 스냅 적용 (소수점 배치 튐으로 인한 틈새 발생 방지)
                data.x = Mathf.RoundToInt(obj.transform.position.x / gridUnitSize);
                data.y = Mathf.RoundToInt(obj.transform.position.y / gridUnitSize);

                //  TileManager 프리셋 대조
                int matchedId = 0;
                if (tileManager != null && tileManager.tilePresets != null)
                {
                    string cleanInput = cleanName.ToLower().Replace("_0", "").Replace(" ", "").Replace("_", "").Replace("1", "");
                    for (int i = 0; i < tileManager.tilePresets.Count; i++)
                    {
                        var tile = tileManager.tilePresets[i];
                        if (tile != null)
                        {
                            string cleanTileName = tile.name.ToLower().Replace("_0", "").Replace(" ", "").Replace("_", "").Replace("1", "");
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

        MapData scannedMapData = new MapData { tiles = scannedTiles };

        // Resources/Maps 폴더에 Stage1.json 파일로 다이렉트 저장
#if UNITY_EDITOR
        string folderPath = Path.Combine(Application.dataPath, "Resources/Maps");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, "Stage1.json");
        string jsonString = JsonUtility.ToJson(scannedMapData, true);
        File.WriteAllText(filePath, jsonString);

        // 유니티 프로젝트 에셋 데이터베이스 새로고침 
        AssetDatabase.Refresh();
        Debug.Log($"<color=green>[스캐너 완료]</color> 씬의 타일 오브젝트 {scannedTiles.Count}개를 1.28 스냅 보정 적용하여 '{filePath}'에 저장 완료!");
#endif
    }
}
