using System;
using UnityEngine;

/// <summary>
/// 기존 PaletteInventory 참조를 유지하는 호환 컴포넌트입니다.
/// 실제 데이터는 같은 씬의 StagePaletteManager 한 곳에서 관리합니다.
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
            paletteManager.OnPaletteItemCountChanged +=
                HandleCountChanged;
        }
    }

    private void OnDisable()
    {
        if (paletteManager != null)
        {
            paletteManager.OnPaletteItemCountChanged -=
                HandleCountChanged;
        }
    }

    public int Add(int amount = 1)
    {
        ResolvePaletteManager();

        return paletteManager != null
            ? paletteManager.EquipPaletteItem(amount)
            : 0;
    }

    public bool AddPaint(
        ElementType element,
        int amount = 1)
    {
        ResolvePaletteManager();

        return paletteManager != null &&
               paletteManager.RegisterPaint(
                   element,
                   amount);
    }

    public int GetPaintCount(ElementType element)
    {
        ResolvePaletteManager();

        return paletteManager != null
            ? paletteManager.GetCollectedPaintCount(element)
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
            return;

        paletteManager =
            StagePaletteManager.FindForScene(this);
    }
}
