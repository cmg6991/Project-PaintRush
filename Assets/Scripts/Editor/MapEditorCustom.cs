#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapEditor))]
public class MapEditorCustom : Editor
{
    private MapEditor mapEditor;

    private void OnEnable()
    {
        mapEditor = (MapEditor)target;
        SceneView.duringSceneGui += CustomOnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= CustomOnSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(15);
        GUILayout.Label("--- 스프라이트 맵 에디터 컨트롤 ---", EditorStyles.boldLabel);

        // 페인트 모드 토글
        Color oldColor = GUI.backgroundColor;
        GUI.backgroundColor = mapEditor.isPaintMode ? Color.green : Color.grey;
        if (GUILayout.Button(mapEditor.isPaintMode ? " 배치 모드 가동 중 (클릭 시 종료)" : " 배치 모드 켜기 (씬 뷰 드로잉)", GUILayout.Height(35)))
        {
            mapEditor.isPaintMode = !mapEditor.isPaintMode;
            if (mapEditor.isPaintMode) mapEditor.isEraserMode = false;
        }

        // 지우개 모드 토글
        GUI.backgroundColor = mapEditor.isEraserMode ? Color.red : Color.grey;
        if (GUILayout.Button(mapEditor.isEraserMode ? " 지우개 모드 가동 중 (클릭 시 종료)" : " 지우개 모드 켜기 (Shift + 좌클릭)", GUILayout.Height(30)))
        {
            mapEditor.isEraserMode = !mapEditor.isEraserMode;
            if (mapEditor.isEraserMode) mapEditor.isPaintMode = false;
        }
        GUI.backgroundColor = oldColor;

        // 프리팹 선택 그리드
        if (mapEditor.blockPrefabs != null && mapEditor.blockPrefabs.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label("배치할 오브젝트 프리팹 선택:", EditorStyles.boldLabel);

            string[] names = new string[mapEditor.blockPrefabs.Count];
            for (int i = 0; i < mapEditor.blockPrefabs.Count; i++)
            {
                names[i] = mapEditor.blockPrefabs[i] != null ? mapEditor.blockPrefabs[i].name : "Empty";
            }

            mapEditor.selectedPrefabIndex = GUILayout.SelectionGrid(mapEditor.selectedPrefabIndex, names, 2);
        }
        else
        {
            EditorGUILayout.HelpBox("Block Prefabs 리스트에 배치할 오브젝트 프리팹들을 채워 넣어 주세요!", MessageType.Info);
        }

        GUILayout.Space(15);
        GUILayout.Label("세이브 / 로드 (JSON 연동)", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("씬 데이터 세이브 (JSON 굽기)", GUILayout.Height(40)))
        {
            SaveSceneToJson();
        }
        if (GUILayout.Button("JSON 맵 불러오기 (씬 복원)", GUILayout.Height(40)))
        {
            LoadJsonToEditorScene();
        }
        GUILayout.EndHorizontal();

        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
        if (GUILayout.Button("씬 내 모든 블록 청소하기", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("경고", "배치된 모든 블록 오브젝트가 삭제됩니다. 진행하시겠습니까?", "예", "아니오"))
            {
                ClearAllBlocks();
            }
        }
        GUI.backgroundColor = oldColor;
    }

    private void CustomOnSceneGUI(SceneView sceneView)
    {
        if (mapEditor == null) return;
        if (!mapEditor.isPaintMode && !mapEditor.isEraserMode) return;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Vector3 mouseWorldPos = ray.origin;
        mouseWorldPos.z = 0f;

        // 그리드 스냅 계산
        float snapX = Mathf.Round(mouseWorldPos.x / mapEditor.gridUnitSize) * mapEditor.gridUnitSize;
        float snapY = Mathf.Round(mouseWorldPos.y / mapEditor.gridUnitSize) * mapEditor.gridUnitSize;
        Vector2 snapPos = new Vector2(snapX, snapY);

        // 고스트 프리뷰
        Color previewColor = mapEditor.isEraserMode || e.shift ? new Color(1f, 0f, 0f, 0.4f) : new Color(0f, 1f, 0f, 0.4f);
        Handles.color = previewColor;
        Handles.DrawSolidRectangleWithOutline(new Rect(snapX - mapEditor.gridUnitSize / 2f, snapY - mapEditor.gridUnitSize / 2f, mapEditor.gridUnitSize, mapEditor.gridUnitSize), previewColor, Color.white);
        sceneView.Repaint();

        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
        {
            if (mapEditor.isEraserMode || e.shift)
            {
                DeleteObjectAtPosition(snapPos);
            }
            else if (mapEditor.isPaintMode)
            {
                SpawnObjectAtPosition(snapPos);
            }
            e.Use();
        }
    }

