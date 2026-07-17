using System;
using UnityEngine;

/// <summary>
/// 이전 PaletteInventory 참조를 유지하기 위한 호환 컴포넌트입니다.
/// 신규 코드는 StagePaletteManager를 직접 사용하는 것을 권장합니다.
/// 별도의 카운트를 저장하지 않으므로 데이터가 이중으로 관리되지 않습니다.
/// </summary>
public class PaletteInventory : MonoBehaviour
{
    [SerializeField] private StagePaletteManager paletteManager;

    public int Count =>
        paletteManager != null
            ? paletteManager.PaletteItemCount
            : 0;

    public bool HasPalette => Count > 0;

    public event Action<int> OnCountChanged;

    private void Awake()
    {
        ResolvePaletteManager();
    }

    private void OnEnable()
    {
        ResolvePaletteManager();

        if (paletteManager != null)
        {
            paletteManager.OnPaletteItemCountChanged += HandleCountChanged;
        }
    }

    private void OnDisable()
    {
        if (paletteManager != null)
        {
            paletteManager.OnPaletteItemCountChanged -= HandleCountChanged;
        }
    }

    public int Add(int amount = 1)
    {
        ResolvePaletteManager();

        return paletteManager != null
            ? paletteManager.EquipPaletteItem(amount)
            : 0;
    }

    public bool TryConsume(int amount = 1)
    {
        ResolvePaletteManager();

        return paletteManager != null &&
               paletteManager.TryConsumePaletteItems(amount);
    }

    public void Clear()
    {
        ResolvePaletteManager();
        paletteManager?.ClearPaletteItems();
    }

    private void HandleCountChanged(int count)
    {
        OnCountChanged?.Invoke(count);
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
