using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지의 팔레트 아이템, 수집 색상, 특수공격 진행 상태를 관리한다.
/// 실제 입력과 데미지는 PaletteSpecialAttack이 담당한다.
/// </summary>
public class StagePaletteManager : MonoBehaviour
{
    public static StagePaletteManager Instance { get; private set; }

    [Header("스테이지 설정")]
    [Tooltip("현재 스테이지에서 모아야 하는 색상 ID")]
    [SerializeField]
    private List<string> requiredColorIds = new List<string>();

    [Header("피버 종료 규칙")]
    [Tooltip("피버가 끝났을 때 팔레트 아이템을 초기화")]
    [SerializeField]
    private bool resetPaletteItemOnFeverEnd = true;

    [Tooltip("피버가 끝났을 때 수집한 색을 초기화")]
    [SerializeField]
    private bool resetCollectedColorsOnFeverEnd = true;

    [Header("씬 전환")]
    [SerializeField]
    private bool persistAcrossScenes = false;

    [Header("런타임 확인")]
    [SerializeField]
    private List<string> collectedColorIds = new List<string>();

    [SerializeField, Min(0)]
    private int paletteItemCount;

    [SerializeField]
    private bool hasAllRequiredColors;

    [SerializeField]
    private bool canUseSpecialAttack;

    [SerializeField]
    private bool isSpecialAttackActive;

    private readonly HashSet<string> collectedColorSet =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> RequiredColorIds => requiredColorIds;
    public IReadOnlyList<string> CollectedColorIds => collectedColorIds;

    public int RequiredColorCount => requiredColorIds.Count;
    public int CollectedRequiredColorCount => CountCollectedRequiredColors();
    public int PaletteItemCount => paletteItemCount;

    public float Progress01
    {
        get
        {
            if (RequiredColorCount <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01(
                (float)CollectedRequiredColorCount /
                RequiredColorCount
            );
        }
    }

    public bool HasPaletteItem => paletteItemCount > 0;
    public bool HasAllRequiredColors => hasAllRequiredColors;
    public bool CanUseSpecialAttack => canUseSpecialAttack;
    public bool IsSpecialAttackActive => isSpecialAttackActive;

    public event Action OnPaletteStateChanged;
    public event Action<string> OnColorCollected;
    public event Action<int> OnPaletteItemCountChanged;
    public event Action OnSpecialAttackReady;
    public event Action OnSpecialAttackStarted;
    public event Action OnSpecialAttackEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        NormalizeRequiredColorIds();
        RebuildCollectedSet();
        RefreshState(false);
    }

    public bool RegisterColor(string colorId)
    {
        if (!HasPaletteItem)
        {
            return false;
        }

        string normalizedId = NormalizeColorId(colorId);

        if (string.IsNullOrEmpty(normalizedId))
        {
            Debug.LogWarning(
                "[StagePaletteManager] 비어 있는 색상 ID는 등록할 수 없습니다."
            );
            return false;
        }

        if (!IsRequiredColor(normalizedId))
        {
            return false;
        }

        if (!collectedColorSet.Add(normalizedId))
        {
            return false;
        }

        collectedColorIds.Add(normalizedId);

        Debug.Log(
            $"[팔레트] 색상 수집: {normalizedId} " +
            $"({CollectedRequiredColorCount}/{RequiredColorCount})"
        );

        OnColorCollected?.Invoke(normalizedId);
        RefreshState(true);
        return true;
    }

    public int EquipPaletteItem(int amount = 1)
    {
        if (amount <= 0)
        {
            return paletteItemCount;
        }

        paletteItemCount += amount;

        Debug.Log(
            $"[팔레트] 팔레트 아이템 장착. 현재 보유: {paletteItemCount}"
        );

        OnPaletteItemCountChanged?.Invoke(paletteItemCount);
        RefreshState(true);

        return paletteItemCount;
    }

    /// <summary>
    /// 이전 PaletteInventory와의 호환용.
    /// 지정한 개수만큼 팔레트 아이템을 소비한다.
    /// </summary>
    public bool TryConsumePaletteItems(int amount = 1)
    {
        if (amount <= 0 || paletteItemCount < amount)
        {
            return false;
        }

        paletteItemCount -= amount;
        OnPaletteItemCountChanged?.Invoke(paletteItemCount);
        RefreshState(true);

        return true;
    }

    /// <summary>
    /// 이전 PaletteInventory와의 호환용.
    /// 보유 중인 팔레트 아이템을 모두 제거한다.
    /// </summary>
    public void ClearPaletteItems()
    {
        if (paletteItemCount == 0)
        {
            return;
        }

        paletteItemCount = 0;
        OnPaletteItemCountChanged?.Invoke(paletteItemCount);
        RefreshState(true);
    }

