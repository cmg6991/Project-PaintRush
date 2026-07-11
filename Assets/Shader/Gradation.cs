using System.Collections;
//using System.Drawing;
using UnityEngine;

public class Gradation : MonoBehaviour
{
    public Material mat;
    private float progress;
    public ColorMinus colorMinus;

    private SpriteRenderer sr;

    private void Start()
    {

        sr = GetComponent<SpriteRenderer>();
        mat = sr.material;

        mat.SetColor("_TargetColor", colorMinus.OriginalColor);   // Å×½ºÆ®

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
}
