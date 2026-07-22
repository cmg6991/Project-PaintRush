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
    [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.08f;
    [Header("유령 발판 이탈 방지")]
    [Tooltip("유령이 진행 방향 앞쪽에서 바닥을 검사할 거리입니다.")]
    [SerializeField, Min(0.05f)] private float ghostEdgeLookAhead = 0.3f;
    [Tooltip("공중에 떠 있는 유령이 아래 발판을 찾기 위한 검사 거리입니다.")]
    [SerializeField, Min(0.1f)] private float ghostGroundProbeDistance = 3f;
    [Tooltip("타일 사이 작은 틈을 낭떠러지로 오인하지 않도록 하는 검사 반지름입니다.")]
    [SerializeField, Min(0.01f)] private float ghostGroundProbeRadius = 0.12f;
    [Tooltip("유령 발판 끝 감지 로그를 표시합니다.")]
    [SerializeField] private bool logGhostEdgeTurn;

    [SerializeField, Min(0f)] private float wallCheckDistance = 0.4f;
    [Tooltip("벽 감지 BoxCast의 높이 비율입니다. 몸체 높이의 일부만 검사해 바닥을 벽으로 오인하지 않게 합니다.")]
    [SerializeField, Range(0.1f, 1f)] private float wallCheckHeightRatio = 0.55f;
    [Tooltip("벽을 만난 뒤 플레이어 추적이 즉시 같은 방향으로 덮어쓰지 않도록 유지하는 시간입니다.")]
    [SerializeField, Min(0f)] private float wallAvoidanceDuration = 0.6f;
    [Tooltip("개구리가 벽에 밀착했을 때 반대쪽으로 아주 조금 떼어내는 거리입니다.")]
    [SerializeField, Min(0f)] private float frogWallSeparationDistance = 0.04f;
    [Tooltip("개구리가 옆면 충돌 후 플레이어 방향으로 다시 돌아서지 않는 시간입니다.")]
    [SerializeField, Min(0.05f)] private float frogCollisionTurnLockDuration = 1.2f;
    [Tooltip("옆면 충돌 순간 개구리를 반대쪽으로 밀어내는 수평 속도입니다.")]
    [SerializeField, Min(0f)] private float frogCollisionPushSpeed = 0.8f;
    [Tooltip("개구리 옆면 충돌 방향 전환 로그를 표시합니다.")]
    [SerializeField] private bool logFrogCollisionTurn;

    [Header("개구리 선제 방향 전환")]
    [Tooltip("개구리 몸 앞에서 장애물을 미리 검사하는 거리입니다.")]
    [SerializeField, Min(0.02f)] private float frogForwardCheckDistance = 0.18f;
    [Tooltip("개구리 몸체 높이 중 장애물 검사에 사용할 비율입니다.")]
    [SerializeField, Range(0.1f, 0.8f)] private float frogForwardCheckHeightRatio = 0.35f;
    [Tooltip("개구리 앞쪽 바닥을 얼마나 멀리 내다볼지 설정합니다.")]
    [SerializeField, Min(0.05f)] private float frogEdgeLookAhead = 0.35f;
    [Tooltip("개구리가 장애물 또는 발판 끝을 미리 감지했을 때 로그를 표시합니다.")]
    [SerializeField] private bool logFrogPredictiveTurn;
    [Header("공격")]
    [SerializeField, Min(1)] private int attackDamage = 1;
    [SerializeField, Min(0f)] private float attackCooldown = 1.5f;
    [SerializeField] private MonsterAttackTrigger attackTrigger;
    [Header("피라냐 공격 제한")]
    [Tooltip("플레이어가 행거에 매달린 동안 피라냐가 공격을 시작하거나 피해를 주지 않습니다.")]
    [SerializeField] private bool blockPiranhaAttackWhilePlayerHanging = true;
    [SerializeField] private bool logBlockedPiranhaAttack;
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
    [SerializeField, Min(0.1f)] private float attackHitEffectScale = 1f;
    [SerializeField, Min(0.1f)] private float attackHitEffectLifetime = 1f;
    [SerializeField] private Vector3 attackHitEffectOffset;
    [Header("피격 이펙트")]
    [SerializeField] private GameObject monsterHitEffectPrefab;
    [SerializeField] private Vector3 monsterHitEffectOffset = Vector3.zero;
    [SerializeField, Min(0.1f)] private float monsterHitEffectLifetime = 1f;
    [Header("피격 사운드")]
    [Tooltip("몬스터별 피격음을 여러 개 넣으면 무작위로 재생합니다.")]
    [SerializeField] private AudioClip[] monsterHitSounds;
    [SerializeField] private AudioSource monsterHitAudioSource;
    [SerializeField, Range(0f, 1f)] private float monsterHitSoundVolume = 1f;
    [SerializeField] private Vector2 monsterHitPitchRange = new(0.95f, 1.05f);
    [SerializeField, Range(0f, 1f)] private float monsterHitSpatialBlend = 0f;
    [Header("팔레트 이펙트")]
    [SerializeField] private GameObject sparkleEffectPrefab;
    private GameObject sparkleInstance;
    [Header("속성")]
    [SerializeField] private ElementType currentElement = ElementType.None;
    [SerializeField] private Color redElementColor = Color.red;
    [SerializeField] private Color blueElementColor = Color.blue;
    [SerializeField] private Color yellowElementColor = Color.yellow;
    [SerializeField] private Color greenElementColor = new Color(0f, 1f, 0f, 1f);
    [SerializeField] private Color purpleElementColor = new Color(170f / 255f, 0f, 1f, 1f);
    [SerializeField, Min(0.001f)] private float colorTolerance = 0.12f;
    [Header("상태 아이콘")]
    [SerializeField] private GameObject noticeIcon;
    [SerializeField] private GameObject runAwayIcon;
    [SerializeField] private GameObject paletteIcon;
    [Header("드롭 아이템")]
    [SerializeField] private GameObject defaultDropPrefab;
    [SerializeField] private GameObject redDropPrefab;
    [SerializeField] private GameObject blueDropPrefab;
    [SerializeField] private GameObject yellowDropPrefab;
    [SerializeField] private GameObject greenDropPrefab;
    [SerializeField] private GameObject purpleDropPrefab;
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
    private Collider2D bodyCollider;
    private int currentHp;
    private int moveDirection = 1;
    private string monsterUniqueId;
    private float startX;
    private float lastPatrolTurnTime;
    private float wallAvoidanceUntil;
    private float frogCollisionTurnLockUntil;
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
    private Project.Player.PlayerController2D playerController;
    private Coroutine hitRoutine;

    #endregion

    #region Public API

    public MonsterState CurrentState => currentState;
    public ElementType CurrentElement => currentElement;
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDead => isDead;
    public bool HasPaletteItem => hasPaletteItem;
    public bool IsPiranha =>
        movement != null &&
        movement.IsPiranha;
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
        PaletteSpecialAttack fever =
            PaletteSpecialAttack.FindForScene(gameObject);
        bool feverApplied = fever != null &&
            fever.ApplyMonsterDamageModifiers(ref damage, ref ignoreElement);
        SyncElementFromFillColor();
        if (!CanReceiveDamage(attackColor, ignoreElement))
            return;
        currentHp = Mathf.Max(0, currentHp - damage);

        // 색상 판정을 통과해 실제 체력이 감소했을 때만 재생합니다.
        PlayMonsterHitSound();
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

    private void ConfigureMonsterHitAudio()
    {
        if (monsterHitAudioSource == null &&
            HasMonsterHitSound())
        {
            monsterHitAudioSource =
                gameObject.AddComponent<AudioSource>();
        }

        if (monsterHitAudioSource == null)
            return;

        monsterHitAudioSource.playOnAwake = false;
        monsterHitAudioSource.loop = false;
        monsterHitAudioSource.spatialBlend =
            monsterHitSpatialBlend;
    }

    private bool HasMonsterHitSound()
    {
        if (monsterHitSounds == null)
            return false;

        foreach (AudioClip clip in monsterHitSounds)
        {
            if (clip != null)
                return true;
        }

        return false;
    }

    private void PlayMonsterHitSound()
    {
        if (!HasMonsterHitSound())
            return;

        if (monsterHitAudioSource == null)
            ConfigureMonsterHitAudio();

        if (monsterHitAudioSource == null)
            return;

        AudioClip clip = GetRandomMonsterHitSound();

        if (clip == null)
            return;

        float minPitch =
            Mathf.Min(
                monsterHitPitchRange.x,
                monsterHitPitchRange.y);

        float maxPitch =
            Mathf.Max(
                monsterHitPitchRange.x,
                monsterHitPitchRange.y);

        monsterHitAudioSource.pitch =
            Random.Range(minPitch, maxPitch);

        monsterHitAudioSource.PlayOneShot(
            clip,
            monsterHitSoundVolume);
    }

    private AudioClip GetRandomMonsterHitSound()
    {
        int validCount = 0;

        foreach (AudioClip clip in monsterHitSounds)
        {
            if (clip != null)
                validCount++;
        }

        if (validCount == 0)
            return null;

        int selectedIndex =
            Random.Range(0, validCount);

        foreach (AudioClip clip in monsterHitSounds)
        {
            if (clip == null)
                continue;

            if (selectedIndex == 0)
                return clip;

            selectedIndex--;
        }

        return null;
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
        bodyCollider = FindBodyCollider();
        fillColor ??= GetComponent<FillColor>();
        attackTrigger ??= GetComponentInChildren<MonsterAttackTrigger>(true);
        ConfigureMonsterHitAudio();
        currentHp = maxHp;
        basePatrolSpeed = patrolSpeed;
        baseChaseSpeed = chaseSpeed;
        baseRunAwaySpeed = runAwaySpeed;
        rb.freezeRotation = true;
        ResolvePaletteIcon();
        HideAllIcons();
    }

    private void Start() {
        if (player == null)
            FindPlayer();
        else
            CachePlayerController();

        monsterManager ??= MonsterManager.Instance;
        monsterManager ??= FindAnyObjectByType<MonsterManager>();
        RegisterToManager();

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

        // 피라냐가 돌진 중이어도 플레이어가 행거를 잡는 즉시 공격을 취소합니다.
        if (movement.IsPiranha &&
            movement.IsAttacking &&
            IsPlayerHanging())
        {
            CancelPiranhaAttackForHangingPlayer();
        }

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleFrogWallCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // 이미 벽에 붙어 있는 상태에서 방향만 바뀐 경우에도
        // Enter가 다시 발생하지 않을 수 있으므로 Stay에서도 보완합니다.
        HandleFrogWallCollision(collision);
    }

    private void HandleFrogWallCollision(Collision2D collision)
    {
        if (isDead ||
            movement == null ||
            movement.Type != MonsterType.Frog ||
            collision == null)
        {
            return;
        }

        foreach (ContactPoint2D contact in collision.contacts)
        {
            // 바닥 접촉(normal.y 중심)은 무시하고,
            // 레이어·상태와 관계없이 옆면 접촉은 모두 장애물로 처리합니다.
            if (Mathf.Abs(contact.normal.x) < 0.2f)
                continue;

            int awayDirection =
                contact.normal.x > 0f ? 1 : -1;

            ForceFrogTurnFromCollision(
                awayDirection,
                collision.gameObject);

            return;
        }
    }

    private void ForceFrogTurnFromCollision(
        int awayDirection,
        GameObject obstacle)
    {
        awayDirection = awayDirection >= 0 ? 1 : -1;

        moveDirection = awayDirection;

        float lockUntil =
            Time.time + frogCollisionTurnLockDuration;

        frogCollisionTurnLockUntil =
            Mathf.Max(frogCollisionTurnLockUntil, lockUntil);

        wallAvoidanceUntil =
            Mathf.Max(wallAvoidanceUntil, lockUntil);

        lastPatrolTurnTime = Time.time;

        // 공격 모션이 벽 방향 속도를 계속 덮어쓰는 경우를 차단합니다.
        if (movement.IsAttacking)
            movement.CancelAttackMotion();

        if (rb != null)
        {
            float pushSpeed =
                frogCollisionPushSpeed * moveDirection;

            rb.linearVelocity = new Vector2(
                pushSpeed,
                rb.linearVelocity.y);

            if (frogWallSeparationDistance > 0f)
            {
                rb.position +=
                    Vector2.right *
                    moveDirection *
                    frogWallSeparationDistance;
            }
        }

        UpdateFacing();

        if (logFrogCollisionTurn)
        {
            string obstacleName =
                obstacle != null ? obstacle.name : "Unknown";

            Debug.Log(
                $"{name}: 개구리 옆면 충돌 → " +
                $"방향 {moveDirection}, 대상 {obstacleName}");
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

        // 행거에 매달린 플레이어는 피라냐의 공격 대상에서 제외합니다.
        if (IsPlayerHanging())
            return;

        if (movement.CanPiranhaEngage(player))
            TryStartPiranhaAttack();
    }

    private bool TryStartPiranhaAttack()
    {
        if (!movement.IsPiranha ||
            player == null ||
            movement.IsAttacking ||
            IsPlayerHanging() ||
            Time.time - lastAttackTime < attackCooldown)
        {
            return false;
        }

        PlayerHealth target = FindPlayerHealth(player);

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

        bool moved = TryMoveSafely(patrolSpeed, true);

        if (movement.Type == MonsterType.Piranha) {
            visual.SetState(MonsterVisualState.VerticalMove);
            visual.SetVerticalDirection(movement.VerticalDirection);
            return;
        }

        visual.SetState(
            moved && IsActuallyMoving()
                ? MonsterVisualState.Move
                : MonsterVisualState.Idle);
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

        if (player == null) {
            visual.SetState(MonsterVisualState.Idle);
            return;
        }

        FacePlayer();
        bool moved = TryMoveSafely(chaseSpeed, false);

        visual.SetState(
            moved && IsActuallyMoving()
                ? MonsterVisualState.Move
                : MonsterVisualState.Idle);
    }

    private void UpdateSearch() {
        SetStateIcons(false, false);

        bool moved = TryMoveSafely(patrolSpeed, true);
        visual.SetState(
            moved && IsActuallyMoving()
                ? MonsterVisualState.Move
                : MonsterVisualState.Idle);

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
        bool frogDirectionLocked =
            movement.Type == MonsterType.Frog &&
            Time.time < frogCollisionTurnLockUntil;

        if (Time.time >= wallAvoidanceUntil &&
            !frogDirectionLocked)
        {
            moveDirection =
                transform.position.x >= player.position.x ? 1 : -1;
        }

        bool moved = TryMoveSafely(runAwaySpeed, false);
        visual.SetState(
            moved ? MonsterVisualState.Move : MonsterVisualState.Idle);
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

    /// <summary>
    /// 진행 방향 앞쪽에 바닥이 있는지 작은 원 범위로 검사합니다.
    /// 한 줄 Raycast보다 개별 타일 사이의 미세한 틈에 덜 민감합니다.
    /// </summary>
    private bool HasGroundAhead(float lookAhead)
    {
        Vector2 origin = GetGroundProbeOrigin(lookAhead);

        RaycastHit2D hit = Physics2D.CircleCast(
            origin,
            groundCheckRadius,
            Vector2.down,
            groundCheckDistance,
            groundLayer);

        return hit.collider != null;
    }

    private Vector2 GetGroundProbeOrigin(float lookAhead)
    {
        if (groundCheck != null)
        {
            return (Vector2)groundCheck.position +
                   Vector2.right * moveDirection * lookAhead;
        }

        if (bodyCollider == null)
            bodyCollider = FindBodyCollider();

        if (bodyCollider != null)
        {
            Bounds bounds = bodyCollider.bounds;

            return new Vector2(
                bounds.center.x +
                moveDirection * (bounds.extents.x + lookAhead),
                bounds.min.y + groundCheckRadius + 0.02f);
        }

        return (Vector2)transform.position +
               new Vector2(moveDirection * lookAhead, -0.2f);
    }

    private bool TryMoveGhostWithinPlatform(float speed)
    {
        if (bodyCollider == null)
            bodyCollider = FindBodyCollider();

        float halfWidth =
            bodyCollider != null
                ? bodyCollider.bounds.extents.x
                : 0.2f;

        Vector2 probeOrigin = new Vector2(
            transform.position.x +
            moveDirection *
            (halfWidth + ghostEdgeLookAhead),
            bodyCollider != null
                ? bodyCollider.bounds.center.y
                : transform.position.y);

        RaycastHit2D hit = Physics2D.CircleCast(
            probeOrigin,
            ghostGroundProbeRadius,
            Vector2.down,
            ghostGroundProbeDistance,
            groundLayer);

        if (hit.collider == null)
        {
            if (Time.time - lastPatrolTurnTime >= patrolTurnCooldown)
            {
                TurnAround();
                lastPatrolTurnTime = Time.time;

                if (logGhostEdgeTurn)
                {
                    Debug.Log(
                        $"{name}: 유령 발판 끝 감지 → 방향 {moveDirection}");
                }
            }

            return false;
        }

        movement.Move(moveDirection, speed);
        return true;
    }

    private bool ShouldFrogTurnBeforeMove(out string reason)
    {
        reason = string.Empty;

        if (movement == null ||
            movement.Type != MonsterType.Frog ||
            !movement.IsFrogGrounded())
        {
            return false;
        }

        if (HasFrogObstacleAhead())
        {
            reason = "앞쪽 장애물";
            return true;
        }

        if (!HasGroundAhead(frogEdgeLookAhead))
        {
            reason = "발판 끝";
            return true;
        }

        return false;
    }

    private bool HasFrogObstacleAhead()
    {
        if (bodyCollider == null)
            bodyCollider = FindBodyCollider();

        if (bodyCollider == null)
            return false;

        Bounds bounds = bodyCollider.bounds;

        Vector2 origin = new Vector2(
            bounds.center.x +
            moveDirection * (bounds.extents.x + 0.01f),
            bounds.center.y + bounds.extents.y * 0.12f);

        Vector2 size = new Vector2(
            0.04f,
            Mathf.Max(
                0.08f,
                bounds.size.y * frogForwardCheckHeightRatio));

        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            origin,
            size,
            0f,
            Vector2.right * moveDirection,
            frogForwardCheckDistance,
            groundLayer);

        foreach (RaycastHit2D hit in hits)
        {
            Collider2D hitCollider = hit.collider;

            if (hitCollider == null ||
                hitCollider.isTrigger ||
                hitCollider.transform == transform ||
                hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void ForceFrogPredictiveTurn(string reason)
    {
        if (Time.time < frogCollisionTurnLockUntil)
            return;

        int newDirection = -moveDirection;

        moveDirection = newDirection;

        float lockUntil =
            Time.time + frogCollisionTurnLockDuration;

        frogCollisionTurnLockUntil = lockUntil;
        wallAvoidanceUntil =
            Mathf.Max(wallAvoidanceUntil, lockUntil);
        lastPatrolTurnTime = Time.time;

        if (movement.IsAttacking)
            movement.CancelAttackMotion();

        StopHorizontalMotion();

        if (rb != null &&
            frogWallSeparationDistance > 0f)
        {
            rb.position +=
                Vector2.right *
                moveDirection *
                frogWallSeparationDistance;
        }

        UpdateFacing();

        if (logFrogPredictiveTurn)
        {
            Debug.Log(
                $"{name}: 개구리 선제 방향 전환 " +
                $"({reason}) → 방향 {moveDirection}");
        }
    }

    private bool HasWallAhead()
    {
        if (wallCheckDistance <= 0f)
            return false;

        if (bodyCollider == null)
            bodyCollider = FindBodyCollider();

        Vector2 direction = Vector2.right * moveDirection;
        Vector2 origin;
        Vector2 probeSize;

        if (bodyCollider != null)
        {
            Bounds bounds = bodyCollider.bounds;

            // 몸체 바로 바깥에서 시작하므로 자기 Collider를 감지하지 않습니다.
            origin = new Vector2(
                bounds.center.x +
                moveDirection * (bounds.extents.x + 0.015f),
                bounds.center.y + bounds.extents.y * 0.05f);

            probeSize = new Vector2(
                0.04f,
                Mathf.Max(0.08f, bounds.size.y * wallCheckHeightRatio));
        }
        else
        {
            origin = wallCheck != null
                ? (Vector2)wallCheck.position
                : (Vector2)transform.position +
                  new Vector2(moveDirection * 0.25f, 0f);

            probeSize = new Vector2(0.04f, 0.2f);
        }

        RaycastHit2D hit = Physics2D.BoxCast(
            origin,
            probeSize,
            0f,
            direction,
            wallCheckDistance,
            groundLayer);

        return hit.collider != null;
    }

    /// <summary>
    /// 플랫폼 끝이나 벽을 확인한 뒤 안전하게 이동합니다.
    /// 순찰/탐색에서는 방향을 전환하고, 추적/도망에서는 장애물 앞에서 멈춥니다.
    /// </summary>
    private bool TryMoveSafely(float speed, bool turnAtEdge)
    {
        // 유령은 일반 GroundCheck를 사용하지 않고,
        // 진행 방향 앞쪽에서 아래로 긴 CircleCast를 내려
        // 연속된 발판 위에서만 이동합니다.
        if (movement.Type == MonsterType.Ghost)
        {
            return TryMoveGhostWithinPlatform(speed);
        }

        if (!movement.UsesGroundObstacleCheck)
        {
            movement.Move(moveDirection, speed);
            return true;
        }

        bool blockedByWall = HasWallAhead();

        // 벽은 상태와 관계없이 반드시 반대 방향으로 회피합니다.
        // Chase/RunAway에서도 플레이어 방향 갱신이 잠시 잠겨서
        // 같은 벽을 향해 즉시 다시 돌아서는 현상을 막습니다.
        if (blockedByWall)
        {
            StopHorizontalMotion();
            TurnAwayFromWall();
            return false;
        }

        // 개구리는 방향 전환 직후 잠금 시간 동안
        // 새 방향으로 실제 이동/점프할 기회를 먼저 줍니다.
        // 이전 코드는 잠금 중에도 매 프레임 return false가 되어
        // 방향만 바뀌고 영원히 점프하지 못할 수 있었습니다.
        if (movement.Type == MonsterType.Frog)
        {
            if (Time.time < frogCollisionTurnLockUntil)
            {
                movement.Move(moveDirection, speed);
                return true;
            }

            if (ShouldFrogTurnBeforeMove(out string reason))
            {
                ForceFrogPredictiveTurn(reason);
                return false;
            }

            movement.Move(moveDirection, speed);
            return true;
        }

        float lookAhead =
            Mathf.Max(0.05f, speed * Time.fixedDeltaTime);

        bool hasGroundAhead = HasGroundAhead(lookAhead);

        if (hasGroundAhead && !blockedByWall)
        {
            movement.Move(moveDirection, speed);
            return true;
        }

        StopHorizontalMotion();

        if (turnAtEdge)
            TryTurnAround();

        return false;
    }

    private bool IsActuallyMoving()
    {
        if (rb == null)
            return false;

        if (movement != null &&
            movement.Type == MonsterType.Frog)
        {
            return Mathf.Abs(rb.linearVelocity.x) > 0.02f ||
                   Mathf.Abs(rb.linearVelocity.y) > 0.02f;
        }

        return Mathf.Abs(rb.linearVelocity.x) > 0.02f;
    }

    private void FacePlayer() {
        if (player == null ||
            Time.time < wallAvoidanceUntil ||
            (movement != null &&
             movement.Type == MonsterType.Frog &&
             Time.time < frogCollisionTurnLockUntil) ||
            Time.time - lastTurnTime < turnDelay)
        {
            return;
        }
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
        UpdateFacing();
    }

    private bool TryTurnAround()
    {
        if (Time.time - lastPatrolTurnTime < patrolTurnCooldown)
            return false;

        TurnAround();
        lastPatrolTurnTime = Time.time;
        return true;
    }

    private void TurnAround()
    {
        moveDirection *= -1;
        StopHorizontalMotion();
        UpdateFacing();
    }

    private void TurnAwayFromWall()
    {
        ForceDirectionAwayFromWall(-moveDirection);
    }

    private void ForceDirectionAwayFromWall(int direction)
    {
        direction = direction >= 0 ? 1 : -1;

        if (moveDirection == direction &&
            Time.time < wallAvoidanceUntil)
        {
            return;
        }

        moveDirection = direction;
        wallAvoidanceUntil =
            Time.time + Mathf.Max(0.05f, wallAvoidanceDuration);
        lastPatrolTurnTime = Time.time;

        StopHorizontalMotion();

        if (movement != null &&
            movement.Type == MonsterType.Frog &&
            rb != null &&
            frogWallSeparationDistance > 0f)
        {
            rb.position +=
                Vector2.right *
                moveDirection *
                frogWallSeparationDistance;
        }

        UpdateFacing();
    }

    private void StopHorizontalMotion()
    {
        movement.Stop();

        // 개구리는 공중에서 MonsterMovement.Stop()이 X 속도를 유지할 수 있으므로
        // Rigidbody의 수평 속도도 직접 제거합니다.
        if (rb != null)
        {
            rb.linearVelocity =
                new Vector2(0f, rb.linearVelocity.y);
        }
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

    private Collider2D FindBodyCollider()
    {
        foreach (Collider2D candidate in
                 GetComponentsInChildren<Collider2D>(true))
        {
            if (candidate != null && !candidate.isTrigger)
                return candidate;
        }

        return null;
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
        CachePlayerController();
    }

    private void CachePlayerController() {
        playerController = null;

        if (player == null)
            return;

        playerController =
            player.GetComponent<Project.Player.PlayerController2D>() ??
            player.GetComponentInChildren<Project.Player.PlayerController2D>(true) ??
            player.GetComponentInParent<Project.Player.PlayerController2D>();
    }

    private bool IsPlayerHanging() {
        if (!blockPiranhaAttackWhilePlayerHanging || player == null)
            return false;

        if (playerController == null)
            CachePlayerController();

        return playerController != null && playerController.isHanging;
    }

    private static PlayerHealth FindPlayerHealth(Transform targetRoot) {
        if (targetRoot == null)
            return null;

        return targetRoot.GetComponent<PlayerHealth>() ??
               targetRoot.GetComponentInChildren<PlayerHealth>(true) ??
               targetRoot.GetComponentInParent<PlayerHealth>();
    }

    private void CancelPiranhaAttackForHangingPlayer() {
        if (!movement.IsPiranha || !movement.IsAttacking)
            return;

        movement.CancelAttackMotion();
        movement.ConsumePiranhaAttack();
        BeginPiranhaReturn();

        if (logBlockedPiranhaAttack)
            Debug.Log($"{name}: 플레이어가 행거에 매달려 피라냐 공격 취소");
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

        // 공격 시작 이후 플레이어가 행거를 잡은 경우에도 피라냐 피해를 막습니다.
        if (movement.IsPiranha && IsPlayerHanging())
        {
            if (logBlockedPiranhaAttack)
                Debug.Log($"{name}: 행거 상태라 피라냐 피해 무효");

            return;
        }

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

        effect.transform.localScale =
            attackHitEffectPrefab.transform.localScale *
            attackHitEffectScale;

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

    private void SyncElementFromFillColor()
    {
        if (currentElement != ElementType.None ||
            fillColor == null ||
            !fillColor.HasColor)
        {
            return;
        }

        Color color = fillColor.CurrentColor;

        Debug.Log(
            $"{name} 흡수 색상: " +
            $"R={color.r:F3}, G={color.g:F3}, B={color.b:F3}, A={color.a:F3}");

        if (TryResolveElement(color, out ElementType element))
        {
            Debug.Log($"{name} 변환 성공: {element}");
            SetElement(element);
        }
        else
        {
            Debug.LogWarning(
                $"{name} ElementType 변환 실패 / tolerance={colorTolerance}");
        }
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
        element = ElementType.None;
        float nearestDistance = float.MaxValue;

        SelectNearestElement(
            color,
            ElementType.Red,
            redElementColor,
            ref element,
            ref nearestDistance);

        SelectNearestElement(
            color,
            ElementType.Blue,
            blueElementColor,
            ref element,
            ref nearestDistance);

        SelectNearestElement(
            color,
            ElementType.Yellow,
            yellowElementColor,
            ref element,
            ref nearestDistance);

        SelectNearestElement(
            color,
            ElementType.Green,
            greenElementColor,
            ref element,
            ref nearestDistance);

        SelectNearestElement(
            color,
            ElementType.Purple,
            purpleElementColor,
            ref element,
            ref nearestDistance);

        if (nearestDistance <= colorTolerance)
            return true;

        element = ElementType.None;
        return false;
    }

    private static void SelectNearestElement(
        Color source,
        ElementType candidateElement,
        Color candidateColor,
        ref ElementType nearestElement,
        ref float nearestDistance)
    {
        float distance = ColorDistance(source, candidateColor);

        if (distance >= nearestDistance)
            return;

        nearestDistance = distance;
        nearestElement = candidateElement;
    }

    private Color GetElementColor(ElementType element) {
        switch (element) {
            case ElementType.Red: return redElementColor;
            case ElementType.Blue: return blueElementColor;
            case ElementType.Yellow: return yellowElementColor;
            case ElementType.Green: return greenElementColor;
            case ElementType.Purple: return purpleElementColor;
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
            case ElementType.Green:
                return GetDropOrDefault(greenDropPrefab, "Green");
            case ElementType.Purple:
                return GetDropOrDefault(purpleDropPrefab, "Purple");
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
            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundCheckRadius);
            Gizmos.DrawLine(
                groundCheck.position,
                groundCheck.position +
                Vector3.down * groundCheckDistance);
        }
        if (wallCheck != null || bodyCollider != null) {
            Gizmos.color = Color.magenta;

            Collider2D colliderForGizmo =
                bodyCollider != null ? bodyCollider : FindBodyCollider();

            if (colliderForGizmo != null)
            {
                Bounds bounds = colliderForGizmo.bounds;
                Vector3 origin = new Vector3(
                    bounds.center.x +
                    moveDirection * (bounds.extents.x + 0.015f),
                    bounds.center.y + bounds.extents.y * 0.05f,
                    transform.position.z);

                Vector3 size = new Vector3(
                    0.04f,
                    Mathf.Max(0.08f, bounds.size.y * wallCheckHeightRatio),
                    0f);

                Gizmos.DrawWireCube(origin, size);
                Gizmos.DrawLine(
                    origin,
                    origin +
                    Vector3.right * moveDirection * wallCheckDistance);
            }
            else if (wallCheck != null)
            {
                Gizmos.DrawLine(
                    wallCheck.position,
                    wallCheck.position +
                    Vector3.right * moveDirection * wallCheckDistance);
            }
        }

        MonsterMovement previewMovement =
            movement != null ? movement : GetComponent<MonsterMovement>();

        Collider2D previewCollider =
            bodyCollider != null ? bodyCollider : FindBodyCollider();

        if (previewMovement != null &&
            previewMovement.Type == MonsterType.Ghost)
        {
            float halfWidth =
                previewCollider != null
                    ? previewCollider.bounds.extents.x
                    : 0.2f;

            Vector3 ghostProbeOrigin = new Vector3(
                transform.position.x +
                moveDirection *
                (halfWidth + ghostEdgeLookAhead),
                previewCollider != null
                    ? previewCollider.bounds.center.y
                    : transform.position.y,
                transform.position.z);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(
                ghostProbeOrigin,
                ghostGroundProbeRadius);
            Gizmos.DrawLine(
                ghostProbeOrigin,
                ghostProbeOrigin +
                Vector3.down * ghostGroundProbeDistance);
        }

        Collider2D frogCollider =
            bodyCollider != null ? bodyCollider : FindBodyCollider();

        if (previewMovement != null &&
            previewMovement.Type == MonsterType.Frog &&
            frogCollider != null)
        {
            Bounds bounds = frogCollider.bounds;

            Vector3 obstacleOrigin = new Vector3(
                bounds.center.x +
                moveDirection * (bounds.extents.x + 0.01f),
                bounds.center.y + bounds.extents.y * 0.12f,
                transform.position.z);

            Vector3 obstacleSize = new Vector3(
                0.04f,
                Mathf.Max(
                    0.08f,
                    bounds.size.y * frogForwardCheckHeightRatio),
                0f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(obstacleOrigin, obstacleSize);
            Gizmos.DrawLine(
                obstacleOrigin,
                obstacleOrigin +
                Vector3.right *
                moveDirection *
                frogForwardCheckDistance);

            Vector3 edgeOrigin = new Vector3(
                bounds.center.x +
                moveDirection *
                (bounds.extents.x + frogEdgeLookAhead),
                bounds.min.y + groundCheckRadius + 0.02f,
                transform.position.z);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(
                edgeOrigin,
                groundCheckRadius);
            Gizmos.DrawLine(
                edgeOrigin,
                edgeOrigin +
                Vector3.down * groundCheckDistance);
        }
    }

    #endregion
}