    private void SpawnObjectAtPosition(Vector2 position)
    {
        if (mapEditor.blockPrefabs == null || mapEditor.blockPrefabs.Count == 0) return;
        GameObject prefab = mapEditor.blockPrefabs[mapEditor.selectedPrefabIndex];
        if (prefab == null) return;

        // 이미 해당 위치에 블록이 있다면 중복 생성 방지
        if (FindObjectAtPosition(position) != null) return;

        Transform parent = GetOrCreateParent();

        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        obj.transform.position = new Vector3(position.x, position.y, 0f);

        Undo.RegisterCreatedObjectUndo(obj, "Spawn Block");
        EditorUtility.SetDirty(obj);
    }

    private void DeleteObjectAtPosition(Vector2 position)
    {
        GameObject target = FindObjectAtPosition(position);
        if (target != null)
        {
            Undo.DestroyObjectImmediate(target);
        }
    }

    private GameObject FindObjectAtPosition(Vector2 position)
    {
        Transform parent = mapEditor.spawnParent;
        if (parent == null)
        {
            GameObject foundParent = GameObject.Find("MapRoot_" + mapEditor.mapName);
            if (foundParent != null) parent = foundParent.transform;
            else return null;
        }

        foreach (Transform child in parent)
        {
            if (Vector2.Distance(child.position, position) < 0.1f)
            {
                return child.gameObject;
            }
        }
        return null;
    }

    private Transform GetOrCreateParent()
    {
        if (mapEditor.spawnParent != null) return mapEditor.spawnParent;

        string parentName = "MapRoot_" + mapEditor.mapName;
        GameObject parentObj = GameObject.Find(parentName);
        if (parentObj == null)
        {
            parentObj = new GameObject(parentName);
        }
        mapEditor.spawnParent = parentObj.transform;
        return mapEditor.spawnParent;
    }

