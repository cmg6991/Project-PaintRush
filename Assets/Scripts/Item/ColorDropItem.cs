using UnityEngine;

/// <summary>
/// 몬스터가 떨어뜨리는 물감 아이템입니다.
///
/// 획득 우선순위:
/// 1. 총이 비어 있음: 총에 새 색을 가득 충전합니다.
/// 2. 총에 같은 색이 조금 남음: 총만 가득 재충전합니다.
/// 3. 총이 같은 색으로 가득 참: 팔레트 조건을 만족하면 팔레트에 저장합니다.
/// 4. 총과 다른 색: 총은 유지하고 팔레트 조건을 만족하면 피버 게이지/수량에 저장합니다.
///
/// 총 충전에 사용된 물감은 StagePaletteManager에 등록하지 않으므로
/// 피버 게이지와 팔레트 물감 수가 증가하지 않습니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class ColorDropItem : MonoBehaviour
{
    private enum ReceiveRoute
    {
        None,
        FillEmptyGun,
        RefillSameColorGun,
        StoreInPalette
    }

    [Header("물감 정보")]
    [SerializeField] private string colorId = "Red";
    [SerializeField] private Color paintColor = Color.red;

    [Header("바닥 물리")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField, Min(0f)] private float fallingGravityScale = 1f;
    [SerializeField, Min(0f)] private float landingLift = 0.03f;

    [Header("둥둥 연출")]
    [SerializeField, Min(0f)] private float floatSpeed = 3f;
    [SerializeField, Min(0f)] private float floatAmount = 0.12f;

    [Header("자석 범위")]
    [SerializeField, Min(0.1f)] private float magnetRange = 2.5f;
    [SerializeField, Min(0.01f)] private float collectDistance = 0.35f;

    [Header("자석 이동")]
    [SerializeField, Min(0f)] private float initialPullSpeed = 2f;
    [SerializeField, Min(0f)] private float maximumPullSpeed = 12f;
    [SerializeField, Min(0f)] private float pullAcceleration = 18f;
    [SerializeField, Min(0f)] private float pullWobbleAmount = 0.08f;
    [SerializeField, Min(0f)] private float pullWobbleSpeed = 18f;

    [Header("참조")]
    [SerializeField] private StagePaletteManager paletteManager;
    [SerializeField] private bool showDebugLogs;

    private Rigidbody2D rb;
    private Collider2D itemCollider;
    private Transform playerTransform;
    private Collider2D playerBodyCollider;
    private FillColor gunFillColor;

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

        ConfigureInitialPhysics();

        if (itemCollider.isTrigger)
        {
            Debug.LogWarning(
                $"{name}: ColorDropItem Collider2D의 Is Trigger를 해제해야 " +
                "바닥에 정상적으로 착지합니다.");
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
            return;

        if (playerTransform == null || gunFillColor == null)
            FindPlayerReferences();

        if (!isBeingPulled)
            TryStartPull();
    }

    private void FixedUpdate()
    {
        if (isCollected)
            return;

        if (isBeingPulled)
        {
            UpdatePullMovement();
            CheckCollectDistance();
            return;
        }

        if (isGrounded)
            UpdateFloatingMovement();
    }

    public void StartMagnet(Transform target, float startSpeed)
    {
        if (isCollected || isBeingPulled || target == null)
            return;

        if (ResolveReceiveRoute() == ReceiveRoute.None)
            return;

        BeginPull(target, startSpeed);
    }

    public void StartMagnet(Transform target)
    {
        StartMagnet(target, initialPullSpeed);
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
        if (playerTransform == null || gunFillColor == null)
            return;

        if (ResolveReceiveRoute() == ReceiveRoute.None)
            return;

        float distance = Vector2.Distance(
            rb.position,
            GetPlayerTargetPosition());

        if (distance <= magnetRange)
            BeginPull(playerTransform, initialPullSpeed);
    }

    private ReceiveRoute ResolveReceiveRoute()
    {
        if (gunFillColor == null || gunFillColor.IsFever)
            return ReceiveRoute.None;

        if (!gunFillColor.HasColor)
            return ReceiveRoute.FillEmptyGun;

        bool sameColor =
            gunFillColor.IsSameColor(paintColor);

        // 같은 색이고 총이 덜 찼을 때만 총을 우선 재충전합니다.
        if (sameColor && !gunFillColor.IsFull)
            return ReceiveRoute.RefillSameColorGun;

        ResolvePaletteManager();

        // 총과 다른 색이거나, 같은 색이지만 총이 이미 가득 찬 경우:
        // 팔레트를 보유했고 현재 스테이지 허용 색이면 피버 게이지/수량으로 보냅니다.
        if (paletteManager != null &&
            paletteManager.HasPaletteItem &&
            paletteManager.IsRequiredColor(colorId))
        {
            return ReceiveRoute.StoreInPalette;
        }

        return ReceiveRoute.None;
    }

    private void BeginPull(Transform target, float startSpeed)
    {
        playerTransform = target;
        isBeingPulled = true;
        isGrounded = false;
        pullSpeed = Mathf.Max(0f, startSpeed);
        pullStartTime = Time.time;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (showDebugLogs)
            Debug.Log($"[물감 드롭] {colorId} 끌어오기 시작");
    }

    private void UpdatePullMovement()
    {
        if (playerTransform == null || ResolveReceiveRoute() == ReceiveRoute.None)
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
            direction.Normalize();

        Vector2 perpendicular = new(-direction.y, direction.x);
        float wobble = Mathf.Sin(
            (Time.time - pullStartTime) * pullWobbleSpeed) *
            pullWobbleAmount;

        Vector2 curvedTarget = targetPosition + perpendicular * wobble;
        Vector2 nextPosition = Vector2.MoveTowards(
            currentPosition,
            curvedTarget,
            pullSpeed * Time.fixedDeltaTime);

        rb.MovePosition(nextPosition);
    }

    private void CheckCollectDistance()
    {
        if (playerTransform == null)
            return;

        if (Vector2.Distance(rb.position, GetPlayerTargetPosition()) <= collectDistance)
            TryCollect();
    }

    private void TryCollect()
    {
        if (isCollected || gunFillColor == null)
            return;

        ReceiveRoute route = ResolveReceiveRoute();
        bool collected = route switch
        {
            ReceiveRoute.FillEmptyGun => TryChargeGun("빈 총 충전"),
            ReceiveRoute.RefillSameColorGun => TryChargeGun("같은 색 재충전"),
            ReceiveRoute.StoreInPalette => TryStoreInPalette(),
            _ => false
        };

        if (collected)
            CompleteCollection();
        else
            CancelPull();
    }

    private bool TryChargeGun(string reason)
    {
        bool charged = gunFillColor.TryFillOrRefill(paintColor);

        if (charged && showDebugLogs)
        {
            Debug.Log(
                $"[물감 드롭] {colorId}: {reason}. " +
                "팔레트 및 피버 게이지에는 반영하지 않음");
        }

        return charged;
    }

    private bool TryStoreInPalette()
    {
        ResolvePaletteManager();

        if (paletteManager == null || !paletteManager.HasPaletteItem)
            return false;

        bool registered = paletteManager.RegisterColor(colorId);

        if (registered && showDebugLogs)
            Debug.Log($"[물감 드롭] {colorId} 팔레트 저장");

        return registered;
    }

    private void CompleteCollection()
    {
        if (isCollected)
            return;

        isCollected = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (itemCollider != null)
            itemCollider.enabled = false;

        Destroy(gameObject);
    }

    private void CancelPull()
    {
        if (isCollected)
            return;

        isBeingPulled = false;
        pullSpeed = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallingGravityScale;
        isGrounded = false;
    }

    private void UpdateFloatingMovement()
    {
        float wave = (Mathf.Sin(Time.time * floatSpeed) + 1f) * 0.5f;
        Vector2 targetPosition = hoverBasePosition +
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
        if (isCollected || isBeingPulled || isGrounded ||
            !IsGroundLayer(collision.gameObject.layer))
        {
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y <= 0.35f)
                continue;

            LandOnGround();
            return;
        }
    }

    private void LandOnGround()
    {
        isGrounded = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        hoverBasePosition = rb.position + Vector2.up * landingLift;
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
            ? (Vector2)playerTransform.position
            : rb.position;
    }

    private void FindPlayerReferences()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            playerTransform = null;
            playerBodyCollider = null;
            gunFillColor = null;
            return;
        }

        playerTransform = playerObject.transform;
        gunFillColor = playerObject.GetComponentInChildren<FillColor>(true);
        playerBodyCollider = FindPlayerBodyCollider(playerObject);
    }

    private static Collider2D FindPlayerBodyCollider(GameObject playerObject)
    {
        foreach (Collider2D candidate in
                 playerObject.GetComponentsInChildren<Collider2D>(true))
        {
            if (candidate != null && candidate.enabled && !candidate.isTrigger)
                return candidate;
        }

        return null;
    }

    private void ResolvePaletteManager()
    {
        if (paletteManager != null &&
            paletteManager.gameObject.scene == gameObject.scene)
        {
            return;
        }

        paletteManager = StagePaletteManager.FindForScene(this);
    }

    private void OnValidate()
    {
        colorId = string.IsNullOrWhiteSpace(colorId)
            ? "UnnamedColor"
            : colorId.Trim();

        collectDistance = Mathf.Min(collectDistance, magnetRange);
        maximumPullSpeed = Mathf.Max(maximumPullSpeed, initialPullSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, magnetRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collectDistance);
    }
}
