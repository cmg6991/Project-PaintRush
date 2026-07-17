using UnityEngine;

/// <summary>
/// 몬스터가 처음 밟은 타일의 속성을 전달합니다.
/// 자식 Collider가 닿아도 부모의 MonsterAI를 찾습니다.
/// </summary>
public class ElementTile : MonoBehaviour
{
    [SerializeField] private ElementType tileElement = ElementType.None;

    private void OnTriggerEnter2D(Collider2D other)
    {
        MonsterAI monster =
            other.GetComponentInParent<MonsterAI>();

        if (monster != null)
        {
            monster.ChangeElement(tileElement);
        }
    }
}
