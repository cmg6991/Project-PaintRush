using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHp = 5;

    private int currentHp;
    private bool isDead;

    private void Awake()
    {
        currentHp = maxHp;
        isDead = false;
    }

    public void TakeDamage(
        int damage,
        ElementType attackElement,
        GameObject attacker,
        bool ignoreElement)
    {
        if (isDead) return;

        currentHp -= damage;

        Debug.Log($"플레이어 피격! HP : {currentHp}");

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("플레이어 사망");
    }
}