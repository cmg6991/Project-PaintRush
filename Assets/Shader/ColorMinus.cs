using UnityEngine;
using System.Collections;

public class ColorMinus : MonoBehaviour
{
    //public Material mat;
    //private float progress;

    //private SpriteRenderer sr;
    //private Color originalColor;

    //private void Start()
    //{
    //    sr = GetComponent<SpriteRenderer>();
    //    originalColor = sr.color; 

    //    StartCoroutine(Fill());
    //}

    //IEnumerator Fill()
    //{
    //    while (progress < 1f)
    //    {
    //        progress += Time.deltaTime;

    //        sr.color = Color.Lerp(originalColor, Color.white, progress);
    //        //mat.SetFloat("_Progress", progress);

    //        yield return null;
    //    }
    //}
    //public Color OriginalColor;
    //private SpriteRenderer sr;
    //private Material mat;
    //private float progress = 0f;

    //public float fillSpeed = 0.5f;

    //public Color OriginalColor { get; private set; }

    ////public Color OriginalColor => sr.color;

    //private void Start()
    //{
    //    sr = GetComponent<SpriteRenderer>();
    //    mat = sr.material;

    //    OriginalColor = sr.color;
    //    mat.SetColor("_OriginalColor", OriginalColor);
    //    sr.color = Color.white;

    //    StartCoroutine(Fill());
    //}

    //IEnumerator Fill()
    //{
    //    while (progress < 1f)
    //    {
    //        progress += Time.deltaTime * fillSpeed;

    //        mat.SetFloat("_Progress", progress);

    //        yield return null;
    //    }
    //}
    private SpriteRenderer sr;
    private Material mat;

    private float progress = 0f;

    public float fillSpeed = 0.5f;
    private bool isAbsorbed = false;
    public bool IsAbsorbed => isAbsorbed;

    public Color OriginalColor { get; private set; }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mat = new Material(sr.material);
        sr.material = mat;

        // 원래 색 저장
        OriginalColor = sr.color;

        // 셰이더에 전달
        mat.SetColor("_OriginalColor", OriginalColor);

        // 화면에는 흰색으로 보이게
        //sr.color = Color.white;
    }

    public void Play()
    {
        progress = 0f;

        sr.color = Color.white;

        if (isAbsorbed)
            return;
        isAbsorbed = true;

        StopAllCoroutines();
        StartCoroutine(Fill());
    }

    IEnumerator Fill()
    {
        while (progress < 1f)
        {
            progress += Time.deltaTime * fillSpeed;

            mat.SetFloat("_Progress", progress);

            yield return null;
        }
    }
}