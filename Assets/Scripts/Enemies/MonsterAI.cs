using System.Collections;
using UnityEngine;

public enum MonsterState
{
    Patrol,
    Notice,
    Chase,
    Search,
    Attack,
    RunAway
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(MonsterMovement))]
[RequireComponent(typeof(MonsterVisual))]
public class MonsterAI : MonoBehaviour, IDamageable
{
    private const float DirectionThreshold = 0.2f;

    [Header("피격 및 사망")]
    public float hitSpriteTime = 0.15f;
    public float deadSpriteTime = 0.3f;

    [Header("상태")]
    public MonsterState currentState = MonsterState.Patrol;

    [Header("속성")]
    public ElementType currentElement = ElementType.None;

    [Header("순찰 체크")]
    public Transform groundCheck;
    public Transform wallCheck;
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.7f;
    public float wallCheckDistance = 0.4f;

    [Header("순찰 범위")]
    public float patrolRange = 5f;
    public float patrolTurnCooldown = 0.2f;
    public bool randomStartDirection = true;

    [Header("이동 속도")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float detectRange = 4f;
    public float attackRange = 1.2f;

    [Header("AI 반응")]
    public float noticeTime = 0.5f;
    public float turnDelay = 0.4f;
    public float searchTime = 1.2f;

    [Header("공격")]
    public float attackCooldown = 1.5f;

    [Header("도망")]
    public bool canRunAway = true;
    public int runAwayHp = 1;
    public float runAwaySpeed = 4f;

    [Header("체력")]
    public int maxHp = 3;

    [Header("상태 아이콘")]
    public GameObject noticeIcon;
    public GameObject runAwayIcon;

    [Header("드롭 아이템")]
    public GameObject defaultDropPrefab;
    public GameObject redDropPrefab;
    public GameObject blueDropPrefab;
    public GameObject yellowDropPrefab;

    [Header("팔레트 아이템")]
    public bool hasPaletteItem;
    public GameObject paletteItemPrefab;

    [Range(0f, 1f)]
    public float paintDropChance = 0.7f;

    [Header("대상")]
    public Transform player;
    public FillColor fillcolor;

    private Rigidbody2D rb;
    private MonsterMovement monsterMovement;
    private MonsterVisual monsterVisual;

    private int currentHp;
    private int moveDirection = 1;

    private float startX;
    private float lastPatrolTurnTime;

    private float noticeTimer;
    private float searchTimer;
    private float lastTurnTime;
    private float lastAttackTime;

    private float basePatrolSpeed;
    private float baseChaseSpeed;
    private float baseRunAwaySpeed;

    private bool hasNoticedPlayer;
    private bool isHit;
    private bool isDead;

    private Coroutine hitRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        monsterMovement = GetComponent<MonsterMovement>();
        monsterVisual = GetComponent<MonsterVisual>();

        if (monsterMovement == null || monsterVisual == null)
        {
            Debug.LogError(
                $"{gameObject.name}: " +
                "MonsterMovement 또는 MonsterVisual이 없습니다."
            );

            enabled = false;
            return;
        }

        currentHp = maxHp;

        basePatrolSpeed = patrolSpeed;
        baseChaseSpeed = chaseSpeed;
        baseRunAwaySpeed = runAwaySpeed;

        if (rb != null)
        {
            rb.freezeRotation = true;
        }

        SetNoticeIcon(false);
        SetRunAwayIcon(false);
    }

    private void Start()
    {
        fillcolor = GetComponent<FillColor>();

        if (monsterMovement.UsesPlayerTracking &&
            player == null)
        {
            FindPlayer();
        }

        startX = transform.position.x;

        if (randomStartDirection)
        {
            moveDirection =
                Random.value < 0.5f ? -1 : 1;
        }

        ChangeElement(currentElement);
        UpdateFacing();

        SetRunAwayIcon(hasPaletteItem);
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        if (monsterMovement.UsesPlayerTracking)
        {
            CheckPlayerDistance();
        }

        UpdateFacing();
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            return;
        }

