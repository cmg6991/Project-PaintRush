using System;
using UnityEngine;

/// <summary>
/// 몬스터가 드롭하는 색상 물감 아이템.
///
/// 획득 규칙:
/// - 총이 비어 있으면 해당 색으로 총을 강제로 충전한다.
/// - 총에 색이 있으면 팔레트 등록을 시도한다.
/// - 등록 실패, 중복 색, 팔레트 미보유 상태여도 아이템은 항상 획득되어 사라진다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ColorDropItem : MonoBehaviour
{
    [Header("물감 정보")]
    [SerializeField]
    private string colorId = "Red";

    [SerializeField]
    private Color paintColor = Color.red;

    [Header("바닥 물리")]
    [SerializeField]
    private LayerMask groundLayer;

    [SerializeField, Min(0f)]
    private float fallingGravityScale = 1f;

    [SerializeField, Min(0f)]
    private float landingLift = 0.03f;

    [Header("둥둥 연출")]
    [SerializeField, Min(0f)]
    private float floatSpeed = 3f;

    [SerializeField, Min(0f)]
    private float floatAmount = 0.12f;

    [Header("자석 범위")]
    [SerializeField, Min(0.1f)]
    private float magnetRange = 2.5f;

    [SerializeField, Min(0.01f)]
    private float collectDistance = 0.35f;

    [Header("자석 이동")]
    [SerializeField, Min(0f)]
    private float initialPullSpeed = 2f;

    [SerializeField, Min(0f)]
    private float maximumPullSpeed = 12f;

    [SerializeField, Min(0f)]
    private float pullAcceleration = 18f;

    [SerializeField, Min(0f)]
    private float pullWobbleAmount = 0.08f;

    [SerializeField, Min(0f)]
    private float pullWobbleSpeed = 18f;

    [Header("참조")]
    [Tooltip("비워두면 씬에서 자동 탐색")]
    [SerializeField]
    private StagePaletteManager paletteManager;

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

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallingGravityScale;
        rb.freezeRotation = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (itemCollider.isTrigger)
        {
            Debug.LogWarning(
                $"{gameObject.name}: 바닥 착지를 사용하려면 " +
                "ColorDropItem Collider2D의 Is Trigger를 해제하세요."
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

        if (distance <= magnetRange)
        {
            BeginPull(playerTransform, initialPullSpeed);
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

    private void BeginPull(
        Transform target,
        float startSpeed)
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
        Vector2 direction = targetPosition - currentPosition;

        if (direction.sqrMagnitude > 0.0001f)
        {
            direction.Normalize();
        }

        Vector2 perpendicular =
            new Vector2(-direction.y, direction.x);

        float wobble =
            Mathf.Sin(
                (Time.time - pullStartTime) *
                pullWobbleSpeed
            ) * pullWobbleAmount;

        Vector2 curvedTarget =
            targetPosition +
            perpendicular * wobble;

        rb.MovePosition(
            Vector2.MoveTowards(
                currentPosition,
                curvedTarget,
                pullSpeed * Time.fixedDeltaTime
            )
        );
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

        if (distance <= collectDistance)
        {
            CollectAlways();
        }
    }

    private void CollectAlways()
    {
        if (isCollected)
        {
            return;
        }

        if (gunFillColor == null)
        {
            FindPlayerReferences();
        }

        bool filledGun = false;
        bool registeredPalette = false;

        // SetColor 대신 GunSetColor를 사용해 빈 총을 확실하게 충전한다.
        if (gunFillColor != null &&
            !gunFillColor.HasColor)
        {
            gunFillColor.GunSetColor(paintColor);
            filledGun = true;
        }
        else
        {
            ResolvePaletteManager();

            if (paletteManager != null &&
                paletteManager.HasPaletteItem)
            {
                registeredPalette =
                    paletteManager.RegisterColor(colorId);
            }
        }

        if (filledGun)
        {
            Debug.Log(
                $"[물감 드롭] {colorId} 색으로 총을 충전했습니다."
            );
        }
        else if (registeredPalette)
        {
            Debug.Log(
                $"[물감 드롭] {colorId} 색을 팔레트에 등록했습니다."
            );
        }
        else
        {
            Debug.Log(
                $"[물감 드롭] {colorId} 아이템을 획득했지만 " +
                "총 충전 또는 팔레트 등록은 하지 않았습니다."
            );
        }

        CompleteCollection();
    }

    private void CompleteCollection()
    {
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
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallingGravityScale;

        isGrounded = false;
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
            !IsGroundLayer(collision.gameObject.layer))
        {
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.35f)
            {
                LandOnGround();
                return;
            }
        }
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
            (groundLayer.value & (1 << layer)) != 0;
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
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            playerTransform = null;
            playerBodyCollider = null;
            gunFillColor = null;
            return;
        }

        playerTransform = playerObject.transform;
        playerBodyCollider =
            FindPlayerBodyCollider(playerObject);
        gunFillColor =
            FindGunFillColor(playerObject);
    }

    private static FillColor FindGunFillColor(
        GameObject playerObject)
    {
        FillColor[] candidates =
            playerObject.GetComponentsInChildren<FillColor>(true);

        if (candidates.Length == 0)
        {
            return null;
        }

        // 이름에 Gun이 포함된 오브젝트를 우선 사용한다.
        foreach (FillColor candidate in candidates)
        {
            if (candidate != null &&
                candidate.gameObject.name.IndexOf(
                    "Gun",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return candidate;
            }
        }

        return candidates[0];
    }

    private static Collider2D FindPlayerBodyCollider(
        GameObject playerObject)
    {
        Collider2D[] colliders =
            playerObject.GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D candidate in colliders)
        {
            if (candidate != null &&
                candidate.enabled &&
                !candidate.isTrigger)
            {
                return candidate;
            }
        }

        return null;
    }

    private void ResolvePaletteManager()
    {
        if (paletteManager != null)
        {
            return;
        }

        paletteManager = StagePaletteManager.Instance;

        if (paletteManager == null)
        {
            paletteManager =
                FindAnyObjectByType<StagePaletteManager>();
        }
    }

    private void OnValidate()
    {
        colorId = string.IsNullOrWhiteSpace(colorId)
            ? "UnnamedColor"
            : colorId.Trim();

        collectDistance =
            Mathf.Min(collectDistance, magnetRange);

        maximumPullSpeed =
            Mathf.Max(maximumPullSpeed, initialPullSpeed);
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
