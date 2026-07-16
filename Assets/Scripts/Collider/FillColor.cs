using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class FillColor : MonoBehaviour
{
    public bool HasColor { get; private set; }
    public Color CurrentColor { get; private set; }
    public Color ShootColor
    {
        get
        {
            if (IsFever)
                return gradation.DisplayColor;

            return CurrentColor;
        }
    }
    public float ColorAmount { get; private set; }
    public bool IsFever { get; private set; }
    private Gradation gradation;

    [SerializeField] private Light2D feverLight;

    [SerializeField] private float minIntensity = 0.5f;
    [SerializeField] private float maxIntensity = 2f;

    [SerializeField] private float flashSpeed = 5f;

    [SerializeField] private ParticleSystem feverSparkle;

    private float lightTime;

    private bool prevHasColor;
    private Color prevColor;
    private float prevAmount;

    private void Awake()
    {
        gradation = GetComponent<Gradation>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            FeverOn();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            FeverOff();
        }
        UpdateFeverLight();
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
        if (IsFever) return;

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
    public void FeverOn()
    {
        prevHasColor = HasColor;
        prevColor = CurrentColor;
        prevAmount = ColorAmount;

        IsFever = true;

        HasColor = true;
        ColorAmount = 1f;

        if (gradation != null)
        {
            gradation.FeverOn();
            gradation.Play(Color.white);
        }
        if (feverSparkle != null)
            feverSparkle.Play();
        UIManager.Instance.ShowFeverUI();
        StartCoroutine(FeverTimer());
    }

    IEnumerator FeverTimer()
    {
        yield return new WaitForSeconds(5f);

        FeverOff();
    }

    public void FeverOff()
    {
        IsFever = false;

        if (feverSparkle != null)
            feverSparkle.Stop();

        if (gradation != null)
            gradation.FeverOff();

        HasColor = prevHasColor;
        CurrentColor = prevColor;
        ColorAmount = prevAmount;

        if (HasColor)
        {
            gradation.Play(CurrentColor);
            UpdateVisual();
        }
        else
        {
            ClearColor();
        }

        UIManager.Instance.HideFeverUI();
    }

    private void UpdateFeverLight()
    {
        if (feverLight == null)
            return;


        if (!IsFever)
        {
            feverLight.intensity = 0;
            return;
        }


        lightTime += Time.deltaTime * flashSpeed;


        float value = Mathf.Lerp(
            minIntensity,
            maxIntensity,
            (Mathf.Sin(lightTime) + 1f) * 0.5f
        );


        feverLight.intensity = value;


        // 무지개 색 변화
        float hue = Mathf.Repeat(Time.time * 0.5f, 1f);

        feverLight.color = Color.HSVToRGB(
            hue,
            0.8f,
            1f
        );
    }
}
