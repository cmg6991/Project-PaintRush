using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 현재 씬의 팔레트 아이템, 색상별 물감 수량, 피버 준비 상태를 관리합니다.
/// 다른 씬의 매니저를 검색하지 않도록 씬 단위 검색 API를 제공합니다.
/// </summary>
public class StagePaletteManager : MonoBehaviour
{
    [Serializable]
    public sealed class PaintRequirement
    {
        [SerializeField] private ElementType element = ElementType.Red;
        [SerializeField, HideInInspector, Min(1)] private int requiredCount = 1;
        [SerializeField] private Sprite icon;
        [SerializeField] private Color gaugeColor = Color.white;

        public ElementType Element => element;
        public int RequiredCount => Mathf.Max(1, requiredCount);
        public Sprite Icon => icon;
        public Color GaugeColor => gaugeColor;

        public PaintRequirement(
            ElementType element,
            int requiredCount,
            Sprite icon = null,
            Color? gaugeColor = null)
        {
            this.element = element;
            this.requiredCount = Mathf.Max(1, requiredCount);
            this.icon = icon;
            this.gaugeColor = gaugeColor ?? GetDefaultColor(element);
        }

        public void Sanitize()
        {
            requiredCount = Mathf.Max(1, requiredCount);

            if (gaugeColor.a <= 0f)
                gaugeColor = GetDefaultColor(element);
        }

        private static Color GetDefaultColor(ElementType element)
        {
            return element switch
            {
                ElementType.Red => new Color(1f, 0.25f, 0.25f, 1f),
                ElementType.Blue => new Color(0.25f, 0.55f, 1f, 1f),
                ElementType.Yellow => new Color(1f, 0.85f, 0.2f, 1f),
                ElementType.Green => new Color32(0, 255, 0, 255),
                ElementType.Purple => new Color32(170, 0, 255, 255),
                _ => Color.white
            };
        }
    }

    [Serializable]
    private sealed class PaintCountState
    {
        [SerializeField] private ElementType element;
        [SerializeField, Min(0)] private int count;

        public ElementType Element => element;
        public int Count => Mathf.Max(0, count);

        public PaintCountState(ElementType element, int count)
        {
            this.element = element;
            this.count = Mathf.Max(0, count);
        }

        public void SetCount(int value)
        {
            count = Mathf.Max(0, value);
        }

        public void Add(int amount)
        {
            count = Mathf.Max(0, count + amount);
        }
    }

    public static StagePaletteManager Instance { get; private set; }

    [Header("스테이지 물감 요구량")]
    [Tooltip("현재 스테이지에서 필요한 물감 색과 개수를 설정합니다.")]
    [SerializeField]
    private List<PaintRequirement> paintRequirements = new();

    [Header("피버 게이지 규칙")]
    [Tooltip("피버에 필요한 서로 다른 색상의 수입니다. 같은 색은 게이지에 한 번만 들어갑니다.")]
    [SerializeField, Min(1)]
    private int feverRequiredPaintCount = 5;

    [Tooltip("켜면 Paint Requirements에 등록된 색상만 게이지에 들어갑니다.")]
    [SerializeField]
    private bool onlyConfiguredStageColors = true;

    [Tooltip("피버 사용 중에 새 물감으로 게이지를 다시 충전할 수 있게 합니다.")]
    [SerializeField]
    private bool allowGaugeChargeDuringFever = false;

    [Header("획득 규칙")]
    [Tooltip("팔레트 아이템을 획득한 뒤부터 물감 진행도를 올립니다.")]
    [SerializeField]
    private bool requirePaletteBeforePaintCollection = true;

    [Tooltip("이전 버전 호환 필드입니다. 실제 소지량은 항상 계속 증가합니다.")]
    [SerializeField, HideInInspector]
    private bool capCollectedPaintToRequirement;

    [Header("피버 종료 규칙")]
    [Tooltip("팔레트 HUD를 스테이지 끝까지 유지하려면 끄세요.")]
    [SerializeField]
    private bool resetPaletteItemOnFeverEnd = false;

    [Tooltip("켜면 피버 종료 시 모든 물감 수량을 0으로 초기화합니다.")]
    [SerializeField]
    private bool resetCollectedPaintOnFeverEnd = false;

    [Tooltip("피버 종료 시 보유 중인 각 물감 색상의 수량을 1개씩 감소시킵니다.")]
    [SerializeField]
    private bool decreaseEachPaintCountOnFeverEnd = true;

