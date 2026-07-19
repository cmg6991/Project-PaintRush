using System.Collections;
using UnityEngine;

/// <summary>몬스터의 행동 상태입니다.</summary>
public enum MonsterState {
    Patrol,
    Notice,
    Chase,
    Search,
    Attack,
    RunAway
}
/// <summary>
/// 몬스터의 상태 전환과 전투 흐름을 관리합니다.
/// 이동과 스프라이트 처리는 MonsterMovement와 MonsterVisual에 위임합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(MonsterMovement))]
[RequireComponent(typeof(MonsterVisual))]
public class MonsterAI : MonoBehaviour, IDamageable {
    private const float DirectionThreshold = 0.2f;
    private const float WallNormalThreshold = 0.5f;
    private const string PlayerTag = "Player";
    private const string PaletteIconName = "PaletteIcon";

    #region Fields
    [Header("상태 및 체력")]
    [SerializeField] private MonsterState currentState = MonsterState.Patrol;
    [SerializeField, Min(1)] private int maxHp = 3;
    [Header("이동 및 감지")]
    [SerializeField, Min(0f)] private float patrolSpeed = 2f;
    [SerializeField, Min(0f)] private float chaseSpeed = 3.5f;
    [SerializeField, Min(0f)] private float detectRange = 4f;
    [SerializeField, Min(0f)] private float attackRange = 1.2f;
    [SerializeField, Min(0f)] private float noticeTime = 0.5f;
    [SerializeField, Min(0f)] private float turnDelay = 0.4f;
    [SerializeField, Min(0f)] private float searchTime = 1.2f;
    [Header("순찰")]
    [SerializeField, Min(0f)] private float patrolRange = 5f;
    [SerializeField, Min(0f)] private float patrolTurnCooldown = 0.2f;
    [SerializeField] private bool randomStartDirection = true;
    [Tooltip("발 앞쪽에서 아래 방향으로 바닥을 검사합니다.")]
    [SerializeField] private Transform groundCheck;
    [Tooltip("진행 방향 앞쪽의 벽을 검사합니다.")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField, Min(0f)] private float groundCheckDistance = 0.7f;
    [SerializeField, Min(0f)] private float wallCheckDistance = 0.4f;
    [Header("공격")]
    [SerializeField, Min(1)] private int attackDamage = 1;
    [SerializeField, Min(0f)] private float attackCooldown = 1.5f;
    [SerializeField] private MonsterAttackTrigger attackTrigger;
    [Header("도망")]
    [SerializeField] private bool canRunAway = true;
    [SerializeField, Min(0)] private int runAwayHp = 10;
    [SerializeField, Min(0f)] private float runAwaySpeed = 4f;
    [SerializeField, Min(0f)] private float runAwayDistance = 6f;
    [SerializeField, Min(0f)] private float runAwayDuration = 2.5f;
    [Header("피격 및 사망")]
    [SerializeField, Min(0f)] private float hitSpriteTime = 1.0f;
    [SerializeField, Min(0f)] private float deadSpriteTime = 1.2f;
    [SerializeField, Min(0f)] private float deadFadeDuration = 0.9f;
    [SerializeField] private bool stopWhileHit;
    [Header("공격 이펙트")]
    [SerializeField] private GameObject attackHitEffectPrefab;
    [SerializeField, Min(0.1f)] private float attackHitEffectScale = 5f;
    [SerializeField, Min(0.1f)] private float attackHitEffectLifetime = 1f;
    [SerializeField] private Vector3 attackHitEffectOffset;
    [Header("피격 이펙트")]
    [SerializeField] private GameObject monsterHitEffectPrefab;
    [SerializeField] private Vector3 monsterHitEffectOffset = Vector3.zero;
    [SerializeField, Min(0.1f)] private float monsterHitEffectLifetime = 1f;
    [Header("팔레트 이펙트")]
    [SerializeField] private GameObject sparkleEffectPrefab;
    private GameObject sparkleInstance;
    [Header("속성")]
    [SerializeField] private ElementType currentElement = ElementType.None;
    [SerializeField] private Color redElementColor = Color.red;
    [SerializeField] private Color blueElementColor = Color.blue;
    [SerializeField] private Color yellowElementColor = Color.yellow;
    [SerializeField, Min(0.001f)] private float colorTolerance = 0.08f;
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
    private MonsterMovement movement;
    private MonsterVisual visual;
    private int currentHp;
    private int moveDirection = 1;
    private string monsterUniqueId;
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
    private bool isPiranhaReturning;
    private Coroutine hitRoutine;

