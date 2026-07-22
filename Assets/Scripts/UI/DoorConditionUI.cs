using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 문 개방 조건을 한 줄 HUD로 표시합니다.
/// 남은 처치 수와 아직 문에 칠하지 않은 필수 색상만 표시합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class DoorConditionUI : MonoBehaviour
{
    [Serializable]
    public sealed class PaintConditionBinding
    {
        [SerializeField] private ElementType element;
        [SerializeField] private GameObject root;
        [SerializeField] private Image paintIcon;

        public ElementType Element => element;
        public GameObject Root => root;
        public Image PaintIcon => paintIcon;
    }

    [Header("참조")]
    [SerializeField] private DoorOpen door;
    [SerializeField] private StagePaletteManager paletteManager;
    [SerializeField] private CanvasGroup rootCanvasGroup;

    [Header("남은 몬스터")]
    [SerializeField] private TMP_Text remainingMonsterText;
    [SerializeField]
    private string remainingTextFormat =
        "남은 몬스터: {0}  /";

    [Header("남은 색상")]
    [Tooltip("Red~Purple 바인딩을 등록하면, 아직 문에 칠하지 않은 필수 색상만 표시됩니다.")]
    [SerializeField]
    private List<PaintConditionBinding> paintBindings = new();

    [Header("표시 규칙")]
    [SerializeField] private bool hideWhenDoorOpened;

    private bool subscribed;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
        Refresh();
    }

    private void Start()
    {
        ResolveReferences();
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void ResolveReferences()
    {
        door ??= FindAnyObjectByType<DoorOpen>();

        if (paletteManager == null ||
            paletteManager.gameObject.scene != gameObject.scene)
        {
            paletteManager =
                StagePaletteManager.FindForScene(this);
        }

        if (rootCanvasGroup != null)
            return;

        rootCanvasGroup = GetComponent<CanvasGroup>();
        rootCanvasGroup ??= gameObject.AddComponent<CanvasGroup>();
    }

    private void Subscribe()
    {
        if (subscribed || door == null)
            return;

        door.OnConditionChanged += Refresh;
        door.OnDoorOpened += Refresh;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || door == null)
            return;

        door.OnConditionChanged -= Refresh;
        door.OnDoorOpened -= Refresh;
        subscribed = false;
    }

    private void Refresh()
    {
        if (door == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(!hideWhenDoorOpened || !door.IsOpened);
        RefreshRemainingMonsterText();
        RefreshRemainingColors();
    }

    private void RefreshRemainingMonsterText()
    {
        if (remainingMonsterText == null)
            return;

        remainingMonsterText.text = string.Format(
            remainingTextFormat,
            door.RemainingKillCount);
    }

    private void RefreshRemainingColors()
    {
        foreach (PaintConditionBinding binding in paintBindings)
        {
            if (binding == null)
                continue;

            bool shouldShow =
                door.IsElementRequired(binding.Element) &&
                !door.IsElementPainted(binding.Element);

            if (binding.Root != null)
                binding.Root.SetActive(shouldShow);

            if (!shouldShow ||
                binding.PaintIcon == null ||
                paletteManager == null)
            {
                continue;
            }

            Sprite stageIcon =
                paletteManager.GetElementIcon(binding.Element);

            if (stageIcon != null)
                binding.PaintIcon.sprite = stageIcon;
        }
    }

    private void SetVisible(bool visible)
    {
        if (rootCanvasGroup == null)
            return;

        rootCanvasGroup.alpha = visible ? 1f : 0f;
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;
    }
}