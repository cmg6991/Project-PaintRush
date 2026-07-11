using UnityEngine;

public class MonsterTileCol : MonoBehaviour
{
    private ColorMinus colorMinus;

    private void Awake()
    {
        colorMinus = GetComponent<ColorMinus>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        FillColor monsterColor = collision.GetComponent<FillColor>();

        if (monsterColor == null)
            return;

        if (colorMinus.IsAbsorbed)
            return;

        if (monsterColor.HasColor)
            return;

        colorMinus.Play();

        monsterColor.SetColor(colorMinus.OriginalColor);
    }
}
