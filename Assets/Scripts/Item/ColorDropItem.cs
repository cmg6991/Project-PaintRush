using UnityEngine;

/// <summary>
/// 몬스터가 드롭하는 색상 물감 아이템.
///
/// 동작 순서:
/// 1. 생성 직후 Dynamic Rigidbody2D로 바닥에 떨어진다.
/// 2. Ground에 착지하면 Kinematic으로 전환한 뒤 둥둥 떠 있는다.
/// 3. 플레이어가 획득 가능한 상태에서 Magnet Range 안으로 들어오면 끌려간다.
/// 4. Collect Distance 안에 도달하면 총 또는 팔레트에 색을 등록한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ColorDropItem : MonoBehaviour
{
    [Header("물감 정보")]
    [Tooltip("팔레트 시스템에서 사용할 색상 ID. 예: Red, Blue, Yellow")]
    [SerializeField]
    private string colorId = "Red";

    [Tooltip("플레이어 총에 실제로 적용할 색")]
    [SerializeField]
    private Color paintColor = Color.red;

    [Header("바닥 물리")]
    [Tooltip("아이템이 착지할 바닥 레이어")]
    [SerializeField]
    private LayerMask groundLayer;

    [Tooltip("생성 직후 아이템에 적용할 중력")]
    [SerializeField, Min(0f)]
    private float fallingGravityScale = 1f;

    [Tooltip("바닥에 닿은 뒤 위쪽으로 약간 띄울 높이")]
    [SerializeField, Min(0f)]
    private float landingLift = 0.03f;

    [Header("둥둥 연출")]
    [SerializeField, Min(0f)]
    private float floatSpeed = 3f;

    [Tooltip("바닥 위에서 떠오르는 높이")]
    [SerializeField, Min(0f)]
    private float floatAmount = 0.12f;

    [Header("자석 범위")]
    [Tooltip("이 거리 안에 플레이어가 들어오면 끌려가기 시작")]
    [SerializeField, Min(0.1f)]
    private float magnetRange = 2.5f;

    [Tooltip("플레이어 중심과 이 거리 이내가 되면 획득")]
    [SerializeField, Min(0.01f)]
    private float collectDistance = 0.35f;

    [Header("자석 이동 속도")]
    [SerializeField, Min(0f)]
    private float initialPullSpeed = 2f;

    [SerializeField, Min(0f)]
    private float maximumPullSpeed = 12f;

    [SerializeField, Min(0f)]
    private float pullAcceleration = 18f;

    [Header("자석 이동 연출")]
    [Tooltip("끌려가면서 좌우로 흔들리는 정도")]
    [SerializeField, Min(0f)]
    private float pullWobbleAmount = 0.08f;

    [SerializeField, Min(0f)]
    private float pullWobbleSpeed = 18f;

    [Header("참조")]
    [Tooltip("비워두면 씬에서 자동으로 탐색")]
    [SerializeField]
    private StagePaletteManager paletteManager;

    private Rigidbody2D rb;
    private Collider2D itemCollider;

    private Transform playerTransform;
    private Collider2D playerBodyCollider;
    private FillColor gunFillColor;

    private RigidbodyType2D initialBodyType;

    private bool isGrounded;
    private bool isBeingPulled;
    private bool isCollected;

    private float pullSpeed;
    private float pullStartTime;

    private Vector2 hoverBasePosition;

    public string ColorId => colorId;
    public Color PaintColor => paintColor;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        itemCollider = GetComponent<Collider2D>();

        initialBodyType = rb.bodyType;

        ConfigureInitialPhysics();

        if (itemCollider.isTrigger)
        {
            Debug.LogWarning(
                $"{gameObject.name}: 바닥 충돌을 위해 " +
                "ColorDropItem의 Collider2D Is Trigger를 해제해야 합니다."
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

        if (playerTransform == null ||
            gunFillColor == null)
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

    private void ConfigureInitialPhysics()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallingGravityScale;
        rb.freezeRotation = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void TryStartPull()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning(
                $"[물감 드롭/{colorId}] Player 태그 오브젝트를 찾지 못함"
            );
            return;
        }

        if (gunFillColor == null)
        {
            Debug.LogWarning(
                $"[물감 드롭/{colorId}] 플레이어 자식에서 FillColor를 찾지 못함"
            );
            return;
        }

        ResolvePaletteManager();

        bool gunHasColor = gunFillColor.HasColor;
        bool hasPalette =
            paletteManager != null &&
            paletteManager.HasPaletteItem;

        float distance = Vector2.Distance(
            rb.position,
            GetPlayerTargetPosition()
        );

        Debug.Log(
            $"[물감 드롭/{colorId}] " +
            $"거리={distance:F2}, " +
            $"자석범위={magnetRange:F2}, " +
            $"총색상={gunHasColor}, " +
            $"팔레트={hasPalette}"
        );

        if (!CanPlayerReceiveItem())
        {
            return;
        }

        if (distance > magnetRange)
        {
            return;
        }

        BeginPull(
            playerTransform,
            initialPullSpeed
        );
    }

    /// <summary>
    /// 기존 PlayerMagnet 등의 외부 스크립트가 호출할 수 있는 함수.
    /// </summary>
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

        if (!CanPlayerReceiveItem())
        {
            return;
        }

        BeginPull(target, startSpeed);
    }

    /// <summary>
    /// 속도 인자를 전달하지 않는 기존 코드와의 호환용.
    /// </summary>
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

        isBeingPulled = true;
        isGrounded = false;

        pullSpeed = Mathf.Max(
            0f,
            startSpeed
        );

        pullStartTime = Time.time;

        // 물리 충돌의 영향을 받지 않고
        // 플레이어를 향해 이동하도록 Kinematic으로 전환한다.
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        Debug.Log(
            $"[물감 드롭] {colorId} 아이템이 " +
            "플레이어에게 끌려가기 시작합니다."
        );
    }

    private bool CanPlayerReceiveItem()
    {
        if (gunFillColor == null)
        {
            return false;
        }

        // 총에 색이 없으면 물감을 총에 채울 수 있다.
        if (!gunFillColor.HasColor)
        {
            return true;
        }

        ResolvePaletteManager();

        // 총에 색이 있는 경우에는
        // 팔레트를 장착해야 색을 등록할 수 있다.
        if (paletteManager == null ||
            !paletteManager.HasPaletteItem)
        {
            return false;
        }

        if (!paletteManager.IsRequiredColor(colorId))
        {
            return false;
        }

        if (paletteManager.IsColorCollected(colorId))
        {
            return false;
        }

        return true;
    }

    private void UpdatePullMovement()
    {
        if (playerTransform == null)
        {
            CancelPull();
            return;
        }

        if (!CanPlayerReceiveItem())
        {
            CancelPull();
            return;
        }

        pullSpeed = Mathf.Min(
            maximumPullSpeed,
            pullSpeed +
            pullAcceleration *
            Time.fixedDeltaTime
        );

        Vector2 currentPosition = rb.position;
        Vector2 targetPosition =
            GetPlayerTargetPosition();

        Vector2 direction =
            targetPosition - currentPosition;

        if (direction.sqrMagnitude >
            0.0001f)
        {
            direction.Normalize();
        }

        Vector2 perpendicular =
            new Vector2(
                -direction.y,
                direction.x
            );

        float wobble =
            Mathf.Sin(
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
                pullSpeed *
                Time.fixedDeltaTime
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

        if (distance >
            collectDistance)
        {
            return;
        }

        TryCollect();
    }

    private void TryCollect()
    {
        if (isCollected ||
            gunFillColor == null)
        {
            return;
        }

        /*
         * 총이 비어 있는 상태에서 먹은 물감은
         * 오직 총 충전에만 사용합니다.
         *
         * 이 분기에서는 StagePaletteManager.RegisterColor /
         * RegisterPaint를 절대 호출하지 않으므로
         * 피버 게이지와 물감 개수가 증가하지 않습니다.
         */
        if (!gunFillColor.HasColor)
        {
            TryFillEmptyGun();
            return;
        }

        /*
         * 총에 이미 색이 있을 때만
         * 팔레트 수집 대상으로 처리합니다.
         */
        TryStorePaintInPalette();
    }

    private void TryFillEmptyGun()
    {
        bool colorApplied =
            gunFillColor.SetColor(paintColor);

        if (!colorApplied)
        {
            CancelPull();
            return;
        }

        Debug.Log(
            $"[물감 드롭] 총이 비어 있어 " +
            $"{colorId} 색을 총에만 채웠습니다. " +
            "피버 게이지에는 반영하지 않습니다.");

        CompleteCollection();
    }

    private void TryStorePaintInPalette()
    {
        ResolvePaletteManager();

        if (paletteManager == null ||
            !paletteManager.HasPaletteItem)
        {
            Debug.Log(
                $"[물감 드롭] 팔레트를 장착하지 않아 " +
                $"{colorId} 색을 등록하지 못했습니다.");

            CancelPull();
            return;
        }

        bool registered =
            paletteManager.RegisterColor(colorId);

        if (!registered)
        {
            CancelPull();
            return;
        }

        Debug.Log(
            $"[물감 드롭] {colorId} 색을 " +
            "팔레트에 등록했습니다.");

        CompleteCollection();
    }

    private void CompleteCollection()
    {
        if (isCollected)
        {
            return;
        }

        isCollected = true;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }

        Destroy(gameObject);
    }

    private void CancelPull()
    {
        isBeingPulled = false;
        pullSpeed = 0f;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        /*
         * 끌려오다가 조건이 사라진 경우:
         * 현재 위치에서 다시 중력으로 떨어지도록 한다.
         */
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallingGravityScale;

        isGrounded = false;
    }

    private void UpdateFloatingMovement()
    {
        /*
         * -floatAmount ~ +floatAmount가 아니라
         * 0 ~ floatAmount 사이에서만 움직이게 한다.
         * 그래야 바닥 안으로 파고들지 않는다.
         */
        float wave =
            (Mathf.Sin(
                Time.time * floatSpeed
            ) + 1f) * 0.5f;

        float verticalOffset =
            wave * floatAmount;

        Vector2 targetPosition =
            hoverBasePosition +
            Vector2.up * verticalOffset;

        rb.MovePosition(targetPosition);
    }

    private void OnCollisionEnter2D(
        Collision2D collision)
    {
        TryLandOnGround(collision);
    }

    private void OnCollisionStay2D(
        Collision2D collision)
    {
        TryLandOnGround(collision);
    }

    private void TryLandOnGround(
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

        /*
         * 벽 옆면 접촉을 바닥 착지로 오인하지 않도록
         * 접촉면의 노멀 방향도 확인한다.
         */
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

        /*
         * 착지 후에는 물리 중력으로 떨어질 필요가 없으므로
         * Kinematic으로 바꾸고 MovePosition으로 둥둥 움직인다.
         */
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
            playerBodyCollider.gameObject
                .activeInHierarchy)
        {
            return
                playerBodyCollider.bounds.center;
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
            gunFillColor = null;
            return;
        }

        playerTransform =
            playerObject.transform;

        gunFillColor =
            playerObject
                .GetComponentInChildren
                <FillColor>(true);

        playerBodyCollider =
            FindPlayerBodyCollider(
                playerObject
            );
    }

    private static Collider2D
        FindPlayerBodyCollider(
            GameObject playerObject)
    {
        Collider2D[] colliders =
            playerObject
                .GetComponentsInChildren
                <Collider2D>(true);

        foreach (Collider2D candidate
                 in colliders)
        {
            if (candidate == null ||
                !candidate.enabled ||
                candidate.isTrigger)
            {
                continue;
            }

            return candidate;
        }

        return null;
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
                FindAnyObjectByType
                <StagePaletteManager>();
        }
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(
                colorId))
        {
            colorId = "UnnamedColor";
        }
        else
        {
            colorId = colorId.Trim();
        }

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