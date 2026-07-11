using System.Collections;
//using System.Drawing;
using UnityEngine;

public class Gradation : MonoBehaviour
{
    private SpriteRenderer sr;
    private Material mat;

    private float progress;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mat = new Material(sr.material);
        sr.material = mat;
    }

    public void SetAmount(float amount,Color baseColor)
    {
        mat.SetFloat("_FillAmount", amount);
        mat.SetColor("_TargetColor", baseColor);
    }

    public void Play(Color targetColor)
    {
        progress = 0f;

        mat.SetColor("_TargetColor", targetColor);
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
}
