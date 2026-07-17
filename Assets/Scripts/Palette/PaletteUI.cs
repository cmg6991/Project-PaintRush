using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StagePaletteManager의 진행도를 화면 왼쪽 상단 게이지에 표시합니다.
/// </summary>
public class PaletteUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private StagePaletteManager paletteManager;

    [Header("게이지")]
    [Tooltip("Image Type을 Filled, Fill Method를 Horizontal, Origin을 Left로 설정")]
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text progressText;

    [Header("상태 표시")]
    [SerializeField] private GameObject paletteEquippedRoot;
    [SerializeField] private GameObject specialAttackReadyRoot;

    private void Awake()
    {
        ResolvePaletteManager();
    }

    private void OnEnable()
    {
        ResolvePaletteManager();

        if (paletteManager == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: StagePaletteManager를 찾지 못했습니다.");

            RefreshEmpty();
            return;
        }

        paletteManager.OnPaletteStateChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (paletteManager != null)
        {
            paletteManager.OnPaletteStateChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        if (paletteManager == null)
        {
            RefreshEmpty();
            return;
        }

        if (progressFill != null)
        {
            progressFill.fillAmount = paletteManager.Progress01;
        }

        if (progressText != null)
        {
            progressText.text =
                $"{paletteManager.CollectedRequiredColorCount}" +
                $"/{paletteManager.RequiredColorCount}";
        }

        if (paletteEquippedRoot != null)
        {
            paletteEquippedRoot.SetActive(
                paletteManager.HasPaletteItem);
        }

        if (specialAttackReadyRoot != null)
        {
            specialAttackReadyRoot.SetActive(
                paletteManager.CanUseSpecialAttack);
        }
    }

    private void RefreshEmpty()
    {
        if (progressFill != null)
        {
            progressFill.fillAmount = 0f;
        }

        if (progressText != null)
        {
            progressText.text = "0/0";
        }

        if (paletteEquippedRoot != null)
        {
            paletteEquippedRoot.SetActive(false);
        }

        if (specialAttackReadyRoot != null)
        {
            specialAttackReadyRoot.SetActive(false);
        }
    }

    private void ResolvePaletteManager()
    {
        if (paletteManager != null)
        {
            return;
        }

        paletteManager =
            StagePaletteManager.Instance != null
                ? StagePaletteManager.Instance
                : FindAnyObjectByType<StagePaletteManager>();
    }
}
