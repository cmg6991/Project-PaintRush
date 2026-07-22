using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 문 개방 조건을 관리합니다.
/// 조건 1: 피라냐를 제외한 몬스터의 지정 비율 이상 처치.
/// 조건 2: 현재 스테이지에 필요한 모든 색을 문에 한 번씩 칠하기.
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

    [Header("색상 조건")]
    [Tooltip("켜면 StagePaletteManager의 Paint Requirements를 문 필수 색으로 사용합니다.")]
    [SerializeField]
    private bool useStagePaletteRequirements = true;

    [Tooltip("StagePaletteManager를 사용하지 않을 때 직접 지정할 필수 색입니다.")]
    [SerializeField]
    private List<ElementType> requiredElements = new();

    [SerializeField, Min(0.001f)]
    private float colorTolerance = 0.15f;

    [Header("문 열림 연출")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private List<Collider2D> collidersToDisable = new();

    [Header("런타임 확인")]
    [SerializeField] private bool isOpened;
    [SerializeField] private List<ElementType> paintedElements = new();

    private readonly List<ElementType> resolvedRequiredElements = new();
    private readonly HashSet<ElementType> paintedElementSet = new();

    private bool monsterSubscribed;
    private bool paletteSubscribed;

    public bool IsOpened => isOpened;
    public float RequiredKillRatio => requiredKillRatio;
    public IReadOnlyList<ElementType> RequiredElements =>
        resolvedRequiredElements;
    public IReadOnlyList<ElementType> PaintedElements =>
        paintedElements;

    public int TotalKillableCount =>
        monsterManager != null
            ? monsterManager.TotalKillableCount
            : 0;

    public int RequiredKillCount =>
        monsterManager != null
            ? monsterManager.CalculateRequiredKills(
                requiredKillRatio)
            : 0;

    public int KillableKillCount =>
        monsterManager != null
            ? monsterManager.KillableKillCount
            : 0;

    public int CompletedKillCount =>
        Mathf.Min(
            KillableKillCount,
            RequiredKillCount);

    public int RemainingKillCount =>
        monsterManager != null
            ? monsterManager.CalculateRemainingRequiredKills(
                requiredKillRatio)
            : 0;

    public bool IsKillConditionMet =>
        monsterManager != null &&
        monsterManager.IsKillRequirementMet(
            requiredKillRatio);

    public bool IsColorConditionMet
    {
        get
        {
            foreach (ElementType element in
                     resolvedRequiredElements)
            {
                if (!paintedElementSet.Contains(element))
                    return false;
            }

            return true;
        }
    }

    public event Action OnConditionChanged;
    public event Action OnDoorOpened;

    private void Awake()
    {
        RestorePaintedElementSet();
        ResolveReferences();
        RebuildRequiredElements();
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
        // 실행 순서상 Manager가 OnEnable 이후 생성된 경우를 보완합니다.
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
        return TryResolveElement(
                   color,
                   out ElementType element) &&
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
            $"문 색상 진행도: " +
            $"{paintedElementSet.Count}/" +
            $"{resolvedRequiredElements.Count} " +
            $"({element})");

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

    private void ResolveReferences()
    {
        monsterManager ??=
            MonsterManager.Instance;

        monsterManager ??=
            FindFirstObjectByType<MonsterManager>();

        if (paletteManager == null ||
            paletteManager.gameObject.scene != gameObject.scene)
        {
            paletteManager =
                StagePaletteManager.FindForScene(this);
        }
    }

    private void Subscribe()
    {
        if (!monsterSubscribed &&
            monsterManager != null)
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
        if (monsterSubscribed &&
            monsterManager != null)
        {
            monsterManager.OnMonsterProgressChanged -=
                HandleMonsterProgressChanged;
        }

        if (paletteSubscribed &&
            paletteManager != null)
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

        if (useStagePaletteRequirements &&
            paletteManager != null)
        {
            foreach (StagePaletteManager.PaintRequirement
                     requirement in paletteManager.Requirements)
            {
                AddRequiredElement(
                    requirement.Element);
            }
        }

        if (resolvedRequiredElements.Count == 0)
        {
            foreach (ElementType element in requiredElements)
                AddRequiredElement(element);
        }

        paintedElements.RemoveAll(
            element =>
                !resolvedRequiredElements.Contains(element));

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

        for (int i = paintedElements.Count - 1;
             i >= 0;
             i--)
        {
            ElementType element = paintedElements[i];

            if (element == ElementType.None ||
                !paintedElementSet.Add(element))
            {
                paintedElements.RemoveAt(i);
            }
        }
    }

    private bool TryResolveElement(
        Color color,
        out ElementType resolved)
    {
        resolved = ElementType.None;

        float nearestDistance =
            float.PositiveInfinity;

        foreach (ElementType candidate in
                 resolvedRequiredElements)
        {
            float distance =
                GetElementColorDistance(
                    color,
                    candidate);

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

        Debug.Log(
            $"문 색상 판정 실패. " +
            $"입력=({color.r:F3}, {color.g:F3}, {color.b:F3}), " +
            $"tolerance={colorTolerance:F3}");

        return false;
    }

    private float GetElementColorDistance(
        Color input,
        ElementType element)
    {
        float canonicalDistance =
            ColorDistance(
                input,
                GetCanonicalElementColor(element));

        if (paletteManager == null)
            return canonicalDistance;

        float gaugeDistance =
            ColorDistance(
                input,
                paletteManager.GetElementGaugeColor(
                    element));

        return Mathf.Min(
            canonicalDistance,
            gaugeDistance);
    }

    private static Color GetCanonicalElementColor(
        ElementType element)
    {
        return element switch
        {
            ElementType.Red => Color.red,
            ElementType.Blue => Color.blue,
            ElementType.Yellow => Color.yellow,
            ElementType.Green =>
                new Color(0f, 1f, 0f, 1f),
            ElementType.Purple =>
                new Color(170f / 255f, 0f, 1f, 1f),
            _ => Color.white
        };
    }

    private static float ColorDistance(
        Color first,
        Color second)
    {
        Vector3 difference =
            new(
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
            OpenDoor();
        }
    }

    private void OpenDoor()
    {
        isOpened = true;

        if (animator != null &&
            !string.IsNullOrWhiteSpace(openTrigger))
        {
            animator.SetTrigger(openTrigger);
        }

        foreach (Collider2D target in collidersToDisable)
        {
            if (target != null)
                target.enabled = false;
        }

        Debug.Log("문이 열립니다!");

        OnConditionChanged?.Invoke();
        OnDoorOpened?.Invoke();
    }
}
