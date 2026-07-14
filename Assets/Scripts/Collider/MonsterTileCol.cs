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
        if (colorMinus.IsAbsorbed)
            return;

        FillColor fillColor = collision.GetComponent<FillColor>();

        if (fillColor == null)
            return;

        switch (collision.tag)
        {
            case "Monster":
                // 몬스터가 이미 색이 있으면 흡수하지 않음
                if (fillColor.HasColor)
                    return;

                colorMinus.Play();
                fillColor.SetColor(colorMinus.OriginalColor);
                break;

            case "Gun":
                colorMinus.Play();
                fillColor.GunSetColor(colorMinus.OriginalColor);
                break;
        }
    }
}
