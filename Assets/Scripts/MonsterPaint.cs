using UnityEngine;

[RequireComponent(typeof(MonsterAI))]
public class MonsterPaint : MonoBehaviour, IPaintable
{
    [SerializeField, Min(1)] private int damage = 10;

    private MonsterAI monster;

    private void Awake()
    {
        monster = GetComponent<MonsterAI>();
    }

    public void Paint(Color color, Vector2 hitPoint)
    {
        if (monster == null || monster.IsDead)
        {
            return;
        }

        monster.TakeDamage(
            damage,
            color,
            gameObject,
            false
        );
    }
}
