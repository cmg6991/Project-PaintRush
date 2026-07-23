using System.Collections;
//using System.Drawing;
using UnityEngine;

public class Gradation : MonoBehaviour
{
    private SpriteRenderer sr;
    private Material mat;

    private float progress;
    private bool isFever = false;
    private Color originColor = Color.white;
    public Color DisplayColor { get; private set; }

    [SerializeField] private float rainbowSpeed = 0.5f;

    [SerializeField] private float glowSpeed = 5f;
    [SerializeField] private float glowPower = 1.5f;
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mat = new Material(sr.material);
        sr.material = mat;

        DisplayColor = Color.white;
    }

    void Update()
    {
        if (!isFever)
            return;

        float hue = Mathf.Repeat(Time.time * rainbowSpeed, 1f);

        DisplayColor = Color.HSVToRGB(hue, 1f, 1f);

        float glow = Mathf.Lerp(
        1f,
        glowPower,
        (Mathf.Sin(Time.time * glowSpeed) + 1f) * 0.5f
    );


        mat.SetColor("_TargetColor", DisplayColor*glow);
    }


    public void SetAmount(float amount,Color baseColor)
    {
        //mat.SetFloat("_FillAmount", amount);
        //mat.SetColor("_TargetColor", baseColor);

        originColor = baseColor;

        if (!isFever)
        {
            DisplayColor = baseColor;
            mat.SetColor("_TargetColor", baseColor);
        }

        float displayAmount = amount <= 0f ? 0f : Mathf.Lerp(0.15f, 1f, amount);
        mat.SetFloat("_FillAmount", displayAmount);

    }

    public void Play(Color targetColor)
    {
        progress = 0f;

        //mat.SetColor("_TargetColor", targetColor);

        originColor = targetColor;

        if (!isFever)
        {
            DisplayColor = targetColor;
            mat.SetColor("_TargetColor", targetColor);
        }

        mat.SetFloat("_FillAmount", 1f);
        StopAllCoroutines();
        StartCoroutine(Fill());
    }

    IEnumerator Fill()
    {
        while (progress < 1f)
        {
            progress += Time.deltaTime;

            mat.SetFloat("_Progress", progress);

            yield return null;
        }
    }
    public void FeverOn()
    {
        isFever = true;
    }

    public void FeverOff()
    {
        isFever = false;

        DisplayColor = originColor;

        mat.SetColor("_TargetColor", originColor);
    }
}
