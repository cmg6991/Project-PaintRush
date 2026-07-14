using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PalettePickup : MonoBehaviour
{
    [SerializeField, Min(1)] private int amount = 1;

    private bool isCollected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected)
        {
            return;
        }

        PaletteInventory inventory =
            other.GetComponentInParent<PaletteInventory>();

        if (inventory == null)
        {
            return;
        }

        isCollected = true;
        inventory.Add(amount);

        Debug.Log(
            $"팔레트 획득! 현재 보유 수: {inventory.Count}"
        );

        Destroy(gameObject);
    }
}
