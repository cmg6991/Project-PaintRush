using UnityEngine;
using System.Collections;

public class SplashShine : MonoBehaviour
{
    private SpriteRenderer sr;
    private Material originMaterial;

    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material glowMaterial;
    [SerializeField] private float speed = 2f;

    private Material runtimeGlow;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // 원래 머티리얼 저장
        originMaterial = sr.sharedMaterial;
    }

    private void OnEnable()
    {
        // Glow 머티리얼 복사본 생성
        runtimeGlow = new Material(glowMaterial);

        sr.material = runtimeGlow;

        StopAllCoroutines();
        StartCoroutine(Shine());
    }

    IEnumerator Shine()
    {
        float t = -0.3f;

        while (t < 1.3f)
        {
            t += Time.deltaTime * speed;

            runtimeGlow.SetFloat("_ShinePos", t);

            yield return null;
        }

        runtimeGlow.SetFloat("_ShinePos", -1);

        // Shine 끝나면 원래 머티리얼 복원
        sr.material = originMaterial;

        Destroy(runtimeGlow);
    }
}
