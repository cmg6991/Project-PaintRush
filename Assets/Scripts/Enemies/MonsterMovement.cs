using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MonsterMovement : MonoBehaviour
{
    [Header("몬스터 종류")]
    [SerializeField] private MonsterType monsterType;

    [Header("공통 이동")]
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float groundGravityScale = 1.5f;

    [Header("몬스터별 속도 배율")]
    [SerializeField] private float slimeSpeedMultiplier = 0.45f;
    [SerializeField] private float ghostSpeedMultiplier = 0.55f;
    [SerializeField] private float spiderSpeedMultiplier = 0.75f;
    [SerializeField] private float frogSpeedMultiplier = 0.55f;

    [Header("유령 이동")]
    [SerializeField] private float ghostFloatHeight = 0.2f;
    [SerializeField] private float ghostFloatFrequency = 1.5f;
    [SerializeField] private float ghostVerticalCorrection = 4f;
    [SerializeField] private float ghostMaxVerticalSpeed = 1.2f;

    [Header("피라냐 이동")]
    [SerializeField] private float piranhaJumpHeight = 1.8f;
    [SerializeField] private float piranhaFrequency = 1.5f;

    [Header("개구리 이동")]
    [SerializeField] private Transform frogGroundCheck;
    [SerializeField] private LayerMask frogGroundLayer;
    [SerializeField] private float frogGroundCheckRadius = 0.2f;
    [SerializeField] private float frogJumpPower = 4f;
    [SerializeField] private float frogJumpInterval = 1.5f;

    private Rigidbody2D rb;

    private int verticalDirection = 1;

    public int VerticalDirection => verticalDirection;

    private Vector2 startPosition;
    private float spawnTime;
    private float ghostPhase;
    private float nextFrogJumpTime;

    public MonsterType Type => monsterType;

    public bool UsesPlayerTracking =>
        monsterType != MonsterType.Piranha;

    public bool UsesGroundObstacleCheck =>
        monsterType == MonsterType.Slime ||
        monsterType == MonsterType.Spider;

    public bool CanTurnOnWallCollision =>
        monsterType == MonsterType.Slime ||
        monsterType == MonsterType.Spider ||
        monsterType == MonsterType.Frog;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        startPosition = rb.position;
        spawnTime = Time.time;
        ghostPhase = Random.Range(0f, Mathf.PI * 2f);

        ConfigureRigidbody();
    }

    private void Start()
    {
        if (monsterType == MonsterType.Frog &&
            frogGroundCheck == null)
        {
            Debug.LogError(
                $"{gameObject.name}: Frog Ground Check가 연결되지 않았습니다."
            );
        }
    }

    public void Move(int direction, float speed)
    {
        if (rb == null)
        {
            return;
        }

        switch (monsterType)
        {
            case MonsterType.Slime:
                MoveGround(
                    direction,
                    speed * slimeSpeedMultiplier
                );
                break;

            case MonsterType.Ghost:
                MoveGhost(
                    direction,
                    speed * ghostSpeedMultiplier
                );
                break;

            case MonsterType.Piranha:
                MovePiranha();
                break;

            case MonsterType.Spider:
                MoveGround(
                    direction,
                    speed * spiderSpeedMultiplier
                );
                break;

            case MonsterType.Frog:
                MoveFrog(
                    direction,
                    speed * frogSpeedMultiplier
                );
                break;
        }
    }

    public void Stop()
    {
        if (rb == null)
        {
            return;
        }

        switch (monsterType)
        {
            case MonsterType.Ghost:
                MoveGhost(0, 0f);
                break;

            case MonsterType.Piranha:
                MovePiranha();
                break;

            case MonsterType.Frog:
                // 개구리는 공중에서 수평 속도를 강제로 없애지 않음
                if (IsFrogGrounded())
                {
                    SetHorizontalVelocity(0f);
                }
                break;

            default:
                SetHorizontalVelocity(0f);
                break;
        }
    }

    private void MoveGround(int direction, float speed)
    {
        SetHorizontalVelocity(direction * speed);
    }

    private void MoveGhost(int direction, float speed)
    {
        float targetXVelocity = direction * speed;

        float newXVelocity = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetXVelocity,
            acceleration * Time.fixedDeltaTime
        );

        float targetY =
            startPosition.y +
            Mathf.Sin(
                Time.time * ghostFloatFrequency + ghostPhase
            ) * ghostFloatHeight;

        float yVelocity =
            (targetY - rb.position.y) *
            ghostVerticalCorrection;

        yVelocity = Mathf.Clamp(
            yVelocity,
            -ghostMaxVerticalSpeed,
            ghostMaxVerticalSpeed
        );

        rb.linearVelocity = new Vector2(
            newXVelocity,
            yVelocity
        );
    }

    private void MovePiranha()
    {
        float elapsedTime = Time.time - spawnTime;

        float phase =
            elapsedTime * piranhaFrequency -
            Mathf.PI * 0.5f;

        float normalizedHeight =
            (Mathf.Sin(phase) + 1f) * 0.5f;

        float targetY =
            startPosition.y +
            normalizedHeight * piranhaJumpHeight;

        // 코사인 값으로 현재 이동 방향 판단
        float verticalVelocity = Mathf.Cos(phase);

        if (verticalVelocity > 0.01f)
        {
            verticalDirection = 1;
        }
        else if (verticalVelocity < -0.01f)
        {
            verticalDirection = -1;
        }

        rb.MovePosition(
            new Vector2(
                startPosition.x,
                targetY
            )
        );
    }

    private void MoveFrog(int direction, float speed)
    {
        if (frogGroundCheck == null)
        {
            return;
        }

        // 공중에서는 현재 점프 속도를 유지
        if (!IsFrogGrounded())
        {
            return;
        }

        // 착지 후 다음 점프 시간까지 대기
        if (Time.time < nextFrogJumpTime)
        {
            SetHorizontalVelocity(0f);
            return;
        }

        rb.linearVelocity = new Vector2(
            direction * speed,
            frogJumpPower
        );

        nextFrogJumpTime =
            Time.time + frogJumpInterval;
    }

    private bool IsFrogGrounded()
    {
        if (frogGroundCheck == null)
        {
            return false;
        }

        Collider2D groundCollider =
            Physics2D.OverlapCircle(
                frogGroundCheck.position,
                frogGroundCheckRadius,
                frogGroundLayer
            );

        return groundCollider != null;
    }

    private void SetHorizontalVelocity(float targetSpeed)
    {
        float newXVelocity = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetSpeed,
            acceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector2(
            newXVelocity,
            rb.linearVelocity.y
        );
    }

    private void ConfigureRigidbody()
    {
        rb.freezeRotation = true;

        switch (monsterType)
        {
            case MonsterType.Ghost:
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                break;

            case MonsterType.Piranha:
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                break;

            default:
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = groundGravityScale;
                break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (frogGroundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            frogGroundCheck.position,
            frogGroundCheckRadius
        );
    }
}
