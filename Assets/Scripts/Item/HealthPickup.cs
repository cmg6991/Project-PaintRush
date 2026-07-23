using UnityEngine;

/// <summary>
/// 색이 없는 몬스터가 떨어뜨리는 체력 회복 아이템입니다.
/// 플레이어가 체력을 잃은 상태일 때만 끌려오며,
/// 실제 회복에 성공한 경우에만 소모됩니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public sealed class HealthPickup : MonoBehaviour
{
    [Header("회복")]
    [SerializeField, Min(1)] private int healAmount = 1;

    [Header("바닥 물리")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField, Min(0f)] private float fallingGravityScale = 1f;
    [SerializeField, Min(0f)] private float landingLift = 0.04f;

    [Header("둥둥 연출")]
    [SerializeField, Min(0f)] private float floatSpeed = 3f;
    [SerializeField, Min(0f)] private float floatAmount = 0.12f;

    [Header("자석")]
    [SerializeField, Min(0.1f)] private float magnetRange = 2.5f;
    [SerializeField, Min(0.01f)] private float collectDistance = 0.35f;
    [SerializeField, Min(0f)] private float initialPullSpeed = 2f;
    [SerializeField, Min(0f)] private float maximumPullSpeed = 12f;
    [SerializeField, Min(0f)] private float pullAcceleration = 18f;

    [Header("획득 연출")]
    [SerializeField] private GameObject collectEffectPrefab;
    [SerializeField, Min(0.1f)] private float collectEffectLifetime = 1f;

    private Rigidbody2D rb;
    private Collider2D itemCollider;
    private PlayerHealth playerHealth;
    private Collider2D playerBodyCollider;

    private bool isGrounded;
    private bool isBeingPulled;
    private bool isCollected;
    private float pullSpeed;
    private Vector2 hoverBasePosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        itemCollider = GetComponent<Collider2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallingGravityScale;
        rb.freezeRotation = true;
        rb.linearVelocity = Vector2.zero;

        if (itemCollider.isTrigger)
        {
            Debug.LogWarning(
                $"{name}: HealthPickup Collider2D의 Is Trigger를 해제해야 " +
                "바닥에 정상적으로 착지합니다.");
        }
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (isCollected)
            return;

        if (playerHealth == null)
            FindPlayer();

        if (!isBeingPulled && CanPlayerCollect())
        {
            float distance = Vector2.Distance(
                rb.position,
                GetPlayerTargetPosition());

            if (distance <= magnetRange)
                BeginPull();
        }
    }

    private void FixedUpdate()
    {
        if (isCollected)
            return;

        if (isBeingPulled)
        {
            UpdatePull();
            return;
        }

        if (isGrounded)
        {
            float wave = (Mathf.Sin(Time.time * floatSpeed) + 1f) * 0.5f;
            rb.MovePosition(
                hoverBasePosition + Vector2.up * (wave * floatAmount));
        }
    }

    private bool CanPlayerCollect()
    {
        return playerHealth != null && playerHealth.CanHeal;
    }

    private void BeginPull()
    {
        isBeingPulled = true;
        isGrounded = false;
        pullSpeed = initialPullSpeed;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    private void UpdatePull()
    {
        if (!CanPlayerCollect())
        {
            CancelPull();
            return;
        }

        pullSpeed = Mathf.Min(
            maximumPullSpeed,
            pullSpeed + pullAcceleration * Time.fixedDeltaTime);

        Vector2 target = GetPlayerTargetPosition();
        rb.MovePosition(Vector2.MoveTowards(
            rb.position,
            target,
            pullSpeed * Time.fixedDeltaTime));

        if (Vector2.Distance(rb.position, target) <= collectDistance)
            TryCollect();
    }

    private void TryCollect()
    {
        if (isCollected || playerHealth == null)
            return;

        // 같은 FixedUpdate에 플레이어와 접촉해 밀어내는 것을 먼저 차단합니다.
        if (itemCollider != null)
            itemCollider.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        int healed = playerHealth.Heal(healAmount);
        if (healed <= 0)
        {
            if (rb != null)
                rb.simulated = true;

            if (itemCollider != null)
                itemCollider.enabled = true;

            CancelPull();
            return;
        }

        isCollected = true;

        if (collectEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                collectEffectPrefab,
                transform.position,
                Quaternion.identity);
            Destroy(effect, collectEffectLifetime);
        }

        Destroy(gameObject);
    }

    private void CancelPull()
    {
        isBeingPulled = false;
        pullSpeed = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallingGravityScale;
        rb.linearVelocity = Vector2.zero;
        isGrounded = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryLand(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryLand(collision);
    }

    private void TryLand(Collision2D collision)
    {
        if (isGrounded || isBeingPulled || isCollected ||
            (groundLayer.value & (1 << collision.gameObject.layer)) == 0)
        {
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y <= 0.35f)
                continue;

            isGrounded = true;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            hoverBasePosition = rb.position + Vector2.up * landingLift;
            return;
        }
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        playerHealth = player.GetComponent<PlayerHealth>() ??
                       player.GetComponentInParent<PlayerHealth>() ??
                       player.GetComponentInChildren<PlayerHealth>(true);

        Collider2D[] playerColliders =
            player.GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D candidate in playerColliders)
        {
            if (candidate == null)
                continue;

            // 회복 아이템은 플레이어를 물리적으로 밀면 안 됩니다.
            // 바닥 충돌은 유지하고 플레이어 Collider와의 충돌만 무시합니다.
            if (itemCollider != null)
                Physics2D.IgnoreCollision(itemCollider, candidate, true);

            if (playerBodyCollider == null &&
                candidate.enabled &&
                !candidate.isTrigger)
            {
                playerBodyCollider = candidate;
            }
        }
    }

    private Vector2 GetPlayerTargetPosition()
    {
        if (playerBodyCollider != null && playerBodyCollider.enabled)
            return playerBodyCollider.bounds.center;

        return playerHealth != null
            ? (Vector2)playerHealth.transform.position
            : rb.position;
    }

    private void OnValidate()
    {
        healAmount = Mathf.Max(1, healAmount);
        collectDistance = Mathf.Min(collectDistance, magnetRange);
        maximumPullSpeed = Mathf.Max(maximumPullSpeed, initialPullSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, magnetRange);
    }
}
