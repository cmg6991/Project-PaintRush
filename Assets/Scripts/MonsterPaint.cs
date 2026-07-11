using UnityEngine;

public class MonsterPaint : MonoBehaviour, IPaintable
{
    [SerializeField] private int damage = 10;

    private MonsterAI monster;

    void Awake()
    {
        monster = GetComponent<MonsterAI>();
    }

    public void Paint(Color color, Vector2 hitpont)
    {
        monster.TakeDamage(
            damage,
            color,       // Shoot에서 넘어온 총의 현재 색깔
            gameObject,
            false
        );
    }
}