    /// <summary>
    /// 피버 시작 조건을 확인하고 사용 중 상태로 예약한다.
    /// PaletteSpecialAttack.TryActivate()에서 호출한다.
    /// </summary>
    public bool TryStartSpecialAttack()
    {
        if (!CanUseSpecialAttack)
        {
            Debug.Log(
                "[팔레트] 피버 사용 불가. " +
                $"색상 완료={HasAllRequiredColors}, " +
                $"팔레트 장착={HasPaletteItem}, " +
                $"이미 사용 중={IsSpecialAttackActive}"
            );

            return false;
        }

        isSpecialAttackActive = true;
        RefreshState(true);
        OnSpecialAttackStarted?.Invoke();

        return true;
    }

    /// <summary>
    /// 피버 종료 후 팔레트와 수집 색을 초기화한다.
    /// </summary>
    public void CompleteSpecialAttack()
    {
        if (!isSpecialAttackActive)
        {
            return;
        }

        isSpecialAttackActive = false;

        if (resetPaletteItemOnFeverEnd)
        {
            paletteItemCount = 0;
            OnPaletteItemCountChanged?.Invoke(paletteItemCount);
        }

        if (resetCollectedColorsOnFeverEnd)
        {
            collectedColorIds.Clear();
            collectedColorSet.Clear();
        }

        RefreshState(true);
        OnSpecialAttackEnded?.Invoke();
    }

    /// <summary>
    /// 이전 코드와의 호환용. 새 코드는 TryStartSpecialAttack을 사용한다.
    /// </summary>
    public bool TryConsumeForSpecialAttack()
    {
        return TryStartSpecialAttack();
    }

    public bool IsRequiredColor(string colorId)
    {
        string normalizedId = NormalizeColorId(colorId);

        foreach (string requiredId in requiredColorIds)
        {
            if (string.Equals(
                    requiredId,
                    normalizedId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsColorCollected(string colorId)
    {
        return collectedColorSet.Contains(
            NormalizeColorId(colorId)
        );
    }

    public void ConfigureStage(
        IEnumerable<string> newRequiredColors,
        bool preserveCollectedColors)
    {
        requiredColorIds.Clear();

        if (newRequiredColors != null)
        {
            foreach (string colorId in newRequiredColors)
            {
                string normalizedId = NormalizeColorId(colorId);

                if (string.IsNullOrEmpty(normalizedId))
                {
                    continue;
                }

                if (!ContainsIgnoreCase(requiredColorIds, normalizedId))
                {
                    requiredColorIds.Add(normalizedId);
                }
            }
        }

        if (!preserveCollectedColors)
        {
            collectedColorIds.Clear();
            collectedColorSet.Clear();
        }

        RefreshState(true);
    }

    public void ResetPaletteProgress(
        bool clearPaletteItems,
        bool clearCollectedColors)
    {
        if (clearPaletteItems)
        {
            paletteItemCount = 0;
            OnPaletteItemCountChanged?.Invoke(paletteItemCount);
        }

        if (clearCollectedColors)
        {
            collectedColorIds.Clear();
            collectedColorSet.Clear();
        }

        RefreshState(true);
    }

    private void RefreshState(bool invokeEvent)
    {
        bool wasAvailable = canUseSpecialAttack;

        hasAllRequiredColors =
            requiredColorIds.Count > 0 &&
            CountCollectedRequiredColors() >= requiredColorIds.Count;

        canUseSpecialAttack =
            hasAllRequiredColors &&
            HasPaletteItem &&
            !isSpecialAttackActive;

        if (!wasAvailable && canUseSpecialAttack)
        {
            Debug.Log("[팔레트] 모든 조건 완료! 피버 사용 가능");
            OnSpecialAttackReady?.Invoke();
        }

        if (invokeEvent)
        {
            OnPaletteStateChanged?.Invoke();
        }
    }

    private int CountCollectedRequiredColors()
    {
        int count = 0;

        foreach (string requiredId in requiredColorIds)
        {
            if (collectedColorSet.Contains(requiredId))
            {
                count++;
            }
        }

        return count;
    }

    private void NormalizeRequiredColorIds()
    {
        List<string> normalizedList = new List<string>();

        foreach (string colorId in requiredColorIds)
        {
            string normalizedId = NormalizeColorId(colorId);

            if (string.IsNullOrEmpty(normalizedId))
            {
                continue;
            }

            if (!ContainsIgnoreCase(normalizedList, normalizedId))
            {
                normalizedList.Add(normalizedId);
            }
        }

        requiredColorIds = normalizedList;
    }

    private void RebuildCollectedSet()
    {
        collectedColorSet.Clear();

        List<string> normalizedList = new List<string>();

        foreach (string colorId in collectedColorIds)
        {
            string normalizedId = NormalizeColorId(colorId);

            if (string.IsNullOrEmpty(normalizedId))
            {
                continue;
            }

            if (collectedColorSet.Add(normalizedId))
            {
                normalizedList.Add(normalizedId);
            }
        }

        collectedColorIds = normalizedList;
    }

    private static string NormalizeColorId(string colorId)
    {
        return string.IsNullOrWhiteSpace(colorId)
            ? string.Empty
            : colorId.Trim();
    }

    private static bool ContainsIgnoreCase(
        List<string> source,
        string value)
    {
        foreach (string item in source)
        {
            if (string.Equals(
                    item,
                    value,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