    #endregion

    #region Public API

    public MonsterState CurrentState => currentState;
    public ElementType CurrentElement => currentElement;
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDead => isDead;
    public bool HasPaletteItem => hasPaletteItem;
    /// <summary>속성이 없는 몬스터에게 최초 속성을 지정합니다.</summary>

    public bool ChangeElement(ElementType newElement) {
        if (newElement == ElementType.None)
            return false;
        if (currentElement != ElementType.None)
            return currentElement == newElement;
        SetElement(newElement);
        return true;
    }
    /// <summary>색상과 피버 보정을 확인한 뒤 피해를 적용합니다.</summary>

    public void TakeDamage(
        int damage,
        Color attackColor,
        GameObject attacker,
        bool ignoreElement) {
        if (isDead)
            return;
        if (damage <= 0) {
            Debug.LogWarning($"{name}: 0 이하의 데미지가 전달되었습니다. ({damage})");
            return;
        }
        PaletteSpecialAttack fever = PaletteSpecialAttack.Instance;
        bool feverApplied = fever != null &&
            fever.ApplyMonsterDamageModifiers(ref damage, ref ignoreElement);
        SyncElementFromFillColor();
        if (!CanReceiveDamage(attackColor, ignoreElement))
            return;
        currentHp = Mathf.Max(0, currentHp - damage);

        // 실제 데미지가 적용됐을 때 몬스터 피격 이펙트 생성
        SpawnMonsterHitEffect();

        Debug.Log(
            $"{name} 피격! 데미지: {damage}, 남은 체력: {currentHp}" +
            (feverApplied ? " (피버 공격)" : string.Empty));

        SyncStatsToDataManager();
        if (currentHp <= 0) {
            isDead = true;
            StartCoroutine(DieRoutine());
            return;
        }
        RestartHitRoutine();
        if (currentHp <= runAwayHp)
            EnterRunAway();
    }

    private void SpawnMonsterHitEffect()
    {
        if (monsterHitEffectPrefab == null)
            return;

        Vector3 spawnPosition =
            GetMonsterEffectPosition() + monsterHitEffectOffset;

        GameObject effect = Instantiate(
            monsterHitEffectPrefab,
            spawnPosition,
            Quaternion.identity);

        Destroy(effect, monsterHitEffectLifetime);
    }

    private Vector3 GetMonsterEffectPosition()
    {
        Collider2D bodyCollider = GetComponent<Collider2D>();

        if (bodyCollider != null)
            return bodyCollider.bounds.center;

        return transform.position;
    }
    /// <summary>팔레트 아이템 보유 여부와 드롭 프리팹을 설정합니다.</summary>

    public void SetPaletteCarrier(
        bool isCarrier,
        GameObject overridePaletteItemPrefab = null) {
        hasPaletteItem = isCarrier;
        if (overridePaletteItemPrefab != null)
            paletteItemPrefab = overridePaletteItemPrefab;
        ResolvePaletteIcon();
        SetActive(paletteIcon, hasPaletteItem);
        if (!hasPaletteItem)
            return;
        if (paletteIcon == null) {
            Debug.LogWarning(
                $"{name}: PaletteIcon이 없습니다. " +
                "프리팹 자식에 만들거나 Inspector에서 연결하세요.");
        }
        Debug.Log($"{name}: 팔레트 보유 몬스터로 지정됨");
    }

    #endregion

