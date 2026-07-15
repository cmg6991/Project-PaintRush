using TMPro;
using UnityEngine;

public class PaletteUI : MonoBehaviour
{
    [SerializeField] private PaletteInventory inventory;
    [SerializeField] private GameObject availableRoot;
    [SerializeField] private TMP_Text countText;

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<PaletteInventory>();
        }
    }

    private void OnEnable()
    {
        if (inventory == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: PaletteInventory가 연결되지 않았습니다."
            );
            Refresh(0);
            return;
        }

        inventory.OnCountChanged += Refresh;
        Refresh(inventory.Count);
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnCountChanged -= Refresh;
        }
    }

    private void Refresh(int count)
    {
        if (availableRoot != null)
        {
            availableRoot.SetActive(count > 0);
        }

        if (countText != null)
        {
            countText.text = count.ToString();
        }
    }
}
