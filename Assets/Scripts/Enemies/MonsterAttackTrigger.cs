using System.Collections.Generic;
using UnityEngine;

public class MonsterAttackTrigger : MonoBehaviour
{
    private readonly Dictionary<Collider2D, PlayerHealth> targets =
        new Dictionary<Collider2D, PlayerHealth>();

    public bool HasTarget
    {
        get
        {
            RemoveInvalidTargets();
            return targets.Count > 0;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryRegisterTarget(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryRegisterTarget(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        targets.Remove(other);
    }

    private void TryRegisterTarget(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        // MagnetSensor 같은 플레이어 자식 Trigger는 공격 대상으로 사용하지 않음
        if (other.isTrigger)
        {
            return;
        }

        // Collider에서 부모 방향으로 올라가며 Player 태그 오브젝트를 찾음
        Transform playerRoot = FindPlayerRoot(other.transform);

        if (playerRoot == null)
        {
            return;
        }

        // PlayerHealth가 플레이어 루트나 자식 어디에 있어도 찾음
        PlayerHealth playerHealth =
            playerRoot.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            playerHealth =
                playerRoot.GetComponentInChildren<PlayerHealth>(true);
        }

        if (playerHealth == null)
        {
            Debug.LogWarning(
                $"[공격범위] {playerRoot.name}에서 PlayerHealth를 찾지 못했습니다."
            );

            return;
        }

        if (!targets.ContainsKey(other))
        {
            targets.Add(other, playerHealth);

            Debug.Log(
                $"[공격범위] 플레이어 등록: {other.name}"
            );
        }
        else
        {
            targets[other] = playerHealth;
        }
    }

    public bool TryGetTarget(out PlayerHealth target)
    {
        RemoveInvalidTargets();

        foreach (KeyValuePair<Collider2D, PlayerHealth> pair in targets)
        {
            if (pair.Value == null)
            {
                continue;
            }

            target = pair.Value;
            return true;
        }

        target = null;
        return false;
    }

    private static Transform FindPlayerRoot(Transform start)
    {
        Transform current = start;

        while (current != null)
        {
            if (current.CompareTag("Player"))
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    private void RemoveInvalidTargets()
    {
        if (targets.Count == 0)
        {
            return;
        }

        List<Collider2D> invalidTargets =
            new List<Collider2D>();

        foreach (KeyValuePair<Collider2D, PlayerHealth> pair in targets)
        {
            Collider2D targetCollider = pair.Key;
            PlayerHealth targetHealth = pair.Value;

            if (targetCollider == null ||
                targetHealth == null ||
                !targetCollider.enabled ||
                !targetCollider.gameObject.activeInHierarchy)
            {
                invalidTargets.Add(targetCollider);
            }
        }

        foreach (Collider2D invalidTarget in invalidTargets)
        {
            targets.Remove(invalidTarget);
        }
    }

    private void OnDisable()
    {
        targets.Clear();
    }
}