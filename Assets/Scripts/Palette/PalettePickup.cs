using UnityEngine;

/// <summary>
/// 팔레트 보유 몬스터가 드롭하는 특수 팔레트 아이템.
///
/// 생성 → 바닥 낙하 → 둥둥 떠 있음
/// → 플레이어 근처에서 자석 이동
/// → 가까워지면 팔레트 장착
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PalettePickup : MonoBehaviour
{
    [Header("팔레트 장착")]
    [SerializeField, Min(1)]
    private int equipAmount = 1;

    [Header("바닥 물리")]
    [SerializeField]
    private LayerMask groundLayer;

    [SerializeField, Min(0f)]
    private float fallingGravityScale = 1f;

    [SerializeField, Min(0f)]
    private float landingLift = 0.03f;

    [Header("둥둥 연출")]
    [SerializeField, Min(0f)]
    private float floatSpeed = 2.5f;

    [SerializeField, Min(0f)]
    private float floatAmount = 0.12f;

    [Header("자석 이동")]
    [SerializeField, Min(0.1f)]
    private float magnetRange = 3f;

    [SerializeField, Min(0.01f)]
    private float collectDistance = 0.4f;

    [SerializeField, Min(0f)]
    private float initialPullSpeed = 2f;

    [SerializeField, Min(0f)]
    private float maximumPullSpeed = 14f;

    [SerializeField, Min(0f)]
    private float pullAcceleration = 20f;

    [Header("자석 연출")]
    [SerializeField, Min(0f)]
    private float pullWobbleAmount = 0.08f;

    [SerializeField, Min(0f)]
    private float pullWobbleSpeed = 18f;

    [Header("참조")]
    [SerializeField]
    private StagePaletteManager paletteManager;

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

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallingGravityScale;
        rb.freezeRotation = true;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (itemCollider.isTrigger)
        {
            Debug.LogWarning(
                $"{gameObject.name}: 팔레트 아이템의 Collider2D는 " +
                "바닥 충돌을 위해 Is Trigger를 해제해야 합니다."
            );
        }
    }

    private void Start()
    {
        ResolvePaletteManager();
        FindPlayerReferences();
    }

    private void Update()
    {
        if (isCollected)
        {
            return;
        }

        if (playerTransform == null)
        {
            FindPlayerReferences();
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
            CheckCollectDistance();
            return;
        }

        if (isGrounded)
        {
            UpdateFloatingMovement();
        }
    }

    private void TryStartPull()
    {
        if (playerTransform == null)
        {
            return;
        }

        float distance = Vector2.Distance(
            rb.position,
            GetPlayerTargetPosition()
        );

        if (distance > magnetRange)
        {
            return;
        }

        BeginPull(
            playerTransform,
            initialPullSpeed
        );
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
        StartMagnet(
            target,
            initialPullSpeed
        );
    }

    private void BeginPull(
        Transform target,
        float startSpeed)
    {
        playerTransform = target;

        FindPlayerBodyCollider();

        isBeingPulled = true;
        isGrounded = false;

        pullSpeed = Mathf.Max(
            0f,
            startSpeed
        );

        pullStartTime = Time.time;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        /*
         * 획득은 거리로 판정한다.
         * 끌려오는 동안 플레이어나 지형 Collider에 막히지 않게 한다.
         */
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }

        Debug.Log(
            "[팔레트 아이템] 플레이어에게 끌려가기 시작합니다."
        );
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
            pullSpeed +
            pullAcceleration * Time.fixedDeltaTime
        );

        Vector2 currentPosition = rb.position;
        Vector2 targetPosition = GetPlayerTargetPosition();

        Vector2 direction =
            targetPosition - currentPosition;

        if (direction.sqrMagnitude > 0.0001f)
        {
            direction.Normalize();
        }

        Vector2 perpendicular = new Vector2(
            -direction.y,
            direction.x
        );

        float wobble = Mathf.Sin(
            (Time.time - pullStartTime) *
            pullWobbleSpeed
        ) * pullWobbleAmount;

        Vector2 curvedTarget =
            targetPosition +
            perpendicular * wobble;

        Vector2 nextPosition =
            Vector2.MoveTowards(
                currentPosition,
                curvedTarget,
                pullSpeed * Time.fixedDeltaTime
            );

        rb.MovePosition(nextPosition);
    }

    private void CheckCollectDistance()
    {
        if (playerTransform == null)
        {
            return;
        }

        float distance = Vector2.Distance(
            rb.position,
            GetPlayerTargetPosition()
        );

        if (distance > collectDistance)
        {
            return;
        }

        CollectPaletteItem();
    }

    private void CollectPaletteItem()
    {
        if (isCollected)
        {
            return;
        }

        ResolvePaletteManager();

        if (paletteManager == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: StagePaletteManager를 찾지 못했습니다."
            );

            CancelPull();
            return;
        }

        isCollected = true;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        paletteManager.EquipPaletteItem(
            equipAmount
        );

        Debug.Log(
            $"[팔레트 아이템] 획득 및 장착 +{equipAmount}"
        );

        Destroy(gameObject);
    }

    private void CancelPull()
    {
        isBeingPulled = false;
        isGrounded = false;

        pullSpeed = 0f;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallingGravityScale;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (itemCollider != null)
        {
            itemCollider.enabled = true;
        }
    }

    private void UpdateFloatingMovement()
    {
        float wave =
            (Mathf.Sin(Time.time * floatSpeed) + 1f) *
            0.5f;

        Vector2 targetPosition =
            hoverBasePosition +
            Vector2.up * (wave * floatAmount);

        rb.MovePosition(targetPosition);
    }

    private void OnCollisionEnter2D(
        Collision2D collision)
    {
        TryLand(collision);
    }

    private void OnCollisionStay2D(
        Collision2D collision)
    {
        TryLand(collision);
    }

    private void TryLand(
        Collision2D collision)
    {
        if (isCollected ||
            isBeingPulled ||
            isGrounded)
        {
            return;
        }

        if (!IsGroundLayer(
                collision.gameObject.layer))
        {
            return;
        }

        bool touchedGroundTop = false;

        for (int i = 0;
             i < collision.contactCount;
             i++)
        {
            ContactPoint2D contact =
                collision.GetContact(i);

            if (contact.normal.y > 0.35f)
            {
                touchedGroundTop = true;
                break;
            }
        }

        if (!touchedGroundTop)
        {
            return;
        }

        LandOnGround();
    }

    private void LandOnGround()
    {
        isGrounded = true;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        hoverBasePosition =
            rb.position +
            Vector2.up * landingLift;
    }

    private bool IsGroundLayer(int layer)
    {
        return
            (groundLayer.value &
             (1 << layer)) != 0;
    }

    private Vector2 GetPlayerTargetPosition()
    {
        if (playerBodyCollider != null &&
            playerBodyCollider.enabled &&
            playerBodyCollider.gameObject.activeInHierarchy)
        {
            return playerBodyCollider.bounds.center;
        }

        if (playerTransform != null)
        {
            return playerTransform.position;
        }

        return rb.position;
    }

    private void FindPlayerReferences()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (playerObject == null)
        {
            playerTransform = null;
            playerBodyCollider = null;
            return;
        }

        playerTransform =
            playerObject.transform;

        FindPlayerBodyCollider();
    }

    private void FindPlayerBodyCollider()
    {
        if (playerTransform == null)
        {
            playerBodyCollider = null;
            return;
        }

        Collider2D[] colliders =
            playerTransform
                .GetComponentsInChildren<Collider2D>(true);

        playerBodyCollider = null;

        foreach (Collider2D candidate in colliders)
        {
            if (candidate == null ||
                !candidate.enabled ||
                candidate.isTrigger)
            {
                continue;
            }

            playerBodyCollider = candidate;
            break;
        }
    }

    private void ResolvePaletteManager()
    {
        if (paletteManager != null)
        {
            return;
        }

        paletteManager =
            StagePaletteManager.Instance;

        if (paletteManager == null)
        {
            paletteManager =
                FindAnyObjectByType<StagePaletteManager>();
        }
    }

    private void OnValidate()
    {
        collectDistance = Mathf.Min(
            collectDistance,
            magnetRange
        );

        maximumPullSpeed = Mathf.Max(
            maximumPullSpeed,
            initialPullSpeed
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            magnetRange
        );

        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            transform.position,
            collectDistance
        );
    }
}