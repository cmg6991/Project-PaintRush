using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int damage, ElementType attackElement, GameObject attacker, bool ignoreElement);
}