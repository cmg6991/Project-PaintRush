using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int damage, Color attackColor, GameObject attacker, bool ignoreElement);
}