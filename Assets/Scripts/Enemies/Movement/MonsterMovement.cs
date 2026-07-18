using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
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

    [Header("공격 모션")]
    [Tooltip("슬라임: 몸을 움츠린 뒤 포물선으로 통 튀어오릅니다.")]
    [SerializeField] private AttackMotionSettings slimeAttack =
        new AttackMotionSettings(0.12f, 0.18f, 0.18f, 0.65f, 0.16f, 0.55f);

    [Tooltip("유령: 짧게 뒤로 빠졌다가 플레이어에게 빠르게 후웅 돌진합니다.")]
    [SerializeField] private AttackMotionSettings ghostAttack =
        new AttackMotionSettings(0.08f, 0.12f, 0.20f, 0.18f, 0.22f, 0.70f);

    [Tooltip("피라냐: 빠르게 튀어올라 물고 뒤로 튕겨납니다.")]
    [SerializeField] private AttackMotionSettings piranhaAttack =
        new AttackMotionSettings(0.06f, 0.10f, 0.14f, 0.35f, 0.18f, 0.60f);

    [Tooltip("거미: 낮고 빠르게 직선 돌진합니다.")]
    [SerializeField] private AttackMotionSettings spiderAttack =
        new AttackMotionSettings(0.05f, 0.15f, 0.09f, 0.05f, 0.14f, 0.50f);

    [Tooltip("개구리: 높은 포물선으로 플레이어에게 덮칩니다.")]
    [SerializeField] private AttackMotionSettings frogAttack =
        new AttackMotionSettings(0.15f, 0.20f, 0.22f, 1.00f, 0.20f, 0.65f);

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

    public MonsterType Type => monsterType;
    public int VerticalDirection => verticalDirection;
    public bool IsAttacking { get; private set; }

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
        bodyCollider = FindBodyCollider(transform);

        startPosition = rb.position;
        spawnTime = Time.time;
        ghostPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        ConfigureRigidbody();
    }

    private void Start()
    {
        if (monsterType == MonsterType.Frog &&
            frogGroundCheck == null)
        {
            Debug.LogError(
                $"{gameObject.name}: Frog Ground Check가 연결되지 않았습니다.");
        }
    }

    private void OnDisable()
    {
        CancelAttackMotion();
    }

    public void Move(int direction, float speed)
    {
        if (rb == null || IsAttacking)
            return;

        switch (monsterType)
        {
            case MonsterType.Slime:
                MoveGround(direction, speed * slimeSpeedMultiplier);
                break;

            case MonsterType.Ghost:
                MoveGhost(direction, speed * ghostSpeedMultiplier);
                break;

            case MonsterType.Piranha:
                MovePiranha();
                break;

            case MonsterType.Spider:
                MoveGround(direction, speed * spiderSpeedMultiplier);
                break;

            case MonsterType.Frog:
                MoveFrog(direction, speed * frogSpeedMultiplier);
                break;
        }
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
        if (target == null || IsAttacking || rb == null)
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
        Vector2 initialTarget = GetOverlapPosition(target);
        Vector2 attackDirection =
            (initialTarget - attackStart).normalized;

        if (attackDirection.sqrMagnitude < 0.001f)
            attackDirection = Vector2.right;

        IgnoreTargetCollision(target);

        // 1. 공격 전 살짝 반대 방향으로 물러나며 힘을 모읍니다.
        Vector2 windupTarget =
            attackStart - attackDirection * settings.windupDistance;

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

        // 부동소수점 오차 없이 플레이어 몸체 중심과 정확히 겹칩니다.
        rb.position = GetOverlapPosition(target);
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
            Vector2 targetPosition = GetOverlapPosition(target);
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
        float elapsedTime = Time.time - spawnTime;
        float phase =
            elapsedTime * piranhaFrequency -
            Mathf.PI * 0.5f;

        float normalizedHeight =
            (Mathf.Sin(phase) + 1f) * 0.5f;

        float targetY =
            startPosition.y +
            normalizedHeight * piranhaJumpHeight;

        float verticalVelocity = Mathf.Cos(phase);

        if (verticalVelocity > 0.01f)
            verticalDirection = 1;
        else if (verticalVelocity < -0.01f)
            verticalDirection = -1;

        rb.MovePosition(
            new Vector2(startPosition.x, targetY));
    }

    private void MoveFrog(int direction, float speed)
    {
        if (frogGroundCheck == null || !IsFrogGrounded())
            return;

        if (Time.time < nextFrogJumpTime)
        {
            SetHorizontalVelocity(0f);
            return;
        }

        rb.linearVelocity =
            new Vector2(direction * speed, frogJumpPower);

        nextFrogJumpTime =
            Time.time + frogJumpInterval;
    }

    private bool IsFrogGrounded()
    {
        if (frogGroundCheck == null)
            return false;

        return Physics2D.OverlapCircle(
            frogGroundCheck.position,
            frogGroundCheckRadius,
            frogGroundLayer) != null;
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

    private void OnDrawGizmosSelected()
    {
        if (frogGroundCheck == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            frogGroundCheck.position,
            frogGroundCheckRadius);
    }
}