        switch (currentState)
        {
            case MonsterState.Patrol:
                Patrol();
                break;

            case MonsterState.Notice:
                NoticePlayer();
                break;

            case MonsterState.Chase:
                ChasePlayer();
                break;

            case MonsterState.Search:
                SearchPlayer();
                break;

            case MonsterState.Attack:
                AttackPlayer();
                break;

            case MonsterState.RunAway:
                RunAway();
                break;
        }
    }

    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: Player 태그 오브젝트를 찾지 못함"
            );

            return;
        }

        player = playerObject.transform;
    }

    private void CheckPlayerDistance()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        if (currentState == MonsterState.RunAway)
        {
            return;
        }

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        if (distance <= detectRange)
        {
            HandlePlayerDetected(distance);
        }
        else
        {
            HandlePlayerLost();
        }
    }

    private void HandlePlayerDetected(float distance)
    {
        if (!hasNoticedPlayer &&
            currentState == MonsterState.Patrol)
        {
            EnterNotice();
            return;
        }

        if (currentState == MonsterState.Notice)
        {
            return;
        }

        currentState = distance <= attackRange
            ? MonsterState.Attack
            : MonsterState.Chase;
    }

    private void HandlePlayerLost()
    {
        if (currentState == MonsterState.Chase ||
            currentState == MonsterState.Attack)
        {
            EnterSearch();
            return;
        }

        if (currentState != MonsterState.Search)
        {
            EnterPatrol();
        }
    }

    private void EnterNotice()
    {
        currentState = MonsterState.Notice;
        noticeTimer = noticeTime;
        hasNoticedPlayer = true;
    }

    private void EnterSearch()
    {
        currentState = MonsterState.Search;
        searchTimer = searchTime;
    }

    private void EnterPatrol()
    {
        currentState = MonsterState.Patrol;
        hasNoticedPlayer = false;
    }

    private void Patrol()
    {
        UpdateStateIcons(false, hasPaletteItem);

        CheckPatrolDirection();

        monsterMovement.Move(
            moveDirection,
            patrolSpeed
        );

        if (monsterMovement.Type == MonsterType.Piranha)
        {
            monsterVisual.SetState(
                MonsterVisualState.VerticalMove
            );

            monsterVisual.SetVerticalDirection(
                monsterMovement.VerticalDirection
            );
        }
        else
        {
            monsterVisual.SetState(
                MonsterVisualState.Move
            );
        }
    }
    private void NoticePlayer()
    {
        UpdateStateIcons(true, false);

        monsterVisual.SetState(
            MonsterVisualState.Attack
        );

        monsterMovement.Stop();

        noticeTimer -= Time.fixedDeltaTime;

        if (noticeTimer <= 0f)
        {
            UpdateStateIcons(false, false);
            currentState = MonsterState.Chase;
        }
    }

    private void ChasePlayer()
    {
        UpdateStateIcons(false, false);

        monsterVisual.SetState(
            MonsterVisualState.Move
        );

        if (player == null)
        {
            return;
        }

        UpdateDirectionToPlayer();

        monsterMovement.Move(
            moveDirection,
            chaseSpeed
        );
    }

    private void SearchPlayer()
    {
        UpdateStateIcons(false, false);

        monsterVisual.SetState(
            MonsterVisualState.Move
        );

        monsterMovement.Move(
            moveDirection,
            patrolSpeed
        );

        searchTimer -= Time.fixedDeltaTime;

        if (searchTimer <= 0f)
        {
            EnterPatrol();
        }
    }

    private void AttackPlayer()
    {
        UpdateStateIcons(false, false);

        monsterVisual.SetState(
            MonsterVisualState.Attack
        );

        if (player != null)
        {
            UpdateDirectionToPlayer();
        }

        monsterMovement.Stop();

        if (Time.time - lastAttackTime < attackCooldown)
        {
            return;
        }

        lastAttackTime = Time.time;

        Debug.Log($"{gameObject.name}: 몬스터 공격");
    }

    private void RunAway()
    {
        if (!canRunAway ||
            !monsterMovement.UsesPlayerTracking)
        {
            EnterPatrol();
            return;
        }

        UpdateStateIcons(false, true);

        monsterVisual.SetState(
            MonsterVisualState.Move
        );

        if (player == null)
        {
            return;
        }

        float directionFromPlayer =
            transform.position.x - player.position.x;

        moveDirection =
            directionFromPlayer >= 0f ? 1 : -1;

        monsterMovement.Move(
            moveDirection,
            runAwaySpeed
        );
    }

    private void CheckPatrolDirection()
    {
        if (monsterMovement.Type == MonsterType.Piranha)
        {
            return;
        }

        if (Time.time - lastPatrolTurnTime <
            patrolTurnCooldown)
        {
            return;
        }

        float leftLimit = startX - patrolRange;
        float rightLimit = startX + patrolRange;

        if (transform.position.x <= leftLimit)
        {
            moveDirection = 1;
            lastPatrolTurnTime = Time.time;
            return;
        }

        if (transform.position.x >= rightLimit)
        {
            moveDirection = -1;
            lastPatrolTurnTime = Time.time;
            return;
        }

        if (monsterMovement.UsesGroundObstacleCheck &&
            ShouldTurnAround())
        {
            TurnAround();
            lastPatrolTurnTime = Time.time;
        }
    }

    private bool ShouldTurnAround()
    {
        if (groundCheck == null || wallCheck == null)
        {
            return false;
        }

        bool hasGroundAhead = Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        bool hasWallAhead = Physics2D.Raycast(
            wallCheck.position,
            new Vector2(moveDirection, 0f),
            wallCheckDistance,
            groundLayer
        );

        return !hasGroundAhead || hasWallAhead;
    }

    private void OnCollisionEnter2D(
        Collision2D collision)
    {
        if (isDead ||
            !monsterMovement.CanTurnOnWallCollision)
        {
            return;
        }

        bool isGroundLayer =
            ((1 << collision.gameObject.layer) &
             groundLayer) != 0;

        if (!isGroundLayer)
        {
            return;
        }

        foreach (ContactPoint2D contact in
                 collision.contacts)
        {
            if (Mathf.Abs(contact.normal.x) <= 0.5f)
            {
                continue;
            }

            TurnAround();
            lastPatrolTurnTime = Time.time;
            return;
        }
    }

    private void TurnAround()
    {
        moveDirection *= -1;
        monsterMovement.Stop();
    }

    private void UpdateDirectionToPlayer()
    {
        if (player == null)
        {
            return;
        }

        if (Time.time - lastTurnTime < turnDelay)
        {
            return;
        }

        float directionToPlayer =
            player.position.x - transform.position.x;

        if (directionToPlayer > DirectionThreshold)
        {
            moveDirection = 1;
            lastTurnTime = Time.time;
        }
        else if (
            directionToPlayer < -DirectionThreshold)
        {
            moveDirection = -1;
            lastTurnTime = Time.time;
        }
    }

    public void ChangeElement(ElementType newElement)
    {
        currentElement = newElement;

        ApplyElementColor();
        ApplyElementStats();
    }

    private void ApplyElementColor()
    {
        if (monsterVisual == null)
        {
            return;
        }

        switch (currentElement)
        {
            case ElementType.Red:
                monsterVisual.SetElementTint(Color.red);
                break;

            case ElementType.Blue:
                monsterVisual.SetElementTint(Color.blue);
                break;

            case ElementType.Yellow:
                monsterVisual.SetElementTint(Color.yellow);
                break;

            default:
                monsterVisual.SetElementTint(Color.white);
                break;
        }
    }

    private void ApplyElementStats()
    {
        switch (currentElement)
        {
            case ElementType.Red:
                patrolSpeed = basePatrolSpeed * 1.1f;
                chaseSpeed = baseChaseSpeed * 1.15f;
                runAwaySpeed = baseRunAwaySpeed * 1.1f;
                break;

            case ElementType.Blue:
                patrolSpeed = basePatrolSpeed * 0.75f;
                chaseSpeed = baseChaseSpeed * 0.75f;
                runAwaySpeed = baseRunAwaySpeed * 0.8f;
                break;

            case ElementType.Yellow:
                patrolSpeed = basePatrolSpeed * 0.95f;
                chaseSpeed = baseChaseSpeed;
                runAwaySpeed = baseRunAwaySpeed * 1.25f;
                break;

            default:
                patrolSpeed = basePatrolSpeed;
                chaseSpeed = baseChaseSpeed;
                runAwaySpeed = baseRunAwaySpeed;
                break;
        }
    }


    public void TakeDamage(
        int damage,
        Color attackColor,
        GameObject attacker,
        bool ignoreElement)
    {
        if(!ignoreElement)
        {
            if (!fillcolor.HasColor)
            {
                Debug.Log("색이 없음");
                return;
            }
            if (Vector4.Distance(fillcolor.CurrentColor, attackColor) > 0.01f)
            {
                Debug.Log("색이 다름");
                return;
            }
        }


        //if (isDead)
        //{
        //    return;
        //}

        //if (!ignoreElement &&
        //    attackColor != currentElement)
        //{
        //    Debug.Log(
        //        $"{gameObject.name}: 색상이 달라 데미지 무효"
        //    );

        //    return;
        //}

        currentHp = Mathf.Max(0,currentHp - damage);

        Debug.Log(
            $"{gameObject.name} 피격! 남은 체력: {currentHp}"
        );

        if (currentHp <= 0)
        {
            StartCoroutine(DieRoutine());
            return;
        }

        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
        }

        hitRoutine = StartCoroutine(HitRoutine());

        if (canRunAway &&
            monsterMovement.UsesPlayerTracking &&
            currentHp <= runAwayHp)
        {
            currentState = MonsterState.RunAway;
        }
    }

    private IEnumerator HitRoutine()
    {
        isHit = true;

        monsterVisual.PlayHit(hitSpriteTime);

        yield return new WaitForSeconds(hitSpriteTime);

        isHit = false;
        hitRoutine = null;
    }

    private IEnumerator DieRoutine()
    {
        if (isDead)
        {
            yield break;
        }

        isDead = true;

        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
            hitRoutine = null;
        }

        monsterMovement.Stop();

        SetNoticeIcon(false);
        SetRunAwayIcon(false);

        monsterVisual.PlayDead();

        DisableDamageAndCollisions();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        Debug.Log($"{gameObject.name}: 몬스터 사망");

        yield return new WaitForSeconds(deadSpriteTime);

        DropItem();

        Destroy(gameObject);
    }

    private void DisableDamageAndCollisions()
    {
        MonsterAttackTrigger[] attackTriggers =
            GetComponentsInChildren<MonsterAttackTrigger>();

        foreach (MonsterAttackTrigger attackTrigger
                 in attackTriggers)
        {
            attackTrigger.enabled = false;
        }

        Collider2D[] colliders =
            GetComponentsInChildren<Collider2D>();

        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }
    }

    private void DropItem()
    {
        if (hasPaletteItem)
        {
            if (paletteItemPrefab != null)
            {
                Instantiate(
                    paletteItemPrefab,
                    transform.position,
                    Quaternion.identity
                );

                Debug.Log("팔레트 아이템 드롭");
            }

            return;
        }

        if (Random.value > paintDropChance)
        {
            Debug.Log("물감 드롭 안 됨");
            return;
        }

        GameObject dropPrefab = GetDropPrefab();

        if (dropPrefab == null)
        {
            return;
        }

        Instantiate(
            dropPrefab,
            transform.position,
            Quaternion.identity
        );

        Debug.Log("물감 드롭");
    }

    private GameObject GetDropPrefab()
    {
        switch (currentElement)
        {
            case ElementType.Red:
                return redDropPrefab != null
                    ? redDropPrefab
                    : defaultDropPrefab;

            case ElementType.Blue:
                return blueDropPrefab != null
                    ? blueDropPrefab
                    : defaultDropPrefab;

            case ElementType.Yellow:
                return yellowDropPrefab != null
                    ? yellowDropPrefab
                    : defaultDropPrefab;

            default:
                return defaultDropPrefab;
        }
    }

    private void UpdateStateIcons(
        bool showNotice,
        bool showRunAway)
    {
        SetNoticeIcon(showNotice);
        SetRunAwayIcon(showRunAway);
    }

    private void SetNoticeIcon(bool isActive)
    {
        if (noticeIcon != null)
        {
            noticeIcon.SetActive(isActive);
        }
    }

    private void SetRunAwayIcon(bool isActive)
    {
        if (runAwayIcon != null)
        {
            runAwayIcon.SetActive(isActive);
        }
    }

    private void UpdateFacing()
    {
        if (monsterVisual != null)
        {
            monsterVisual.SetDirection(moveDirection);
        }

        if (groundCheck != null)
        {
            Vector3 position =
                groundCheck.localPosition;

            position.x =
                Mathf.Abs(position.x) *
                moveDirection;

            groundCheck.localPosition = position;
        }

        if (wallCheck != null)
        {
            Vector3 position =
                wallCheck.localPosition;

            position.x =
                Mathf.Abs(position.x) *
                moveDirection;

            wallCheck.localPosition = position;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            detectRange
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );

        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            new Vector3(
                transform.position.x - patrolRange,
                transform.position.y,
                transform.position.z
            ),
            new Vector3(
                transform.position.x + patrolRange,
                transform.position.y,
                transform.position.z
            )
        );

        if (groundCheck != null)
        {
            Gizmos.color = Color.blue;

            Gizmos.DrawLine(
                groundCheck.position,
                groundCheck.position +
                Vector3.down * groundCheckDistance
            );
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.magenta;

            Gizmos.DrawLine(
                wallCheck.position,
                wallCheck.position +
                Vector3.right *
                moveDirection *
                wallCheckDistance
            );
        }
    }
}