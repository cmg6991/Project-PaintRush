#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapEditor))]
public class MapEditorCustom : Editor
{
    private MapEditor mapEditor;
    private Vector2 lastMouseGridPos;

    private void OnEnable()
    {
        mapEditor = (MapEditor)target;
        // 씬 뷰 마우스 무브 이벤트를 실시간으로 캡처하기 위해 등록
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 속성 그리기
        DrawDefaultInspector();

        GUILayout.Space(15);
        GUILayout.Label("--- 맵 에디터 컨트롤 ---", EditorStyles.boldLabel);

        // 페인트 모드 토글 버튼 (화려한 HSL 스펙트럼 색상 시각화)
        Color oldColor = GUI.backgroundColor;
        GUI.backgroundColor = mapEditor.isPaintMode ? Color.green : Color.grey;
        if (GUILayout.Button(mapEditor.isPaintMode ? " 브러쉬 모드 가동 중 (클릭 시 종료)" : " 브러쉬 모드 켜기 (씬 뷰 드로잉 활성화)", GUILayout.Height(35)))
        {
            mapEditor.isPaintMode = !mapEditor.isPaintMode;
            if (mapEditor.isPaintMode) mapEditor.isEraserMode = false;
        }

        // 지우개 모드 토글 버튼
        GUI.backgroundColor = mapEditor.isEraserMode ? Color.red : Color.grey;
        if (GUILayout.Button(mapEditor.isEraserMode ? " 지우개 모드 가동 중 (클릭 시 종료)" : " 지우개 모드 켜기 (Shift + 좌클릭으로도 가능)", GUILayout.Height(30)))
        {
            mapEditor.isEraserMode = !mapEditor.isEraserMode;
            if (mapEditor.isEraserMode) mapEditor.isPaintMode = false;
        }
        GUI.backgroundColor = oldColor;

        // 브러쉬 프리팹 리스트 선택 그리드
        if (mapEditor.tilePrefabs != null && mapEditor.tilePrefabs.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label("배치할 브러쉬 타일 선택:", EditorStyles.boldLabel);

            string[] names = new string[mapEditor.tilePrefabs.Count];
            for (int i = 0; i < mapEditor.tilePrefabs.Count; i++)
            {
                names[i] = mapEditor.tilePrefabs[i] != null ? mapEditor.tilePrefabs[i].name : "Empty";
            }

            mapEditor.selectedPrefabIndex = GUILayout.SelectionGrid(mapEditor.selectedPrefabIndex, names, 2);
        }
        else
        {
            EditorGUILayout.HelpBox("아래 프리팹 리스트(Tile Prefabs)에 배치할 타일 프리팹들을 채워 넣어 주세요!", MessageType.Info);
        }

        GUILayout.Space(15);
        GUILayout.Label("세이브 / 로드 (JSON 연동)", EditorStyles.boldLabel);

        // 세이브 및 로드 버튼 배치
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("씬 데이터 세이브 (JSON 굽기)", GUILayout.Height(40)))
        {
            SaveSceneToJson();
        }
        if (GUILayout.Button("JSON 맵 불러오기 (씬 복원)", GUILayout.Height(40)))
        {
            LoadJsonToScene();
        }
        GUILayout.EndHorizontal();

        // 씬 초기화 버튼
        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
        if (GUILayout.Button("씬 내 모든 임시 타일 싹 청소하기", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("경고", "씬에 배치된 모든 'tile_' 오브젝트가 영구 삭제됩니다. 진행하시겠습니까?", "예", "아니오"))
            {
                ClearAllSpawnedTiles();
            }
        }
        GUI.backgroundColor = oldColor;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (mapEditor == null) return;

        // 페인트 모드나 지우개 모드가 켜져 있을 때만 마우스 드로잉 가동
        if (!mapEditor.isPaintMode && !mapEditor.isEraserMode) return;

        // 씬 뷰에서 기본 마우스 드래그 선택 박스가 튀어나가는 현상 차단
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Vector3 mouseWorldPos = ray.origin;
        mouseWorldPos.z = 0f;

