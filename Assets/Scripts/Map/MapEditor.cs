using System.Collections.Generic;
using UnityEngine;

public class MapEditor : MonoBehaviour
{
    [Header("--- 맵 에디터 설정 ---")]
    public string mapName = "Stage1";
    public float gridUnitSize = 1.28f;

    [Tooltip("배치되는 블록들이 모일 부모 오브젝트 (없으면 자동 생성)")]
    public Transform spawnParent;

    [Header("--- 데이터 카탈로그 ---")]
    public PaintColorCatalog paintColorCatalog;

    [Header("--- 브러쉬용 스프라이트 프리팹 리스트 ---")]
    public List<GameObject> blockPrefabs = new List<GameObject>();

    [HideInInspector] public int selectedPrefabIndex = 0;
    [HideInInspector] public bool isPaintMode = false;
    [HideInInspector] public bool isEraserMode = false;

    private void Awake()
    {
        this.enabled = false;
    }
}