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
    public Color OriginalColor;
    private SpriteRenderer sr;
    private Material mat;
    private float progress = 0f;

    public float fillSpeed = 0.5f;

    //public Color OriginalColor => sr.color;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        mat = sr.material;

        OriginalColor = sr.color;
        mat.SetColor("_OriginalColor", OriginalColor);
        sr.color = Color.white;

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