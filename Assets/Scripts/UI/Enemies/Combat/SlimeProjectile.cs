using UnityEngine;

/// <summary>
/// 슬라임이 발사하는 포물선 점액 투사체입니다.
/// Rigidbody2D 중력을 이용해 목표 지점까지 도달하는 초기 속도를 계산합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public sealed class SlimeProjectile : MonoBehaviour
{
    [Header("물리")]
    [SerializeField, Min(0f)] private float gravityScale = 1f;
    [SerializeField, Min(0.1f)] private float minimumTravelTime = 0.25f;
    [SerializeField, Min(0.1f)] private float maximumTravelTime = 1.5f;
    [SerializeField, Min(0.1f)] private float lifetime = 3f;

    [Header("충돌")]
    [Tooltip("비워두면 소유자와 자기 자신을 제외한 모든 비 Trigger 충돌체에 닿을 때 사라집니다.")]
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField, Min(0.1f)] private float impactEffectLifetime = 1f;

    private Rigidbody2D rb;
    private Collider2D projectileCollider;
    private GameObject owner;
    private int damage;
    private bool hasImpacted;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        projectileCollider = GetComponent<Collider2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravityScale;
        rb.freezeRotation = true;
    }

    public void Initialize(
        Vector2 targetPosition,
        int attackDamage,
        GameObject attackOwner,
        float requestedTravelTime)
    {
        owner = attackOwner;
        damage = Mathf.Max(1, attackDamage);

        IgnoreOwnerCollisions();

        float travelTime = Mathf.Clamp(
            requestedTravelTime,
            minimumTravelTime,
            maximumTravelTime);

        Vector2 start = rb.position;
        Vector2 displacement = targetPosition - start;
        float gravity = Physics2D.gravity.y * rb.gravityScale;

        float velocityX = displacement.x / travelTime;
        float velocityY =
            (displacement.y - 0.5f * gravity * travelTime * travelTime) /
            travelTime;

        rb.linearVelocity = new Vector2(velocityX, velocityY);
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleImpact(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleImpact(other);
    }

    private void HandleImpact(Collider2D other)
    {
        if (hasImpacted || other == null || IsOwnerCollider(other))
            return;

        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(
                damage,
                Color.white,
                owner,
                true);

            Impact();
            return;
        }

        if (other.isTrigger)
            return;

        if (collisionLayers.value != 0 &&
            (collisionLayers.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        Impact();
    }

    private void Impact()
    {
        if (hasImpacted)
            return;

        hasImpacted = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        projectileCollider.enabled = false;

        if (impactEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                impactEffectPrefab,
                transform.position,
                Quaternion.identity);
            Destroy(effect, impactEffectLifetime);
        }

        Destroy(gameObject);
    }

    private void IgnoreOwnerCollisions()
    {
        if (owner == null || projectileCollider == null)
            return;

        foreach (Collider2D ownerCollider in
                 owner.GetComponentsInChildren<Collider2D>(true))
        {
            if (ownerCollider != null)
            {
                Physics2D.IgnoreCollision(
                    projectileCollider,
                    ownerCollider,
                    true);
            }
        }
    }

    private bool IsOwnerCollider(Collider2D collider)
    {
        return owner != null &&
               (collider.gameObject == owner ||
                collider.transform.IsChildOf(owner.transform));
    }

    private void OnValidate()
    {
        gravityScale = Mathf.Max(0f, gravityScale);
        minimumTravelTime = Mathf.Max(0.1f, minimumTravelTime);
        maximumTravelTime = Mathf.Max(minimumTravelTime, maximumTravelTime);
        lifetime = Mathf.Max(0.1f, lifetime);
    }
}
