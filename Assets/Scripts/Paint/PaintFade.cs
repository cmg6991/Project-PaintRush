using UnityEngine;
using System.Collections;
public class PaintFade : MonoBehaviour
{
    private SpriteRenderer sr;

    private float fadeTime;
    private float minAlpha;


    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }


    public void StartFade(float time, float targetAlpha)
    {
        fadeTime = time;
        minAlpha = targetAlpha;

        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }


    private IEnumerator FadeRoutine()
    {
        Color color = sr.color;

        float startAlpha = color.a;
        float timer = 0f;


        while (timer < fadeTime)
        {
            timer += Time.deltaTime;

            color.a = Mathf.Lerp(
                startAlpha,
                minAlpha,
                timer / fadeTime
            );

            sr.color = color;

            yield return null;
        }


        color.a = minAlpha;
        sr.color = color;


        // 완전히 사라지면 삭제
        if (minAlpha <= 0)
        {
            Destroy(gameObject);
        }
    }
}
