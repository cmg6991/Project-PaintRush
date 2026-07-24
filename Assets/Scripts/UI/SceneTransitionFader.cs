using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class SceneTransitionFader : MonoBehaviour
{
    public static SceneTransitionFader Instance { get; private set; }

    [Header("UI 참조")]
    [SerializeField] private RectTransform splatContainer;

    [Header("물감 이미지")]
    [SerializeField] private Sprite[] splatSprites;

    [Header("랜덤 색상")]
    [SerializeField] private Color[] paintColors;

    [Header("화면 덮기")]
    [Tooltip("E 상호작용 후 물감 전환이 시작되기까지 기다리는 시간입니다.")]
    [SerializeField, Min(0f)] private float transitionStartDelay = 1f;
    [SerializeField, Min(2)] private int columns = 8;
    [SerializeField, Min(2)] private int rows = 5;
    [SerializeField, Min(0)] private int extraRandomSplats = 14;
    [SerializeField, Range(0f, 0.8f)] private float positionJitter = 0.35f;
    [SerializeField, Min(0.2f)] private float minimumScale = 0.85f;
    [SerializeField, Min(0.2f)] private float maximumScale = 1.45f;
    [SerializeField, Min(0.001f)] private float spawnInterval = 0.018f;
    [SerializeField, Min(0.01f)] private float popDuration = 0.12f;
    [SerializeField, Min(0f)] private float coveredHoldTime = 0.12f;

    [Header("새 씬에서 사라지기")]
    [SerializeField, Min(0.01f)] private float revealDuration = 0.45f;
    [SerializeField, Min(0f)] private float revealDelay = 0.03f;

    [Header("기타")]
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField] private bool logTransition;

    private Canvas transitionCanvas;
    private readonly List<Image> activeSplats = new();
    private bool isLoading;

    public bool IsLoading => isLoading;

    private static readonly Color[] DefaultPaintColors =
    {
        new(0.95f, 0.20f, 0.25f, 1f),
        new(1.00f, 0.78f, 0.08f, 1f),
        new(0.20f, 0.70f, 1.00f, 1f),
        new(0.20f, 0.85f, 0.45f, 1f),
        new(0.55f, 0.25f, 0.95f, 1f)
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        transitionCanvas = GetComponent<Canvas>();

        if (transitionCanvas != null)
        {
            transitionCanvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            transitionCanvas.overrideSorting = true;
            transitionCanvas.sortingOrder = 32000;
            transitionCanvas.enabled = true;
        }

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);

        if (splatContainer == null)
            splatContainer = transform as RectTransform;

        SetContainerVisible(false);
    }

    public bool LoadScene(string sceneName)
    {
        if (isLoading || string.IsNullOrWhiteSpace(sceneName))
            return false;

        if (!ValidateSetup())
            return false;

        StartCoroutine(LoadSceneRoutine(sceneName));
        return true;
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        // E 입력 후 문 애니메이션을 보여주는 시간
        if (transitionStartDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                transitionStartDelay
            );
        }

        gameObject.SetActive(true);
        SetContainerVisible(true);

        splatContainer.localScale = Vector3.one;
        splatContainer.SetAsLastSibling();

        if (transitionCanvas != null)
        {
            // Canvas를 껐다 켜서 현재 씬에서 강제로 다시 렌더링
            transitionCanvas.enabled = false;
            transitionCanvas.enabled = true;

            transitionCanvas.overrideSorting = true;
            transitionCanvas.sortingOrder = 32000;
        }

        Canvas.ForceUpdateCanvases();

        Canvas canvas = GetComponent<Canvas>();
        canvas.sortingOrder = 9999;

        // 중요:
        // 활성화한 바로 그 프레임에는 UI 크기와 렌더링이
        // 아직 적용되지 않을 수 있으므로 한 프레임 기다림
        yield return null;
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();

        Debug.Log(
            $"[PaintTransition] 현재 씬에서 물감 시작 / " +
            $"Container Size={splatContainer.rect.size}",
            this
        );

        // 현재 씬에서 물감이 전부 덮일 때까지 기다림
        yield return SpawnCoverSplats();

        // 생성이 끝난 뒤 실제 화면에 한 번 이상 렌더링되도록 대기
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();

        // 화면이 완전히 덮인 상태 유지
        if (coveredHoldTime > 0f)
        {
            yield return new WaitForSecondsRealtime(
                coveredHoldTime
            );
        }

        Debug.Log(
            "[PaintTransition] 현재 화면 덮기 완료 → 씬 로드 시작",
            this
        );

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogError(
                $"씬 로드 실패: {sceneName}",
                this
            );

            yield return RevealAndClear();
            SetContainerVisible(false);
            isLoading = false;
            yield break;
        }

        while (!operation.isDone)
            yield return null;

        // 새 씬이 한 프레임 표시될 때까지
        // 기존 물감 화면을 그대로 유지
        splatContainer.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();

        yield return null;
        yield return new WaitForEndOfFrame();

        if (revealDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                revealDelay
            );
        }

        Debug.Log(
            "[PaintTransition] 새 씬에서 물감 제거 시작",
            this
        );

        yield return RevealAndClear();

        SetContainerVisible(false);
        isLoading = false;
    }

    private IEnumerator SpawnCoverSplats()
    {
        ClearExistingSplats();

        if (splatContainer == null)
            yield break;

        splatContainer.gameObject.SetActive(true);
        splatContainer.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();

        // UI가 켜진 뒤 크기가 적용될 때까지 기다림
        yield return null;
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();

        Rect rect = splatContainer.rect;

        if (rect.width <= 1f || rect.height <= 1f)
        {
            Debug.LogError(
                $"SplatContainer 크기가 비정상입니다. " +
                $"Width={rect.width}, Height={rect.height}",
                this
            );

            yield break;
        }

        float cellWidth = rect.width / columns;
        float cellHeight = rect.height / rows;

        List<Vector2> positions =
            new List<Vector2>(
                columns * rows + extraRandomSplats
            );

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0;
                 column < columns;
                 column++)
            {
                float x =
                    rect.xMin +
                    (column + 0.5f) * cellWidth;

                float y =
                    rect.yMin +
                    (row + 0.5f) * cellHeight;

                x += Random.Range(
                    -cellWidth * positionJitter,
                    cellWidth * positionJitter
                );

                y += Random.Range(
                    -cellHeight * positionJitter,
                    cellHeight * positionJitter
                );

                positions.Add(new Vector2(x, y));
            }
        }

        for (int i = 0; i < extraRandomSplats; i++)
        {
            positions.Add(
                new Vector2(
                    Random.Range(rect.xMin, rect.xMax),
                    Random.Range(rect.yMin, rect.yMax)
                )
            );
        }

        Shuffle(positions);

        float baseSize =
            Mathf.Max(cellWidth, cellHeight) * 2.2f;

        foreach (Vector2 position in positions)
        {
            Image splat =
                CreateSplat(position, baseSize);

            activeSplats.Add(splat);

            StartCoroutine(
                PopIn(splat.rectTransform)
            );

            if (spawnInterval > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    spawnInterval
                );
            }
        }

        // 마지막 물감의 팝 애니메이션이 끝날 때까지 기다림
        yield return new WaitForSecondsRealtime(
            popDuration
        );

        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
    }
    private Image CreateSplat(
    Vector2 anchoredPosition,
    float baseSize)
    {
        // 기존 프리팹을 Instantiate하지 않고
        // UI 오브젝트를 런타임에 깨끗하게 직접 생성합니다.
        GameObject splatObject = new GameObject(
            "PaintSplat_Runtime",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        splatObject.layer = LayerMask.NameToLayer("UI");

        RectTransform rect =
            splatObject.GetComponent<RectTransform>();

        Image splat =
            splatObject.GetComponent<Image>();

        // SplatContainer의 자식으로 넣습니다.
        rect.SetParent(
            splatContainer,
            false
        );

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.anchoredPosition =
            anchoredPosition;

        rect.sizeDelta =
            new Vector2(baseSize, baseSize);

        rect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                Random.Range(0f, 360f)
            );

        rect.localScale = Vector3.zero;

        splat.sprite =
            splatSprites[
                Random.Range(
                    0,
                    splatSprites.Length
                )
            ];

        Color[] colors =
            paintColors != null &&
            paintColors.Length > 0
                ? paintColors
                : DefaultPaintColors;

        Color randomColor =
            colors[
                Random.Range(
                    0,
                    colors.Length
                )
            ];

        // 혹시 Inspector 색상의 Alpha가 0이어도
        // 무조건 불투명하게 생성합니다.
        randomColor.a = 1f;

        splat.color = randomColor;
        splat.enabled = true;
        splat.preserveAspect = true;
        splat.raycastTarget = false;
        splat.maskable = false;

        // Image 색과 별개로 CanvasRenderer Alpha도
        // 강제로 1로 설정합니다.
        splat.canvasRenderer.SetAlpha(1f);
        splat.canvasRenderer.cull = false;

        // UI 갱신 강제
        splat.SetVerticesDirty();
        splat.SetMaterialDirty();

        rect.SetAsLastSibling();

        splatObject.SetActive(true);

        return splat;
    }
    private IEnumerator PopIn(RectTransform target)
    {
        float finalScale = Random.Range(minimumScale, maximumScale);
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            if (target == null)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float overshoot = Mathf.Sin(t * Mathf.PI) * 0.12f;

            target.localScale = Vector3.one * finalScale * (eased + overshoot);
            yield return null;
        }

        if (target != null)
            target.localScale = Vector3.one * finalScale;
    }

    private IEnumerator RevealAndClear()
    {
        float elapsed = 0f;
        List<Vector3> startScales = new(activeSplats.Count);

        foreach (Image splat in activeSplats)
        {
            startScales.Add(
                splat != null ? splat.rectTransform.localScale : Vector3.zero);
        }

        while (elapsed < revealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / revealDuration);
            float eased = t * t * (3f - 2f * t);

            for (int i = 0; i < activeSplats.Count; i++)
            {
                Image splat = activeSplats[i];
                if (splat == null)
                    continue;

                Color color = splat.color;
                color.a = 1f - eased;
                splat.color = color;

                splat.rectTransform.localScale =
                    Vector3.Lerp(startScales[i], startScales[i] * 0.55f, eased);
            }

            yield return null;
        }

        ClearExistingSplats();
    }

    private bool ValidateSetup()
    {
        if (splatContainer == null)
        {
            Debug.LogError(
                "SceneTransitionFader: " +
                "Splat Container가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        if (splatSprites == null ||
            splatSprites.Length == 0)
        {
            Debug.LogError(
                "SceneTransitionFader: " +
                "Splat Sprites가 비어 있습니다.",
                this
            );

            return false;
        }

        return true;
    }

    private void SetContainerVisible(bool visible)
    {
        if (splatContainer != null)
            splatContainer.gameObject.SetActive(visible);
    }

    private void ClearExistingSplats()
    {
        foreach (Image splat in activeSplats)
        {
            if (splat != null)
                Destroy(splat.gameObject);
        }

        activeSplats.Clear();
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
