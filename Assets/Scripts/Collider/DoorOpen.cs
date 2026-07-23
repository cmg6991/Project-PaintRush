using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 문 개방 조건과 준비 상태를 관리합니다.
/// 조건이 완료되면 문은 즉시 씬을 이동시키지 않고 Ready 상태가 되며,
/// 실제 열림 애니메이션과 씬 전환은 DoorStageTransition이 담당합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class DoorOpen : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private StagePaletteManager paletteManager;

    [Header("몬스터 조건")]
    [SerializeField, Range(0f, 1f)]
    private float requiredKillRatio = 0.7f;
    [Tooltip("스포너 등록 전에 총 몬스터 수가 0인 순간 문이 준비되는 것을 막습니다.")]
    [SerializeField] private bool requireAtLeastOneKillableMonster = true;

    [Header("색상 조건")]
    [SerializeField] private bool useStagePaletteRequirements = true;
    [SerializeField] private List<ElementType> requiredElements = new();
    [SerializeField, Min(0.001f)] private float colorTolerance = 0.15f;

    [Header("문 상태 연출")]
    [SerializeField] private Animator animator;
    [Tooltip("Animator에 해당 Bool 파라미터가 있을 때 Ready 상태에서 켭니다.")]
    [SerializeField] private string readyBool = "Ready";
    [Tooltip("E 상호작용을 시작할 때 실행할 열림 Trigger입니다.")]
    [SerializeField] private string openTrigger = "Open";
    [Tooltip("조건 완료 시 문 위 반짝임, 화살표 등의 표시입니다.")]
    [SerializeField] private GameObject readyIndicator;
    [Tooltip("실제 문 열림을 시작할 때 비활성화할 물리 Collider입니다.")]
    [SerializeField] private List<Collider2D> collidersToDisable = new();

    [Header("런타임 확인")]
    [SerializeField] private bool isOpened;
    [SerializeField] private bool isTransitioning;
    [SerializeField] private List<ElementType> paintedElements = new();

    private readonly List<ElementType> resolvedRequiredElements = new();
    private readonly HashSet<ElementType> paintedElementSet = new();

    private bool monsterSubscribed;
    private bool paletteSubscribed;

    /// <summary>조건을 모두 완료해 문 사용이 가능하거나 이미 전환 중인 상태입니다.</summary>
    public bool IsOpened => isOpened;
    public bool IsReady => isOpened && !isTransitioning;
    public bool IsTransitioning => isTransitioning;

    public float RequiredKillRatio => requiredKillRatio;
    public IReadOnlyList<ElementType> RequiredElements => resolvedRequiredElements;
    public IReadOnlyList<ElementType> PaintedElements => paintedElements;

    public int TotalKillableCount =>
        monsterManager != null ? monsterManager.TotalKillableCount : 0;

    public int RequiredKillCount =>
        monsterManager != null
            ? monsterManager.CalculateRequiredKills(requiredKillRatio)
            : 0;

    public int KillableKillCount =>
        monsterManager != null ? monsterManager.KillableKillCount : 0;

    public int CompletedKillCount =>
        Mathf.Min(KillableKillCount, RequiredKillCount);

    public int RemainingKillCount =>
        monsterManager != null
            ? monsterManager.CalculateRemainingRequiredKills(requiredKillRatio)
            : 0;

    public bool IsKillConditionMet =>
        monsterManager != null &&
        (!requireAtLeastOneKillableMonster ||
         monsterManager.TotalKillableCount > 0) &&
        monsterManager.IsKillRequirementMet(requiredKillRatio);

    public bool IsColorConditionMet
    {
        get
        {
            foreach (ElementType element in resolvedRequiredElements)
            {
                if (!paintedElementSet.Contains(element))
                    return false;
            }

            return true;
        }
    }

    public event Action OnConditionChanged;
    public event Action OnDoorReady;
    public event Action OnTransitionStarted;

    /// <summary>
    /// 이전 코드 호환용입니다. 이제 조건 완료 시점이 아니라
    /// 실제 E 상호작용으로 문 열림이 시작될 때 호출됩니다.
    /// </summary>
    public event Action OnDoorOpened;

    private void Awake()
    {
        RestorePaintedElementSet();
        ResolveReferences();
        RebuildRequiredElements();
        RefreshReadyVisual();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
        RebuildRequiredElements();
        RefreshAndCheck();
    }

    private void Start()
    {
        ResolveReferences();
        Subscribe();
        RebuildRequiredElements();
        RefreshAndCheck();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public bool AddPaintColor(Color color)
    {
        return TryResolveElement(color, out ElementType element) &&
               AddPaintElement(element);
    }

    public bool AddPaintElement(ElementType element)
    {
        if (isOpened ||
            element == ElementType.None ||
            !resolvedRequiredElements.Contains(element) ||
            !paintedElementSet.Add(element))
        {
            return false;
        }

        paintedElements.Add(element);

        Debug.Log(
            $"문 색상 진행도: {paintedElementSet.Count}/" +
            $"{resolvedRequiredElements.Count} ({element})");

        RefreshAndCheck();
        return true;
    }

    public bool IsElementRequired(ElementType element)
    {
        return resolvedRequiredElements.Contains(element);
    }

    public bool IsElementPainted(ElementType element)
    {
        return paintedElementSet.Contains(element);
    }

    /// <summary>
    /// DoorStageTransition이 E 입력을 받았을 때 호출합니다.
    /// 문 열림 애니메이션을 실행하고 중복 입력을 차단합니다.
    /// </summary>
    public void BeginTransition()
    {
        if (isTransitioning)
            return;

        isTransitioning = true;

        Debug.Log(
            $"[Door] BeginTransition 호출됨 / " +
            $"Animator={animator} / Trigger={openTrigger}",
            this
        );

        if (readyIndicator != null)
            readyIndicator.SetActive(false);

        if (animator != null)
        {
            if (!string.IsNullOrWhiteSpace(readyBool))
                animator.SetBool(readyBool, false);

            if (!string.IsNullOrWhiteSpace(openTrigger))
            {
                animator.ResetTrigger(openTrigger);
                animator.SetTrigger(openTrigger);

                Debug.Log(
                    $"[Door] Open 트리거 실행: {openTrigger}",
                    this
                );
            }
        }
        else
        {
            Debug.LogError(
                "[Door] Animator가 연결되지 않았습니다.",
                this
            );
        }

        foreach (Collider2D colliderToDisable in collidersToDisable)
        {
            if (colliderToDisable != null)
                colliderToDisable.enabled = false;
        }
    }

    private void ResolveReferences()
    {
        monsterManager ??= MonsterManager.Instance;
        monsterManager ??= FindAnyObjectByType<MonsterManager>();

        if (paletteManager == null ||
            paletteManager.gameObject.scene != gameObject.scene)
        {
            paletteManager = StagePaletteManager.FindForScene(this);
        }
    }

    private void Subscribe()
    {
        if (!monsterSubscribed && monsterManager != null)
        {
            monsterManager.OnMonsterProgressChanged +=
                HandleMonsterProgressChanged;
            monsterSubscribed = true;
        }

        if (!paletteSubscribed &&
            useStagePaletteRequirements &&
            paletteManager != null)
        {
            paletteManager.OnPaletteStateChanged +=
                HandlePaletteStateChanged;
            paletteSubscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if (monsterSubscribed && monsterManager != null)
        {
            monsterManager.OnMonsterProgressChanged -=
                HandleMonsterProgressChanged;
        }

        if (paletteSubscribed && paletteManager != null)
        {
            paletteManager.OnPaletteStateChanged -=
                HandlePaletteStateChanged;
        }

        monsterSubscribed = false;
        paletteSubscribed = false;
    }

    private void HandleMonsterProgressChanged()
    {
        RefreshAndCheck();
    }

    private void HandlePaletteStateChanged()
    {
        RebuildRequiredElements();
        RefreshAndCheck();
    }

    private void RebuildRequiredElements()
    {
        resolvedRequiredElements.Clear();

        if (useStagePaletteRequirements && paletteManager != null)
        {
            foreach (StagePaletteManager.PaintRequirement requirement in
                     paletteManager.Requirements)
            {
                AddRequiredElement(requirement.Element);
            }
        }

        if (resolvedRequiredElements.Count == 0)
        {
            foreach (ElementType element in requiredElements)
                AddRequiredElement(element);
        }

        paintedElements.RemoveAll(
            element => !resolvedRequiredElements.Contains(element));

        RestorePaintedElementSet();
    }

    private void AddRequiredElement(ElementType element)
    {
        if (element != ElementType.None &&
            !resolvedRequiredElements.Contains(element))
        {
            resolvedRequiredElements.Add(element);
        }
    }

    private void RestorePaintedElementSet()
    {
        paintedElementSet.Clear();

        for (int i = paintedElements.Count - 1; i >= 0; i--)
        {
            ElementType element = paintedElements[i];

            if (element == ElementType.None ||
                !paintedElementSet.Add(element))
            {
                paintedElements.RemoveAt(i);
            }
        }
    }

    private bool TryResolveElement(Color color, out ElementType resolved)
    {
        resolved = ElementType.None;
        float nearestDistance = float.PositiveInfinity;

        foreach (ElementType candidate in resolvedRequiredElements)
        {
            float distance = GetElementColorDistance(color, candidate);

            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            resolved = candidate;
        }

        if (resolved != ElementType.None &&
            nearestDistance <= colorTolerance)
        {
            return true;
        }

        resolved = ElementType.None;
        return false;
    }

    private float GetElementColorDistance(Color input, ElementType element)
    {
        float canonicalDistance =
            ColorDistance(input, GetCanonicalElementColor(element));

        if (paletteManager == null)
            return canonicalDistance;

        float gaugeDistance = ColorDistance(
            input,
            paletteManager.GetElementGaugeColor(element));

        return Mathf.Min(canonicalDistance, gaugeDistance);
    }

    private static Color GetCanonicalElementColor(ElementType element)
    {
        return element switch
        {
            ElementType.Red => Color.red,
            ElementType.Blue => Color.blue,
            ElementType.Yellow => Color.yellow,
            ElementType.Green => new Color(0f, 1f, 0f, 1f),
            ElementType.Purple => new Color(170f / 255f, 0f, 1f, 1f),
            _ => Color.white
        };
    }

    private static float ColorDistance(Color first, Color second)
    {
        Vector3 difference = new(
            first.r - second.r,
            first.g - second.g,
            first.b - second.b);

        return difference.magnitude;
    }

    private void RefreshAndCheck()
    {
        OnConditionChanged?.Invoke();

        if (!isOpened &&
            IsKillConditionMet &&
            IsColorConditionMet)
        {
            SetDoorReady();
        }
    }

    private void SetDoorReady()
    {
        isOpened = true;
        isTransitioning = false;

        SetAnimatorBool(readyBool, true);
        RefreshReadyVisual();

        Debug.Log("문 조건 완료! 문 앞에서 E키로 다음 스테이지로 이동할 수 있습니다.");

        OnConditionChanged?.Invoke();
        OnDoorReady?.Invoke();
    }

    private void RefreshReadyVisual()
    {
        if (readyIndicator != null)
            readyIndicator.SetActive(IsReady);
    }

    private void SetAnimatorBool(string parameterName, bool value)
    {
        if (animator == null ||
            string.IsNullOrWhiteSpace(parameterName) ||
            !HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
        {
            return;
        }

        animator.SetBool(parameterName, value);
    }

    private void SetAnimatorTrigger(string parameterName)
    {
        if (animator == null ||
            string.IsNullOrWhiteSpace(parameterName) ||
            !HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Trigger))
        {
            return;
        }

        animator.SetTrigger(parameterName);
    }

    private bool HasAnimatorParameter(
        string parameterName,
        AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == type && parameter.name == parameterName)
                return true;
        }

        return false;
    }

    private void OnValidate()
    {
        requiredKillRatio = Mathf.Clamp01(requiredKillRatio);
        colorTolerance = Mathf.Max(0.001f, colorTolerance);
    }
}
