using System.Collections.Generic;
using UnityEngine;

public class MonsterAttackTrigger : MonoBehaviour
{
    // 실제 공격 시점과 쿨타임은 MonsterAI가 관리한다.
    // 이 컴포넌트는 공격 범위 안의 플레이어만 추적한다.
    private readonly Dictionary<Collider2D, PlayerHealth> targetsByCollider =
        new Dictionary<Collider2D, PlayerHealth>();

    public bool HasTarget
    {
        get
        {
            return TryGetTarget(out _);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth playerHealth = FindPlayerHealth(other);

        if (playerHealth == null)
        {
            return;
        }

        targetsByCollider[other] = playerHealth;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        targetsByCollider.Remove(other);
    }

    public bool TryGetTarget(out PlayerHealth target)
    {
        target = null;

        if (targetsByCollider.Count == 0)
        {
            return false;
        }

        List<Collider2D> invalidColliders = null;

        foreach (KeyValuePair<Collider2D, PlayerHealth> pair
                 in targetsByCollider)
        {
            if (pair.Key == null || pair.Value == null)
            {
                invalidColliders ??= new List<Collider2D>();
                invalidColliders.Add(pair.Key);
                continue;
            }

            target = pair.Value;
            break;
        }

        if (invalidColliders != null)
        {
            foreach (Collider2D invalidCollider in invalidColliders)
            {
                targetsByCollider.Remove(invalidCollider);
            }
        }

        return target != null;
    }

    private static PlayerHealth FindPlayerHealth(Collider2D other)
    {
        PlayerHealth playerHealth =
            other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            playerHealth =
                other.GetComponentInParent<PlayerHealth>();
        }

        return playerHealth;
    }

    private void OnDisable()
    {
        targetsByCollider.Clear();
    }
}