    private void ClearAllBlocks()
    {
        Transform parent = mapEditor.spawnParent;
        if (parent == null)
        {
            GameObject foundParent = GameObject.Find("MapRoot_" + mapEditor.mapName);
            if (foundParent != null) parent = foundParent.transform;
        }

        if (parent != null)
        {
            Undo.RegisterCreatedObjectUndo(parent.gameObject, "Clear All Blocks");
            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in parent) children.Add(child.gameObject);
            foreach (var child in children) Undo.DestroyObjectImmediate(child);
        }
        Debug.Log("[MapEditor] 모든 블록이 청소되었습니다.");
    }

    private void SaveSceneToJson()
    {
        Transform parent = mapEditor.spawnParent;
        if (parent == null)
        {
            GameObject foundParent = GameObject.Find("MapRoot_" + mapEditor.mapName);
            if (foundParent != null) parent = foundParent.transform;
        }

        if (parent == null)
        {
            Debug.LogWarning("[MapEditor] 저장할 맵 오브젝트 부모가 없습니다.");
            return;
        }

        List<TileData> scannedTiles = new List<TileData>();

        foreach (Transform child in parent)
        {
            TileData data = new TileData();
            data.name = child.name.Replace("(Clone)", "").Trim();
            data.x = child.position.x;
            data.y = child.position.y;
            data.scaleX = child.localScale.x; // 크기 저장 추가
            data.scaleY = child.localScale.y;
            data.rotation = child.eulerAngles.z; // 회전 저장 추가

            SpriteRenderer sr = child.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                data.color = "#" + ColorUtility.ToHtmlStringRGBA(sr.color);
            }
            else
            {
                data.color = "#FFFFFF";
            }

            scannedTiles.Add(data);
        }

        MapData mapData = new MapData { tiles = scannedTiles };
        string json = JsonUtility.ToJson(mapData, true);

        string dirPath = Path.Combine(Application.dataPath, "Resources/Maps");
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

        string filePath = Path.Combine(dirPath, mapEditor.mapName + ".json");
        File.WriteAllText(filePath, json);

        AssetDatabase.Refresh();
        Debug.Log($"[MapEditor] 스프라이트 블록 {scannedTiles.Count}개 JSON 저장 완료: {filePath}");
    }

    public void LoadJsonToEditorScene()
    {
        MapEditor editor = (MapEditor)target;
        if (editor == null) return;

        // 1. 기존에 배치된 맵 부모 오브젝트가 있다면 먼저 청소
        Transform parent = editor.spawnParent;
        if (parent == null)
        {
            GameObject parentObj = GameObject.Find("MapRoot_" + editor.mapName);
            if (parentObj != null) parent = parentObj.transform;
        }

        if (parent != null)
        {
            // 에디터 환경에서는 DestroyImmediate를 써야 씬에서 즉시 삭제됩니다.
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }
        else
        {
            GameObject newParent = new GameObject("MapRoot_" + editor.mapName);
            parent = newParent.transform;
            editor.spawnParent = parent;
        }

        // 2. JSON 파일 경로 탐색 및 로드
        string filePath = Path.Combine(Application.dataPath, "Resources/Maps", editor.mapName + ".json");
        if (!File.Exists(filePath))
        {
            // PersistentDataPath도 확인
            filePath = Path.Combine(Application.persistentDataPath, "Maps/" + editor.mapName + ".json");
        }

        if (!File.Exists(filePath))
        {
            Debug.LogError($"[MapEditor] 불러올 JSON 파일을 찾을 수 없습니다: {editor.mapName}");
            return;
        }

        string jsonText = File.ReadAllText(filePath);
        MapData mapData = JsonUtility.FromJson<MapData>(jsonText);

        if (mapData == null || mapData.tiles == null)
        {
            Debug.LogError("[MapEditor] 맵 데이터가 비어있거나 형식이 잘못되었습니다.");
            return;
        }

        // 3. 에디터 상에 타일 오브젝트들을 낱개로 생성 (에디터 전용이므로 Instantiate 대신 UnityEditor.PrefabUtility 등을 쓰거나 기본 Instantiate 활용 가능)
        foreach (var data in mapData.tiles)
        {
            GameObject prefab = GetEditorPrefabByName(editor, data.name);
            if (prefab == null) continue;

            Vector3 spawnPos = new Vector3(data.x, data.y, 0f);

            // 에디터에서 즉시 생성되도록 Instantiate 사용
            GameObject instance = Instantiate(prefab, spawnPos, Quaternion.Euler(0, 0, data.rotation), parent);
            instance.transform.localScale = new Vector3(data.scaleX != 0 ? data.scaleX : 1f, data.scaleY != 0 ? data.scaleY : 1f, 1f);
            instance.name = data.name;
        }

        Debug.Log($"[MapEditor] 에디터 화면에 '{editor.mapName}' 불러오기 완료! (총 {mapData.tiles.Count}개 타일)");
    }

    // 에디터용 프리팹 검색 헬퍼
    private GameObject GetEditorPrefabByName(MapEditor editor, string name)
    {
        string cleanInput = name.ToLower().Trim();
        int index = cleanInput.IndexOf(" (");
        if (index > 0) cleanInput = cleanInput.Substring(0, index);

        foreach (var prefab in editor.blockPrefabs)
        {
            if (prefab != null)
            {
                string prefabClean = prefab.name.ToLower().Trim();
                int pIndex = prefabClean.IndexOf(" (");
                if (pIndex > 0) prefabClean = prefabClean.Substring(0, pIndex);

                if (prefabClean == cleanInput) return prefab;
            }
        }
        return null;
    }
}
#endif