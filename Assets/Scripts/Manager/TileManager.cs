using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    [Header("--- 맵 생성 설정 ---")]
    public string targetMapName = "Stage1";
    public float gridUnitSize = 1.28f;
    public Transform mapParent;

    [Header("--- 에셋 및 카탈로그 ---")]
    public List<GameObject> blockPrefabs = new List<GameObject>(); // 에디터와 공유할 프리팹 리스트
    public PaintColorCatalog paintColorCatalog;

    [Header("--- 동적 블록 랜덤 밸런스 설정 (%) ---")]
    [Range(0f, 100f)] public float redPercent = 33f;
    [Range(0f, 100f)] public float bluePercent = 33f;
    [Range(0f, 100f)] public float yellowPercent = 34f;

    // 색상 흡수 기믹이 적용될 블록들을 모아둘 리스트
    private List<ColorMinus> interactiveBlockList = new List<ColorMinus>();

    void Start()
    {
        LoadMap(targetMapName);
    }

    // JSON을 읽어와서 스프라이트 오브젝트들을 낱개로 싹 세팅하는 핵심 함수
    public void LoadMap(string fileName)
    {
        // 1. 기존에 생성된 맵 부모가 있다면 청소
        Transform parent = GetOrCreateParent();
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }

        interactiveBlockList.Clear();

        // 2. JSON 데이터 로드
        MapData mapData = LoadMapData("Maps/" + fileName);
        if (mapData == null || mapData.tiles == null)
        {
            Debug.LogError($"[TileManager] 맵 데이터를 찾을 수 없습니다: {fileName}");
            return;
        }

        // 3. 데이터 기반 오브젝트 개별 생성
        foreach (var data in mapData.tiles)
        {
            GameObject prefab = GetPrefabByName(data.name);
            if (prefab == null) continue;

            Vector3 spawnPos = new Vector3(data.x, data.y, 0f);
            GameObject instance = Instantiate(prefab, spawnPos, Quaternion.Euler(0, 0, data.rotation), parent);
            instance.transform.localScale = new Vector3(data.scaleX != 0 ? data.scaleX : 1f, data.scaleY != 0 ? data.scaleY : 1f, 1f);
            instance.name = data.name;

            string nameLower = data.name.ToLower();

            // 사다리(Ladder), 행거(Grab/Hanger), 함정(Spike/Trap)은 색상 흡수 기믹 예외 처리
            bool isExcluded = nameLower.Contains("ladder") ||
                              nameLower.Contains("grab") ||
                              nameLower.Contains("hanger") ||
                              nameLower.Contains("spike");

            if (!isExcluded)
            {
                // 제외되지 않은 일반 블록 및 상자들에만 색상 흡수 기믹 자동 부여
                ColorMinus cMinus = instance.GetComponent<ColorMinus>();
                if (cMinus == null)
                {
                    cMinus = instance.AddComponent<ColorMinus>();
                }

                if (cMinus != null)
                {
                    interactiveBlockList.Add(cMinus);
                }
            }
        }

        Debug.Log($"[TileManager] 스프라이트 맵 '{fileName}' 로드 완료! (색상 기믹 대상 블록: {interactiveBlockList.Count}개)");

        // 5. 렌더링 안정화 대기 후 무작위 색상 배분 셔플 실행
        StartCoroutine(ColorShuffleRoutine());
    }

    // 프리팹 리스트에서 이름으로 원본 찾기 (정제 포함)
    private GameObject GetPrefabByName(string name)
    {
        string cleanInput = CleanName(name);
        foreach (var prefab in blockPrefabs)
        {
            if (prefab != null && CleanName(prefab.name) == cleanInput)
            {
                return prefab;
            }
        }
        return null;
    }

    private string CleanName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        int index = name.IndexOf(" (");
        if (index > 0) name = name.Substring(0, index);
        return name.ToLower().Replace("_0", "").Replace(" ", "").Replace("_", "").Replace("1", "").Replace("2", "");
    }

    private Transform GetOrCreateParent()
    {
        if (mapParent != null) return mapParent;
        string parentName = "MapRoot_" + targetMapName;
        GameObject parentObj = GameObject.Find(parentName);
        if (parentObj == null) parentObj = new GameObject(parentName);
        mapParent = parentObj.transform;
        return mapParent;
    }

    // 사다리/행거 제외 모든 블록에 설정된 비율에 맞춰 노랑, 빨강, 파랑 무작위 주입
    private IEnumerator ColorShuffleRoutine()
    {
        yield return new WaitForSeconds(0.2f);

        if (interactiveBlockList.Count == 0 || paintColorCatalog == null) yield break;

        // Fisher-Yates 셔플 알고리즘으로 리스트 무작위 섞기
        for (int i = 0; i < interactiveBlockList.Count; i++)
        {
            ColorMinus temp = interactiveBlockList[i];
            int randomIndex = Random.Range(i, interactiveBlockList.Count);
            interactiveBlockList[i] = interactiveBlockList[randomIndex];
            interactiveBlockList[randomIndex] = temp;
        }

        int total = interactiveBlockList.Count;
        int redCount = Mathf.RoundToInt(total * (redPercent / 100f));
        int blueCount = Mathf.RoundToInt(total * (bluePercent / 100f));
        int yellowCount = Mathf.RoundToInt(total * (yellowPercent / 100f));

        paintColorCatalog.TryGetColor(ElementType.Red, out Color redColor);
        paintColorCatalog.TryGetColor(ElementType.Blue, out Color blueColor);
        paintColorCatalog.TryGetColor(ElementType.Yellow, out Color yellowColor);

        for (int i = 0; i < total; i++)
        {
            Color targetColor = Color.white;
            if (i < redCount) targetColor = redColor;
            else if (i < redCount + blueCount) targetColor = blueColor;
            else if (i < redCount + blueCount + yellowCount) targetColor = yellowColor;

            ForceSetBlockColor(interactiveBlockList[i], targetColor);
        }

        Debug.Log($"[TileManager] 블록 색상 무작위 밸런스 배치 완료 (Red:{redCount}, Blue:{blueCount}, Yellow:{yellowCount})");
    }

    // 아트 팀의 ColorMinus 컴포넌트를 건드리지 않고 색상을 강제 주입하는 헬퍼
    private void ForceSetBlockColor(ColorMinus colorMinus, Color newColor)
    {
        var sr = colorMinus.GetComponent<SpriteRenderer>();
        var field = typeof(ColorMinus).GetField("<OriginalColor>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null) field.SetValue(colorMinus, newColor);

        if (sr != null)
        {
            sr.color = newColor;
            if (sr.material != null)
            {
                sr.material.SetColor("_OriginalColor", newColor);
            }
        }
    }

    private MapData LoadMapData(string resourcePath)
    {
        TextAsset mapTextAsset = Resources.Load<TextAsset>(resourcePath);
        if (mapTextAsset != null) return JsonUtility.FromJson<MapData>(mapTextAsset.text);

        string filePath = Path.Combine(Application.dataPath, "Resources", resourcePath + ".json");
        if (File.Exists(filePath)) return JsonUtility.FromJson<MapData>(File.ReadAllText(filePath));

        return new MapData { tiles = new List<TileData>() };
    }
}

[System.Serializable]
public class TileData
{
    public int id;
    public string name;
    public string type;
    public float x;
    public float y;
    public string color;

    // 스프라이트 프리팹 개별 조절을 위해 추가된 변수들
    public float scaleX;
    public float scaleY;
    public float rotation;

    public bool isColorAbsorbed;
    public string originalColorHex;
    public string colorId;
    public bool isRandom;
}

[System.Serializable]
public class MapData
{
    public System.Collections.Generic.List<TileData> tiles;
}