    [Tooltip("팔레트를 한 번이라도 획득했다면 현재 스테이지가 끝날 때까지 HUD를 계속 표시합니다.")]
    [SerializeField]
    private bool keepHudVisibleAfterPalettePickup = true;

    [Header("씬 범위")]
    [Tooltip("다른 씬에 영향을 주지 않으려면 끄세요.")]
    [SerializeField]
    private bool persistAcrossScenes = false;

    [Header("호환용 설정")]
    [Tooltip("이전 버전의 색상 ID 목록입니다. 새 요구량 목록이 비어 있을 때만 자동 변환됩니다.")]
    [SerializeField]
    private List<string> requiredColorIds = new();

    [Header("런타임 확인")]
    [SerializeField]
    private List<PaintCountState> collectedPaintCounts = new();

    [SerializeField]
    private List<string> collectedColorIds = new();

    [Tooltip("게이지에 채워진 순서입니다. 실제 진행도에 반영된 물감만 저장합니다.")]
    [SerializeField]
    private List<ElementType> collectedPaintSequence = new();

    [SerializeField, Min(0)]
    private int paletteItemCount;

    [SerializeField]
    private bool hasEverAcquiredPalette;

    [SerializeField]
    private bool hasAllRequiredColors;

    [SerializeField]
    private bool canUseSpecialAttack;

    [SerializeField]
    private bool isSpecialAttackActive;

    [SerializeField, Range(0f, 1f)]
    private float feverGaugeRemaining01 = 1f;

    public IReadOnlyList<PaintRequirement> Requirements => paintRequirements;
    public IReadOnlyList<string> RequiredColorIds => requiredColorIds;
    public IReadOnlyList<string> CollectedColorIds => collectedColorIds;
    public IReadOnlyList<ElementType> CollectedPaintSequence =>
        collectedPaintSequence;

    // 기존 코드와의 호환용: 색상 종류의 수를 의미합니다.
    public int RequiredColorCount => paintRequirements.Count;
    public int CollectedRequiredColorCount => CountCollectedRequirementKinds();

    // 같은 색은 한 번만 충전되므로, 요구 칸 수도 사용 가능한 고유 색상 수를 넘지 않습니다.
    public int TotalRequiredPaintCount
    {
        get
        {
            int maximumUniqueColors = GetMaximumUniqueGaugeColorCount();

            return maximumUniqueColors <= 0
                ? 0
                : Mathf.Clamp(
                    feverRequiredPaintCount,
                    1,
                    maximumUniqueColors);
        }
    }

    public int CollectedRequiredPaintCount =>
        Mathf.Min(collectedPaintSequence.Count, TotalRequiredPaintCount);

    public int PaletteItemCount => paletteItemCount;
    public bool HasPaletteItem => paletteItemCount > 0;

    public bool ShouldShowPaletteHud =>
        HasPaletteItem ||
        (keepHudVisibleAfterPalettePickup &&
         hasEverAcquiredPalette);

    public bool HasAllRequiredColors => hasAllRequiredColors;
    public bool CanUseSpecialAttack => canUseSpecialAttack;
    public bool IsSpecialAttackActive => isSpecialAttackActive;
    public float FeverGaugeRemaining01 => feverGaugeRemaining01;

    public float Progress01 =>
        TotalRequiredPaintCount <= 0
            ? 0f
            : Mathf.Clamp01(
                (float)CollectedRequiredPaintCount /
                TotalRequiredPaintCount);

    public event Action OnPaletteStateChanged;
    public event Action<string> OnColorCollected;
    public event Action<ElementType, int> OnPaintCountChanged;
    public event Action<ElementType> OnPaintProgressUnitAdded;
    public event Action<float> OnFeverGaugeRemainingChanged;
    public event Action<int> OnPaletteItemCountChanged;
    public event Action OnSpecialAttackReady;
    public event Action OnSpecialAttackStarted;
    public event Action OnSpecialAttackEnded;

    private void Awake()
    {
        StagePaletteManager duplicate =
            FindOtherManagerInScene(gameObject.scene);

        if (duplicate != null)
        {
            Debug.LogError(
                $"[{nameof(StagePaletteManager)}] 같은 씬에 매니저가 둘 이상 있습니다. " +
                $"{gameObject.name}을 비활성화합니다.");

            enabled = false;
            return;
        }

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);

