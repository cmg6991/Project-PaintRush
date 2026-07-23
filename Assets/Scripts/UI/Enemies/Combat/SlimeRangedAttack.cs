using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 슬라임 전용 원거리 공격입니다.
/// 몸을 웅크리는 준비 시간 뒤 원래 자세로 돌아오기 시작하는 순간
/// 포물선 점액 투사체를 발사합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SlimeRangedAttack : MonoBehaviour
{
    [Header("투사체")]
    [SerializeField] private SlimeProjectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField, Min(0.1f)] private float projectileTravelTime = 0.75f;
    [SerializeField, Min(0f)] private float targetLeadTime = 0.12f;

    [Header("공격 리듬")]
    [SerializeField, Min(0f)] private float crouchTime = 0.35f;
    [SerializeField, Min(0f)] private float recoverTime = 0.2f;

    private Coroutine attackRoutine;

    public bool IsAttacking => attackRoutine != null;
    public bool IsConfigured => projectilePrefab != null;

    public bool TryStartAttack(
        Transform target,
        int damage,
        Action onComplete = null)
    {
        if (target == null ||
            damage <= 0 ||
            projectilePrefab == null ||
            IsAttacking)
        {
            return false;
        }

        attackRoutine = StartCoroutine(
            AttackRoutine(target, damage, onComplete));

        return true;
    }

    public void CancelAttack()
    {
        if (attackRoutine == null)
            return;

        StopCoroutine(attackRoutine);
        attackRoutine = null;
    }

    private IEnumerator AttackRoutine(
        Transform target,
        int damage,
        Action onComplete)
    {
        if (crouchTime > 0f)
            yield return new WaitForSeconds(crouchTime);

        if (target != null)
            FireProjectile(target, damage);

        if (recoverTime > 0f)
            yield return new WaitForSeconds(recoverTime);

        attackRoutine = null;
        onComplete?.Invoke();
    }

    private void FireProjectile(Transform target, int damage)
    {
        Vector3 spawnPosition = firePoint != null
            ? firePoint.position
            : transform.position;

        Vector2 targetPosition = target.position;
        Rigidbody2D targetBody = target.GetComponentInParent<Rigidbody2D>();

        if (targetBody != null)
            targetPosition += targetBody.linearVelocity * targetLeadTime;

        SlimeProjectile projectile = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.identity);

        projectile.Initialize(
            targetPosition,
            damage,
            gameObject,
            projectileTravelTime);
    }

    private void OnDisable()
    {
        CancelAttack();
    }
}
