using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAttackTrigger : MonoBehaviour
{
    [Header("접촉 데미지")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageInterval = 1f;

    private readonly HashSet<PlayerHealth> playersInRange = new();
    private Coroutine damageCoroutine;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth playerHealth = FindPlayerHealth(other);

        if (playerHealth == null)
        {
            return;
        }

        playersInRange.Add(playerHealth);

        if (damageCoroutine == null)
        {
            damageCoroutine = StartCoroutine(DamageRoutine());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerHealth playerHealth = FindPlayerHealth(other);

        if (playerHealth == null)
        {
            return;
        }

        playersInRange.Remove(playerHealth);

        if (playersInRange.Count == 0 && damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
    }

    private IEnumerator DamageRoutine()
    {
        while (playersInRange.Count > 0)
        {
            // 반복 중 컬렉션이 바뀌는 문제를 피하기 위해 복사본 사용
            PlayerHealth[] targets = new PlayerHealth[playersInRange.Count];
            playersInRange.CopyTo(targets);

            foreach (PlayerHealth target in targets)
            {
                if (target == null)
                {
                    playersInRange.Remove(target);
                    continue;
                }

                target.TakeDamage(
                    damage,
                    ElementType.None,
                    transform.root.gameObject,
                    true
                );

                Debug.Log($"몬스터 접촉 데미지 적용: {damage}");
            }

            yield return new WaitForSeconds(damageInterval);
        }

        damageCoroutine = null;
    }

    private PlayerHealth FindPlayerHealth(Collider2D other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            playerHealth = other.GetComponentInParent<PlayerHealth>();
        }

        return playerHealth;
    }

    private void OnDisable()
    {
        playersInRange.Clear();

        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
    }
}