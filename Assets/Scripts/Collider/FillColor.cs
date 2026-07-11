using UnityEngine;

public class FillColor : MonoBehaviour
{
    public bool HasColor { get; private set; }
    public Color CurrentColor { get; private set; }
    public float ColorAmount { get; private set; }

    private Gradation gradation;
    

    private void Awake()
    {
        gradation = GetComponent<Gradation>();
    }

    public bool SetColor(Color color)
    {
        // 이미 색이 있으면 실패
        if (HasColor)
            return false;

        HasColor = true;
        CurrentColor = color;
        ColorAmount = 1f;

        // 색이 차오르는 연출
        gradation.Play(color);
        UpdateVisual();
        return true;
    }

    public void GunSetColor(Color color)
    {
        HasColor = true;
        CurrentColor = color;
        ColorAmount = 1f;

        gradation.Play(color);
        UpdateVisual();
    }

    public void Consume(float amount)
    {
        if (!HasColor) return;

        ColorAmount -= amount;
        ColorAmount = Mathf.Clamp01(ColorAmount);

        UpdateVisual();

        if (ColorAmount <= 0f)
        {
            HasColor = false;
            ClearColor();
        }
    }

    public void ClearColor()
    {
        HasColor = false;
        CurrentColor = Color.white;

        ColorAmount = 0f;
        gradation.Play(Color.white); // 흰색으로 돌아가는 연출
        UpdateVisual();
    }

    public void UpdateVisual()
    {
        gradation.SetAmount(ColorAmount,CurrentColor);
    }

}
