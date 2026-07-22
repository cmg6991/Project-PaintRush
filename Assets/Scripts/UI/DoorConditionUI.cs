using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 문 개방 조건 HUD입니다.
/// - 문을 열기 위해 필요한 처치 수만큼 Toggle을 표시합니다.
/// - 현재 처치 수 / 필요 처치 수를 텍스트로 표시합니다.
/// - 필수 색상은 항상 표시하고, 완료된 색은 흐리게 한 뒤 완료 마크를 켭니다.
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
        [SerializeField] private GameObject usedMark;

        public ElementType Element => element;
        public GameObject Root => root;
        public Image PaintIcon => paintIcon;
        public GameObject UsedMark => usedMark;
    }

    [Header("참조")]
    [SerializeField] private DoorOpen door;
    [SerializeField] private StagePaletteManager paletteManager;
    [SerializeField] private CanvasGroup rootCanvasGroup;

    [Header("몬스터 처치 조건")]
    [Tooltip("문 개방까지 앞으로 더 처치해야 하는 몬스터 수를 표시합니다.")]
    [SerializeField] private TMP_Text remainingMonsterText;

    [Tooltip("{0} 자리에 남은 필요 처치 수가 들어갑니다.")]
    [SerializeField] private string remainingMonsterTextFormat = "X {0}";

    [Header("문 색상 조건")]
    [Tooltip("Red~Purple 바인딩을 등록하면 현재 스테이지의 필수 색만 표시됩니다.")]
    [SerializeField] private List<PaintConditionBinding> paintBindings = new();

    [SerializeField, Range(0f, 1f)]
    private float completedPaintIconAlpha = 0.35f;

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
        // 실행 순서상 DoorOpen이 OnEnable 이후 준비되는 경우를 보완합니다.
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
        if (door == null || door.gameObject.scene != gameObject.scene)
            door = FindDoorInCurrentScene();

        if (paletteManager == null ||
            paletteManager.gameObject.scene != gameObject.scene)
        {
            paletteManager = StagePaletteManager.FindForScene(this);
        }

        if (rootCanvasGroup == null)
        {
            rootCanvasGroup = GetComponent<CanvasGroup>();
            rootCanvasGroup ??= gameObject.AddComponent<CanvasGroup>();
        }
    }

    private DoorOpen FindDoorInCurrentScene()
    {
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
            return null;

        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            DoorOpen found = root.GetComponentInChildren<DoorOpen>(true);

            if (found != null)
                return found;
        }

        return null;
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
        RefreshKillCondition();
        RefreshPaintConditions();
    }

    private void RefreshKillCondition()
    {
        int requiredCount =
            Mathf.Max(0, door.RequiredKillCount);

        int completedCount =
            Mathf.Clamp(
                door.CompletedKillCount,
                0,
                requiredCount);

        // 전체 생존 몬스터 수가 아니라
        // 문을 열기 위해 앞으로 더 처치해야 하는 수
        int remainingCount =
            Mathf.Max(
                0,
                requiredCount - completedCount);

        if (remainingMonsterText != null)
        {
            remainingMonsterText.text =
                string.Format(
                    remainingMonsterTextFormat,
                    remainingCount);
        }
    }



    private void RefreshPaintConditions()
    {
        foreach (PaintConditionBinding binding in paintBindings)
        {
            if (binding == null)
                continue;

            bool required = door.IsElementRequired(binding.Element);

            if (binding.Root != null)
                binding.Root.SetActive(required);

            if (!required)
                continue;

            if (binding.PaintIcon != null)
            {
                if (paletteManager != null)
                {
                    Sprite stageIcon =
                        paletteManager.GetElementIcon(binding.Element);

                    if (stageIcon != null)
                        binding.PaintIcon.sprite = stageIcon;
                }

                bool painted = door.IsElementPainted(binding.Element);
                Color iconColor = binding.PaintIcon.color;
                iconColor.a = painted ? completedPaintIconAlpha : 1f;
                binding.PaintIcon.color = iconColor;
            }

            if (binding.UsedMark != null)
            {
                binding.UsedMark.SetActive(
                    door.IsElementPainted(binding.Element));
            }
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
