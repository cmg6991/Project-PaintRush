using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공격 범위 안에 들어온 플레이어의 실제 몸체 Collider를 추적합니다.
/// </summary>
public class MonsterAttackTrigger : MonoBehaviour
{
    [SerializeField] private bool showDebugLogs;

    private readonly Dictionary<Collider2D, PlayerHealth> targets = new();

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
        if (other != null)
        {
            targets.Remove(other);
        }
    }

    public bool TryGetTarget(out PlayerHealth target)
    {
        RemoveInvalidTargets();

        foreach (PlayerHealth playerHealth in targets.Values)
        {
            if (playerHealth != null)
            {
                target = playerHealth;
                return true;
            }
        }

        target = null;
        return false;
    }

    private void TryRegisterTarget(Collider2D other)
    {
        if (other == null || other.isTrigger)
        {
            return;
        }

        Transform playerRoot = FindTaggedParent(
            other.transform,
            "Player");

        if (playerRoot == null)
        {
            return;
        }

        PlayerHealth playerHealth =
            playerRoot.GetComponent<PlayerHealth>() ??
            playerRoot.GetComponentInChildren<PlayerHealth>(true);

        if (playerHealth == null)
        {
            Debug.LogWarning(
                $"[공격범위] {playerRoot.name}에서 " +
                "PlayerHealth를 찾지 못했습니다.");

            return;
        }

        bool isNewTarget = !targets.ContainsKey(other);
        targets[other] = playerHealth;

        if (showDebugLogs && isNewTarget)
        {
            Debug.Log(
                $"[공격범위] 플레이어 등록: {other.name}");
        }
    }

    private static Transform FindTaggedParent(
        Transform start,
        string tagName)
    {
        for (Transform current = start;
             current != null;
             current = current.parent)
        {
            if (current.CompareTag(tagName))
            {
                return current;
            }
        }

        return null;
    }

    private void RemoveInvalidTargets()
    {
        if (targets.Count == 0)
        {
            return;
        }

        List<Collider2D> invalidColliders = new();

        foreach (
            KeyValuePair<Collider2D, PlayerHealth> pair
            in targets)
        {
            Collider2D targetCollider = pair.Key;
            PlayerHealth targetHealth = pair.Value;

            if (targetCollider == null ||
                targetHealth == null ||
                !targetCollider.enabled ||
                !targetCollider.gameObject.activeInHierarchy)
            {
                invalidColliders.Add(targetCollider);
            }
        }

        foreach (Collider2D invalidCollider in invalidColliders)
        {
            targets.Remove(invalidCollider);
        }
    }

    private void OnDisable()
    {
        targets.Clear();
    }
}
