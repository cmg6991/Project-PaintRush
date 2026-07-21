using UnityEngine;

// 상자 프리팹에 부착할 스크립트
public class ItemBox : MonoBehaviour, IDamageable
{
    public ElementType boxElement = ElementType.None;

    public void TakeDamage(int damage, Color attackColor, GameObject attacker, bool ignoreElement)
    {
        // TODO: 나중에 여기에 카탈로그 물감 드롭 로직을 추가할 예정
        Debug.Log($"[{gameObject.name}] 상자 파괴!");
        Destroy(gameObject);
    }
}