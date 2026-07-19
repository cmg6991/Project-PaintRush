using UnityEngine;

/// <summary>
/// 팔레트 보유 몬스터가 드롭하는 특수 팔레트 아이템입니다.
/// 바닥에 떨어진 뒤 둥둥 떠 있고, 플레이어가 가까워지면 자동 장착됩니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PalettePickup : MonoBehaviour
{
    [Header("팔레트 장착")]
    [SerializeField, Min(1)] private int equipAmount = 1;

    [Header("바닥 물리")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField, Min(0f)] private float fallingGravityScale = 1f;
    [SerializeField, Min(0f)] private float landingLift = 0.03f;

    [Header("둥둥 연출")]
    [SerializeField, Min(0f)] private float floatSpeed = 2.5f;
    [SerializeField, Min(0f)] private float floatAmount = 0.12f;

    [Header("자석")]
    [SerializeField, Min(0.1f)] private float magnetRange = 3f;
    [SerializeField, Min(0.01f)] private float collectDistance = 0.4f;
    [SerializeField, Min(0f)] private float initialPullSpeed = 2f;
    [SerializeField, Min(0f)] private float maximumPullSpeed = 14f;
    [SerializeField, Min(0f)] private float pullAcceleration = 20f;
    [SerializeField, Min(0f)] private float pullWobbleAmount = 0.08f;
    [SerializeField, Min(0f)] private float pullWobbleSpeed = 18f;

    [Header("참조")]
    [SerializeField] private StagePaletteManager paletteManager;

    private Rigidbody2D rb;
    private Collider2D itemCollider;

    private Transform playerTransform;
    private Collider2D playerBodyCollider;

    private bool isGrounded;
    private bool isBeingPulled;
    private bool isCollected;

    private float pullSpeed;
    private float pullStartTime;
    private Vector2 hoverBasePosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        itemCollider = GetComponent<Collider2D>();

        ConfigureFallingPhysics();

        if (itemCollider.isTrigger)
        {
            Debug.LogWarning(
                $"{gameObject.name}: 바닥 충돌을 위해 " +
                "Collider2D의 Is Trigger를 해제해야 합니다.");
        }
    }

    private void Start()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (isCollected)
        {
            return;
        }

        if (playerTransform == null)
        {
            ResolvePlayerReferences();
        }

        if (!isBeingPulled)
        {
            TryStartPull();
        }
    }

    private void FixedUpdate()
    {
        if (isCollected)
        {
            return;
        }

        if (isBeingPulled)
        {
            UpdatePullMovement();
            TryCollectByDistance();
        }
        else if (isGrounded)
        {
            UpdateFloatingMovement();
        }
    }

    public void StartMagnet(
        Transform target,
        float startSpeed)
    {
        if (isCollected ||
            isBeingPulled ||
            target == null)
        {
            return;
        }

        BeginPull(target, startSpeed);
    }

    public void StartMagnet(Transform target)
    {
        StartMagnet(target, initialPullSpeed);
    }

    private void TryStartPull()
    {
        if (playerTransform == null)
        {
            return;
        }

        float distance = Vector2.Distance(
            rb.position,
            GetPlayerTargetPosition());

        if (distance <= magnetRange)
        {
            BeginPull(playerTransform, initialPullSpeed);
        }
    }

    private void BeginPull(
        Transform target,
        float startSpeed)
    {
        playerTransform = target;
        ResolvePlayerBodyCollider();

        isBeingPulled = true;
        isGrounded = false;

        pullSpeed = Mathf.Max(0f, startSpeed);
        pullStartTime = Time.time;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        itemCollider.enabled = false;

        Debug.Log("[팔레트 아이템] 플레이어에게 끌려갑니다.");
    }

    private void UpdatePullMovement()
    {
        if (playerTransform == null)
        {
            CancelPull();
            return;
        }

        pullSpeed = Mathf.Min(
            maximumPullSpeed,
            pullSpeed + pullAcceleration * Time.fixedDeltaTime);

        Vector2 currentPosition = rb.position;
        Vector2 targetPosition = GetPlayerTargetPosition();
        Vector2 direction = targetPosition - currentPosition;

        if (direction.sqrMagnitude > 0.0001f)
        {
            direction.Normalize();
        }

        Vector2 perpendicular =
            new(-direction.y, direction.x);

        float wobble =
            Mathf.Sin(
                (Time.time - pullStartTime) *
                pullWobbleSpeed) *
            pullWobbleAmount;

        Vector2 curvedTarget =
            targetPosition + perpendicular * wobble;

        rb.MovePosition(
            Vector2.MoveTowards(
                currentPosition,
                curvedTarget,
                pullSpeed * Time.fixedDeltaTime));
    }

    private void TryCollectByDistance()
    {
        float distance = Vector2.Distance(
            rb.position,
            GetPlayerTargetPosition());

        if (distance <= collectDistance)
        {
            Collect();
        }
    }

    private void Collect()
    {
        if (isCollected)
        {
            return;
        }

        ResolvePaletteManager();

        if (paletteManager == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: StagePaletteManager를 찾지 못했습니다.");

            CancelPull();
            return;
        }

        SoundManager.Instance.PlaySFX(SFXType.Item);
        isCollected = true;

        paletteManager.EquipPaletteItem(equipAmount);

        Debug.Log(
            $"[팔레트 아이템] 획득 및 장착 +{equipAmount}");

        Destroy(gameObject);
    }

    private void CancelPull()
    {
        isBeingPulled = false;
        isGrounded = false;
        pullSpeed = 0f;

        itemCollider.enabled = true;
        ConfigureFallingPhysics();
    }

    private void ConfigureFallingPhysics()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallingGravityScale;
        rb.freezeRotation = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void UpdateFloatingMovement()
    {
        float wave =
            (Mathf.Sin(Time.time * floatSpeed) + 1f) * 0.5f;

        Vector2 targetPosition =
            hoverBasePosition +
            Vector2.up * (wave * floatAmount);

        rb.MovePosition(targetPosition);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryLandOnGround(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryLandOnGround(collision);
    }

    private void TryLandOnGround(Collision2D collision)
    {
        if (isCollected ||
            isBeingPulled ||
            isGrounded ||
            !IsGroundLayer(collision.gameObject.layer) ||
            !HasUpwardContact(collision))
        {
            return;
        }

        isGrounded = true;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        hoverBasePosition =
            rb.position + Vector2.up * landingLift;
    }

    private static bool HasUpwardContact(Collision2D collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.35f)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsGroundLayer(int layer)
    {
        return (groundLayer.value & (1 << layer)) != 0;
    }

    private Vector2 GetPlayerTargetPosition()
    {
        if (playerBodyCollider != null &&
            playerBodyCollider.enabled &&
            playerBodyCollider.gameObject.activeInHierarchy)
        {
            return playerBodyCollider.bounds.center;
        }

        return playerTransform != null
            ? playerTransform.position
            : rb.position;
    }

    private void ResolveReferences()
    {
        ResolvePaletteManager();
        ResolvePlayerReferences();
    }

    private void ResolvePaletteManager()
    {
        if (paletteManager != null)
        {
            return;
        }

        paletteManager =
            StagePaletteManager.Instance != null
                ? StagePaletteManager.Instance
                : FindAnyObjectByType<StagePaletteManager>();
    }

    private void ResolvePlayerReferences()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            playerTransform = null;
            playerBodyCollider = null;
            return;
        }

        playerTransform = playerObject.transform;
        ResolvePlayerBodyCollider();
    }

    private void ResolvePlayerBodyCollider()
    {
        playerBodyCollider = null;

        if (playerTransform == null)
        {
            return;
        }

        Collider2D[] colliders =
            playerTransform.GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D candidate in colliders)
        {
            if (candidate != null &&
                candidate.enabled &&
                !candidate.isTrigger)
            {
                playerBodyCollider = candidate;
                return;
            }
        }
    }

    private void OnValidate()
    {
        collectDistance =
            Mathf.Min(collectDistance, magnetRange);

        maximumPullSpeed =
            Mathf.Max(maximumPullSpeed, initialPullSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, magnetRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collectDistance);
    }
}
