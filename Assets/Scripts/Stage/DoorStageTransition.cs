using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ready 상태의 문 근처에서 E키 입력을 받아
/// 문 열림 연출과 다음 스테이지 씬 전환을 시작합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class DoorStageTransition : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private DoorOpen door;
    [SerializeField] private SceneTransitionFader transitionFader;
    [SerializeField] private GameObject interactionPrompt;

    [Header("상호작용")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField, Min(0f)] private float animationLeadTime = 0.45f;

    [Header("다음 씬")]
    [Tooltip("비워두면 현재 Build Index + 1 씬을 로드합니다.")]
    [SerializeField] private string nextSceneName;
    [SerializeField] private bool loadNextBuildIndexWhenNameEmpty = true;

    private int playerColliderCount;
    private bool transitionRequested;

    private void Awake()
    {
        door ??= GetComponent<DoorOpen>();
        door ??= GetComponentInParent<DoorOpen>();
        transitionFader ??= SceneTransitionFader.Instance;
        SetPrompt(false);
    }

    private void OnEnable()
    {
        if (door != null)
        {
            door.OnDoorReady += HandleDoorReady;
            door.OnTransitionStarted += HandleTransitionStarted;
        }

    }

    private void OnDisable()
    {
        if (door != null)
        {
            door.OnDoorReady -= HandleDoorReady;
            door.OnTransitionStarted -= HandleTransitionStarted;
        }
    }

    private void Update()
    {
        if (transitionRequested ||
            door == null ||
            !door.IsReady ||
            playerColliderCount <= 0)
        {
            return;
        }

        if (Input.GetKeyDown(interactionKey))
            StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        string targetScene = ResolveTargetSceneName();

        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogError(
                $"{name}: 다음 씬을 찾을 수 없습니다. " +
                "Build Settings 또는 Next Scene Name을 확인하세요.");
            yield break;
        }

        transitionRequested = true;
        SetPrompt(false);

        door.BeginTransition();

        if (animationLeadTime > 0f)
            yield return new WaitForSeconds(animationLeadTime);

        transitionFader ??= SceneTransitionFader.Instance;

        if (transitionFader != null)
        {
            transitionFader.LoadScene(targetScene);
            yield break;
        }

        SceneManager.LoadScene(targetScene);
    }

    private string ResolveTargetSceneName()
    {
        if (!string.IsNullOrWhiteSpace(nextSceneName))
            return nextSceneName.Trim();

        if (!loadNextBuildIndexWhenNameEmpty)
            return string.Empty;

        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex < 0 || nextIndex >= SceneManager.sceneCountInBuildSettings)
            return string.Empty;

        string path = SceneUtility.GetScenePathByBuildIndex(nextIndex);
        return System.IO.Path.GetFileNameWithoutExtension(path);
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
        if (playerColliderCount <= 0 && IsPlayerCollider(other))
            playerColliderCount = 1;

        RefreshPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerColliderCount = Mathf.Max(0, playerColliderCount - 1);
        RefreshPrompt();
    }

    private static bool IsPlayerCollider(Collider2D collider)
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
        SetPrompt(false);
    }

    private void RefreshPrompt()
    {
        SetPrompt(
            !transitionRequested &&
            door != null &&
            door.IsReady &&
            playerColliderCount > 0);
    }

    private void SetPrompt(bool visible)
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(visible);
    }
}
