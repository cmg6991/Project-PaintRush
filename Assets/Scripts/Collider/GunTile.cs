using UnityEngine;

public class GunTile : MonoBehaviour
{
    private ColorMinus colorMinus;

    private void Awake()
    {
        colorMinus = GetComponent<ColorMinus>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        FillColor gunColor = collision.GetComponent<FillColor>();

        if (gunColor == null)
            return;

        if (colorMinus.IsAbsorbed)
            return;

        colorMinus.Play();

        gunColor.GunSetColor(colorMinus.OriginalColor);
    }
}