        // 그리드 스냅 계산
        float snapX = Mathf.Round(mouseWorldPos.x / mapEditor.gridUnitSize) * mapEditor.gridUnitSize;
        float snapY = Mathf.Round(mouseWorldPos.y / mapEditor.gridUnitSize) * mapEditor.gridUnitSize;
        Vector2 snapPos = new Vector2(snapX, snapY);

        // 반투명 고스트 프리뷰 그리기 (설치 범위 비주얼 가이드)
        Color previewColor = mapEditor.isEraserMode || e.shift ? new Color(1f, 0f, 0f, 0.4f) : new Color(0f, 1f, 0f, 0.4f);
        Handles.color = previewColor;
        Handles.DrawSolidRectangleWithOutline(new Rect(snapX - mapEditor.gridUnitSize / 2f, snapY - mapEditor.gridUnitSize / 2f, mapEditor.gridUnitSize, mapEditor.gridUnitSize), previewColor, Color.white);
        sceneView.Repaint();

        // 마우스 입력 처리 (그리기 및 지우기)
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
        {
            if (mapEditor.isEraserMode || e.shift)
            {
                // 지우기 시전
                DeleteTileAtPosition(snapPos);
            }
            else if (mapEditor.isPaintMode)
            {
                // 그리기 시전
                PaintTileAtPosition(snapPos);
            }
            e.Use(); // 이벤트 소비
        }
    }

    private void PaintTileAtPosition(Vector2 position)
    {
        if (mapEditor.tilePrefabs == null || mapEditor.tilePrefabs.Count == 0) return;
        GameObject prefab = mapEditor.tilePrefabs[mapEditor.selectedPrefabIndex];
        if (prefab == null) return;

        // 해당 좌표에 이미 동일한 이름의 오브젝트가 존재하면 중복 생성 스킵
        Transform parent = mapEditor.spawnParent != null ? mapEditor.spawnParent : mapEditor.transform;
        foreach (Transform child in parent)
        {
            if (Vector2.Distance(child.position, position) < 0.05f)
            {
                return; // 겹쳐진 곳이 있으므로 배치 건너뜀
            }
        }

        // PrefabUtility.InstantiatePrefab을 써서 유니티 프리팹 연결 정합성을 100% 보존
        GameObject newTile = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        if (newTile != null)
        {
            newTile.transform.position = new Vector3(position.x, position.y, 0f);
            newTile.name = prefab.name;

            // 유니티 되돌리기(Ctrl + Z) 지원 연동
            Undo.RegisterCreatedObjectUndo(newTile, "Paint Tile");
        }
    }

    private void DeleteTileAtPosition(Vector2 position)
    {
        Transform parent = mapEditor.spawnParent != null ? mapEditor.spawnParent : mapEditor.transform;
        List<GameObject> toDestroy = new List<GameObject>();

        foreach (Transform child in parent)
        {
            if (Vector2.Distance(child.position, position) < 0.05f)
            {
                toDestroy.Add(child.gameObject);
            }
        }

        foreach (GameObject obj in toDestroy)
        {
            // 되돌리기(Ctrl + Z)를 지원하며 즉각적 삭제
            Undo.DestroyObjectImmediate(obj);
        }
    }

    private void ClearAllSpawnedTiles()
    {
        Transform parent = mapEditor.spawnParent != null ? mapEditor.spawnParent : mapEditor.transform;
        List<GameObject> toDestroy = new List<GameObject>();

        foreach (Transform child in parent)
        {
            if (child.name.ToLower().StartsWith("tile_"))
            {
                toDestroy.Add(child.gameObject);
            }
        }

        foreach (GameObject obj in toDestroy)
        {
            Undo.DestroyObjectImmediate(obj);
        }
        Debug.Log("[MapEditor] 씬 내 모든 임시 타일이 싹 정리되었습니다.");
    }

    // JSON 저장 구현 (씬 내 오브젝트들을 다 긁어모아 JSON 덮어쓰기)
    private void SaveSceneToJson()
    {
        Transform parent = mapEditor.spawnParent != null ? mapEditor.spawnParent : mapEditor.transform;
        List<TileData> scannedTiles = new List<TileData>();

        // 씬 내의 프리팹 이름들과 비교할 딕셔너리 구성
        Dictionary<string, int> prefabIdMap = new Dictionary<string, int>();
        for (int i = 0; i < mapEditor.tilePrefabs.Count; i++)
        {
            if (mapEditor.tilePrefabs[i] != null)
            {
                prefabIdMap[mapEditor.tilePrefabs[i].name.ToLower()] = i;
            }
        }

        foreach (Transform child in parent)
        {
            if (child.name.ToLower().StartsWith("tile_"))
            {
                TileData data = new TileData();

                // 이름 정제
                string cleanName = child.name;
                int index = cleanName.IndexOf(" (");
                if (index > 0)
                {
                    cleanName = cleanName.Substring(0, index);
                }
                data.name = cleanName;

                // 1.28 그리드 인덱스로 나누어 저장 (소수점 깨짐 완치)
                data.x = Mathf.RoundToInt(child.position.x / mapEditor.gridUnitSize);
                data.y = Mathf.RoundToInt(child.position.y / mapEditor.gridUnitSize);

                // ID 자동 매핑
                int matchedId = 0;
                string cleanInput = CleanTileName(cleanName);
                foreach (var kp in prefabIdMap)
                {
                    string cleanTileName = CleanTileName(kp.Key);
                    if (cleanTileName == cleanInput)
                    {
                        matchedId = kp.Value;
                        break;
                    }
                }
                data.id = matchedId;

                // 색상 정보 및 기믹 타입 수집
                SpriteRenderer sr = child.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    data.color = "#" + ColorUtility.ToHtmlStringRGBA(sr.color);
                }
                else
                {
                    data.color = "#FFFFFF";
                }

                // 타입 판별 (8가지 세분화 분류)
                string nameLower = cleanName.ToLower();
                if (nameLower.Contains("spike"))
                {
                    data.type = "Spike";
                }
                else if (nameLower.Contains("grab") || nameLower.Contains("hanger"))
                {
                    data.type = "Grab";
                }
                else if (nameLower.Contains("ladder"))
                {
                    data.type = "Ladder";
                }
                else if (nameLower.Contains("flag") || nameLower.Contains("fence"))
                {
                    data.type = "Decoration";
                }
                else if (nameLower.Contains("door"))
                {
                    data.type = "Door";
                }
                else if (nameLower.Contains("chest") || nameLower.Contains("crate") || nameLower.Contains("item"))
                {
                    data.type = "ItemBox";
                }
                else if (nameLower.Contains("belt") || nameLower.Contains("conveyor"))
                {
                    data.type = "Belt";
                }
                else
                {
                    data.type = "Block";
                }

                // ColorMinus 컴포넌트 데이터 연동 수집
                ColorMinus colorMinus = child.GetComponent<ColorMinus>();
                if (colorMinus != null)
                {
                    data.isColorAbsorbed = colorMinus.IsAbsorbed;
                    data.originalColorHex = "#" + ColorUtility.ToHtmlStringRGBA(colorMinus.OriginalColor);
                }
                else
                {
                    data.isColorAbsorbed = false;
                    data.originalColorHex = data.color;
                }

                scannedTiles.Add(data);
            }
        }

        MapData mapData = new MapData { tiles = scannedTiles };
        string folderPath = Path.Combine(Application.dataPath, "Resources/Maps");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, mapEditor.mapName + ".json");
        string jsonString = JsonUtility.ToJson(mapData, true);
        File.WriteAllText(filePath, jsonString);
        AssetDatabase.Refresh(); // 유니티 데이터베이스 새로고침

        Debug.Log($"<color=green>[MapEditor]</color> 씬 데이터를 '{mapEditor.mapName}.json' 파일로 성공적으로 구워냈습니다! (경로: {filePath})");
        EditorUtility.DisplayDialog("성공", $"'{mapEditor.mapName}.json' 저장 완료!", "확인");
    }

    // JSON 불러오기 구현 (파일로부터 프리팹을 스냅 위치에 낱개 스폰 배치)
    private void LoadJsonToScene()
    {
        string filePath = Path.Combine(Application.dataPath, "Resources/Maps/" + mapEditor.mapName + ".json");
        if (!File.Exists(filePath))
        {
            EditorUtility.DisplayDialog("오류", $"불러올 JSON 파일을 찾을 수 없습니다: {filePath}", "확인");
            return;
        }

        // 기존 씬 타일 청소
        ClearAllSpawnedTiles();

        string jsonString = File.ReadAllText(filePath);
        MapData mapData = JsonUtility.FromJson<MapData>(jsonString);
        if (mapData == null || mapData.tiles == null) return;

        Transform parent = mapEditor.spawnParent != null ? mapEditor.spawnParent : mapEditor.transform;

        foreach (var data in mapData.tiles)
        {
            if (data.id >= 0 && data.id < mapEditor.tilePrefabs.Count)
            {
                GameObject prefab = mapEditor.tilePrefabs[data.id];
                if (prefab != null)
                {
                    // 스냅 좌표 복원
                    Vector3 position = new Vector3(data.x * mapEditor.gridUnitSize, data.y * mapEditor.gridUnitSize, 0f);

                    GameObject newTile = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                    if (newTile != null)
                    {
                        newTile.transform.position = position;
                        newTile.name = prefab.name;

                        // 색상 복원
                        SpriteRenderer sr = newTile.GetComponentInChildren<SpriteRenderer>();
                        if (sr != null && ColorUtility.TryParseHtmlString(data.color, out Color customColor))
                        {
                            sr.color = customColor;
                        }

                        // ColorMinus 컴포넌트 복원 처리 (리플렉션)
                        ColorMinus colorMinus = newTile.GetComponent<ColorMinus>();
                        if (colorMinus != null)
                        {
                            if (data.isColorAbsorbed)
                            {
                                System.Reflection.FieldInfo field = typeof(ColorMinus).GetField("isAbsorbed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (field != null)
                                {
                                    field.SetValue(colorMinus, true);
                                }
                                if (sr != null)
                                {
                                    sr.color = Color.white;
                                }
                            }
                            else if (ColorUtility.TryParseHtmlString(data.originalColorHex, out Color origColor))
                            {
                                if (sr != null)
                                {
                                    sr.color = origColor;
                                }
                            }
                        }

                        Undo.RegisterCreatedObjectUndo(newTile, "Load Tile");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[MapEditor] 등록되지 않은 타일 프리팹 ID: {data.id}");
            }
        }

        Debug.Log($"<color=green>[MapEditor]</color> '{mapEditor.mapName}.json' 파일로부터 씬에 낱개 오브젝트 복원 배치 완료!");
        EditorUtility.DisplayDialog("성공", $"'{mapEditor.mapName}.json' 불러오기 완료!", "확인");
    }

    // 복사본 숫자, 공백, 언더바, _0 접미사 등을 도려내는 Fuzzy Match 이름 정제 함수 (TileManager와 동일하게 유지)
    private string CleanTileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";

        // 괄호 복사본 접미사 정리: "tile_brick_0 (1)" -> "tile_brick_0"
        int index = name.IndexOf(" (");
        if (index > 0)
        {
            name = name.Substring(0, index);
        }

        // 소문자 변환 후 불필요한 노이즈 도려냄
        return name.ToLower()
                   .Replace("_0", "")
                   .Replace(" ", "")
                   .Replace("_", "")
                   .Replace("1", "")
                   .Replace("2", "")
                   .Replace("3", "")
                   .Replace("4", "")
                   .Replace("5", "")
                   .Replace("6", "")
                   .Replace("7", "")
                   .Replace("8", "")
                   .Replace("9", "");
    }
}
#endif
