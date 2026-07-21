using UnityEngine;

/// <summary>
/// 기존 ReadyText 오브젝트의 글꼴, 문구, 색상은 유지하고
/// 스케일 펄스, 상하 움직임, 알파 점멸만 추가합니다.
/// </summary>
[DisallowMultipleComponent]
public class FeverReadyAnimator : MonoBehaviour
{
    [Header("펄스")]
    [SerializeField, Min(0f)] private float pulseAmount = 0.08f;
    [SerializeField, Min(0.1f)] private float pulseSpeed = 4f;

    [Header("상하 움직임")]
    [SerializeField, Min(0f)] private float bobAmount = 2f;
    [SerializeField, Min(0.1f)] private float bobSpeed = 3f;

    [Header("점멸")]
    [SerializeField, Range(0f, 1f)] private float minimumAlpha = 0.65f;
    [SerializeField, Min(0.1f)] private float alphaSpeed = 3f;

    [Header("시간")]
    [SerializeField] private bool useUnscaledTime = true;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Vector3 baseScale;
    private Vector2 basePosition;

    private void Awake()
    {
        rectTransform = transform as RectTransform;

        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        CacheBaseValues();
    }

    private void OnEnable()
    {
        CacheBaseValues();
        ApplyImmediateState();
    }

    private void Update()
    {
        float time =
            useUnscaledTime
                ? Time.unscaledTime
                : Time.time;

        float pulse =
            1f +
            Mathf.Sin(time * pulseSpeed) *
            pulseAmount;

        transform.localScale =
            baseScale * pulse;

        if (rectTransform != null)
        {
            Vector2 position = basePosition;
            position.y +=
                Mathf.Sin(time * bobSpeed) *
                bobAmount;

            rectTransform.anchoredPosition =
                position;
        }

        float alphaWave =
            (Mathf.Sin(time * alphaSpeed) + 1f) *
            0.5f;

        canvasGroup.alpha =
            Mathf.Lerp(
                minimumAlpha,
                1f,
                alphaWave);
    }

    private void OnDisable()
    {
        ResetToBase();
    }

    private void CacheBaseValues()
    {
        baseScale = transform.localScale;

        if (rectTransform != null)
            basePosition = rectTransform.anchoredPosition;
    }

    private void ApplyImmediateState()
    {
        transform.localScale = baseScale;

        if (rectTransform != null)
            rectTransform.anchoredPosition = basePosition;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private void ResetToBase()
    {
        transform.localScale = baseScale;

        if (rectTransform != null)
            rectTransform.anchoredPosition = basePosition;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }
}
