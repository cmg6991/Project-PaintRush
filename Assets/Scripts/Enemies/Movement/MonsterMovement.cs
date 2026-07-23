using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MonsterMoveResult
{
    Moved,
    Waiting,
    BlockedByEdge,
    BlockedByObstacle
}

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class MonsterMovement : MonoBehaviour
{
    [System.Serializable]
    private struct AttackMotionSettings
    {
        [Min(0f)] public float windupTime;
        [Min(0f)] public float windupDistance;
        [Min(0.01f)] public float lungeTime;
        [Min(0f)] public float arcHeight;
        [Min(0f)] public float recoilTime;
        [Min(0f)] public float recoilDistance;

        public AttackMotionSettings(
            float windupTime,
            float windupDistance,
            float lungeTime,
            float arcHeight,
            float recoilTime,
            float recoilDistance)
        {
            this.windupTime = windupTime;
            this.windupDistance = windupDistance;
            this.lungeTime = lungeTime;
            this.arcHeight = arcHeight;
            this.recoilTime = recoilTime;
            this.recoilDistance = recoilDistance;
        }
    }

    [Header("몬스터 종류")]
    [SerializeField] private MonsterType monsterType;

    [Header("공통 이동")]
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float groundGravityScale = 1.5f;

    [Header("몬스터별 속도 배율")]
    [SerializeField, Min(0f)] private float slimeSpeedMultiplier = 0.45f;
    [SerializeField, Min(0f)] private float snailSpeedMultiplier = 0.3f;
    [SerializeField, Min(0f)] private float ghostSpeedMultiplier = 0.55f;
    [SerializeField, Min(0f)] private float spiderSpeedMultiplier = 0.75f;
    [SerializeField, Min(0f)] private float frogSpeedMultiplier = 0.55f;

    [Header("몬스터별 최대 이동 속도")]
    [Tooltip("기본 AI 속도가 지나치게 커도 몬스터 개성이 무너지지 않도록 상한을 둡니다.")]
    [SerializeField, Min(0.1f)] private float slimeMaxSpeed = 1.8f;
    [SerializeField, Min(0.1f)] private float snailMaxSpeed = 0.9f;
    [SerializeField, Min(0.1f)] private float ghostMaxSpeed = 2.4f;
    [SerializeField, Min(0.1f)] private float spiderMaxSpeed = 3.8f;
    [SerializeField, Min(0.1f)] private float frogMaxSpeed = 2f;

    [Header("유령 이동")]
    [SerializeField] private float ghostFloatHeight = 0.2f;
    [SerializeField] private float ghostFloatFrequency = 1.5f;
    [SerializeField] private float ghostVerticalCorrection = 4f;
    [SerializeField] private float ghostMaxVerticalSpeed = 1.2f;

    [Header("피라냐 이동")]
    [SerializeField] private float piranhaJumpHeight = 1.8f;
    [SerializeField] private float piranhaFrequency = 1.5f;

    [Header("피라냐 공격 조건")]
    [SerializeField, Range(0f, 1f)] private float piranhaEngageHeightRatio = 0.6f;
    [SerializeField, Min(0.1f)] private float piranhaEngageDistance = 2.5f;
    [SerializeField, Min(0.1f)] private float piranhaReturnSpeed = 3f;
    [SerializeField, Min(0.01f)] private float piranhaReturnThreshold = 0.08f;

    [Header("개구리 이동")]
    [SerializeField] private LayerMask frogGroundLayer;
    [SerializeField, Min(0.01f)] private float frogGroundCheckDistance = 0.18f;
    [SerializeField, Range(0.1f, 1f)] private float frogGroundCheckWidth = 0.7f;
    [SerializeField, Min(0f)] private float frogGroundedMaxUpSpeed = 0.05f;
    [SerializeField] private float frogJumpPower = 4f;
    [SerializeField] private float frogJumpInterval = 1.5f;

    [Header("플랫폼 이동 제한")]
    [Tooltip("진행 방향 앞쪽을 검사할 거리입니다.")]
    [SerializeField, Min(0.01f)] private float edgeForwardDistance = 0.12f;
    [Tooltip("앞쪽 바닥을 아래로 검사할 거리입니다.")]
    [SerializeField, Min(0.1f)] private float edgeDownDistance = 0.6f;
    [Tooltip("유령처럼 지면에서 떠 있는 몬스터의 아래쪽 검사 거리입니다.")]
    [SerializeField, Min(0.1f)] private float floatingEdgeDownDistance = 3f;
    [Tooltip("타일 사이의 작은 경계 틈을 낭떠러지로 오인하지 않도록 하는 반지름입니다.")]
    [SerializeField, Min(0.01f)] private float edgeProbeRadius = 0.08f;

    [Header("앞쪽 장애물 검사")]
    [SerializeField, Min(0.01f)] private float obstacleCheckDistance = 0.14f;
    [SerializeField, Range(0.1f, 1f)] private float obstacleCheckHeightRatio = 0.55f;

    [Header("개구리 안전 점프")]
    [Tooltip("점프 경로 아래의 연속된 바닥을 확인할 샘플 수입니다.")]
    [SerializeField, Range(2, 12)] private int frogGroundPathSamples = 6;
    [Tooltip("안전한 착지 경로가 없으면 수평 거리를 줄여 같은 발판 안에서 점프합니다.")]
    [SerializeField] private bool preventFrogFromLeavingPlatform = true;
    [Tooltip("안전한 수평 점프 거리를 찾을 때 속도를 몇 단계로 줄여 검사할지 설정합니다.")]
    [SerializeField, Range(2, 8)] private int frogSafeSpeedSteps = 5;
    [Tooltip("작은 발판에서 수평 점프가 불가능할 때 제자리 수직 점프를 허용합니다.")]
    [SerializeField] private bool allowFrogVerticalHopWhenTrapped = true;

    [Header("공격 쿨타임 배율")]
    [SerializeField, Min(0.1f)] private float slimeAttackCooldownMultiplier = 1f;
    [SerializeField, Min(0.1f)] private float snailAttackCooldownMultiplier = 1.35f;
    [SerializeField, Min(0.1f)] private float ghostAttackCooldownMultiplier = 0.9f;
    [SerializeField, Min(0.1f)] private float spiderAttackCooldownMultiplier = 0.7f;
    [SerializeField, Min(0.1f)] private float frogAttackCooldownMultiplier = 1.2f;

    [Header("공격 이동 제한")]
    [Tooltip("달팽이가 플레이어에게 순간이동하듯 달라붙지 않도록 한 번에 이동할 최대 거리입니다.")]
    [SerializeField, Min(0.1f)] private float snailMaxAttackTravel = 0.75f;
    [Tooltip("개구리가 공격 점프로 발판 밖까지 이동하지 않도록 제한하는 최대 수평 거리입니다.")]
    [SerializeField, Min(0.1f)] private float frogMaxAttackTravel = 1f;

    [Header("공격 모션")]
    [Tooltip("슬라임: 몸을 움츠린 뒤 포물선으로 통 튀어오릅니다.")]
    [SerializeField] private AttackMotionSettings slimeAttack =
        new AttackMotionSettings(0.14f, 0.16f, 0.20f, 0.75f, 0.18f, 0.55f);

    [Tooltip("달팽이: 짧게 몸을 당긴 뒤 낮게 밀고 들어옵니다.")]
    [SerializeField] private AttackMotionSettings snailAttack =
        new AttackMotionSettings(0.22f, 0.08f, 0.32f, 0.05f, 0.24f, 0.32f);

    [Tooltip("유령: 짧게 뒤로 빠졌다가 플레이어에게 빠르게 후웅 돌진합니다.")]
    [SerializeField] private AttackMotionSettings ghostAttack =
        new AttackMotionSettings(0.08f, 0.18f, 0.16f, 0.12f, 0.24f, 0.80f);

    [Tooltip("피라냐: 빠르게 튀어올라 물고 뒤로 튕겨납니다.")]
    [SerializeField] private AttackMotionSettings piranhaAttack =
        new AttackMotionSettings(0.06f, 0.10f, 0.14f, 0.35f, 0.18f, 0.60f);

    [Tooltip("거미: 낮고 빠르게 직선 돌진합니다.")]
    [SerializeField] private AttackMotionSettings spiderAttack =
        new AttackMotionSettings(0.04f, 0.12f, 0.08f, 0.02f, 0.12f, 0.48f);

    [Tooltip("개구리: 높은 포물선으로 플레이어에게 덮칩니다.")]
    [SerializeField] private AttackMotionSettings frogAttack =
        new AttackMotionSettings(0.18f, 0.18f, 0.26f, 1.15f, 0.22f, 0.68f);

    [SerializeField] private InteractableWater water;

    private bool wasAboveSurface;


    private readonly struct CollisionPair
    {
        public readonly Collider2D first;
        public readonly Collider2D second;

        public CollisionPair(
            Collider2D first,
            Collider2D second)
        {
            this.first = first;
            this.second = second;
        }
    }

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private readonly List<CollisionPair> ignoredCollisions = new();
    private Coroutine attackRoutine;

    private int verticalDirection = 1;
    private Vector2 startPosition;
    private float spawnTime;
    private float ghostPhase;
    private float nextFrogJumpTime;
    private float piranhaHeightRatio;
    private bool piranhaAttackUsedThisCycle;

    private LayerMask platformLayer;

    public MonsterType Type => monsterType;
    public int VerticalDirection => verticalDirection;
    public bool IsAttacking { get; private set; }

    public bool IsPiranha =>
        monsterType == MonsterType.Piranha;

    public bool UsesPlayerTracking =>
        !IsPiranha;

    public bool CanAttackPlayer => true;

    public bool IsPiranhaRising =>
        IsPiranha && verticalDirection > 0;

    public float AttackCooldownMultiplier {
        get {
            switch (monsterType) {
                case MonsterType.Snail: return snailAttackCooldownMultiplier;
                case MonsterType.Ghost: return ghostAttackCooldownMultiplier;
                case MonsterType.Spider: return spiderAttackCooldownMultiplier;
                case MonsterType.Frog: return frogAttackCooldownMultiplier;
                default: return slimeAttackCooldownMultiplier;
            }
        }
    }

    public bool UsesGroundObstacleCheck =>
        monsterType == MonsterType.Slime ||
        monsterType == MonsterType.Snail ||
        monsterType == MonsterType.Spider ||
        monsterType == MonsterType.Ghost ||
        monsterType == MonsterType.Frog;

    public bool CanTurnOnWallCollision =>
        monsterType == MonsterType.Slime ||
        monsterType == MonsterType.Snail ||
        monsterType == MonsterType.Spider ||
        monsterType == MonsterType.Frog;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = FindBodyCollider(transform);

        startPosition = rb.position;
        spawnTime = Time.time;
        ghostPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        if (water == null)
        {
            water = FindAnyObjectByType<InteractableWater>();
        }

        ConfigureRigidbody();
    }

    private void Start()
    {
        spawnTime = Time.time;
        nextFrogJumpTime = Time.time + Random.Range(0f, frogJumpInterval * 0.35f);

        if (IsPiranha && water != null)
        {
            wasAboveSurface =
                transform.position.y > water.GetSurfaceY();
        }
    }

    private void OnDisable()
    {
        CancelAttackMotion();
    }

    /// <summary>
    /// 바닥 레이어를 전달받습니다.
    /// 특정 타일 Collider 하나에 몬스터를 묶지 않고,
    /// 진행 방향에 실제 바닥이 이어지는지를 매번 검사합니다.
    /// </summary>
    public void InitializePlatformConstraint(
        LayerMask groundMask,
        float fallbackProbeDistance)
    {
        platformLayer = groundMask;

        if (frogGroundLayer.value == 0)
            frogGroundLayer = groundMask;

        edgeDownDistance = Mathf.Max(
            edgeDownDistance,
            fallbackProbeDistance);
    }

    /// <summary>
    /// 진행 방향 바로 앞에 바닥이 이어지는지 검사합니다.
    /// 붙어 있는 다른 타일 Collider는 통과하고 실제 틈에서는 멈춥니다.
    /// </summary>
    public bool HasGroundAhead(
        int direction,
        float lookAhead = 0f)
    {
        if (monsterType == MonsterType.Piranha)
            return true;

        if (bodyCollider == null)
            return false;

        Bounds bounds = bodyCollider.bounds;
        float forward = Mathf.Max(edgeForwardDistance, lookAhead);

        Vector2 origin = new(
            direction > 0
                ? bounds.max.x + forward
                : bounds.min.x - forward,
            bounds.min.y + edgeProbeRadius + 0.03f);

        float downDistance =
            monsterType == MonsterType.Ghost
                ? floatingEdgeDownDistance
                : edgeDownDistance;

        LayerMask mask = GetPlatformMask();

        RaycastHit2D hit = Physics2D.CircleCast(
            origin,
            edgeProbeRadius,
            Vector2.down,
            downDistance,
            mask);

        return hit.collider != null &&
               !IsSelfCollider(hit.collider);
    }

    /// <summary>
    /// 이동, 벽 검사, 발판 끝 검사와 개구리 안전 점프를 한 곳에서 처리합니다.
    /// AI는 반환값에 따라 방향만 결정하면 됩니다.
    /// </summary>
    public MonsterMoveResult TryMoveSafely(
        int direction,
        float baseSpeed,
        bool avoidEdges = true)
    {
        if (rb == null || IsAttacking)
            return MonsterMoveResult.Waiting;

        direction = direction >= 0 ? 1 : -1;

        if (monsterType == MonsterType.Piranha)
        {
            MovePiranha();
            return MonsterMoveResult.Moved;
        }

        float adjustedSpeed = GetAdjustedSpeed(baseSpeed);

        if (monsterType == MonsterType.Frog)
        {
            if (!IsFrogGrounded())
                return MonsterMoveResult.Moved;

            if (Time.time < nextFrogJumpTime)
            {
                SetHorizontalVelocity(0f);
                return MonsterMoveResult.Waiting;
            }

            if (HasObstacleAhead(direction))
                return MonsterMoveResult.BlockedByObstacle;

            if (avoidEdges && preventFrogFromLeavingPlatform)
            {
                // 바로 앞에 바닥이 없으면 AI가 먼저 반대 방향으로 전환하게 합니다.
                if (!HasGroundAhead(direction))
                    return MonsterMoveResult.BlockedByEdge;

                float safeSpeed = FindSafeFrogHorizontalSpeed(
                    direction,
                    adjustedSpeed);

                if (safeSpeed < 0f)
                    return MonsterMoveResult.BlockedByEdge;

                LaunchFrog(direction, safeSpeed);
                return MonsterMoveResult.Moved;
            }

            LaunchFrog(direction, adjustedSpeed);
            return MonsterMoveResult.Moved;
        }

        if (HasObstacleAhead(direction))
            return MonsterMoveResult.BlockedByObstacle;

        if (avoidEdges && UsesGroundObstacleCheck &&
            !HasGroundAhead(direction))
        {
            return MonsterMoveResult.BlockedByEdge;
        }

        MoveAdjusted(direction, adjustedSpeed);
        return MonsterMoveResult.Moved;
    }

    public void Move(int direction, float speed)
    {
        if (rb == null || IsAttacking)
            return;

        direction = direction >= 0 ? 1 : -1;

        if (monsterType == MonsterType.Piranha)
        {
            MovePiranha();
            return;
        }

        float adjustedSpeed = GetAdjustedSpeed(speed);

        if (monsterType == MonsterType.Frog)
        {
            MoveFrog(direction, adjustedSpeed);
            return;
        }

        MoveAdjusted(direction, adjustedSpeed);
    }

    public void Stop()
    {
        if (rb == null || IsAttacking)
            return;

        switch (monsterType)
        {
            case MonsterType.Ghost:
                rb.linearVelocity = Vector2.zero;
                break;

            case MonsterType.Piranha:
                rb.linearVelocity = Vector2.zero;
                break;

            case MonsterType.Frog:
                if (IsFrogGrounded())
                    SetHorizontalVelocity(0f);
                break;

            default:
                SetHorizontalVelocity(0f);
                break;
        }
    }

    /// <summary>
    /// 플레이어의 몸체 중심까지 돌진해 완전히 겹친 뒤,
    /// 데미지를 적용하고 반대 방향으로 반동합니다.
    /// </summary>
    public bool TryStartAttackMotion(
        Transform target,
        System.Action onImpact,
        System.Action onComplete = null)
    {
        if (target == null ||
            IsAttacking ||
            rb == null ||
            !CanAttackPlayer)
            return false;

        attackRoutine = StartCoroutine(
            AttackMotionRoutine(target, onImpact, onComplete));

        return true;
    }

    /// <summary>
    /// 사망 또는 비활성화 시 진행 중인 공격 모션을 중단합니다.
    /// </summary>
    public void CancelAttackMotion()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        RestoreTargetCollision();
        IsAttacking = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator AttackMotionRoutine(
        Transform target,
        System.Action onImpact,
        System.Action onComplete)
    {
        IsAttacking = true;
        rb.linearVelocity = Vector2.zero;

        AttackMotionSettings settings = GetAttackSettings();
        Vector2 attackStart = rb.position;
        Vector2 initialTarget = GetAttackDestination(target, attackStart);
        Vector2 attackDirection =
            (initialTarget - attackStart).normalized;

        if (attackDirection.sqrMagnitude < 0.001f)
            attackDirection = Vector2.right;

        IgnoreTargetCollision(target);

        // 1. 공격 전 살짝 반대 방향으로 물러나며 힘을 모읍니다.
        Vector2 windupTarget =
            attackStart - attackDirection * settings.windupDistance;
        windupTarget = ClampGroundAttackPosition(
            attackStart,
            windupTarget);

        yield return MoveLinearly(
            attackStart,
            windupTarget,
            settings.windupTime,
            null);

        // 2. 플레이어가 움직여도 마지막까지 몸체 중심을 추적합니다.
        yield return MoveToTarget(
            windupTarget,
            target,
            settings);

        // 달팽이는 짧게 밀고 들어오고, 다른 근접 몬스터는 대상 위치까지 이동합니다.
        rb.position = GetAttackDestination(target, windupTarget);
        rb.linearVelocity = Vector2.zero;

        onImpact?.Invoke();

        // 3. 타격 위치에서 공격 방향 반대로 튕겨 나갑니다.
        Vector2 impactPosition = rb.position;
        Vector2 recoilDirection =
            (impactPosition - windupTarget).normalized;

        if (recoilDirection.sqrMagnitude < 0.001f)
            recoilDirection = attackDirection;

        Vector2 recoilTarget =
            impactPosition -
            recoilDirection * settings.recoilDistance;
        recoilTarget = ClampGroundAttackPosition(
            impactPosition,
            recoilTarget);

        yield return MoveLinearly(
            impactPosition,
            recoilTarget,
            settings.recoilTime,
            SmoothStep);

        rb.linearVelocity = Vector2.zero;
        RestoreTargetCollision();

        IsAttacking = false;
        attackRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator MoveToTarget(
        Vector2 from,
        Transform target,
        AttackMotionSettings settings)
    {
        float elapsed = 0f;

        while (elapsed < settings.lungeTime)
        {
            if (target == null)
                yield break;

            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / settings.lungeTime);
            float progress = GetLungeProgress(t);
            Vector2 targetPosition =
                GetAttackDestination(target, from);
            Vector2 position = Vector2.Lerp(from, targetPosition, progress);

            // 슬라임과 개구리는 높게, 거미는 낮게 이동합니다.
            float arc = 4f * settings.arcHeight * t * (1f - t);
            position += Vector2.up * arc;

            rb.MovePosition(position);
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator MoveLinearly(
        Vector2 from,
        Vector2 to,
        float duration,
        System.Func<float, float> easing)
    {
        if (duration <= 0f)
        {
            rb.position = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float progress = easing != null ? easing(t) : t;

            rb.MovePosition(Vector2.Lerp(from, to, progress));
            yield return new WaitForFixedUpdate();
        }

        rb.position = to;
    }

    private float GetLungeProgress(float t)
    {
        switch (monsterType)
        {
            // 빠르게 가속한 뒤 부드럽게 도착해 후웅 느낌을 냅니다.
            case MonsterType.Ghost:
                return 1f - Mathf.Pow(1f - t, 3f);

            // 거의 순간적으로 튀어나갑니다.
            case MonsterType.Spider:
                return Mathf.Sqrt(t);

            // 위로 튀어오르는 느낌을 살립니다.
            case MonsterType.Piranha:
                return Mathf.Sin(t * Mathf.PI * 0.5f);

            default:
                return SmoothStep(t);
        }
    }

    private AttackMotionSettings GetAttackSettings()
    {
        switch (monsterType)
        {
            case MonsterType.Slime:
                return slimeAttack;

            case MonsterType.Snail:
                return snailAttack;

            case MonsterType.Ghost:
                return ghostAttack;

            case MonsterType.Piranha:
                return piranhaAttack;

            case MonsterType.Spider:
                return spiderAttack;

            case MonsterType.Frog:
                return frogAttack;

            default:
                return slimeAttack;
        }
    }

    private Vector2 GetAttackDestination(
        Transform target,
        Vector2 attackOrigin)
    {
        Vector2 desired = GetOverlapPosition(target);

        if (monsterType == MonsterType.Snail)
        {
            float deltaX = Mathf.Clamp(
                desired.x - attackOrigin.x,
                -snailMaxAttackTravel,
                snailMaxAttackTravel);

            // 달팽이는 높게 점프하지 않고 같은 지면 높이에서 짧게 밀고 들어옵니다.
            return ClampGroundAttackPosition(
                attackOrigin,
                new Vector2(
                    attackOrigin.x + deltaX,
                    attackOrigin.y));
        }

        if (monsterType == MonsterType.Frog)
        {
            float deltaX = Mathf.Clamp(
                desired.x - attackOrigin.x,
                -frogMaxAttackTravel,
                frogMaxAttackTravel);

            return FindSafeFrogAttackDestination(
                attackOrigin,
                deltaX);
        }

        return desired;
    }

    private Vector2 GetOverlapPosition(Transform target)
    {
        if (target == null)
            return rb.position;

        Transform targetRoot = FindTaggedRoot(target, "Player");
        Collider2D targetCollider = FindBodyCollider(targetRoot);

        Vector2 targetCenter = targetCollider != null
            ? targetCollider.bounds.center
            : (Vector2)targetRoot.position;

        Vector2 selfCenterOffset = bodyCollider != null
            ? (Vector2)bodyCollider.bounds.center - rb.position
            : Vector2.zero;

        return targetCenter - selfCenterOffset;
    }

    private void IgnoreTargetCollision(Transform target)
    {
        RestoreTargetCollision();

        Transform targetRoot = FindTaggedRoot(target, "Player");

        Collider2D[] selfColliders =
            GetComponentsInChildren<Collider2D>(true);

        Collider2D[] targetColliders =
            targetRoot.GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D selfCollider in selfColliders)
        {
            if (!IsBodyCollider(selfCollider))
                continue;

            foreach (Collider2D targetCollider in targetColliders)
            {
                if (!IsBodyCollider(targetCollider))
                    continue;

                Physics2D.IgnoreCollision(
                    selfCollider,
                    targetCollider,
                    true);

                ignoredCollisions.Add(
                    new CollisionPair(
                        selfCollider,
                        targetCollider));
            }
        }
    }

    private void RestoreTargetCollision()
    {
        foreach (CollisionPair pair in ignoredCollisions)
        {
            if (pair.first != null &&
                pair.second != null)
            {
                Physics2D.IgnoreCollision(
                    pair.first,
                    pair.second,
                    false);
            }
        }

        ignoredCollisions.Clear();
    }

    private static Transform FindTaggedRoot(
        Transform start,
        string tagName)
    {
        for (Transform current = start;
             current != null;
             current = current.parent)
        {
            if (current.CompareTag(tagName))
                return current;
        }

        return start;
    }

    private static Collider2D FindBodyCollider(Transform root)
    {
        if (root == null)
            return null;

        foreach (Collider2D collider in
                 root.GetComponentsInChildren<Collider2D>(true))
        {
            if (IsBodyCollider(collider))
                return collider;
        }

        return null;
    }

    private static bool IsBodyCollider(
        Collider2D collider)
    {
        return collider != null &&
               collider.enabled &&
               !collider.isTrigger;
    }

    private static float SmoothStep(float t) =>
        t * t * (3f - 2f * t);

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
            acceleration * Time.fixedDeltaTime);

        float targetY =
            startPosition.y +
            Mathf.Sin(
                Time.time * ghostFloatFrequency + ghostPhase) *
            ghostFloatHeight;

        float yVelocity =
            (targetY - rb.position.y) *
            ghostVerticalCorrection;

        yVelocity = Mathf.Clamp(
            yVelocity,
            -ghostMaxVerticalSpeed,
            ghostMaxVerticalSpeed);

        rb.linearVelocity =
            new Vector2(newXVelocity, yVelocity);
    }

    private void MovePiranha()
    {
        //float elapsedTime = Time.time - spawnTime;
        //float phase =
        //    elapsedTime * piranhaFrequency -
        //    Mathf.PI * 0.5f;

        //piranhaHeightRatio =
        //    (Mathf.Sin(phase) + 1f) * 0.5f;

        //float targetY =
        //    startPosition.y +
        //    piranhaHeightRatio * piranhaJumpHeight;

        //float verticalVelocity = Mathf.Cos(phase);

        //if (verticalVelocity > 0.01f)
        //    verticalDirection = 1;
        //else if (verticalVelocity < -0.01f)
        //    verticalDirection = -1;

        //if (piranhaHeightRatio <= 0.03f &&
        //    verticalDirection >= 0)
        //{
        //    piranhaAttackUsedThisCycle = false;
        //}

        //rb.MovePosition(
        //    new Vector2(startPosition.x, targetY));
        float elapsedTime = Time.time - spawnTime;
        float phase =
            elapsedTime * piranhaFrequency -
            Mathf.PI * 0.5f;

        piranhaHeightRatio =
            (Mathf.Sin(phase) + 1f) * 0.5f;

        float targetY =
            startPosition.y +
            piranhaHeightRatio * piranhaJumpHeight;

        float verticalVelocity = Mathf.Cos(phase);

        if (verticalVelocity > 0.01f)
            verticalDirection = 1;
        else if (verticalVelocity < -0.01f)
            verticalDirection = -1;

        if (piranhaHeightRatio <= 0.03f &&
            verticalDirection >= 0)
        {
            piranhaAttackUsedThisCycle = false;
        }

        rb.MovePosition(new Vector2(startPosition.x, targetY));

        //--------------------------------------------------
        // 수면 통과 체크
        //--------------------------------------------------

        if (water != null)
        {
            bool isAboveSurface = targetY > water.GetSurfaceY();

            if (isAboveSurface != wasAboveSurface)
            {
                float force = Mathf.Clamp(
                    Mathf.Abs(verticalVelocity) * 4f,
                    0.5f,
                    water.MaxForce);

                water.Splash(transform.position.x, force);
                water.SpawnSplashParticle(transform.position.x);
                wasAboveSurface = isAboveSurface;
            }
        }
    }

    public bool CanPiranhaEngage(Transform target)
    {
        if (!IsPiranha ||
            target == null ||
            piranhaAttackUsedThisCycle ||
            !IsPiranhaRising)
        {
            return false;
        }

        float distance = Vector2.Distance(
            transform.position,
            target.position);

        return piranhaHeightRatio >= piranhaEngageHeightRatio &&
               distance <= piranhaEngageDistance;
    }

    public void ConsumePiranhaAttack()
    {
        if (IsPiranha)
            piranhaAttackUsedThisCycle = true;
    }

    public void ReturnPiranhaToStart()
    {
        if (!IsPiranha || rb == null)
            return;

        Vector2 nextPosition = Vector2.MoveTowards(
            rb.position,
            startPosition,
            piranhaReturnSpeed * Time.fixedDeltaTime);

        rb.MovePosition(nextPosition);

        Vector2 difference =
            startPosition - rb.position;

        if (Mathf.Abs(difference.y) > 0.01f)
            verticalDirection =
                difference.y > 0f ? 1 : -1;
    }

    public bool IsNearPiranhaStartPosition()
    {
        return IsPiranha &&
               Vector2.Distance(
                   rb.position,
                   startPosition) <= piranhaReturnThreshold;
    }

    public void ResetPiranhaCycle()
    {
        if (!IsPiranha || rb == null)
            return;

        rb.position = startPosition;
        rb.linearVelocity = Vector2.zero;
        spawnTime = Time.time;
        piranhaHeightRatio = 0f;
        verticalDirection = 1;
        piranhaAttackUsedThisCycle = false;
    }

    private void MoveFrog(int direction, float speed)
    {
        if (!IsFrogGrounded() ||
            Time.time < nextFrogJumpTime)
        {
            return;
        }

        if (preventFrogFromLeavingPlatform)
        {
            if (!HasGroundAhead(direction))
            {
                SetHorizontalVelocity(0f);
                return;
            }

            float safeSpeed = FindSafeFrogHorizontalSpeed(direction, speed);

            if (safeSpeed < 0f)
            {
                SetHorizontalVelocity(0f);
                return;
            }

            LaunchFrog(direction, safeSpeed);
            return;
        }

        LaunchFrog(direction, speed);
    }

    private void LaunchFrog(int direction, float speed)
    {
        rb.linearVelocity = new Vector2(
            direction * speed,
            frogJumpPower);

        nextFrogJumpTime =
            Time.time + frogJumpInterval;
    }

    public bool IsFrogGrounded()
    {
        if (rb == null || bodyCollider == null)
            return false;

        // 위로 올라가는 중에는 착지로 판정하지 않습니다.
        if (rb.linearVelocity.y > frogGroundedMaxUpSpeed)
            return false;

        Bounds bounds = bodyCollider.bounds;
        Vector2 origin = new Vector2(
            bounds.center.x,
            bounds.min.y + 0.04f);

        Vector2 size = new Vector2(
            Mathf.Max(0.05f, bounds.size.x * frogGroundCheckWidth),
            0.06f);

        LayerMask mask =
            frogGroundLayer.value != 0
                ? frogGroundLayer
                : platformLayer;

        RaycastHit2D hit = Physics2D.BoxCast(
            origin,
            size,
            0f,
            Vector2.down,
            frogGroundCheckDistance,
            mask);

        return hit.collider != null &&
               hit.collider != bodyCollider;
    }

    /// <summary>
    /// 요청 속도로 발판을 벗어나면 속도를 단계적으로 낮춰
    /// 같은 발판 안에 착지할 수 있는 수평 속도를 찾습니다.
    /// 작은 발판에서는 0을 반환해 제자리 점프를 사용합니다.
    /// -1은 점프 자체를 취소해야 한다는 뜻입니다.
    /// </summary>
    private float FindSafeFrogHorizontalSpeed(
        int direction,
        float requestedSpeed)
    {
        int steps = Mathf.Max(2, frogSafeSpeedSteps);
        float speed = Mathf.Max(0f, requestedSpeed);

        for (int step = 0; step < steps; step++)
        {
            float ratio = 1f - step / (float)steps;
            float candidate = speed * ratio;

            if (HasContinuousGroundPath(direction, candidate))
                return candidate;
        }

        return allowFrogVerticalHopWhenTrapped ? 0f : -1f;
    }

    private Vector2 FindSafeFrogAttackDestination(
        Vector2 attackOrigin,
        float requestedDeltaX)
    {
        int steps = Mathf.Max(2, frogSafeSpeedSteps);

        for (int step = 0; step <= steps; step++)
        {
            float ratio = 1f - step / (float)steps;
            float candidateX =
                attackOrigin.x + requestedDeltaX * ratio;

            if (HasGroundPathBetween(
                    attackOrigin.x,
                    candidateX))
            {
                return new Vector2(candidateX, attackOrigin.y);
            }
        }

        return attackOrigin;
    }

    private Vector2 ClampGroundAttackPosition(
        Vector2 origin,
        Vector2 desired)
    {
        if (monsterType != MonsterType.Snail &&
            monsterType != MonsterType.Frog)
        {
            return desired;
        }

        return HasGroundPathBetween(origin.x, desired.x)
            ? new Vector2(desired.x, origin.y)
            : origin;
    }

    private bool HasGroundPathBetween(
        float startX,
        float endX)
    {
        if (bodyCollider == null)
            return false;

        float distance = Mathf.Abs(endX - startX);
        int sampleCount = Mathf.Max(
            2,
            Mathf.CeilToInt(distance / 0.2f) + 1);

        Bounds bounds = bodyCollider.bounds;
        LayerMask mask =
            frogGroundLayer.value != 0
                ? frogGroundLayer
                : GetPlatformMask();

        float downDistance = Mathf.Max(
            edgeDownDistance,
            frogGroundCheckDistance + 0.2f);

        for (int i = 0; i <= sampleCount; i++)
        {
            float ratio = i / (float)sampleCount;
            float x = Mathf.Lerp(startX, endX, ratio);
            Vector2 origin = new(
                x,
                bounds.min.y + edgeProbeRadius + 0.08f);

            RaycastHit2D hit = Physics2D.CircleCast(
                origin,
                edgeProbeRadius,
                Vector2.down,
                downDistance,
                mask);

            if (hit.collider == null || IsSelfCollider(hit.collider))
                return false;
        }

        return true;
    }

    private bool HasContinuousGroundPath(
        int direction,
        float horizontalSpeed)
    {
        if (bodyCollider == null)
            return false;

        float gravity =
            Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);

        float flightTime = gravity > 0.001f
            ? (2f * frogJumpPower) / gravity
            : frogJumpInterval;

        float predictedDistance = Mathf.Max(
            bodyCollider.bounds.extents.x + edgeForwardDistance,
            Mathf.Abs(horizontalSpeed) * flightTime);

        Bounds bounds = bodyCollider.bounds;
        LayerMask mask =
            frogGroundLayer.value != 0
                ? frogGroundLayer
                : GetPlatformMask();

        int sampleCount = Mathf.Max(2, frogGroundPathSamples);
        float downDistance = Mathf.Max(
            edgeDownDistance,
            frogGroundCheckDistance + 0.2f);

        for (int i = 1; i <= sampleCount; i++)
        {
            float ratio = i / (float)sampleCount;
            float x = bounds.center.x +
                      direction * predictedDistance * ratio;

            Vector2 origin = new(
                x,
                bounds.min.y + edgeProbeRadius + 0.08f);

            RaycastHit2D hit = Physics2D.CircleCast(
                origin,
                edgeProbeRadius,
                Vector2.down,
                downDistance,
                mask);

            if (hit.collider == null || IsSelfCollider(hit.collider))
                return false;
        }

        return true;
    }

    private bool HasObstacleAhead(int direction)
    {
        if (bodyCollider == null || obstacleCheckDistance <= 0f)
            return false;

        Bounds bounds = bodyCollider.bounds;

        Vector2 origin = new(
            bounds.center.x +
            direction * (bounds.extents.x + 0.015f),
            bounds.center.y + bounds.extents.y * 0.05f);

        Vector2 size = new(
            0.04f,
            Mathf.Max(0.08f, bounds.size.y * obstacleCheckHeightRatio));

        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            origin,
            size,
            0f,
            Vector2.right * direction,
            obstacleCheckDistance,
            GetPlatformMask());

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null ||
                hit.collider.isTrigger ||
                IsSelfCollider(hit.collider))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private LayerMask GetPlatformMask()
    {
        if (platformLayer.value != 0)
            return platformLayer;

        if (frogGroundLayer.value != 0)
            return frogGroundLayer;

        int groundLayerIndex = LayerMask.NameToLayer("Ground");
        if (groundLayerIndex >= 0)
            return 1 << groundLayerIndex;

        return (LayerMask)Physics2D.DefaultRaycastLayers;
    }

    private bool IsSelfCollider(Collider2D collider)
    {
        return collider != null &&
               (collider.transform == transform ||
                collider.transform.IsChildOf(transform));
    }

    private float GetAdjustedSpeed(float baseSpeed)
    {
        float multiplier;
        float maximum;

        switch (monsterType)
        {
            case MonsterType.Snail:
                multiplier = snailSpeedMultiplier;
                maximum = snailMaxSpeed;
                break;
            case MonsterType.Ghost:
                multiplier = ghostSpeedMultiplier;
                maximum = ghostMaxSpeed;
                break;
            case MonsterType.Spider:
                multiplier = spiderSpeedMultiplier;
                maximum = spiderMaxSpeed;
                break;
            case MonsterType.Frog:
                multiplier = frogSpeedMultiplier;
                maximum = frogMaxSpeed;
                break;
            default:
                multiplier = slimeSpeedMultiplier;
                maximum = slimeMaxSpeed;
                break;
        }

        return Mathf.Min(
            Mathf.Max(0f, baseSpeed) * Mathf.Max(0f, multiplier),
            Mathf.Max(0.1f, maximum));
    }

    private void MoveAdjusted(int direction, float adjustedSpeed)
    {
        switch (monsterType)
        {
            case MonsterType.Ghost:
                MoveGhost(direction, adjustedSpeed);
                break;
            default:
                MoveGround(direction, adjustedSpeed);
                break;
        }
    }

    private void SetHorizontalVelocity(float targetSpeed)
    {
        float newXVelocity = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetSpeed,
            acceleration * Time.fixedDeltaTime);

        rb.linearVelocity =
            new Vector2(newXVelocity, rb.linearVelocity.y);
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

    private void OnValidate()
    {
        acceleration = Mathf.Max(0f, acceleration);
        edgeProbeRadius = Mathf.Max(0.01f, edgeProbeRadius);
        obstacleCheckDistance = Mathf.Max(0.01f, obstacleCheckDistance);
        frogJumpInterval = Mathf.Max(0.05f, frogJumpInterval);
        frogJumpPower = Mathf.Max(0f, frogJumpPower);
        frogGroundPathSamples = Mathf.Clamp(frogGroundPathSamples, 2, 12);
        frogSafeSpeedSteps = Mathf.Clamp(frogSafeSpeedSteps, 2, 8);
        snailMaxAttackTravel = Mathf.Max(0.1f, snailMaxAttackTravel);
        frogMaxAttackTravel = Mathf.Max(0.1f, frogMaxAttackTravel);
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D collider =
            bodyCollider != null
                ? bodyCollider
                : GetComponentInChildren<Collider2D>();

        if (collider == null || collider.isTrigger)
            return;

        Bounds bounds = collider.bounds;
        int previewDirection = 1;

        Gizmos.color = Color.yellow;
        Vector3 edgeOrigin = new(
            bounds.max.x + edgeForwardDistance,
            bounds.min.y + edgeProbeRadius + 0.03f,
            transform.position.z);

        Gizmos.DrawWireSphere(edgeOrigin, edgeProbeRadius);
        Gizmos.DrawLine(
            edgeOrigin,
            edgeOrigin + Vector3.down *
            (monsterType == MonsterType.Ghost
                ? floatingEdgeDownDistance
                : edgeDownDistance));

        Gizmos.color = Color.magenta;
        Vector3 obstacleOrigin = new(
            bounds.center.x +
            previewDirection * (bounds.extents.x + 0.015f),
            bounds.center.y + bounds.extents.y * 0.05f,
            transform.position.z);

        Vector3 obstacleSize = new(
            0.04f,
            Mathf.Max(0.08f, bounds.size.y * obstacleCheckHeightRatio),
            0f);

        Gizmos.DrawWireCube(obstacleOrigin, obstacleSize);
        Gizmos.DrawLine(
            obstacleOrigin,
            obstacleOrigin +
            Vector3.right * obstacleCheckDistance);

        if (monsterType == MonsterType.Frog)
        {
            Gizmos.color = Color.cyan;
            Vector3 groundOrigin = new(
                bounds.center.x,
                bounds.min.y + 0.04f,
                transform.position.z);

            Vector3 groundSize = new(
                Mathf.Max(0.05f, bounds.size.x * frogGroundCheckWidth),
                0.06f,
                0f);

            Gizmos.DrawWireCube(groundOrigin, groundSize);
            Gizmos.DrawLine(
                groundOrigin,
                groundOrigin + Vector3.down * frogGroundCheckDistance);
        }
    }
}