    #region Unity Lifecycle

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<MonsterMovement>();
        visual = GetComponent<MonsterVisual>();
        fillColor ??= GetComponent<FillColor>();
        attackTrigger ??= GetComponentInChildren<MonsterAttackTrigger>(true);
        currentHp = maxHp;
        basePatrolSpeed = patrolSpeed;
        baseChaseSpeed = chaseSpeed;
        baseRunAwaySpeed = runAwaySpeed;
        rb.freezeRotation = true;
        ResolvePaletteIcon();
        HideAllIcons();
    }

    private void Start() {
        if (movement.UsesPlayerTracking && player == null)
            FindPlayer();
        monsterManager ??= MonsterManager.Instance;
        monsterManager ??= FindFirstObjectByType<MonsterManager>();
        RegisterToManager();

        movement.InitializePlatformConstraint(
            groundLayer,
            groundCheckDistance);

        startX = transform.position.x;
        if (randomStartDirection)
            moveDirection = Random.value < 0.5f ? -1 : 1;
        if (currentElement == ElementType.None)
            SyncElementFromFillColor();
        else
            ApplyElement();
        SetActive(paletteIcon, hasPaletteItem);
        UpdateFacing();
    }

    private void Update() {
        if (isDead)
            return;

        SyncElementFromFillColor();

        // 공격 모션 중에는 거리 판정으로 상태가 바뀌지 않도록 잠급니다.
        if (!movement.IsAttacking &&
            movement.UsesPlayerTracking &&
            !movement.IsPiranha)
        {
            UpdatePlayerState();
        }

        UpdateFacing();
    }

    private void FixedUpdate() {
        if (isDead || movement.IsAttacking)
            return;

        if (isHit && stopWhileHit) {
            movement.Stop();
            return;
        }

        if (movement.IsPiranha)
        {
            UpdatePiranhaBehavior();
            return;
        }

        UpdateCurrentState();
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (isDead || !movement.CanTurnOnWallCollision || !IsGroundLayer(collision.gameObject.layer)) { return; }
        foreach (ContactPoint2D contact in collision.contacts) {
            if (Mathf.Abs(contact.normal.x) <= WallNormalThreshold)
                continue;
            TurnAround();
            lastPatrolTurnTime = Time.time;
            return;
        }
    }

    private void OnDisable() {
        if (!deathReported)
            UnregisterFromManager();
    }

    #endregion

    #region State Machine

    private void UpdateCurrentState() {
        switch (currentState) {
            case MonsterState.Patrol: UpdatePatrol(); break;
            case MonsterState.Notice: UpdateNotice(); break;
            case MonsterState.Chase: UpdateChase(); break;
            case MonsterState.Search: UpdateSearch(); break;
            case MonsterState.Attack: UpdateAttack(); break;
            case MonsterState.RunAway: UpdateRunAway(); break;
        }
    }

    /// <summary>
    /// 평상시에는 위아래로 이동하고,
    /// 상승 중 플레이어가 가까우면 한 번 공격한 뒤 원위치로 복귀합니다.
    /// </summary>
    private void UpdatePiranhaBehavior()
    {
        if (isPiranhaReturning)
        {
            UpdatePiranhaReturn();
            return;
        }

        if (movement.IsAttacking)
            return;

        currentState = MonsterState.Patrol;
        SetStateIcons(false, false);

        movement.Move(0, 0f);
        visual.SetState(MonsterVisualState.VerticalMove);
        visual.SetVerticalDirection(movement.VerticalDirection);

        if (player == null)
        {
            FindPlayer();
            return;
        }

        if (movement.CanPiranhaEngage(player))
            TryStartPiranhaAttack();
    }

    private bool TryStartPiranhaAttack()
    {
        if (!movement.IsPiranha ||
            player == null ||
            movement.IsAttacking ||
            Time.time - lastAttackTime < attackCooldown)
        {
            return false;
        }

        PlayerHealth target =
            player.GetComponent<PlayerHealth>();

        if (target == null)
            target =
                player.GetComponentInChildren<PlayerHealth>();

        if (target == null)
        {
            Debug.LogWarning(
                $"{name}: 피라냐가 PlayerHealth를 찾지 못했습니다.");
            return false;
        }

        FaceTargetImmediately(target.transform);

        bool started = movement.TryStartAttackMotion(
            target.transform,
            () => ApplyAttackDamage(target),
            OnAttackMotionComplete);

        if (!started)
            return false;

        movement.ConsumePiranhaAttack();
        lastAttackTime = Time.time;
        currentState = MonsterState.Attack;
        visual.SetState(MonsterVisualState.Attack);

        return true;
    }

    private void BeginPiranhaReturn()
    {
        isPiranhaReturning = true;
        currentState = MonsterState.Search;
        SetStateIcons(false, false);
    }

    private void UpdatePiranhaReturn()
    {
        currentState = MonsterState.Search;
        SetStateIcons(false, false);

        movement.ReturnPiranhaToStart();
        visual.SetState(MonsterVisualState.VerticalMove);
        visual.SetVerticalDirection(movement.VerticalDirection);

        if (!movement.IsNearPiranhaStartPosition())
            return;

        movement.ResetPiranhaCycle();
        isPiranhaReturning = false;
        currentState = MonsterState.Patrol;
    }

    private void UpdatePlayerState() {
        if (player == null) {
            FindPlayer();
            return;
        }
        if (currentState == MonsterState.RunAway)
            return;
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > detectRange) {
            if (currentState == MonsterState.Chase || currentState == MonsterState.Attack) { EnterSearch(); }
            else if (currentState != MonsterState.Search) { EnterPatrol(); }
            return;
        }
        if (!hasNoticedPlayer && currentState == MonsterState.Patrol) {
            EnterNotice();
            return;
        }
        if (currentState == MonsterState.Notice)
            return;
        bool targetInTrigger = attackTrigger != null && attackTrigger.HasTarget;
        currentState = targetInTrigger || distance <= attackRange
            ? MonsterState.Attack
            : MonsterState.Chase;
    }

    private void EnterPatrol() {
        currentState = MonsterState.Patrol;
        hasNoticedPlayer = false;
        SetActive(runAwayIcon, false);
    }

    private void EnterNotice() {
        currentState = MonsterState.Notice;
        noticeTimer = noticeTime;
        hasNoticedPlayer = true;
    }

    private void EnterSearch() {
        currentState = MonsterState.Search;
        searchTimer = searchTime;
    }

    private void EnterRunAway() {
        if (!CanRunAway)
            return;
        currentState = MonsterState.RunAway;
        runAwayTimer = 0f;
        SetActive(runAwayIcon, true);
    }

    private void UpdatePatrol() {
        SetStateIcons(false, false);
        CheckPatrolDirection();
        TryMoveSafely(patrolSpeed, true);
        if (movement.Type == MonsterType.Piranha) {
            visual.SetState(MonsterVisualState.VerticalMove);
            visual.SetVerticalDirection(movement.VerticalDirection);
            return;
        }
        visual.SetState(MonsterVisualState.Move);
    }

    private void UpdateNotice() {
        SetStateIcons(true, false);
        visual.SetState(MonsterVisualState.Attack);
        movement.Stop();
        noticeTimer -= Time.fixedDeltaTime;
        if (noticeTimer > 0f)
            return;
        SetStateIcons(false, false);
        currentState = MonsterState.Chase;
    }

    private void UpdateChase() {
        SetStateIcons(false, false);
        visual.SetState(MonsterVisualState.Move);
        if (player == null)
            return;
        FacePlayer();
        TryMoveSafely(chaseSpeed, false);
    }

    private void UpdateSearch() {
        SetStateIcons(false, false);
        visual.SetState(MonsterVisualState.Move);
        TryMoveSafely(patrolSpeed, true);
        searchTimer -= Time.fixedDeltaTime;
        if (searchTimer <= 0f)
            EnterPatrol();
    }

    private void UpdateAttack() {
        SetStateIcons(false, false);
        visual.SetState(MonsterVisualState.Attack);

        if (player != null)
            FacePlayer();

        movement.Stop();
        TryStartAttack();
    }

    private void UpdateRunAway() {
        if (!CanRunAway || player == null) {
            EnterPatrol();
            return;
        }
        SetStateIcons(false, true);
        visual.SetState(MonsterVisualState.Move);
        runAwayTimer += Time.fixedDeltaTime;
        float distance = Vector2.Distance(transform.position, player.position);
        // 도망 거리 또는 도망 시간에 도달하면 순찰 상태로 전환
        bool reachedDistance =runAwayDistance > 0f && distance >= runAwayDistance;
        bool reachedDuration = runAwayDuration > 0f && runAwayTimer >= runAwayDuration;
        if (reachedDistance || reachedDuration) {
            EnterPatrol();
            return;
        }
        moveDirection = transform.position.x >= player.position.x ? 1 : -1;
        TryMoveSafely(runAwaySpeed, false);
    }
    private bool CanRunAway =>
        canRunAway && movement.UsesPlayerTracking;

    #endregion

    #region Movement And Detection

    private void CheckPatrolDirection() {
        if (movement.Type == MonsterType.Piranha || Time.time - lastPatrolTurnTime < patrolTurnCooldown) { return; }
        float x = transform.position.x;
        if (x <= startX - patrolRange) {
            SetPatrolDirection(1);
            return;
        }
        if (x >= startX + patrolRange) {
            SetPatrolDirection(-1);
            return;
        }
        // 플랫폼 끝과 벽 판정은 TryMoveSafely에서 공통 처리합니다.
    }

    private bool ShouldTurnAround() {
        bool cliff = groundCheck != null &&
            !Physics2D.Raycast(
                groundCheck.position,
                Vector2.down,
                groundCheckDistance,
                groundLayer);
        bool wall = wallCheck != null &&
            Physics2D.Raycast(
                wallCheck.position,
                Vector2.right * moveDirection,
                wallCheckDistance,
                groundLayer);
        return cliff || wall;
    }

    /// <summary>
    /// 플랫폼 끝이나 벽을 확인한 뒤 안전하게 이동합니다.
    /// 순찰 상태에서는 방향을 바꾸고, 추적/도망 상태에서는 끝에서 멈춥니다.
    /// </summary>
    private bool TryMoveSafely(float speed, bool turnAtEdge) {
        if (!movement.UsesGroundObstacleCheck) {
            movement.Move(moveDirection, speed);
            return true;
        }

        // 개구리는 착지/점프 가능 여부를 MonsterMovement에서 직접 판단합니다.
        if (movement.Type == MonsterType.Frog) {
            movement.Move(moveDirection, speed);
            return true;
        }

        float lookAhead =
            Mathf.Max(0.05f, speed * Time.fixedDeltaTime);

        bool hasGroundAhead =
            movement.HasGroundAhead(
                moveDirection,
                lookAhead);

        bool blockedByWall =
            wallCheck != null &&
            Physics2D.Raycast(
                wallCheck.position,
                Vector2.right * moveDirection,
                wallCheckDistance,
                groundLayer);

        if (hasGroundAhead && !blockedByWall) {
            movement.Move(moveDirection, speed);
            return true;
        }

        movement.Stop();

        if (turnAtEdge &&
            Time.time - lastPatrolTurnTime >= patrolTurnCooldown) {
            TurnAround();
            lastPatrolTurnTime = Time.time;
        }

        return false;
    }

    private void FacePlayer() {
        if (player == null || Time.time - lastTurnTime < turnDelay)
            return;
        float deltaX = player.position.x - transform.position.x;
        if (deltaX > DirectionThreshold)
            moveDirection = 1;
        else if (deltaX < -DirectionThreshold)
            moveDirection = -1;
        else
            return;
        lastTurnTime = Time.time;
    }

    private void SetPatrolDirection(int direction) {
        moveDirection = direction;
        lastPatrolTurnTime = Time.time;
    }

    private void TurnAround() {
        moveDirection *= -1;
        movement.Stop();
    }

    private void UpdateFacing() {
        visual.SetDirection(moveDirection);
        MirrorCheckPoint(groundCheck);
        MirrorCheckPoint(wallCheck);
    }

    private void MirrorCheckPoint(Transform point) {
        if (point == null)
            return;
        Vector3 localPosition = point.localPosition;
        localPosition.x = Mathf.Abs(localPosition.x) * moveDirection;
        point.localPosition = localPosition;
    }

    private bool IsGroundLayer(int layer) =>
        ((1 << layer) & groundLayer) != 0;

    private void FindPlayer() {
        GameObject target = GameObject.FindGameObjectWithTag(PlayerTag);
        if (target == null) {
            Debug.LogWarning($"{name}: Player 태그 오브젝트를 찾지 못했습니다.");
            return;
        }
        player = target.transform;
    }

    #endregion

    #region Combat And Element

    private bool TryStartAttack() {
        if (!movement.CanAttackPlayer ||
            attackTrigger == null ||
            movement.IsAttacking ||
            Time.time - lastAttackTime <
                attackCooldown * movement.AttackCooldownMultiplier ||
            !attackTrigger.TryGetTarget(out PlayerHealth target)) {
            return false;
        }

        FaceTargetImmediately(target.transform);

        bool started = movement.TryStartAttackMotion(
            target.transform,
            () => ApplyAttackDamage(target),
            OnAttackMotionComplete);

        if (!started)
            return false;

        lastAttackTime = Time.time;
        return true;
    }

    /// <summary>공격 모션이 플레이어 중심에 도달했을 때 피해를 적용합니다.</summary>
    /// 
    private void ApplyAttackDamage(PlayerHealth target)
    {
        if (isDead || target == null)
            return;

        SpawnAttackHitEffect(target);

        target.TakeDamage(
            attackDamage,
            Color.white,
            gameObject,
            true);

        SoundManager.Instance.PlaySFX(SFXType.Hit);

        Debug.Log(
            $"{name}: 플레이어와 겹쳐 공격, 데미지 {attackDamage}");
    }


    private void SpawnAttackHitEffect(PlayerHealth target)
    {
        if (attackHitEffectPrefab == null || target == null)
            return;

        Vector3 spawnPosition =
            target.transform.position + attackHitEffectOffset;

        GameObject effect = Instantiate(
            attackHitEffectPrefab,
            spawnPosition,
            Quaternion.identity);

        Destroy(effect, attackHitEffectLifetime);
    }

    /// <summary>공격 반동이 끝나면 기본 행동 상태로 복귀합니다.</summary>
    private void OnAttackMotionComplete() {
        if (isDead)
            return;

        if (movement.IsPiranha)
        {
            BeginPiranhaReturn();
            return;
        }

        currentState = MonsterState.Chase;
    }

    /// <summary>공격 시작 직전에 회전 지연 없이 대상을 바라봅니다.</summary>
    private void FaceTargetImmediately(Transform target) {
        if (target == null)
            return;

        float deltaX = target.position.x - transform.position.x;

        if (Mathf.Abs(deltaX) > DirectionThreshold)
            moveDirection = deltaX > 0f ? 1 : -1;

        UpdateFacing();
    }

    private bool CanReceiveDamage(Color attackColor, bool ignoreElement) {
        if (ignoreElement)
            return true;
        if (currentElement == ElementType.None) {
            Debug.Log($"{name}: 색이 없어 데미지 무효");
            return false;
        }
        if (ColorDistance(
                attackColor,
                GetElementColor(currentElement)) <= colorTolerance) {
            return true;
        }
        Debug.Log($"{name}: 색이 달라 데미지 무효");
        return false;
    }

    private void RestartHitRoutine() {
        StopHitRoutine();
        hitRoutine = StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine() {
        isHit = true;
        visual.PlayHit(hitSpriteTime);
        yield return new WaitForSeconds(hitSpriteTime);
        isHit = false;
        hitRoutine = null;
    }

    private void StopHitRoutine() {
        if (hitRoutine == null)
            return;
        StopCoroutine(hitRoutine);
        hitRoutine = null;
        isHit = false;
    }

    private void SyncElementFromFillColor() {
        if (currentElement != ElementType.None || fillColor == null || !fillColor.HasColor) { return; }
        if (TryResolveElement(fillColor.CurrentColor, out ElementType element))
            SetElement(element);
    }

    private void SetElement(ElementType newElement) {
        currentElement = newElement;
        ApplyElement();
        Debug.Log($"{name}: 속성 확정 - {currentElement}");
        SyncStatsToDataManager();
    }

    private void ApplyElement() {
        visual.SetElementTint(GetElementColor(currentElement));
        float patrolMultiplier = 1f;
        float chaseMultiplier = 1f;
        float runMultiplier = 1f;
        switch (currentElement) {
            case ElementType.Red:
                patrolMultiplier = 1.1f;
                chaseMultiplier = 1.15f;
                runMultiplier = 1.1f;
                break;
            case ElementType.Blue:
                patrolMultiplier = 0.75f;
                chaseMultiplier = 0.75f;
                runMultiplier = 0.8f;
                break;
            case ElementType.Yellow:
                patrolMultiplier = 0.95f;
                runMultiplier = 1.25f;
                break;
        }
        patrolSpeed = basePatrolSpeed * patrolMultiplier;
        chaseSpeed = baseChaseSpeed * chaseMultiplier;
        runAwaySpeed = baseRunAwaySpeed * runMultiplier;
    }

    private bool TryResolveElement(Color color, out ElementType element) {
        float red = ColorDistance(color, redElementColor);
        float blue = ColorDistance(color, blueElementColor);
        float yellow = ColorDistance(color, yellowElementColor);
        float nearest = Mathf.Min(red, blue, yellow);
        if (nearest > colorTolerance) {
            element = ElementType.None;
            return false;
        }
        element = nearest == red
            ? ElementType.Red
            : nearest == blue
                ? ElementType.Blue
                : ElementType.Yellow;
        return true;
    }

    private Color GetElementColor(ElementType element) {
        switch (element) {
            case ElementType.Red: return redElementColor;
            case ElementType.Blue: return blueElementColor;
            case ElementType.Yellow: return yellowElementColor;
            default: return Color.white;
        }
    }

    private static float ColorDistance(Color first, Color second) {
        Vector4 difference = (Vector4)first - (Vector4)second;
        difference.w = 0f;
        return difference.magnitude;
    }

    #endregion

    #region Death, Drop And Icons
    // 사망 스프라이트 유지 → 페이드아웃 → 드롭 → 제거

    private IEnumerator DieRoutine() {
        PrepareDeath();
        ReportDeath();
        SoundManager.Instance.PlaySFX(SFXType.MonsterDead);
        Debug.Log($"{name}: 몬스터 사망");
        if (deadSpriteTime > 0f)
            yield return new WaitForSeconds(deadSpriteTime);
        if (deadFadeDuration > 0f)
            yield return FadeOutSprites();
        DropItem();
        Destroy(gameObject);
    }

    private void PrepareDeath() {
        StopHitRoutine();
        movement.CancelAttackMotion();
        movement.Stop();
        HideAllIcons();
        visual.PlayDead();
        foreach (MonsterAttackTrigger trigger in
                 GetComponentsInChildren<MonsterAttackTrigger>(true)) {
            trigger.enabled = false;
        }
        foreach (Collider2D collider in
                 GetComponentsInChildren<Collider2D>(true)) {
            collider.enabled = false;
        }
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
    }

    private IEnumerator FadeOutSprites() {
        SpriteRenderer[] renderers =
            GetComponentsInChildren<SpriteRenderer>(true);
        Color[] startColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            startColors[i] = renderers[i].color;
        float elapsed = 0f;
        while (elapsed < deadFadeDuration) {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / deadFadeDuration);
            for (int i = 0; i < renderers.Length; i++) {
                if (renderers[i] == null)
                    continue;
                Color color = startColors[i];
                color.a = Mathf.Lerp(startColors[i].a, 0f, progress);
                renderers[i].color = color;
            }
            yield return null;
        }
    }

    private void DropItem() {
        if (hasPaletteItem) {
            if (paletteItemPrefab == null) {
                Debug.LogWarning(
                    $"{name}: Palette Item Prefab이 연결되지 않았습니다.");
                return;
            }
            Instantiate(
                paletteItemPrefab,
                transform.position,
                Quaternion.identity);
            Debug.Log($"{name}: 팔레트 아이템 드롭");
            return;
        }
        if (Random.value > paintDropChance) {
            Debug.Log($"{name}: 물감 드롭 안 됨");
            return;
        }
        GameObject prefab = GetDropPrefab();
        if (prefab == null) {
            Debug.LogWarning(
                $"{name}: {currentElement} 드롭 프리팹이 없습니다.");
            return;
        }
        Instantiate(prefab, transform.position, Quaternion.identity);
        Debug.Log($"{name}: {currentElement} 물감 드롭");
    }

    private GameObject GetDropPrefab() {
        switch (currentElement) {
            case ElementType.Red:
                return GetDropOrDefault(redDropPrefab, "Red");
            case ElementType.Blue:
                return GetDropOrDefault(blueDropPrefab, "Blue");
            case ElementType.Yellow:
                return GetDropOrDefault(yellowDropPrefab, "Yellow");
            default:
                return defaultDropPrefab;
        }
    }

    private GameObject GetDropOrDefault(
        GameObject colorPrefab,
        string colorName) {
        if (colorPrefab != null)
            return colorPrefab;
        Debug.LogWarning(
            $"{name}: {colorName} 드롭 프리팹이 없어 기본 드롭을 사용합니다.");
        return defaultDropPrefab;
    }

    // 아이콘 세팅
    private void SetStateIcons(bool showNotice, bool showRunAway) {
        SetActive(noticeIcon, showNotice);
        SetActive(runAwayIcon, showRunAway);
    }

    private void HideAllIcons() {
        SetActive(noticeIcon, false);
        SetActive(runAwayIcon, false);
        SetActive(paletteIcon, false);
    }

    private static void SetActive(GameObject target, bool active) {
        if (target != null)
            target.SetActive(active);
    }

    // 팔레트 아이콘을 자식에서 찾아 연결합니다.
    private void ResolvePaletteIcon() {
        if (paletteIcon != null)
            return;
        foreach (Transform child in
                 GetComponentsInChildren<Transform>(true)) {
            if (child != transform &&
                string.Equals(
                    child.name,
                    PaletteIconName,
                    System.StringComparison.OrdinalIgnoreCase)) {
                paletteIcon = child.gameObject;
                return;
            }
        }
    }

    #endregion

    #region Manager, Data And Gizmos

    // 몬스터 매니저에 등록/해제 및 사망 보고
    private void RegisterToManager() {
        if (registeredToManager || monsterManager == null)
            return;
        monsterManager.Register(this);
        registeredToManager = true;
    }

    // 사망 보고 후 매니저에서 해제
    private void UnregisterFromManager() {
        if (!registeredToManager || monsterManager == null)
            return;
        monsterManager.Unregister(this);
        registeredToManager = false;
    }

    private void ReportDeath() {
        if (deathReported)
            return;
        deathReported = true;
        if (monsterManager == null)
            return;
        monsterManager.RegisterDeath(this);
        registeredToManager = false;
    }
    
    private void SyncStatsToDataManager() {
        if (DataManager.Instance == null ||
            string.IsNullOrEmpty(monsterUniqueId)) { return; }

        DataManager.Instance.UpdateMonsterStat(
            monsterUniqueId,
            currentHp,
            maxHp,
            patrolSpeed,
            chaseSpeed,
            detectRange,
            attackRange,
            attackDamage,
            currentElement.ToString());
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        if (runAwayDistance > 0f) {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, runAwayDistance);
        }
        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            transform.position + Vector3.left * patrolRange,
            transform.position + Vector3.right * patrolRange);
        if (groundCheck != null) {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(
                groundCheck.position,
                groundCheck.position +
                Vector3.down * groundCheckDistance);
        }
        if (wallCheck != null) {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(
                wallCheck.position,
                wallCheck.position +
                Vector3.right * moveDirection * wallCheckDistance);
        }
    }

    #endregion
}
