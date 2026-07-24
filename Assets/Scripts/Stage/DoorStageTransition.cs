using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ready 상태의 문 근처에서 E 입력을 받아
/// 문 열림 연출과 다음 스테이지 전환을 요청합니다.
///
/// DoorOpen과 SceneTransitionFader는 수정하지 않고
/// 현재 공개 API만 사용합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class DoorStageTransition : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private DoorOpen door;

    [Tooltip(
        "비워두면 현재 활성화된 SceneTransitionFader.Instance를 자동으로 찾습니다.")]
    [SerializeField] private SceneTransitionFader transitionFader;

    [SerializeField] private GameObject interactionPrompt;

    [Header("상호작용")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Tooltip("문 Open 애니메이션을 보여준 뒤 전환 효과를 시작하기까지의 시간입니다.")]
    [SerializeField, Min(0f)] private float animationLeadTime = 0.45f;

    [Header("다음 씬")]
    [Tooltip("비워두면 현재 Build Index 다음 씬을 불러옵니다.")]
    [SerializeField] private string nextSceneName;

    [SerializeField]
    private bool loadNextBuildIndexWhenNameEmpty = true;

    private int playerColliderCount;
    private bool transitionRequested;
    private Coroutine transitionCoroutine;

    private bool IsPlayerInside => playerColliderCount > 0;

    private void Awake()
    {
        ResolveDoor();
        ConfigureInteractionTrigger();
        SetPromptVisible(false);
    }

    private void OnEnable()
    {
        ResolveDoor();
        SubscribeDoorEvents();
        RefreshPrompt();
    }

    private void OnDisable()
    {
        UnsubscribeDoorEvents();

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        playerColliderCount = 0;
        transitionRequested = false;
        SetPromptVisible(false);
    }

    private void Update()
    {
        if (!CanInteract())
            return;

        if (Input.GetKeyDown(interactionKey))
        {
            transitionCoroutine =
                StartCoroutine(TransitionRoutine());
        }
    }

    private IEnumerator TransitionRoutine()
    {
        transitionRequested = true;
        SetPromptVisible(false);

        string targetScene = ResolveTargetSceneName();

        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogError(
                $"{name}: 다음 씬을 찾을 수 없습니다. " +
                "Next Scene Name 또는 Build Profile의 씬 순서를 확인하세요.",
                this);

            CancelTransitionRequest();
            yield break;
        }

        if (door == null || !door.IsReady)
        {
            Debug.LogWarning(
                $"{name}: 문이 Ready 상태가 아니어서 전환을 취소합니다.",
                this);

            CancelTransitionRequest();
            yield break;
        }

        // DoorOpen.BeginTransition()은 void 반환형입니다.
        door.BeginTransition();

        if (animationLeadTime > 0f)
        {
            yield return new WaitForSecondsRealtime(
                animationLeadTime);
        }

        SceneTransitionFader activeFader =
            ResolveTransitionFader();

        if (activeFader == null)
        {
            Debug.LogError(
                $"{name}: 활성 SceneTransitionFader를 찾지 못했습니다. " +
                "씬 전환 효과를 유지하기 위해 즉시 씬 로드는 실행하지 않습니다.",
                this);

            CancelTransitionRequest();
            yield break;
        }

        bool started =
            activeFader.LoadScene(targetScene);

        if (!started)
        {
            Debug.LogWarning(
                $"{name}: SceneTransitionFader가 전환 요청을 거부했습니다. " +
                "이미 씬 전환 중이거나 Fader 설정이 누락되었는지 확인하세요.",
                this);

            CancelTransitionRequest();
            yield break;
        }

        transitionCoroutine = null;
    }

    private bool CanInteract()
    {
        return !transitionRequested &&
               door != null &&
               door.IsReady &&
               IsPlayerInside;
    }

    private void CancelTransitionRequest()
    {
        transitionRequested = false;
        transitionCoroutine = null;
        RefreshPrompt();
    }

    private void ResolveDoor()
    {
        if (door != null)
            return;

        door = GetComponent<DoorOpen>();

        if (door == null)
            door = GetComponentInParent<DoorOpen>();
    }

    private SceneTransitionFader ResolveTransitionFader()
    {
        // Inspector 참조가 살아 있다면 우선 사용합니다.
        if (transitionFader != null &&
            transitionFader.isActiveAndEnabled)
        {
            return transitionFader;
        }

        // DontDestroyOnLoad로 유지되는 현재 Instance를 다시 찾습니다.
        transitionFader = SceneTransitionFader.Instance;

        if (transitionFader != null &&
            transitionFader.isActiveAndEnabled)
        {
            return transitionFader;
        }

        // Instance가 아직 초기화되지 않은 예외 상황에서만 씬 탐색을 수행합니다.
        transitionFader =
            FindFirstObjectByType<SceneTransitionFader>(
                FindObjectsInactive.Exclude);

        return transitionFader != null &&
               transitionFader.isActiveAndEnabled
            ? transitionFader
            : null;
    }

    private string ResolveTargetSceneName()
    {
        if (!string.IsNullOrWhiteSpace(nextSceneName))
            return nextSceneName.Trim();

        if (!loadNextBuildIndexWhenNameEmpty)
            return string.Empty;

        int nextIndex =
            SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex < 0 ||
            nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            return string.Empty;
        }

        string scenePath =
            SceneUtility.GetScenePathByBuildIndex(nextIndex);

        return System.IO.Path.GetFileNameWithoutExtension(
            scenePath);
    }

    private void ConfigureInteractionTrigger()
    {
        Collider2D interactionCollider =
            GetComponent<Collider2D>();

        if (interactionCollider == null)
            return;

        if (!interactionCollider.isTrigger)
        {
            interactionCollider.isTrigger = true;

            Debug.LogWarning(
                $"{name}: DoorStageTransition의 Collider2D가 " +
                "Trigger가 아니어서 자동으로 활성화했습니다.",
                this);
        }
    }

    private void SubscribeDoorEvents()
    {
        if (door == null)
            return;

        // 중복 구독 방지
        door.OnDoorReady -= HandleDoorReady;
        door.OnTransitionStarted -= HandleTransitionStarted;

        door.OnDoorReady += HandleDoorReady;
        door.OnTransitionStarted += HandleTransitionStarted;
    }

    private void UnsubscribeDoorEvents()
    {
        if (door == null)
            return;

        door.OnDoorReady -= HandleDoorReady;
        door.OnTransitionStarted -= HandleTransitionStarted;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerColliderCount++;
        RefreshPrompt();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
            return;

        if (playerColliderCount <= 0)
            playerColliderCount = 1;

        RefreshPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerColliderCount =
            Mathf.Max(0, playerColliderCount - 1);

        RefreshPrompt();
    }

    private static bool IsPlayerCollider(
        Collider2D collider)
    {
        if (collider == null)
            return false;

        for (Transform current = collider.transform;
             current != null;
             current = current.parent)
        {
            if (current.CompareTag("Player"))
                return true;
        }

        return false;
    }

    private void HandleDoorReady()
    {
        RefreshPrompt();
    }

    private void HandleTransitionStarted()
    {
        SetPromptVisible(false);
    }

    private void RefreshPrompt()
    {
        SetPromptVisible(CanInteract());
    }

    private void SetPromptVisible(bool visible)
    {
        if (interactionPrompt == null ||
            interactionPrompt.activeSelf == visible)
        {
            return;
        }

        interactionPrompt.SetActive(visible);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        animationLeadTime =
            Mathf.Max(0f, animationLeadTime);

        if (!Application.isPlaying)
            ResolveDoor();
    }
#endif
}
