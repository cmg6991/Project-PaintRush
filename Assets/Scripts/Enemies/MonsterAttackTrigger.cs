using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MonsterAttackTrigger : MonoBehaviour
{
    // 실제 공격 시점과 쿨타임은 MonsterAI가 관리한다.
    // 이 컴포넌트는 공격 범위 안에 있는 플레이어의 "본체 Collider"만 추적한다.
    //
    // 플레이어 자식의 MagnetSensor처럼 큰 Trigger Collider까지 플레이어로
    // 인식하면 멀리 떨어져 있어도 공격하는 문제가 생기므로 Trigger Collider는 제외한다.

    private readonly Dictionary<Collider2D, PlayerHealth> targetsByCollider =
        new Dictionary<Collider2D, PlayerHealth>();

    private Collider2D attackCollider;

    public bool HasTarget
    {
        get
        {
            return TryGetTarget(out _);
        }
    }

    private void Awake()
    {
        attackCollider = GetComponent<Collider2D>();

        if (!attackCollider.isTrigger)
        {
            Debug.LogWarning(
                $"{gameObject.name}: 공격 범위 Collider2D의 Is Trigger가 꺼져 있습니다."
            );
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TrackTarget(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TrackTarget(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        targetsByCollider.Remove(other);
    }

    public bool TryGetTarget(out PlayerHealth target)
    {
        target = null;

        if (attackCollider == null ||
            !attackCollider.enabled ||
            !gameObject.activeInHierarchy)
        {
            targetsByCollider.Clear();
            return false;
        }

        if (targetsByCollider.Count == 0)
        {
            return false;
        }

        List<Collider2D> invalidColliders = null;

        foreach (KeyValuePair<Collider2D, PlayerHealth> pair
                 in targetsByCollider)
        {
            Collider2D targetCollider = pair.Key;
            PlayerHealth playerHealth = pair.Value;

            bool isInvalid =
                targetCollider == null ||
                playerHealth == null ||
                !targetCollider.enabled ||
                !targetCollider.gameObject.activeInHierarchy ||
                targetCollider.isTrigger ||
                !IsActuallyOverlapping(targetCollider);

            if (isInvalid)
            {
                invalidColliders ??= new List<Collider2D>();
                invalidColliders.Add(targetCollider);
                continue;
            }

            target = playerHealth;
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

    private void TrackTarget(Collider2D other)
    {
        // MagnetSensor, GroundCheck 등 플레이어 자식의 센서용 Trigger는 무시한다.
        if (other == null || other.isTrigger)
        {
            return;
        }

        PlayerHealth playerHealth = FindPlayerHealth(other);

        if (playerHealth == null)
        {
            return;
        }

        targetsByCollider[other] = playerHealth;
    }

    private bool IsActuallyOverlapping(Collider2D other)
    {
        if (other == null || attackCollider == null)
        {
            return false;
        }

        ColliderDistance2D distance =
            Physics2D.Distance(
                attackCollider,
                other
            );

        return distance.isOverlapped;
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
