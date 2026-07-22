using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PaletteUI : MonoBehaviour
{
    [Serializable]
    public sealed class PaintCounterBinding
    {
        [SerializeField] private ElementType element;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject root;

        public ElementType Element => element;
        public Image IconImage => iconImage;
        public TMP_Text CountText => countText;
        public GameObject Root => root;
    }

    [Header("씬 로컬 참조")]
    [SerializeField] private StagePaletteManager paletteManager;

    [Header("HUD")]
    [SerializeField] private CanvasGroup hudCanvasGroup;
    [SerializeField] private Image paletteIconImage;

    [Header("직접 배치한 색상 카운터")]
    [SerializeField] private List<PaintCounterBinding> counterBindings = new();

    [Header("카운터 표시 통일")]
    [Tooltip("CountText의 줄바꿈과 Rect 크기만 통일합니다.")]
    [SerializeField] private bool normalizeCounterLayout = true;

    [Tooltip("체크하면 기존 TMP 폰트, Material, 크기, 색상, 스타일, 정렬을 그대로 유지합니다.")]
    [SerializeField] private bool preserveCounterTextAppearance = true;

    [SerializeField, Min(10f)] private float counterFontSize = 14f;
    [SerializeField, Min(28f)] private float counterTextWidth = 44f;
    [SerializeField, Min(16f)] private float counterTextHeight = 24f;
    [SerializeField, Min(12f)] private float counterIconSize = 18f;

    [Tooltip("Icon + CountText가 들어갈 Counter Root의 최소 폭입니다.")]
    [SerializeField, Min(50f)] private float counterRootWidth = 72f;

    [SerializeField, Min(20f)] private float counterRootHeight = 26f;
    [SerializeField, Min(0f)] private float counterInnerSpacing = 3f;

    [Header("게이지")]
    [SerializeField] private SequentialPaintGaugeGraphic gaugeGraphic;

    [Header("피버 준비")]
    [SerializeField] private GameObject readyRoot;

    private void Awake()
    {
        ResolveReferences();
        MakeBlankRootTransparent();
        NormalizeCounterBindings();
    }

    private void OnEnable()
    {
        ResolveReferences();
        NormalizeCounterBindings();

        if (paletteManager == null)
        {
            Debug.LogWarning($"{name}: 같은 씬의 StagePaletteManager를 찾지 못했습니다.");
            SetHudVisible(false);
            return;
        }

        paletteManager.OnPaletteStateChanged += RefreshAll;
        paletteManager.OnPaintCountChanged += HandlePaintCountChanged;
        paletteManager.OnPaintProgressUnitAdded += HandleProgressUnitAdded;
        paletteManager.OnFeverGaugeRemainingChanged += HandleFeverGaugeRemainingChanged;

        RefreshAll();
    }

    private void OnDisable()
    {
        if (paletteManager == null)
            return;

        paletteManager.OnPaletteStateChanged -= RefreshAll;
        paletteManager.OnPaintCountChanged -= HandlePaintCountChanged;
        paletteManager.OnPaintProgressUnitAdded -= HandleProgressUnitAdded;
        paletteManager.OnFeverGaugeRemainingChanged -= HandleFeverGaugeRemainingChanged;
    }

    private void ResolveReferences()
    {
        if (paletteManager == null ||
            paletteManager.gameObject.scene != gameObject.scene)
        {
            paletteManager = StagePaletteManager.FindForScene(this);
        }

        if (hudCanvasGroup == null)
        {
            hudCanvasGroup = GetComponent<CanvasGroup>();

            if (hudCanvasGroup == null)
                hudCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void MakeBlankRootTransparent()
    {
        Image rootImage = GetComponent<Image>();

        if (rootImage == null || rootImage.sprite != null)
            return;

        Color color = rootImage.color;
        color.a = 0f;
        rootImage.color = color;
        rootImage.raycastTarget = false;
    }

    private void NormalizeCounterBindings()
    {
        if (!normalizeCounterLayout)
            return;

        foreach (PaintCounterBinding binding in counterBindings)
        {
            if (binding == null)
                continue;

            ConfigureCounterRoot(binding.Root);
            ConfigureCounterIcon(binding.IconImage);
            ConfigureCounterText(binding.CountText);
        }
    }

    private static void SetCounterText(
        TMP_Text countText,
        int count)
    {
        if (countText == null)
            return;

        countText.richText = true;
        countText.isRightToLeftText = false;

        // <nobr>는 TMP가 x와 숫자 사이에서 강제로 줄바꿈하는 것을 막습니다.
        countText.text = $"<nobr>x{count}</nobr>";

        countText.ForceMeshUpdate(
            ignoreActiveState: true,
            forceTextReparsing: true);
    }

    private void ConfigureCounterRoot(GameObject root)
    {
        if (root == null)
            return;

        LayoutElement rootLayout =
            root.GetComponent<LayoutElement>();

        if (rootLayout == null)
            rootLayout = root.AddComponent<LayoutElement>();

        rootLayout.minWidth = counterRootWidth;
        rootLayout.preferredWidth = counterRootWidth;
        rootLayout.minHeight = counterRootHeight;
        rootLayout.preferredHeight = counterRootHeight;
        rootLayout.flexibleWidth = 0f;
        rootLayout.flexibleHeight = 0f;

        HorizontalLayoutGroup row =
            root.GetComponent<HorizontalLayoutGroup>();

        if (row != null)
        {
            row.spacing = counterInnerSpacing;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
        }
    }

    private void ConfigureCounterIcon(Image iconImage)
    {
        if (iconImage == null)
            return;

        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        LayoutElement layout =
            iconImage.GetComponent<LayoutElement>();

        if (layout == null)
            layout = iconImage.gameObject.AddComponent<LayoutElement>();

        layout.minWidth = counterIconSize;
        layout.preferredWidth = counterIconSize;
        layout.minHeight = counterIconSize;
        layout.preferredHeight = counterIconSize;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;
    }

    private void ConfigureCounterText(TMP_Text countText)
    {
        if (countText == null)
            return;

        // 세로 줄바꿈 문제를 막는 데 필요한 설정만 변경합니다.
        // 기존 폰트 Asset, 폰트 Material, 색상, 스타일은 절대 변경하지 않습니다.
        // Unity 6 / 최신 TMP에서는 enableWordWrapping만으로는
        // 줄바꿈이 남는 경우가 있어 textWrappingMode도 함께 지정합니다.
        countText.richText = true;
        countText.isRightToLeftText = false;
        //countText.enableWordWrapping = false;
        countText.textWrappingMode = TextWrappingModes.NoWrap;
        countText.overflowMode = TextOverflowModes.Overflow;
        countText.raycastTarget = false;

        // Auto Size는 폭에 따라 글자가 흔들리므로 항상 끕니다.
        // 폰트 Asset/Material/Color/Style은 변경하지 않습니다.
        countText.enableAutoSizing = false;

        if (!preserveCounterTextAppearance)
        {
            countText.fontSize = counterFontSize;
            countText.alignment = TextAlignmentOptions.MidlineLeft;
        }

        LayoutElement layout =
            countText.GetComponent<LayoutElement>();

        if (layout == null)
            layout = countText.gameObject.AddComponent<LayoutElement>();

        layout.minWidth = counterTextWidth;
        layout.preferredWidth = counterTextWidth;
        layout.minHeight = counterTextHeight;
        layout.preferredHeight = counterTextHeight;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        RectTransform rect = countText.rectTransform;
        rect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            counterTextWidth);
        rect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            counterTextHeight);
    }

    private void HandlePaintCountChanged(ElementType element, int count)
    {
        PaintCounterBinding binding = FindBinding(element);

        if (binding?.CountText != null)
            SetCounterText(binding.CountText, count);
    }

    private void HandleFeverGaugeRemainingChanged(float remaining01)
    {
        if (gaugeGraphic == null)
            return;

        gaugeGraphic.SetDrainProgress(
            remaining01,
            paletteManager != null &&
            paletteManager.IsSpecialAttackActive);
    }

    private void HandleProgressUnitAdded(ElementType element)
    {
        if (gaugeGraphic == null || paletteManager == null)
            return;

        Color color = paletteManager.GetElementGaugeColor(element);
        color.a = 1f;

        gaugeGraphic.SetDrainProgress(1f, false);

        gaugeGraphic.PlayAcquireAnimation(
            paletteManager.CollectedPaintSequence.Count - 1,
            color);

        RefreshCounters();
        RefreshReady();
    }

    private void RefreshAll()
    {
        if (paletteManager == null)
        {
            SetHudVisible(false);
            return;
        }

        SetHudVisible(
            paletteManager.ShouldShowPaletteHud);
        RefreshCounterVisibility();
        RefreshCounters();
        RefreshGaugeImmediate();
        RefreshReady();
    }

    private void RefreshCounterVisibility()
    {
        NormalizeCounterBindings();

        foreach (PaintCounterBinding binding in counterBindings)
        {
            if (binding == null)
                continue;

            StagePaletteManager.PaintRequirement requirement =
                GetRequirement(binding.Element);

            bool required = requirement != null;

            if (binding.Root != null)
                binding.Root.SetActive(required);

            if (!required)
                continue;

            if (binding.IconImage != null && requirement.Icon != null)
            {
                binding.IconImage.sprite = requirement.Icon;
                binding.IconImage.color = Color.white;
                binding.IconImage.preserveAspect = true;
            }
        }
    }

    private void RefreshCounters()
    {
        foreach (PaintCounterBinding binding in counterBindings)
        {
            if (binding == null || binding.CountText == null)
                continue;

            int count = paletteManager.GetCollectedPaintCount(binding.Element);
            SetCounterText(binding.CountText, count);
        }
    }

    private void RefreshGaugeImmediate()
    {
        if (gaugeGraphic == null)
            return;

        List<Color> colors = new();

        foreach (ElementType element in paletteManager.CollectedPaintSequence)
        {
            Color color = paletteManager.GetElementGaugeColor(element);
            color.a = 1f;
            colors.Add(color);
        }

        gaugeGraphic.SetGauge(
            paletteManager.TotalRequiredPaintCount,
            colors);

        gaugeGraphic.SetDrainProgress(
            paletteManager.IsSpecialAttackActive
                ? paletteManager.FeverGaugeRemaining01
                : 1f,
            paletteManager.IsSpecialAttackActive);
    }

    private void RefreshReady()
    {
        if (readyRoot != null && readyRoot != gameObject)
            readyRoot.SetActive(paletteManager.CanUseSpecialAttack);
    }

    private PaintCounterBinding FindBinding(ElementType element)
    {
        return counterBindings.Find(
            binding => binding != null && binding.Element == element);
    }

    private StagePaletteManager.PaintRequirement GetRequirement(
        ElementType element)
    {
        foreach (StagePaletteManager.PaintRequirement requirement
                 in paletteManager.Requirements)
        {
            if (requirement != null &&
                requirement.Element == element)
            {
                return requirement;
            }
        }

        return null;
    }

    private void SetHudVisible(bool visible)
    {
        if (hudCanvasGroup == null)
            return;

        hudCanvasGroup.alpha = visible ? 1f : 0f;
        hudCanvasGroup.interactable = false;
        hudCanvasGroup.blocksRaycasts = false;
    }

    private void OnValidate()
    {
        counterFontSize = Mathf.Max(10f, counterFontSize);
        counterTextWidth = Mathf.Max(20f, counterTextWidth);
        counterTextHeight = Mathf.Max(16f, counterTextHeight);
        counterIconSize = Mathf.Max(12f, counterIconSize);
    }
}
