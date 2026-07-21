using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class FeverUI : MonoBehaviour
{
    [SerializeField] private GameObject BG;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Count")]
    [SerializeField] private Image countImage;
    [SerializeField] private Sprite[] countSprites;

    [SerializeField] private RectTransform panel;

    [SerializeField] private float shakeAmount = 8f;
    [SerializeField] private float shakeSpeed = 15f;

    [SerializeField] private TextMeshProUGUI feverTitleText;   // FEVER TIME 이미지
    [SerializeField] private float rainbowSpeed = 2f;

    private Vector2 originPos;
    private bool isShake;

    private void Awake()
    {
        //Color c = frame.color;
        //c.a = 0;
        //frame.color = c;
        canvasGroup.alpha = 0;

        //BG.SetActive(false);
        originPos = panel.anchoredPosition;

    }

    private void Start()
    {
        UIManager.Instance.RegisterFeverUI(this);
    }

    private void Update()
    {
        if(!isShake)
            return;

        float x = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
        float y = Mathf.Cos(Time.time * shakeSpeed * 0.8f) * shakeAmount;

        panel.anchoredPosition = originPos + new Vector2(x, y);

        if (BG.activeSelf)
        {
            float hue = Mathf.Repeat(Time.time * 0.7f, 1f);

            countImage.color = Color.HSVToRGB(
                hue,
                0.8f,
                1f
            );
        }
    }
    public void FeverOn()
    {
        BG.SetActive(true);
        //StopAllCoroutines();
        //StartCoroutine(Fade(1));
        isShake = true;
        countImage.color = Color.white;
        StopAllCoroutines();

        StartCoroutine(FeverRoutine());
    }


    public void FeverOff()
    {
        isShake = false;
        panel.anchoredPosition = originPos;
        //StopAllCoroutines();
        //StartCoroutine(Fade(0));
        countImage.color = Color.white;
        StopAllCoroutines();

        StartCoroutine(FadeOut());
    }

    IEnumerator FeverRoutine()
    {
        StartCoroutine(Fade(1));

        feverTitleText.gameObject.SetActive(true);

        StartCoroutine(HideTitle());
        for (int i = 5; i >= 1; i--)
        {
            countImage.sprite = countSprites[5 - i];

            yield return Pop();

            if (i == 1)
                yield return new WaitForSeconds(1.3f);   // 마지막만 조금 길게
            else
                yield return new WaitForSeconds(1f);

        }
    }
    IEnumerator HideTitle()
    {
        yield return new WaitForSeconds(1f);
        feverTitleText.gameObject.SetActive(false);
    }

    IEnumerator Fade(float target)
    {
        //Color color = frame.color;

        //float start = color.a;
        //float time = 0;

        //while (time < 1)
        //{
        //    time += Time.deltaTime * 3;

        //    color.a = Mathf.Lerp(start, target, time);

        //    frame.color = color;

        //    yield return null;
        //}
        float start = canvasGroup.alpha;
        float time = 0;

        while (time < 1f)
        {
            time += Time.deltaTime * 3f;

            canvasGroup.alpha = Mathf.Lerp(start, target, time);

            yield return null;
        }
        canvasGroup.alpha = target;
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(0.3f);
        yield return Fade(0);

        BG.SetActive(false);
    }

    IEnumerator Pop()
    {
        Vector3 big = Vector3.one * 1.3f;

        Vector3 normal = Vector3.one;

        float t = 0;

        countImage.transform.localScale = big;

        while (t < 1)
        {
            t += Time.deltaTime * 8;

            countImage.transform.localScale =
                Vector3.Lerp(big, normal, t);

            yield return null;
        }

        countImage.transform.localScale = normal;
    }
}