        if (Instance == null ||
            Instance.gameObject.scene == gameObject.scene ||
            !Instance.persistAcrossScenes)
        {
            Instance = this;
        }

        MigrateLegacyData();
        NormalizeRequirements();
        NormalizeCollectedCounts();
        RebuildSequenceFromCountsIfNeeded();
        SyncLegacyLists();
        if (paletteItemCount > 0)
            hasEverAcquiredPalette = true;

        feverGaugeRemaining01 = 1f;
        RefreshState(false);
    }

    private void Reset()
    {
        paintRequirements = new List<PaintRequirement>
        {
            new PaintRequirement(ElementType.Red, 1),
            new PaintRequirement(ElementType.Blue, 1),
            new PaintRequirement(ElementType.Yellow, 1),
            new PaintRequirement(ElementType.Green, 1),
            new PaintRequirement(ElementType.Purple, 1)
        };

        requiredColorIds.Clear();
    }

    private void OnValidate()
    {
        // 새 항목은 기본값 Red로 추가됩니다.
        // 편집 중 중복을 즉시 병합하면 + 버튼을 눌러도 항목이 사라져 보이므로
        // Inspector에서는 값 보정만 하고, 실행 시 Awake에서 병합합니다.
        paintRequirements ??= new List<PaintRequirement>();

        feverRequiredPaintCount = Mathf.Max(1, feverRequiredPaintCount);
        feverGaugeRemaining01 = Mathf.Clamp01(feverGaugeRemaining01);

        foreach (PaintRequirement requirement in paintRequirements)
            requirement?.Sanitize();
    }

    [ContextMenu("기본 허용 색상 5종 생성")]
    private void CreateDefaultRequirements()
    {
        paintRequirements = new List<PaintRequirement>
        {
            new PaintRequirement(ElementType.Red, 1),
            new PaintRequirement(ElementType.Blue, 1),
            new PaintRequirement(ElementType.Yellow, 1),
            new PaintRequirement(ElementType.Green, 1),
            new PaintRequirement(ElementType.Purple, 1)
        };

        requiredColorIds.Clear();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    /// <summary>
    /// 전달된 컴포넌트와 같은 씬에 있는 매니저만 찾습니다.
    /// 다른 씬의 매니저를 잘못 참조하는 것을 방지합니다.
    /// </summary>
    public static StagePaletteManager FindForScene(Component context)
    {
        if (context == null)
            return Instance;

        return FindForScene(context.gameObject);
    }

    public static StagePaletteManager FindForScene(GameObject context)
    {
        if (context == null)
            return Instance;

        Scene scene = context.scene;

        if (!scene.IsValid() || !scene.isLoaded)
            return Instance;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            StagePaletteManager manager =
                root.GetComponentInChildren<StagePaletteManager>(true);

            if (manager != null && manager.enabled)
                return manager;
        }

        return null;
    }

    public bool RegisterPaint(
        ElementType element,
        int amount = 1)
    {
        if (amount <= 0 || element == ElementType.None)
            return false;

        if (requirePaletteBeforePaintCollection &&
            !HasPaletteItem)
        {
            Debug.Log(
                $"[팔레트] {element} 물감을 먹었지만 팔레트가 없어 진행도에 반영하지 않습니다.");

            return false;
        }

        PaintRequirement requirement =
            GetRequirement(element);

        if (onlyConfiguredStageColors &&
            requirement == null)
        {
            Debug.Log(
                $"[팔레트] {element}은 현재 스테이지 허용 색상이 아닙니다.");

            return false;
        }

        PaintCountState state =
            GetOrCreatePaintState(element);

        int newCount = state.Count + amount;
        state.SetCount(newCount);

        bool canChargeGauge =
            (!isSpecialAttackActive ||
             allowGaugeChargeDuringFever) &&
            collectedPaintSequence.Count < TotalRequiredPaintCount &&
            !collectedPaintSequence.Contains(element);

        bool addedToGauge = canChargeGauge;

        if (addedToGauge)
            collectedPaintSequence.Add(element);

        SyncLegacyLists();

        Debug.Log(
            $"[팔레트] {element} 물감 +{amount}, 현재 {newCount}, " +
            $"게이지 {CollectedRequiredPaintCount}/{TotalRequiredPaintCount}" +
            (addedToGauge ? string.Empty : " (게이지 변화 없음)"));

        InvokeSafely(
            OnColorCollected,
            element.ToString(),
            nameof(OnColorCollected));

        InvokeSafely(
            OnPaintCountChanged,
            element,
            newCount,
            nameof(OnPaintCountChanged));

        RefreshState(true);

        if (addedToGauge)
        {
            InvokeSafely(
                OnPaintProgressUnitAdded,
                element,
                nameof(OnPaintProgressUnitAdded));
        }

        return true;
    }

    /// <summary>
    /// 기존 문자열 기반 물감 획득 코드와의 호환용입니다.
    /// 한 번 호출할 때 해당 색 물감 1개를 획득합니다.
    /// </summary>
    public bool RegisterColor(string colorId)
    {
        return TryParseElement(colorId, out ElementType element) &&
               RegisterPaint(element, 1);
    }

    public int GetCollectedPaintCount(ElementType element)
    {
        PaintCountState state =
            collectedPaintCounts.Find(
                entry => entry.Element == element);

        return state?.Count ?? 0;
    }

    public int GetRequiredPaintCount(ElementType element)
    {
        return GetRequirement(element)?.RequiredCount ?? 0;
    }

    public float GetElementProgress01(ElementType element)
    {
        int required = GetRequiredPaintCount(element);

        if (required <= 0)
            return 0f;

        return Mathf.Clamp01(
            (float)GetCollectedPaintCount(element) /
            required);
    }

    public Sprite GetElementIcon(ElementType element)
    {
        return GetRequirement(element)?.Icon;
    }

    public Color GetElementGaugeColor(ElementType element)
    {
        PaintRequirement requirement =
            GetRequirement(element);

        if (requirement != null)
            return requirement.GaugeColor;

        return element switch
        {
            ElementType.Red => Color.red,
            ElementType.Blue => Color.blue,
            ElementType.Yellow => Color.yellow,
            ElementType.Green => new Color32(0, 255, 0, 255),
            ElementType.Purple => new Color32(170, 0, 255, 255),
            _ => Color.white
        };
    }

    public int EquipPaletteItem(int amount = 1)
    {
        if (amount <= 0)
            return paletteItemCount;

        paletteItemCount += amount;
        hasEverAcquiredPalette = true;

        Debug.Log(
            $"[팔레트] 팔레트 아이템 장착. 현재 보유: {paletteItemCount}");

        InvokeSafely(
            OnPaletteItemCountChanged,
            paletteItemCount,
            nameof(OnPaletteItemCountChanged));
        RefreshState(true);

        return paletteItemCount;
    }

    public bool TryConsumePaletteItems(int amount = 1)
    {
        if (amount <= 0 || paletteItemCount < amount)
            return false;

        paletteItemCount -= amount;
        InvokeSafely(
            OnPaletteItemCountChanged,
            paletteItemCount,
            nameof(OnPaletteItemCountChanged));
        RefreshState(true);

        return true;
    }

    public void ClearPaletteItems()
    {
        if (paletteItemCount == 0)
            return;

        paletteItemCount = 0;
        InvokeSafely(
            OnPaletteItemCountChanged,
            paletteItemCount,
            nameof(OnPaletteItemCountChanged));
        RefreshState(true);
    }

    public bool TryStartSpecialAttack()
    {
        if (!CanUseSpecialAttack)
        {
            Debug.Log(
                "[팔레트] 피버 사용 불가. " +
                $"게이지={Progress01:P0}, " +
                $"팔레트={HasPaletteItem}, " +
                $"사용 중={IsSpecialAttackActive}");

            return false;
        }

        isSpecialAttackActive = true;
        SetSpecialAttackGaugeRemaining(1f);
        RefreshState(true);
        InvokeSafely(OnSpecialAttackStarted, nameof(OnSpecialAttackStarted));

        return true;
    }

    public void CompleteSpecialAttack()
    {
        if (!isSpecialAttackActive)
            return;

        isSpecialAttackActive = false;
        SetSpecialAttackGaugeRemaining(0f);

        if (resetPaletteItemOnFeverEnd)
        {
            paletteItemCount = 0;

            InvokeSafely(
                OnPaletteItemCountChanged,
                paletteItemCount,
                nameof(OnPaletteItemCountChanged));
        }

        // 피버 게이지는 매번 전부 소비합니다.
        collectedPaintSequence.Clear();

        if (resetCollectedPaintOnFeverEnd)
        {
            ClearPaintCounts(true, false);
        }
        else if (decreaseEachPaintCountOnFeverEnd)
        {
            DecreaseEachPaintCountByOne();
        }
        else
        {
            SyncLegacyLists();
        }

        RefreshState(true);
        InvokeSafely(
            OnSpecialAttackEnded,
            nameof(OnSpecialAttackEnded));
    }

    public void SetSpecialAttackGaugeRemaining(float remaining01)
    {
        float clamped = Mathf.Clamp01(remaining01);

        if (Mathf.Approximately(
                feverGaugeRemaining01,
                clamped))
        {
            return;
        }

        feverGaugeRemaining01 = clamped;

        InvokeSafely(
            OnFeverGaugeRemainingChanged,
            feverGaugeRemaining01,
            nameof(OnFeverGaugeRemainingChanged));
    }

    public bool TryConsumeForSpecialAttack()
    {
        return TryStartSpecialAttack();
    }

    public bool IsRequiredColor(string colorId)
    {
        return TryParseElement(colorId, out ElementType element) &&
               GetRequirement(element) != null;
    }

    public bool IsColorCollected(string colorId)
    {
        return TryParseElement(colorId, out ElementType element) &&
               GetCollectedPaintCount(element) > 0;
    }

    /// <summary>
    /// 이전 문자열 목록 기반 스테이지 설정 API를 유지합니다.
    /// 같은 색을 여러 번 넣으면 해당 색의 필요 개수로 변환됩니다.
    /// </summary>
    public void ConfigureStage(
        IEnumerable<string> newRequiredColors,
        bool preserveCollectedColors)
    {
        paintRequirements.Clear();

        if (newRequiredColors != null)
        {
            foreach (string colorId in newRequiredColors)
            {
                if (TryParseElement(
                        colorId,
                        out ElementType element))
                {
                    AddOrIncrementRequirement(element);
                }
            }
        }

        NormalizeRequirements();

        if (!preserveCollectedColors)
            ClearPaintCounts(false);
        else
            RebuildSequenceFromCountsIfNeeded();

        SyncLegacyLists();
        RefreshState(true);
    }

    public void ResetPaletteProgress(
        bool clearPaletteItems,
        bool clearCollectedColors)
    {
        if (clearPaletteItems)
        {
            paletteItemCount = 0;
            InvokeSafely(
            OnPaletteItemCountChanged,
            paletteItemCount,
            nameof(OnPaletteItemCountChanged));
        }

        if (clearCollectedColors)
            ClearPaintCounts(false);

        RefreshState(true);
    }

    public static bool TryParseElement(
        string colorId,
        out ElementType element)
    {
        if (string.IsNullOrWhiteSpace(colorId))
        {
            element = ElementType.None;
            return false;
        }

        string normalized =
            colorId.Trim()
                .Replace("Paint", string.Empty,
                    StringComparison.OrdinalIgnoreCase)
                .Replace("Color", string.Empty,
                    StringComparison.OrdinalIgnoreCase)
                .Trim();

        return Enum.TryParse(
                   normalized,
                   true,
                   out element) &&
               element != ElementType.None;
    }

    private void RefreshState(bool invokeEvent)
    {
        bool wasAvailable = canUseSpecialAttack;

        hasAllRequiredColors =
            TotalRequiredPaintCount > 0 &&
            CollectedRequiredPaintCount >=
            TotalRequiredPaintCount;

        canUseSpecialAttack =
            hasAllRequiredColors &&
            HasPaletteItem &&
            !isSpecialAttackActive;

        if (!wasAvailable && canUseSpecialAttack)
        {
            Debug.Log(
                "[팔레트] 게이지 충전 완료! Q키로 피버 사용 가능");

            InvokeSafely(OnSpecialAttackReady, nameof(OnSpecialAttackReady));
        }

        if (invokeEvent)
            InvokeSafely(
                OnPaletteStateChanged,
                nameof(OnPaletteStateChanged));
    }

    private bool AreAllRequirementsSatisfied()
    {
        return TotalRequiredPaintCount > 0 &&
               CollectedRequiredPaintCount >=
               TotalRequiredPaintCount;
    }

    private int CalculateTotalRequiredPaintCount()
    {
        return TotalRequiredPaintCount;
    }

    private int CalculateCollectedRequiredPaintCount()
    {
        return Mathf.Min(
            collectedPaintSequence.Count,
            TotalRequiredPaintCount);
    }

    private int CountCollectedRequirementKinds()
    {
        int count = 0;

        foreach (PaintRequirement requirement in paintRequirements)
        {
            if (GetCollectedPaintCount(requirement.Element) > 0)
                count++;
        }

        return count;
    }

    private PaintRequirement GetRequirement(
        ElementType element)
    {
        return paintRequirements.Find(
            requirement =>
                requirement.Element == element);
    }

    private PaintCountState GetOrCreatePaintState(
        ElementType element)
    {
        PaintCountState state =
            collectedPaintCounts.Find(
                entry => entry.Element == element);

        if (state != null)
            return state;

        state = new PaintCountState(element, 0);
        collectedPaintCounts.Add(state);

        return state;
    }

    private void DecreaseEachPaintCountByOne()
    {
        foreach (PaintCountState state in collectedPaintCounts)
        {
            if (state.Count <= 0)
                continue;

            int newCount =
                Mathf.Max(0, state.Count - 1);

            state.SetCount(newCount);

            InvokeSafely(
                OnPaintCountChanged,
                state.Element,
                newCount,
                nameof(OnPaintCountChanged));
        }

        SyncLegacyLists();

        Debug.Log(
            "[팔레트] 피버 종료: 보유 중인 각 물감 수량이 1개씩 감소했습니다.");
    }

    private void ClearPaintCounts(
        bool notify,
        bool clearGaugeSequence = true)
    {
        foreach (PaintCountState state in collectedPaintCounts)
        {
            state.SetCount(0);

            if (notify)
            {
                InvokeSafely(
                    OnPaintCountChanged,
                    state.Element,
                    0,
                    nameof(OnPaintCountChanged));
            }
        }

        collectedColorIds.Clear();

        if (clearGaugeSequence)
            collectedPaintSequence.Clear();
    }

    private void RebuildSequenceFromCountsIfNeeded()
    {
        int capacity = TotalRequiredPaintCount;
        List<ElementType> normalizedSequence = new();

        foreach (ElementType element in collectedPaintSequence)
        {
            if (normalizedSequence.Count >= capacity)
                break;

            TryAddUniqueGaugeElement(
                normalizedSequence,
                element);
        }

        if (normalizedSequence.Count == 0)
        {
            if (onlyConfiguredStageColors)
            {
                foreach (PaintRequirement requirement in paintRequirements)
                {
                    if (normalizedSequence.Count >= capacity)
                        break;

                    if (GetCollectedPaintCount(requirement.Element) > 0)
                    {
                        TryAddUniqueGaugeElement(
                            normalizedSequence,
                            requirement.Element);
                    }
                }
            }
            else
            {
                foreach (PaintCountState state in collectedPaintCounts)
                {
                    if (normalizedSequence.Count >= capacity)
                        break;

                    if (state.Count > 0)
                    {
                        TryAddUniqueGaugeElement(
                            normalizedSequence,
                            state.Element);
                    }
                }
            }
        }

        collectedPaintSequence = normalizedSequence;
    }

    private void TryAddUniqueGaugeElement(
        List<ElementType> target,
        ElementType element)
    {
        if (element == ElementType.None ||
            target.Contains(element) ||
            (onlyConfiguredStageColors &&
             GetRequirement(element) == null))
        {
            return;
        }

        target.Add(element);
    }

    private int GetMaximumUniqueGaugeColorCount()
    {
        if (onlyConfiguredStageColors)
        {
            HashSet<ElementType> uniqueElements = new();

            foreach (PaintRequirement requirement in paintRequirements)
            {
                if (requirement != null &&
                    requirement.Element != ElementType.None)
                {
                    uniqueElements.Add(requirement.Element);
                }
            }

            return uniqueElements.Count;
        }

        int count = 0;

        foreach (ElementType element in
                 Enum.GetValues(typeof(ElementType)))
        {
            if (element != ElementType.None)
                count++;
        }

        return count;
    }

    private void MigrateLegacyData()
    {
        if (paintRequirements.Count == 0 &&
            requiredColorIds.Count > 0)
        {
            foreach (string colorId in requiredColorIds)
            {
                if (TryParseElement(
                        colorId,
                        out ElementType element))
                {
                    AddOrIncrementRequirement(element);
                }
            }
        }

        if (collectedPaintCounts.Count == 0 &&
            collectedColorIds.Count > 0)
        {
            foreach (string colorId in collectedColorIds)
            {
                if (!TryParseElement(
                        colorId,
                        out ElementType element))
                {
                    continue;
                }

                GetOrCreatePaintState(element).Add(1);
            }
        }
    }

    private void AddOrIncrementRequirement(
        ElementType element)
    {
        PaintRequirement existing =
            GetRequirement(element);

        if (existing == null)
        {
            paintRequirements.Add(
                new PaintRequirement(
                    element,
                    1));

            return;
        }

        int nextCount =
            existing.RequiredCount + 1;

        Sprite icon = existing.Icon;
        Color color = existing.GaugeColor;

        paintRequirements.Remove(existing);
        paintRequirements.Add(
            new PaintRequirement(
                element,
                nextCount,
                icon,
                color));
    }

    private void NormalizeRequirements()
    {
        List<PaintRequirement> normalized = new();

        foreach (PaintRequirement requirement in paintRequirements)
        {
            if (requirement == null ||
                requirement.Element == ElementType.None)
            {
                continue;
            }

            requirement.Sanitize();

            PaintRequirement duplicate =
                normalized.Find(
                    item =>
                        item.Element ==
                        requirement.Element);

            if (duplicate == null)
            {
                normalized.Add(requirement);
                continue;
            }

            int mergedCount =
                duplicate.RequiredCount +
                requirement.RequiredCount;

            normalized.Remove(duplicate);
            normalized.Add(
                new PaintRequirement(
                    requirement.Element,
                    mergedCount,
                    duplicate.Icon ?? requirement.Icon,
                    duplicate.GaugeColor));
        }

        paintRequirements = normalized;
    }

    private void NormalizeCollectedCounts()
    {
        List<PaintCountState> normalized = new();

        foreach (PaintCountState state in collectedPaintCounts)
        {
            if (state == null ||
                state.Element == ElementType.None)
            {
                continue;
            }

            PaintCountState duplicate =
                normalized.Find(
                    item =>
                        item.Element ==
                        state.Element);

            if (duplicate == null)
            {
                normalized.Add(
                    new PaintCountState(
                        state.Element,
                        state.Count));
            }
            else
            {
                duplicate.Add(state.Count);
            }
        }

        collectedPaintCounts = normalized;
    }

    private void SyncLegacyLists()
    {
        requiredColorIds.Clear();
        collectedColorIds.Clear();

        foreach (PaintRequirement requirement in paintRequirements)
            requiredColorIds.Add(requirement.Element.ToString());

        foreach (PaintCountState state in collectedPaintCounts)
        {
            if (state.Count > 0)
                collectedColorIds.Add(state.Element.ToString());
        }
    }

    private static void InvokeSafely(
        Action action,
        string eventName)
    {
        if (action == null)
            return;

        foreach (Delegate subscriber in action.GetInvocationList())
        {
            try
            {
                ((Action)subscriber).Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    new Exception(
                        $"[StagePaletteManager] {eventName} 구독자 실행 중 오류",
                        exception));
            }
        }
    }

    private static void InvokeSafely<T>(
        Action<T> action,
        T value,
        string eventName)
    {
        if (action == null)
            return;

        foreach (Delegate subscriber in action.GetInvocationList())
        {
            try
            {
                ((Action<T>)subscriber).Invoke(value);
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    new Exception(
                        $"[StagePaletteManager] {eventName} 구독자 실행 중 오류",
                        exception));
            }
        }
    }

    private static void InvokeSafely<TFirst, TSecond>(
        Action<TFirst, TSecond> action,
        TFirst first,
        TSecond second,
        string eventName)
    {
        if (action == null)
            return;

        foreach (Delegate subscriber in action.GetInvocationList())
        {
            try
            {
                ((Action<TFirst, TSecond>)subscriber).Invoke(first, second);
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    new Exception(
                        $"[StagePaletteManager] {eventName} 구독자 실행 중 오류",
                        exception));
            }
        }
    }

    private StagePaletteManager FindOtherManagerInScene(
        Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (StagePaletteManager manager in
                     root.GetComponentsInChildren<StagePaletteManager>(true))
            {
                if (manager != this && manager.enabled)
                    return manager;
            }
        }

        return null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
