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
    private const string PlayerTag = "Player";
    private const string PaletteIconName = "PaletteIcon";

    #region Fields
    [Header("상태 및 체력")]
    [SerializeField] private MonsterState currentState = MonsterState.Patrol;
    [SerializeField, Min(1)] private int maxHp = 3;

    [Header("밸런스 프로필")]
    [Tooltip("연결하면 최대 HP를 제외한 이동·감지·공격 수치를 타입별로 일괄 적용합니다.")]
    [SerializeField] private MonsterBalanceProfile balanceProfile;
    [SerializeField] private bool applyBalanceProfileOnAwake = true;

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

    [Header("이동 안전")]
    [Tooltip("발판과 벽이 사용하는 레이어입니다. 실제 검사는 MonsterMovement 한 곳에서 담당합니다.")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("장애물을 만나 방향을 바꾼 직후 플레이어 방향이 다시 덮어쓰지 않도록 유지하는 시간입니다.")]
    [SerializeField, Min(0f)] private float obstacleTurnLockDuration = 0.6f;

    [Header("공격")]
    [SerializeField, Min(1)] private int attackDamage = 1;
    [SerializeField, Min(0f)] private float attackCooldown = 1.5f;
    [SerializeField] private MonsterAttackTrigger attackTrigger;

    [Header("슬라임 원거리 공격")]
    [Tooltip("Slime 타입에서 연결되면 공통 몸통 박치기 대신 포물선 점액 공격을 사용합니다.")]
    [SerializeField] private SlimeRangedAttack slimeRangedAttack;
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

    [Header("무색 몬스터 회복 드롭")]
    [Tooltip("ElementType.None 상태로 죽은 몬스터가 드롭할 회복 아이템입니다.")]
    [SerializeField] private GameObject healthDropPrefab;
    [SerializeField, Range(0f, 1f)] private float colorlessHealthDropChance = 1f;
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
    private ColorMinus colorMinus;
    private int currentHp;
    private int moveDirection = 1;
    private float startX;
    private float lastPatrolTurnTime;
    private float obstacleTurnLockUntil;
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

    private bool IsAnyAttackRunning =>
        movement != null && movement.IsAttacking ||
        slimeRangedAttack != null && slimeRangedAttack.IsAttacking;

    #endregion

    #region Public API

    public MonsterState CurrentState => currentState;
    public ElementType CurrentElement => currentElement;
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDead => isDead;
    public bool HasPaletteItem => hasPaletteItem;
    public MonsterType Type =>
        movement != null ? movement.Type : MonsterType.Slime;
    public bool IsColorless =>
        currentElement == ElementType.None &&
        (colorMinus == null || !colorMinus.IsAbsorbed);
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<MonsterMovement>();
        visual = GetComponent<MonsterVisual>();
        bodyCollider = FindBodyCollider();
        colorMinus ??= GetComponentInChildren<ColorMinus>(true);
        fillColor ??= GetComponent<FillColor>();
        attackTrigger ??= GetComponentInChildren<MonsterAttackTrigger>(true);
        slimeRangedAttack ??= GetComponent<SlimeRangedAttack>();

        ApplyBalanceProfileIfAvailable();
        ConfigureMonsterHitAudio();

        currentHp = maxHp;
        basePatrolSpeed = patrolSpeed;
        baseChaseSpeed = chaseSpeed;
        baseRunAwaySpeed = runAwaySpeed;

        rb.freezeRotation = true;

        ResolvePaletteIcon();
        HideAllIcons();
    }

    private void Start()
    {
        if (player == null)
            FindPlayer();
        else
            CachePlayerController();

        monsterManager ??= MonsterManager.Instance;
        monsterManager ??= FindAnyObjectByType<MonsterManager>();
        RegisterToManager();

        // 발판 끝, 벽, 유령과 개구리 안전 검사는 MonsterMovement만 담당합니다.
        movement.InitializePlatformConstraint(
            groundLayer,
            0.6f);

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

    private void Update()
    {
        if (isDead)
            return;

        SyncElementFromFillColor();

        if (movement.IsPiranha &&
            movement.IsAttacking &&
            IsPlayerHanging())
        {
            CancelPiranhaAttackForHangingPlayer();
        }

        // 공격 중에는 거리 판정이 상태를 덮어쓰지 않습니다.
        if (!IsAnyAttackRunning &&
            movement.UsesPlayerTracking &&
            !movement.IsPiranha)
        {
            UpdatePlayerState();
        }

        UpdateFacing();
    }

    private void FixedUpdate()
    {
        if (isDead || IsAnyAttackRunning)
            return;

        if (isHit && stopWhileHit)
        {
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

    private void OnDisable()
    {
        slimeRangedAttack?.CancelAttack();

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
        bool canEnterAttackState = CanEnterAttackState(distance);

        currentState = canEnterAttackState
            ? MonsterState.Attack
            : MonsterState.Chase;
    }

    private bool CanEnterAttackState(float distanceToPlayer)
    {
        // 슬라임은 원거리 공격이므로 공격 거리 안에 들어오면 Attack 상태 진입.
        if (movement.Type == MonsterType.Slime &&
            slimeRangedAttack != null &&
            slimeRangedAttack.IsConfigured)
        {
            return distanceToPlayer <= attackRange;
        }

        // 근접 몬스터는 실제 공격 Trigger 안에 플레이어가 들어왔을 때만
        // Attack 상태로 전환합니다.
        // 단순히 attackRange 안에 들어왔다는 이유만으로 멈추면,
        // Trigger에는 아직 닿지 않은 상태에서 제자리 정지할 수 있습니다.
        return attackTrigger != null &&
               attackTrigger.HasTarget;
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
        bool moved = TryMoveSafely(chaseSpeed, true);

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

    private void UpdateAttack()
    {
        SetStateIcons(false, false);

        if (player == null)
        {
            EnterPatrol();
            return;
        }

        float distance = Vector2.Distance(
            transform.position,
            player.position);

        if (!CanEnterAttackState(distance))
        {
            currentState = MonsterState.Chase;
            return;
        }

        FacePlayer();
        movement.Stop();

        // 실제 공격 중일 때만 공격 자세 유지
        if (IsAnyAttackRunning)
        {
            visual.SetState(
                MonsterVisualState.Attack);
            return;
        }

        float adjustedCooldown =
            attackCooldown *
            movement.AttackCooldownMultiplier;

        if (Time.time - lastAttackTime <
            adjustedCooldown)
        {
            visual.SetState(
                MonsterVisualState.Idle);
            return;
        }

        bool started = TryStartAttack();

        visual.SetState(
            started
                ? MonsterVisualState.Attack
                : MonsterVisualState.Idle);
    }

    private void UpdateRunAway()
    {
        if (!CanRunAway || player == null)
        {
            EnterPatrol();
            return;
        }

        SetStateIcons(false, true);
        runAwayTimer += Time.fixedDeltaTime;

        float distance = Vector2.Distance(
            transform.position,
            player.position);

        bool reachedDistance =
            runAwayDistance > 0f &&
            distance >= runAwayDistance;

        bool reachedDuration =
            runAwayDuration > 0f &&
            runAwayTimer >= runAwayDuration;

        if (reachedDistance || reachedDuration)
        {
            EnterPatrol();
            return;
        }

        if (Time.time >= obstacleTurnLockUntil)
        {
            moveDirection =
                transform.position.x >= player.position.x
                    ? 1
                    : -1;
        }

        bool moved = TryMoveSafely(runAwaySpeed, true);

        visual.SetState(
            moved && IsActuallyMoving()
                ? MonsterVisualState.Move
                : MonsterVisualState.Idle);
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
    /// 이동 가능 여부는 MonsterMovement가 판단하고,
    /// 이 클래스는 막혔을 때 방향만 전환합니다.
    /// </summary>
    private bool TryMoveSafely(float speed, bool turnWhenBlocked)
    {
        MonsterMoveResult result =
            movement.TryMoveSafely(
                moveDirection,
                speed,
                true);

        if (result == MonsterMoveResult.Moved)
            return true;

        if (result == MonsterMoveResult.Waiting)
            return IsActuallyMoving();

        movement.Stop();

        if (!turnWhenBlocked ||
            Time.time < obstacleTurnLockUntil ||
            !TryTurnAround())
        {
            return false;
        }

        obstacleTurnLockUntil =
            Time.time + obstacleTurnLockDuration;

        // 개구리는 발판 끝에서 방향만 바꾸고 멈추지 않도록
        // 반대쪽이 안전하면 같은 FixedUpdate에 바로 점프를 시도합니다.
        if (movement.Type == MonsterType.Frog)
        {
            MonsterMoveResult retry =
                movement.TryMoveSafely(
                    moveDirection,
                    speed,
                    true);

            return retry == MonsterMoveResult.Moved;
        }

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

    private void FacePlayer()
    {
        if (player == null ||
            Time.time < obstacleTurnLockUntil ||
            Time.time - lastTurnTime < turnDelay)
        {
            return;
        }

        float deltaX =
            player.position.x - transform.position.x;

        if (Mathf.Abs(deltaX) <= DirectionThreshold)
            return;

        moveDirection = deltaX > 0f ? 1 : -1;
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
        movement.Stop();
        UpdateFacing();
    }

    private void UpdateFacing()
    {
        visual.SetDirection(moveDirection);
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



    private void FindPlayer()
    {
        GameObject target =
            GameObject.FindGameObjectWithTag(PlayerTag);

        if (target == null)
        {
            Debug.LogWarning(
                $"{name}: Player 태그 오브젝트를 찾지 못했습니다.");
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

    private bool TryStartAttack()
    {
        if (IsAnyAttackRunning)
            return false;

        float adjustedCooldown =
            attackCooldown * movement.AttackCooldownMultiplier;

        if (Time.time - lastAttackTime < adjustedCooldown)
            return false;

        // 슬라임은 근접 공격 가능 여부와 AttackTrigger를 검사하지 않습니다.
        // 원거리 공격 컴포넌트와 거리만 확인합니다.
        if (movement.Type == MonsterType.Slime &&
            slimeRangedAttack != null &&
            slimeRangedAttack.IsConfigured)
        {
            if (player == null)
                return false;

            PlayerHealth rangedTarget =
                FindPlayerHealth(player);

            if (rangedTarget == null ||
                rangedTarget.IsDead)
            {
                return false;
            }

            float distance = Vector2.Distance(
                transform.position,
                rangedTarget.transform.position);

            if (distance > attackRange)
                return false;

            FaceTargetImmediately(
                rangedTarget.transform);

            bool rangedStarted =
                slimeRangedAttack.TryStartAttack(
                    rangedTarget.transform,
                    attackDamage,
                    OnAttackMotionComplete);

            if (!rangedStarted)
                return false;

            lastAttackTime = Time.time;
            visual.SetState(
                MonsterVisualState.Attack);

            Debug.Log(
                $"{name}: 슬라임 원거리 공격 시작 / " +
                $"거리={distance:F2}, 사거리={attackRange:F2}",
                this);

            return true;
        }

        // 여기부터 근접 공격 몬스터 전용 검사
        if (!movement.CanAttackPlayer)
            return false;

        if (attackTrigger == null ||
            !attackTrigger.TryGetTarget(
                out PlayerHealth target))
        {
            return false;
        }

        FaceTargetImmediately(target.transform);

        bool started =
            movement.TryStartAttackMotion(
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

        SoundManager.Instance?.PlaySFX(SFXType.Hit);

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
        SoundManager.Instance?.PlaySFX(SFXType.MonsterDead);
        Debug.Log($"{name}: 몬스터 사망");
        if (deadSpriteTime > 0f)
            yield return new WaitForSeconds(deadSpriteTime);
        if (deadFadeDuration > 0f)
            yield return FadeOutSprites();
        DropItem();
        Destroy(gameObject);
    }

    private void PrepareDeath()
    {
        StopHitRoutine();
        slimeRangedAttack?.CancelAttack();
        movement.CancelAttackMotion();
        movement.Stop();

        HideAllIcons();
        visual.PlayDead();

        foreach (MonsterAttackTrigger trigger in
                 GetComponentsInChildren<MonsterAttackTrigger>(true))
        {
            trigger.enabled = false;
        }

        foreach (Collider2D collider in
                 GetComponentsInChildren<Collider2D>(true))
        {
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

    private void DropItem()
    {
        // 팔레트 보유 몬스터는 다른 드롭보다 팔레트 아이템이 우선입니다.
        if (hasPaletteItem)
        {
            if (paletteItemPrefab == null)
            {
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

        // 타일 색을 한 번도 흡수하지 않은 몬스터는 회복 아이템을 드롭합니다.
        if (IsColorless)
        {
            if (Random.value > colorlessHealthDropChance)
                return;

            if (healthDropPrefab == null)
            {
                Debug.LogWarning(
                    $"{name}: 무색 몬스터용 Health Pickup Prefab이 없습니다.");
                return;
            }

            Instantiate(
                healthDropPrefab,
                transform.position,
                Quaternion.identity);

            Debug.Log($"{name}: 무색 몬스터 회복 아이템 드롭");
            return;
        }

        if (Random.value > paintDropChance)
            return;

        GameObject paintPrefab = GetDropPrefab();

        if (paintPrefab == null)
        {
            Debug.LogWarning(
                $"{name}: {currentElement} 물감 드롭 프리팹이 없습니다.");
            return;
        }

        Instantiate(
            paintPrefab,
            transform.position,
            Quaternion.identity);

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



    private void ApplyBalanceProfileIfAvailable()
    {
        if (!applyBalanceProfileOnAwake ||
            balanceProfile == null ||
            movement == null ||
            !balanceProfile.TryGet(
                movement.Type,
                out MonsterBalanceProfile.Entry entry))
        {
            return;
        }

        patrolSpeed = entry.PatrolSpeed;
        chaseSpeed = entry.ChaseSpeed;
        detectRange = entry.DetectRange;
        attackRange = entry.AttackRange;
        attackDamage = entry.AttackDamage;
        attackCooldown = entry.AttackCooldown;
        canRunAway = entry.CanRunAway;
        runAwayHp = Mathf.Clamp(
            entry.RunAwayHp,
            0,
            Mathf.Max(0, maxHp - 1));
        runAwaySpeed = entry.RunAwaySpeed;
        runAwayDistance = entry.RunAwayDistance;
        runAwayDuration = entry.RunAwayDuration;
    }

    [ContextMenu("몬스터 타입 권장 밸런스 적용")]
    private void ApplyRecommendedBalance()
    {
        MonsterMovement targetMovement =
            movement != null
                ? movement
                : GetComponent<MonsterMovement>();

        if (targetMovement == null)
            return;

        switch (targetMovement.Type)
        {
            case MonsterType.Slime:
                patrolSpeed = 2.4f;
                chaseSpeed = 3f;
                detectRange = 5f;
                attackRange = 4f;
                attackDamage = 1;
                attackCooldown = 2.2f;
                canRunAway = false;
                break;

            case MonsterType.Snail:
                patrolSpeed = 2f;
                chaseSpeed = 2.4f;
                detectRange = 3.5f;
                attackRange = 0.75f;
                attackDamage = 1;
                attackCooldown = 2.4f;
                canRunAway = false;
                break;

            case MonsterType.Ghost:
                patrolSpeed = 3f;
                chaseSpeed = 4f;
                detectRange = 5f;
                attackRange = 1.2f;
                attackDamage = 1;
                attackCooldown = 1.5f;
                canRunAway = true;
                runAwaySpeed = 4.5f;
                break;

            case MonsterType.Spider:
                patrolSpeed = 4f;
                chaseSpeed = 5f;
                detectRange = 5.5f;
                attackRange = 0.9f;
                attackDamage = 1;
                attackCooldown = 1.1f;
                canRunAway = true;
                runAwaySpeed = 5.5f;
                break;

            case MonsterType.Frog:
                patrolSpeed = 2.5f;
                chaseSpeed = 3f;
                detectRange = 4.5f;
                attackRange = 1.25f;
                attackDamage = 1;
                attackCooldown = 2f;
                canRunAway = false;
                break;

            case MonsterType.Piranha:
                detectRange = 3f;
                attackRange = 1.2f;
                attackDamage = 1;
                attackCooldown = 2f;
                canRunAway = false;
                break;
        }

        runAwayHp = Mathf.Clamp(
            runAwayHp,
            0,
            Mathf.Max(0, maxHp - 1));

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void OnValidate()
    {
        maxHp = Mathf.Max(1, maxHp);
        runAwayHp = Mathf.Clamp(
            runAwayHp,
            0,
            Mathf.Max(0, maxHp - 1));
        patrolSpeed = Mathf.Max(0f, patrolSpeed);
        chaseSpeed = Mathf.Max(0f, chaseSpeed);
        detectRange = Mathf.Max(0f, detectRange);
        attackRange = Mathf.Max(0f, attackRange);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        patrolRange = Mathf.Max(0f, patrolRange);
        obstacleTurnLockDuration = Mathf.Max(0f, obstacleTurnLockDuration);
        colorlessHealthDropChance = Mathf.Clamp01(colorlessHealthDropChance);
        paintDropChance = Mathf.Clamp01(paintDropChance);
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
            transform.position + Vector3.left * patrolRange,
            transform.position + Vector3.right * patrolRange);
    }

    #endregion
}
