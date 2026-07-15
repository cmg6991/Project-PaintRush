using System;
using UnityEngine;

public class PaletteInventory : MonoBehaviour
{
    [SerializeField, Min(0)] private int startingCount;
    [SerializeField, Min(1)] private int maxCount = 99;

    public int Count { get; private set; }
    public bool HasPalette => Count > 0;

    public event Action<int> OnCountChanged;

    private void Awake()
    {
        Count = Mathf.Clamp(
            startingCount,
            0,
            maxCount
        );
    }

    public int Add(int amount = 1)
    {
        if (amount <= 0)
        {
            return Count;
        }

        int previousCount = Count;

        Count = Mathf.Clamp(
            Count + amount,
            0,
            maxCount
        );

        if (Count != previousCount)
        {
            OnCountChanged?.Invoke(Count);
        }

        return Count;
    }

    public bool TryConsume(int amount = 1)
    {
        if (amount <= 0 || Count < amount)
        {
            return false;
        }

        Count -= amount;
        OnCountChanged?.Invoke(Count);

        return true;
    }

    public void Clear()
    {
        if (Count == 0)
        {
            return;
        }

        Count = 0;
        OnCountChanged?.Invoke(Count);
    }
}
