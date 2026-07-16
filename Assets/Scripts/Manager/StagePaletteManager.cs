using System;
using System.Collections.Generic;
using UnityEngine;

public class StagePaletteManager : MonoBehaviour
{
    public static StagePaletteManager Instance { get; private set; }

    [Header("스테이지 설정")]
    [Tooltip("현재 스테이지에서 모아야 하는 색상 ID")]
    [SerializeField]
    private List<string> requiredColorIds = new List<string>();

    [Header("피버 사용 규칙")]
    [Tooltip("피버 시작 시 팔레트 아이템을 소비할지")]
    [SerializeField]
    private bool consumePaletteItemOnUse = false;

    [Tooltip("피버 시작 시 색을 초기화할지")]
    [SerializeField]
    private bool clearCollectedColorsOnUse = false;

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

    private readonly HashSet<string> collectedColorSet =
        new HashSet<string>(
            StringComparer.OrdinalIgnoreCase
        );

    public IReadOnlyList<string> RequiredColorIds =>
        requiredColorIds;

    public IReadOnlyList<string> CollectedColorIds =>
        collectedColorIds;

    public int RequiredColorCount =>
        requiredColorIds.Count;

    public int CollectedRequiredColorCount =>
        CountCollectedRequiredColors();

    public int PaletteItemCount =>
        paletteItemCount;

    public bool HasPaletteItem =>
        paletteItemCount > 0;

    public bool HasAllRequiredColors =>
        hasAllRequiredColors;

    public bool CanUseSpecialAttack =>
        canUseSpecialAttack;

    public event Action OnPaletteStateChanged;
    public event Action<string> OnColorCollected;
    public event Action<int> OnPaletteItemCountChanged;
    public event Action OnSpecialAttackReady;

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
            Debug.Log(
                "[팔레트] 팔레트 아이템을 장착하지 않아 " +
                "색상을 등록할 수 없습니다."
            );

            return false;
        }

        string normalizedId =
            NormalizeColorId(colorId);

        if (string.IsNullOrEmpty(normalizedId))
        {
            Debug.LogWarning(
                "[StagePaletteManager] " +
                "비어 있는 색상 ID는 등록할 수 없습니다."
            );

            return false;
        }

        if (!IsRequiredColor(normalizedId))
        {
            Debug.Log(
                $"[팔레트] 현재 스테이지 필요 색상이 아닙니다: " +
                $"{normalizedId}"
            );

            return false;
        }

        if (collectedColorSet.Contains(normalizedId))
        {
            Debug.Log(
                $"[팔레트] 이미 수집한 색상: {normalizedId}"
            );

            return false;
        }

        collectedColorSet.Add(normalizedId);
        collectedColorIds.Add(normalizedId);

        Debug.Log(
            $"[팔레트] 색상 수집: {normalizedId} " +
            $"({CollectedRequiredColorCount}/" +
            $"{RequiredColorCount})"
        );

        OnColorCollected?.Invoke(normalizedId);

        RefreshState(true);
        return true;
    }

    public void EquipPaletteItem(int amount = 1)
    {
        if (amount <= 0)
        {
            return;
        }

        paletteItemCount += amount;

        Debug.Log(
            $"[팔레트] 팔레트 아이템 장착. " +
            $"현재 보유: {paletteItemCount}"
        );

        OnPaletteItemCountChanged?.Invoke(
            paletteItemCount
        );

        RefreshState(true);
    }

    public bool TryConsumeForSpecialAttack()
    {
        if (!CanUseSpecialAttack)
        {
            Debug.Log(
                "[팔레트] 피버 사용 불가. " +
                $"색상 완료: {HasAllRequiredColors}, " +
                $"팔레트 장착: {HasPaletteItem}"
            );

            return false;
        }

        if (consumePaletteItemOnUse)
        {
            paletteItemCount =
                Mathf.Max(0, paletteItemCount - 1);

            OnPaletteItemCountChanged?.Invoke(
                paletteItemCount
            );
        }

        if (clearCollectedColorsOnUse)
        {
            collectedColorIds.Clear();
            collectedColorSet.Clear();
        }

        RefreshState(true);
        return true;
    }

    public bool IsRequiredColor(string colorId)
    {
        string normalizedId =
            NormalizeColorId(colorId);

        foreach (string requiredId
                 in requiredColorIds)
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
        string normalizedId =
            NormalizeColorId(colorId);

        return collectedColorSet.Contains(
            normalizedId
        );
    }

    public void ConfigureStage(
        IEnumerable<string> newRequiredColors,
        bool preserveCollectedColors)
    {
        requiredColorIds.Clear();

        if (newRequiredColors != null)
        {
            foreach (string colorId
                     in newRequiredColors)
            {
                string normalizedId =
                    NormalizeColorId(colorId);

                if (string.IsNullOrEmpty(normalizedId))
                {
                    continue;
                }

                if (!ContainsIgnoreCase(
                        requiredColorIds,
                        normalizedId))
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

            OnPaletteItemCountChanged?.Invoke(
                paletteItemCount
            );
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
        bool wasAvailable =
            canUseSpecialAttack;

        hasAllRequiredColors =
            requiredColorIds.Count > 0 &&
            CountCollectedRequiredColors() >=
            requiredColorIds.Count;

        canUseSpecialAttack =
            hasAllRequiredColors &&
            HasPaletteItem;

        if (!wasAvailable &&
            canUseSpecialAttack)
        {
            Debug.Log(
                "[팔레트] 모든 조건 완료! 피버 사용 가능"
            );

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

        foreach (string requiredId
                 in requiredColorIds)
        {
            if (collectedColorSet.Contains(
                    requiredId))
            {
                count++;
            }
        }

        return count;
    }

    private void NormalizeRequiredColorIds()
    {
        List<string> normalizedList =
            new List<string>();

        foreach (string colorId
                 in requiredColorIds)
        {
            string normalizedId =
                NormalizeColorId(colorId);

            if (string.IsNullOrEmpty(normalizedId))
            {
                continue;
            }

            if (!ContainsIgnoreCase(
                    normalizedList,
                    normalizedId))
            {
                normalizedList.Add(normalizedId);
            }
        }

        requiredColorIds = normalizedList;
    }

    private void RebuildCollectedSet()
    {
        collectedColorSet.Clear();

        List<string> normalizedList =
            new List<string>();

        foreach (string colorId
                 in collectedColorIds)
        {
            string normalizedId =
                NormalizeColorId(colorId);

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

    private static string NormalizeColorId(
        string colorId)
    {
        if (string.IsNullOrWhiteSpace(colorId))
        {
            return string.Empty;
        }

        return colorId.Trim();
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