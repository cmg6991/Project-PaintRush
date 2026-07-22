using UnityEngine;
public class TutorialItem : MonoBehaviour
{
    [Header("--- 생성할 물감 아이템 프리팹 ---")]
    public GameObject itemPrefab;
    // 아이템 드롭 (1줄 생성 함수)

    private bool isDropped = false;

    // 유니티 내장 클릭 이벤트: 과녁을 마우스로 클릭하는 순간 즉시 100% 드롭!
    private void OnMouseDown()
    {
        TriggerDrop();
    }
    // 플레이어나 충돌체가 부딪혔을 때도 드롭
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 총에 물감이 충전되어 있다면
        FillColor fillColor = FindAnyObjectByType<FillColor>();
        if (fillColor != null && fillColor.HasColor)
        {
            // 물감이 없으면 과녁이 반응하지 않고 무시됨
            return;
        }
        TriggerDrop();
    }
    private void TriggerDrop()
    {
        if (isDropped) return;
        isDropped = true;
        if (itemPrefab != null)
        {
            Instantiate(itemPrefab, transform.position, Quaternion.identity);
        }
        gameObject.SetActive(false); // 과녁 비활성화
    }
}