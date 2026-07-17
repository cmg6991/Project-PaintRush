using System.Collections.Generic;
using UnityEngine;

public class MapEditor : MonoBehaviour
{
    [Header("--- 맵 에디터 설정 ---")]
    public string mapName = "Stage1";
    public float gridUnitSize = 1.28f;
    
    [Tooltip("비워두면 이 오브젝트의 자식으로 자동 지정")]
    public Transform spawnParent;

    [Header("--- 브러쉬용 타일 프리팹 리스트 ---")]
    public List<GameObject> tilePrefabs = new List<GameObject>();

    // 에디터 스크립트(MapEditorCustom)와 연동할 내부 상태값들
    [HideInInspector] public int selectedPrefabIndex = 0;
    [HideInInspector] public bool isPaintMode = false;
    [HideInInspector] public bool isEraserMode = false;

    private void Awake()
    {
        // 게임 런타임(플레이 모드) 시작 시 맵 에디터 컴포넌트는 아무런 일도 하지 않음
        // (충돌을 차단하기 위함)
        this.enabled = false;
    }
}
