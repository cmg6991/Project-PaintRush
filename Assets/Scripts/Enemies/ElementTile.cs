using UnityEngine;

public class ElementTile : MonoBehaviour {
    public ElementType tileElement = ElementType.None;

    private void OnTriggerEnter2D(Collider2D collision) {
        MonsterAI monster = collision.GetComponent<MonsterAI>();

        if (monster != null) {
            monster.ChangeElement(tileElement);
        }
    }
}