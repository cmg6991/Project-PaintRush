using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 플레이어 총의 현재 색상과 잔량을 관리합니다.
/// 빈 총 충전, 같은 색 재충전, 발사 소모와 피버 상태를 한 곳에서 처리합니다.
/// </summary>
[DisallowMultipleComponent]
public class FillColor : MonoBehaviour
{
    public static FillColor Instance { get; private set; }

    [Header("색상 판정")]
    [SerializeField, Min(0.001f)]
    private float sameColorTolerance = 0.12f;

    [SerializeField, Range(0.9f, 1f)]
    private float fullThreshold = 0.999f;

    [Header("피버 연출")]
    [SerializeField] private Light2D feverLight;
    [SerializeField] private float minIntensity = 0.5f;
    [SerializeField] private float maxIntensity = 2f;
    [SerializeField] private float flashSpeed = 5f;
    [SerializeField] private ParticleSystem feverSparkle;

    private Gradation gradation;
    private float lightTime;
    private bool isPlayerGun;

    private bool previousHasColor;
    private Color previousColor;
    private float previousAmount;

    public bool HasColor { get; private set; }
    public Color CurrentColor { get; private set; } = Color.white;
    public float ColorAmount { get; private set; }
    public bool IsFever { get; private set; }

    public bool IsFull => HasColor && ColorAmount >= fullThreshold;
    public float MissingAmount => Mathf.Clamp01(1f - ColorAmount);

    public Color ShootColor
    {
        get
        {
            if (IsFever && gradation != null)
                return gradation.DisplayColor;

            return CurrentColor;
        }
    }

    private void Awake()
    {
        gradation = GetComponent<Gradation>();
        isPlayerGun = HasTaggedParent(transform, "Player");

        if (isPlayerGun)
            Instance = this;
    }

    private void Start()
    {
        if (isPlayerGun &&
            DataManager.Instance != null &&
            DataManager.Instance.TryGetGunColor(
                out Color savedColor,
                out float savedAmount))
        {
            HasColor = true;
            CurrentColor = savedColor;
            ColorAmount = Mathf.Clamp01(savedAmount);

            if (gradation != null)
                gradation.Play(CurrentColor);

            UpdateVisual();
        }
    }

    private void Update()
    {
        UpdateFeverLight();
    }

    private void OnDestroy()
    {
        if (isPlayerGun && Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 총이 비어 있을 때만 새 색을 최대치로 충전합니다.
    /// </summary>
    public bool SetColor(Color color)
    {
        if (HasColor || IsFever)
            return false;

        ApplyFullColor(color, true);
        return true;
    }

    /// <summary>
    /// 기존 외부 코드와의 호환용 강제 설정 API입니다.
    /// </summary>
    public void GunSetColor(Color color)
    {
        ApplyFullColor(color, true);
    }

    /// <summary>
    /// 총이 비어 있으면 새 색을 충전하고,
    /// 총에 같은 색이 조금 남아 있으면 최대치까지 재충전합니다.
    /// 다른 색이거나 이미 가득 찼으면 실패합니다.
    /// </summary>
    public bool TryFillOrRefill(Color color)
    {
        if (IsFever)
            return false;

        if (!HasColor)
            return SetColor(color);

        return TryRefillSameColor(color);
    }

    /// <summary>
    /// 현재 총 색과 같은 색일 때만 잔량을 최대치로 회복합니다.
    /// </summary>
    public bool TryRefillSameColor(Color color)
    {
        if (!CanRefillWith(color))
            return false;

        ColorAmount = 1f;
        UpdateVisual();
        SyncToDataManager();
        return true;
    }

    public bool CanRefillWith(Color color)
    {
        return HasColor &&
               !IsFever &&
               !IsFull &&
               IsSameColor(color);
    }

    public bool IsSameColor(Color color)
    {
        if (!HasColor)
            return false;

        return ColorDistance(CurrentColor, color) <= sameColorTolerance;
    }

    public void Consume(float amount)
    {
        if (!HasColor || IsFever || amount <= 0f)
            return;

        ColorAmount = Mathf.Clamp01(ColorAmount - amount);

        // 극소량만 남아 발사가 불가능해지는 애매한 상태를 만들지 않습니다.
        if (ColorAmount <= 0.1f)
        {
            ClearColor();
            return;
        }

        UpdateVisual();
        SyncToDataManager();
    }

    public void ClearColor()
    {
        HasColor = false;
        CurrentColor = Color.white;
        ColorAmount = 0f;

        if (gradation != null)
            gradation.Play(Color.white);

        UpdateVisual();
        SyncToDataManager();
    }

    public void UpdateVisual()
    {
        if (gradation != null)
            gradation.SetAmount(ColorAmount, CurrentColor);
    }

    public void FeverOn()
    {
        if (IsFever)
            return;

        previousHasColor = HasColor;
        previousColor = CurrentColor;
        previousAmount = ColorAmount;

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

        UIManager.Instance?.ShowFeverUI();
    }

    public void FeverOff()
    {
        if (!IsFever)
            return;

        IsFever = false;

        if (feverSparkle != null)
            feverSparkle.Stop();

        if (gradation != null)
            gradation.FeverOff();

        HasColor = previousHasColor;
        CurrentColor = previousColor;
        ColorAmount = previousAmount;

        if (HasColor)
        {
            if (gradation != null)
                gradation.Play(CurrentColor);

            UpdateVisual();
        }
        else
        {
            ClearColor();
        }

        SyncToDataManager();
        UIManager.Instance?.HideFeverUI();
    }

    private void ApplyFullColor(Color color, bool playFillAnimation)
    {
        HasColor = true;
        CurrentColor = color;
        ColorAmount = 1f;

        if (playFillAnimation && gradation != null)
            gradation.Play(color);

        UpdateVisual();
        SyncToDataManager();
    }

    private void SyncToDataManager()
    {
        if (!isPlayerGun || IsFever)
            return;

        DataManager.Instance?.UpdateGunColor(
            HasColor,
            CurrentColor,
            ColorAmount);
    }

    private void UpdateFeverLight()
    {
        if (feverLight == null)
            return;

        if (!IsFever)
        {
            feverLight.intensity = 0f;
            return;
        }

        lightTime += Time.deltaTime * flashSpeed;

        feverLight.intensity = Mathf.Lerp(
            minIntensity,
            maxIntensity,
            (Mathf.Sin(lightTime) + 1f) * 0.5f);

        float hue = Mathf.Repeat(Time.time * 0.5f, 1f);
        feverLight.color = Color.HSVToRGB(hue, 0.8f, 1f);
    }

    private static bool HasTaggedParent(
        Transform start,
        string tagName)
    {
        for (Transform current = start;
             current != null;
             current = current.parent)
        {
            if (current.CompareTag(tagName))
                return true;
        }

        return false;
    }

    private static float ColorDistance(Color first, Color second)
    {
        Vector3 difference = new(
            first.r - second.r,
            first.g - second.g,
            first.b - second.b);

        return difference.magnitude;
    }

    private void OnValidate()
    {
        sameColorTolerance = Mathf.Max(0.001f, sameColorTolerance);
        fullThreshold = Mathf.Clamp(fullThreshold, 0.9f, 1f);
    }
}
