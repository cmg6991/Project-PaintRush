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
    [SerializeField, Min(0f)] private float hitSpriteTime = 0.15f;
    [SerializeField, Min(0f)] private float deadSpriteTime = 0.3f;
    [SerializeField] private bool stopWhileHit = false;

    [Header("상태")]
    [SerializeField] private MonsterState currentState = MonsterState.Patrol;

    [Header("속성")]
    [SerializeField] private ElementType currentElement = ElementType.None;
    [SerializeField] private Color redElementColor = Color.red;
    [SerializeField] private Color blueElementColor = Color.blue;
    [SerializeField] private Color yellowElementColor = Color.yellow;
    [SerializeField, Min(0.001f)] private float colorTolerance = 0.08f;

    [Header("순찰 체크")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField, Min(0f)] private float groundCheckDistance = 0.7f;
    [SerializeField, Min(0f)] private float wallCheckDistance = 0.4f;

    [Header("순찰 범위")]
    [SerializeField, Min(0f)] private float patrolRange = 5f;
    [SerializeField, Min(0f)] private float patrolTurnCooldown = 0.2f;
    [SerializeField] private bool randomStartDirection = true;

    [Header("이동 및 감지")]
    [SerializeField, Min(0f)] private float patrolSpeed = 2f;
    [SerializeField, Min(0f)] private float chaseSpeed = 3.5f;
    [SerializeField, Min(0f)] private float detectRange = 4f;
    [SerializeField, Min(0f)] private float attackRange = 1.2f;

    [Header("AI 반응")]
    [SerializeField, Min(0f)] private float noticeTime = 0.5f;
    [SerializeField, Min(0f)] private float turnDelay = 0.4f;
    [SerializeField, Min(0f)] private float searchTime = 1.2f;

    [Header("공격")]
    [SerializeField, Min(1)] private int attackDamage = 1;
    [SerializeField, Min(0f)] private float attackCooldown = 1.5f;
    [SerializeField] private MonsterAttackTrigger attackTrigger;

    [Header("도망")]
    [SerializeField] private bool canRunAway = true;
    [SerializeField, Min(0)] private int runAwayHp = 1;
    [SerializeField, Min(0f)] private float runAwaySpeed = 4f;
    [SerializeField, Min(0f)] private float runAwayDistance = 6f;
    [SerializeField, Min(0f)] private float runAwayDuration = 2.5f;

    [Header("체력")]
    [SerializeField, Min(1)] private int maxHp = 3;

    [Header("상태 아이콘")]
    [SerializeField] private GameObject noticeIcon;
    [SerializeField] private GameObject runAwayIcon;
    [SerializeField] private GameObject paletteIcon;

    [Header("드롭 아이템")]
    [SerializeField] private GameObject defaultDropPrefab;
    [SerializeField] private GameObject redDropPrefab;
    [SerializeField] private GameObject blueDropPrefab;
    [SerializeField] private GameObject yellowDropPrefab;
    [SerializeField, Range(0f, 1f)] private float paintDropChance = 0.7f;

    [Header("팔레트 아이템")]
    [SerializeField] private bool hasPaletteItem;
    [SerializeField] private GameObject paletteItemPrefab;

    [Header("외부 참조")]
    [SerializeField] private Transform player;
    [SerializeField] private FillColor fillColor;
    [SerializeField] private MonsterManager monsterManager;

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
    private float runAwayTimer;

    private float basePatrolSpeed;
    private float baseChaseSpeed;
    private float baseRunAwaySpeed;

    private bool hasNoticedPlayer;
    private bool isHit;
    private bool isDead;
    private bool deathReported;
    private bool registeredToManager;

    private Coroutine hitRoutine;

    public MonsterState CurrentState => currentState;
    public ElementType CurrentElement => currentElement;
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDead => isDead;
    public bool HasPaletteItem => hasPaletteItem;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        monsterMovement = GetComponent<MonsterMovement>();
        monsterVisual = GetComponent<MonsterVisual>();

        if (fillColor == null)
        {
            fillColor = GetComponent<FillColor>();
        }

        if (attackTrigger == null)
        {
            attackTrigger = GetComponentInChildren<MonsterAttackTrigger>(true);
        }

        currentHp = maxHp;

        basePatrolSpeed = patrolSpeed;
        baseChaseSpeed = chaseSpeed;
        baseRunAwaySpeed = runAwaySpeed;

        rb.freezeRotation = true;

        SetNoticeIcon(false);
        SetRunAwayIcon(false);
        SetPaletteIcon(false);
    }

    private void Start()
    {
        if (monsterMovement.UsesPlayerTracking && player == null)
        {
            FindPlayer();
        }

        if (monsterManager == null)
        {
            monsterManager = MonsterManager.Instance;

            if (monsterManager == null)
            {
                monsterManager = FindFirstObjectByType<MonsterManager>();
            }
        }

        RegisterToManager();

        startX = transform.position.x;

        if (randomStartDirection)
        {
            moveDirection = Random.value < 0.5f ? -1 : 1;
        }

        if (currentElement != ElementType.None)
        {
            ApplyElementVisualAndStats();
        }
        else
        {
            SyncElementFromFillColor();
        }

        SetPaletteIcon(hasPaletteItem);
        UpdateFacing();
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        SyncElementFromFillColor();

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

        if (isHit && stopWhileHit)
        {
            monsterMovement.Stop();
            return;
        }

        // 피라냐처럼 플레이어 추적 FSM을 사용하지 않는 몬스터도
        // 공격 트리거에 플레이어가 있으면 접촉 피해를 준다.
        if (!monsterMovement.UsesPlayerTracking)
        {
            TryPerformAttack();
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
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogWarning($"{gameObject.name}: Player 태그 오브젝트를 찾지 못했습니다.");
            return;
        }

        player = playerObject.transform;
    }

    private void RegisterToManager()
    {
        if (registeredToManager || monsterManager == null)
        {
            return;
        }

        monsterManager.Register(this);
        registeredToManager = true;
    }

    private void UnregisterFromManager()
    {
        if (!registeredToManager || monsterManager == null)
        {
            return;
        }

        monsterManager.Unregister(this);
        registeredToManager = false;
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

        float distance = Vector2.Distance(transform.position, player.position);

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
        if (!hasNoticedPlayer && currentState == MonsterState.Patrol)
        {
            EnterNotice();
            return;
        }

        if (currentState == MonsterState.Notice)
        {
            return;
        }

        // 실제 공격 가능 여부는 거리 수치보다 AttackRange 트리거를 우선한다.
        // 기존에는 attackRange가 0.4처럼 작으면 플레이어가 트리거 안에 있어도
        // 계속 Chase 상태에 머무는 문제가 있었다.
        bool hasAttackTarget =
            attackTrigger != null &&
            attackTrigger.HasTarget;

        currentState = hasAttackTarget ||
                       distance <= attackRange
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
        SetRunAwayIcon(false);
    }

    private void EnterRunAway()
    {
        if (!canRunAway || !monsterMovement.UsesPlayerTracking)
        {
            return;
        }

        currentState = MonsterState.RunAway;
        runAwayTimer = 0f;
        SetRunAwayIcon(true);
    }

    private void Patrol()
    {
        UpdateStateIcons(false, false);

        CheckPatrolDirection();
        monsterMovement.Move(moveDirection, patrolSpeed);

        if (monsterMovement.Type == MonsterType.Piranha)
        {
            monsterVisual.SetState(MonsterVisualState.VerticalMove);
            monsterVisual.SetVerticalDirection(monsterMovement.VerticalDirection);
        }
        else
        {
            monsterVisual.SetState(MonsterVisualState.Move);
        }
    }

    private void NoticePlayer()
    {
        UpdateStateIcons(true, false);
        monsterVisual.SetState(MonsterVisualState.Attack);
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
        monsterVisual.SetState(MonsterVisualState.Move);

        if (player == null)
        {
            return;
        }

        UpdateDirectionToPlayer();
        monsterMovement.Move(moveDirection, chaseSpeed);
    }

    private void SearchPlayer()
    {
        UpdateStateIcons(false, false);
        monsterVisual.SetState(MonsterVisualState.Move);
        monsterMovement.Move(moveDirection, patrolSpeed);

        searchTimer -= Time.fixedDeltaTime;

        if (searchTimer <= 0f)
        {
            EnterPatrol();
        }
    }

    private void AttackPlayer()
    {
        UpdateStateIcons(false, false);
        monsterVisual.SetState(MonsterVisualState.Attack);

        if (player != null)
        {
            UpdateDirectionToPlayer();
        }

        monsterMovement.Stop();
        TryPerformAttack();
    }

    private bool TryPerformAttack()
    {
        if (attackTrigger == null)
        {
            return false;
        }

        if (!attackTrigger.TryGetTarget(out PlayerHealth target))
        {
            return false;
        }

        if (Time.time - lastAttackTime < attackCooldown)
        {
            return false;
        }

        lastAttackTime = Time.time;

        target.TakeDamage(
            attackDamage,
            Color.white,
            gameObject,
            true
        );

        Debug.Log($"{gameObject.name}: 플레이어 공격, 데미지 {attackDamage}");
        return true;
    }

    private void RunAway()
    {
        if (!canRunAway || !monsterMovement.UsesPlayerTracking)
        {
            EnterPatrol();
            return;
        }

        UpdateStateIcons(false, true);
        monsterVisual.SetState(MonsterVisualState.Move);

        if (player == null)
        {
            EnterPatrol();
            return;
        }

        runAwayTimer += Time.fixedDeltaTime;

        float distance = Vector2.Distance(transform.position, player.position);

        if ((runAwayDistance > 0f && distance >= runAwayDistance) ||
            (runAwayDuration > 0f && runAwayTimer >= runAwayDuration))
        {
            EnterPatrol();
            return;
        }

        float directionFromPlayer = transform.position.x - player.position.x;
        moveDirection = directionFromPlayer >= 0f ? 1 : -1;

        monsterMovement.Move(moveDirection, runAwaySpeed);
    }

    private void CheckPatrolDirection()
    {
        if (monsterMovement.Type == MonsterType.Piranha)
        {
            return;
        }

        if (Time.time - lastPatrolTurnTime < patrolTurnCooldown)
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

        if (monsterMovement.UsesGroundObstacleCheck && ShouldTurnAround())
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || !monsterMovement.CanTurnOnWallCollision)
        {
            return;
        }

        bool isGroundLayer =
            ((1 << collision.gameObject.layer) & groundLayer) != 0;

        if (!isGroundLayer)
        {
            return;
        }

        foreach (ContactPoint2D contact in collision.contacts)
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
        else if (directionToPlayer < -DirectionThreshold)
        {
            moveDirection = -1;
            lastTurnTime = Time.time;
        }
    }

    /// <summary>
    /// 몬스터 속성은 최초 한 번만 설정된다.
    /// 이미 속성이 있다면 다른 타일을 밟아도 변경하지 않는다.
    /// </summary>
    public bool ChangeElement(ElementType newElement)
    {
        if (newElement == ElementType.None)
        {
            return false;
        }

        if (currentElement != ElementType.None)
        {
            return currentElement == newElement;
        }

        SetElementInternal(newElement);
        return true;
    }

    private void SyncElementFromFillColor()
    {
        if (currentElement != ElementType.None ||
            fillColor == null ||
            !fillColor.HasColor)
        {
            return;
        }

        if (TryResolveElement(fillColor.CurrentColor, out ElementType element))
        {
            SetElementInternal(element);
        }
    }

    private void SetElementInternal(ElementType newElement)
    {
        currentElement = newElement;
        ApplyElementVisualAndStats();

        Debug.Log($"{gameObject.name}: 속성 확정 - {currentElement}");
    }

    private void ApplyElementVisualAndStats()
    {
        monsterVisual.SetElementTint(GetElementColor(currentElement));
        ApplyElementStats();
    }

    private bool TryResolveElement(Color color, out ElementType element)
    {
        float redDistance = ColorDistance(color, redElementColor);
        float blueDistance = ColorDistance(color, blueElementColor);
        float yellowDistance = ColorDistance(color, yellowElementColor);

        float minDistance = Mathf.Min(
            redDistance,
            blueDistance,
            yellowDistance
        );

        if (minDistance > colorTolerance)
        {
            element = ElementType.None;
            return false;
        }

        if (minDistance == redDistance)
        {
            element = ElementType.Red;
        }
        else if (minDistance == blueDistance)
        {
            element = ElementType.Blue;
        }
        else
        {
            element = ElementType.Yellow;
        }

        return true;
    }

    private bool IsMatchingAttackColor(Color attackColor)
    {
        Color elementColor = GetElementColor(currentElement);
        return ColorDistance(attackColor, elementColor) <= colorTolerance;
    }

    private static float ColorDistance(Color a, Color b)
    {
        Vector4 difference = (Vector4)a - (Vector4)b;
        difference.w = 0f;
        return difference.magnitude;
    }

    private Color GetElementColor(ElementType element)
    {
        switch (element)
        {
            case ElementType.Red:
                return redElementColor;

            case ElementType.Blue:
                return blueElementColor;

            case ElementType.Yellow:
                return yellowElementColor;

            default:
                return Color.white;
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
        if (isDead)
        {
            return;
        }

        if (damage <= 0)
        {
            Debug.LogWarning(
                $"{gameObject.name}: 0 이하의 데미지가 전달되었습니다. ({damage})"
            );
            return;
        }

        SyncElementFromFillColor();

        if (!ignoreElement)
        {
            if (currentElement == ElementType.None)
            {
                Debug.Log($"{gameObject.name}: 색이 없어 데미지 무효");
                return;
            }

            if (!IsMatchingAttackColor(attackColor))
            {
                Debug.Log($"{gameObject.name}: 색이 달라 데미지 무효");
                return;
            }
        }

        currentHp = Mathf.Max(0, currentHp - damage);

        Debug.Log($"{gameObject.name} 피격! 남은 체력: {currentHp}");

        if (currentHp <= 0)
        {
            isDead = true;
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
            EnterRunAway();
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
        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
            hitRoutine = null;
        }

        monsterMovement.Stop();

        SetNoticeIcon(false);
        SetRunAwayIcon(false);
        SetPaletteIcon(false);

        monsterVisual.PlayDead();
        DisableDamageAndCollisions();

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        ReportDeath();

        Debug.Log($"{gameObject.name}: 몬스터 사망");

        yield return new WaitForSeconds(deadSpriteTime);

        DropItem();
        Destroy(gameObject);
    }

    private void ReportDeath()
    {
        if (deathReported)
        {
            return;
        }

        deathReported = true;

        if (monsterManager != null)
        {
            monsterManager.RegisterDeath(this);
            registeredToManager = false;
        }
    }

    private void DisableDamageAndCollisions()
    {
        MonsterAttackTrigger[] attackTriggers =
            GetComponentsInChildren<MonsterAttackTrigger>(true);

        foreach (MonsterAttackTrigger trigger in attackTriggers)
        {
            trigger.enabled = false;
        }

        Collider2D[] colliders =
            GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }
    }

    private void DropItem()
    {
        if (hasPaletteItem)
        {
            if (paletteItemPrefab == null)
            {
                Debug.LogWarning(
                    $"{gameObject.name}: Palette Item Prefab이 연결되지 않았습니다."
                );
                return;
            }

            Instantiate(
                paletteItemPrefab,
                transform.position,
                Quaternion.identity
            );

            Debug.Log($"{gameObject.name}: 팔레트 아이템 드롭");
            return;
        }

        if (Random.value > paintDropChance)
        {
            Debug.Log($"{gameObject.name}: 물감 드롭 안 됨");
            return;
        }

        GameObject dropPrefab = GetDropPrefab();

        if (dropPrefab == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: {currentElement} 드롭 프리팹이 없습니다."
            );
            return;
        }

        Instantiate(
            dropPrefab,
            transform.position,
            Quaternion.identity
        );

        Debug.Log(
            $"{gameObject.name}: {currentElement} 물감 드롭"
        );
    }

    private GameObject GetDropPrefab()
    {
        switch (currentElement)
        {
            case ElementType.Red:
                return GetColorDropOrFallback(
                    redDropPrefab,
                    "Red"
                );

            case ElementType.Blue:
                return GetColorDropOrFallback(
                    blueDropPrefab,
                    "Blue"
                );

            case ElementType.Yellow:
                return GetColorDropOrFallback(
                    yellowDropPrefab,
                    "Yellow"
                );

            default:
                return defaultDropPrefab;
        }
    }

    private GameObject GetColorDropOrFallback(
        GameObject colorPrefab,
        string colorName)
    {
        if (colorPrefab != null)
        {
            return colorPrefab;
        }

        Debug.LogWarning(
            $"{gameObject.name}: {colorName} 드롭 프리팹이 없어 기본 드롭을 사용합니다."
        );

        return defaultDropPrefab;
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

    private void SetPaletteIcon(bool isActive)
    {
        if (paletteIcon != null)
        {
            paletteIcon.SetActive(isActive);
        }
    }

    private void UpdateFacing()
    {
        monsterVisual.SetDirection(moveDirection);

        if (groundCheck != null)
        {
            Vector3 position = groundCheck.localPosition;
            position.x = Mathf.Abs(position.x) * moveDirection;
            groundCheck.localPosition = position;
        }

        if (wallCheck != null)
        {
            Vector3 position = wallCheck.localPosition;
            position.x = Mathf.Abs(position.x) * moveDirection;
            wallCheck.localPosition = position;
        }
    }

    private void OnDisable()
    {
        if (!deathReported)
        {
            UnregisterFromManager();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (runAwayDistance > 0f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, runAwayDistance);
        }

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
