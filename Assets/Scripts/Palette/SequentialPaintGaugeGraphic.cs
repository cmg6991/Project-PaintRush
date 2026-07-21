using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class SequentialPaintGaugeGraphic : MaskableGraphic
{
    [Header("게이지")]
    [SerializeField] private Color emptyColor =
        new Color(1f, 1f, 1f, 0.12f);

    [SerializeField, Min(0f)] private float slotSpacing = 1f;
    [SerializeField] private bool drawEmptySlots = true;

    [Header("획득 애니메이션")]
    [SerializeField, Min(0.05f)] private float fillDuration = 0.30f;
    [SerializeField, Min(0f)] private float acquireWaveAmount = 0.10f;
    [SerializeField, Min(0.1f)] private float acquireWaveSpeed = 15f;
    [SerializeField, Range(0f, 1f)] private float flashStrength = 0.30f;

    [Header("피버 감소 애니메이션")]
    [Tooltip("감소 경계가 좌우로 찰랑이는 픽셀 크기입니다.")]
    [SerializeField, Min(0f)] private float drainWaveWidth = 3f;

    [SerializeField, Min(0.1f)] private float drainWaveSpeed = 9f;

    [Tooltip("경계선을 몇 개의 가로 조각으로 나눠 물결을 표현할지 정합니다.")]
    [SerializeField, Range(3, 24)] private int drainWaveBands = 10;

    private readonly List<Color> filledColors = new();
    private readonly Dictionary<int, float> animatedProgress = new();
    private readonly Dictionary<int, float> animatedWave = new();
    private readonly Dictionary<int, float> animatedFlash = new();

    private int totalSlots;
    private float drainRemaining01 = 1f;
    private bool isDraining;

    protected override void Awake()
    {
        EnsureCanvasRenderer();
        base.Awake();
        raycastTarget = false;
    }

    protected override void OnEnable()
    {
        EnsureCanvasRenderer();
        base.OnEnable();
        SetVerticesDirty();
    }

    private void Update()
    {
        if (isDraining)
            SetVerticesDirty();
    }

    private void EnsureCanvasRenderer()
    {
        if (GetComponent<CanvasRenderer>() == null)
            gameObject.AddComponent<CanvasRenderer>();
    }

    public void SetGauge(
        int newTotalSlots,
        IReadOnlyList<Color> colors)
    {
        totalSlots = Mathf.Max(0, newTotalSlots);
        filledColors.Clear();

        if (colors != null)
        {
            for (int i = 0; i < colors.Count; i++)
            {
                Color color = colors[i];
                color.a = 1f;
                filledColors.Add(color);
            }
        }

        animatedProgress.Clear();
        animatedWave.Clear();
        animatedFlash.Clear();
        SetVerticesDirty();
    }

    public void SetDrainProgress(
        float remaining01,
        bool draining)
    {
        drainRemaining01 = Mathf.Clamp01(remaining01);
        isDraining = draining;
        SetVerticesDirty();
    }

    public void PlayAcquireAnimation(
        int slotIndex,
        Color color)
    {
        if (slotIndex < 0)
            return;

        while (filledColors.Count <= slotIndex)
            filledColors.Add(Color.clear);

        color.a = 1f;
        filledColors[slotIndex] = color;

        StartCoroutine(AnimateSlot(slotIndex));
    }

    protected override void OnPopulateMesh(
        VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        if (totalSlots <= 0)
            return;

        Rect rect = GetPixelAdjustedRect();

        float totalSpacing =
            slotSpacing * Mathf.Max(0, totalSlots - 1);

        float slotWidth =
            Mathf.Max(
                0f,
                (rect.width - totalSpacing) / totalSlots);

        float drainBoundary =
            Mathf.Lerp(
                rect.xMin,
                rect.xMax,
                drainRemaining01);

        if (isDraining &&
            drainRemaining01 > 0f &&
            drainRemaining01 < 1f)
        {
            drainBoundary +=
                Mathf.Sin(
                    Time.unscaledTime *
                    drainWaveSpeed) *
                drainWaveWidth;
        }

        drainBoundary =
            Mathf.Clamp(
                drainBoundary,
                rect.xMin,
                rect.xMax);

        for (int index = 0;
             index < totalSlots;
             index++)
        {
            float left =
                rect.xMin +
                index * (slotWidth + slotSpacing);

            float slotRight = left + slotWidth;

            if (drawEmptySlots)
            {
                AddQuad(
                    vertexHelper,
                    new Rect(
                        left,
                        rect.yMin,
                        slotWidth,
                        rect.height),
                    emptyColor);
            }

            if (index >= filledColors.Count)
                continue;

            Color fillColor = filledColors[index];

            if (fillColor.a <= 0f ||
                drainBoundary <= left)
            {
                continue;
            }

            float acquireProgress =
                animatedProgress.TryGetValue(
                    index,
                    out float animated)
                    ? animated
                    : 1f;

            float acquireRight =
                left +
                slotWidth *
                Mathf.Clamp01(acquireProgress);

            float visibleRight =
                Mathf.Min(
                    acquireRight,
                    drainBoundary);

            if (visibleRight <= left)
                continue;

            float acquireWave =
                animatedWave.TryGetValue(
                    index,
                    out float waveValue)
                    ? waveValue
                    : 0f;

            float flash =
                animatedFlash.TryGetValue(
                    index,
                    out float flashValue)
                    ? flashValue
                    : 0f;

            Color finalColor =
                Color.Lerp(
                    fillColor,
                    Color.white,
                    flash * flashStrength);

            finalColor.a = fillColor.a;

            bool boundaryInsideThisSlot =
                isDraining &&
                drainBoundary > left &&
                drainBoundary < acquireRight;

            if (boundaryInsideThisSlot)
            {
                AddWavyClippedFill(
                    vertexHelper,
                    left,
                    visibleRight,
                    acquireRight,
                    rect.yMin,
                    rect.yMax,
                    finalColor);
            }
            else
            {
                float extraHeight =
                    rect.height * acquireWave;

                AddQuad(
                    vertexHelper,
                    new Rect(
                        left,
                        rect.yMin -
                        extraHeight * 0.5f,
                        visibleRight - left,
                        rect.height + extraHeight),
                    finalColor);
            }
        }
    }

    private void AddWavyClippedFill(
        VertexHelper vertexHelper,
        float left,
        float baseRight,
        float maximumRight,
        float bottom,
        float top,
        Color color)
    {
        int bandCount = Mathf.Max(3, drainWaveBands);
        float bandHeight = (top - bottom) / bandCount;

        for (int band = 0; band < bandCount; band++)
        {
            float normalizedY =
                (band + 0.5f) / bandCount;

            float wave =
                Mathf.Sin(
                    Time.unscaledTime * drainWaveSpeed +
                    normalizedY * Mathf.PI * 2f) *
                drainWaveWidth;

            float right =
                Mathf.Clamp(
                    baseRight + wave,
                    left,
                    maximumRight);

            if (right <= left)
                continue;

            AddQuad(
                vertexHelper,
                new Rect(
                    left,
                    bottom + band * bandHeight,
                    right - left,
                    bandHeight + 0.5f),
                color);
        }
    }

    private IEnumerator AnimateSlot(int slotIndex)
    {
        float elapsed = 0f;

        animatedProgress[slotIndex] = 0f;
        animatedWave[slotIndex] = 0f;
        animatedFlash[slotIndex] = 0f;

        while (elapsed < fillDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / fillDuration);

            float eased =
                1f - Mathf.Pow(1f - t, 3f);

            float wave =
                Mathf.Sin(t * acquireWaveSpeed) *
                acquireWaveAmount *
                (1f - t);

            float flash =
                Mathf.Sin(t * Mathf.PI);

            animatedProgress[slotIndex] = eased;
            animatedWave[slotIndex] = wave;
            animatedFlash[slotIndex] = flash;

            SetVerticesDirty();
            yield return null;
        }

        animatedProgress.Remove(slotIndex);
        animatedWave.Remove(slotIndex);
        animatedFlash.Remove(slotIndex);
        SetVerticesDirty();
    }

    private static void AddQuad(
        VertexHelper vertexHelper,
        Rect rect,
        Color color)
    {
        int startIndex =
            vertexHelper.currentVertCount;

        UIVertex vertex =
            UIVertex.simpleVert;

        vertex.color = color;

        vertex.position =
            new Vector3(rect.xMin, rect.yMin);
        vertexHelper.AddVert(vertex);

        vertex.position =
            new Vector3(rect.xMin, rect.yMax);
        vertexHelper.AddVert(vertex);

        vertex.position =
            new Vector3(rect.xMax, rect.yMax);
        vertexHelper.AddVert(vertex);

        vertex.position =
            new Vector3(rect.xMax, rect.yMin);
        vertexHelper.AddVert(vertex);

        vertexHelper.AddTriangle(
            startIndex,
            startIndex + 1,
            startIndex + 2);

        vertexHelper.AddTriangle(
            startIndex,
            startIndex + 2,
            startIndex + 3);
    }

    private void OnValidate()
    {
        slotSpacing = Mathf.Max(0f, slotSpacing);
        fillDuration = Mathf.Max(0.05f, fillDuration);
        acquireWaveSpeed = Mathf.Max(0.1f, acquireWaveSpeed);
        drainWaveWidth = Mathf.Max(0f, drainWaveWidth);
        drainWaveSpeed = Mathf.Max(0.1f, drainWaveSpeed);
        SetVerticesDirty();
    }
}
