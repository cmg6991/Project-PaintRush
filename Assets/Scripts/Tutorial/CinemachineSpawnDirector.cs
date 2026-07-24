using UnityEngine;
using System.Collections;
using Unity.Cinemachine; 

public class CinemachineSpawnDirector : MonoBehaviour
{
    [Header("References")]
    public CinemachineCamera virtualCamera;            // 시네머신 버츄얼 카메라 연결
    public Transform spawnPoint;                       // 연출 및 등장 위치 (문 앞 등)
    public GameObject drawingObject;                   // 만년필 애니메이션 오브젝트 (비활성화 상태로 시작)
    public GameObject realPlayer;                      // 진짜 플레이어 프리팹 (비활성화 상태로 시작)

    [Header("Zoom Settings")]
    public float targetZoomSize = 3.0f;                // 줌인 했을 때 크기
    public float defaultZoomSize = 5.0f;               // 기본 게임 플레이 시 크기
    public float zoomSpeed = 2.0f;                     // 줌 전환 속도

    [Header("Timing Settings")]
    public float preZoomDelay = 0.5f;                  // 줌인 후 잠시 대기
    public float drawingAnimationDuration = 1.0f;      // 만년필 애니메이션이 다 그려지는 시간
    public float postSpawnDelay = 0.5f;                // 플레이어 나오고 잠시 대기

    private void Awake()
    {
        // 씬이 로드되는 첫 프레임(Frame 0) 즉시 카메라를 스폰 위치(-51.3, -8.4)에 붙여 튐 현상 제거
        if (virtualCamera != null && spawnPoint != null)
        {
            Vector3 targetcamPos = new Vector3(spawnPoint.position.x, spawnPoint.position.y, virtualCamera.transform.position.z);
            virtualCamera.transform.position = targetcamPos;
        }
    }

    private void Start()
    {
        StartCoroutine(CinemachineSpawnRoutine());
    }

    private IEnumerator CinemachineSpawnRoutine()
    {
        CinemachineBrain brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : FindAnyObjectByType<CinemachineBrain>();
        if (brain != null)
        {
            brain.ActiveBlend = null;
        }
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.ResetTutorialFlags();
            TutorialManager.Instance.isCutscenePlaying = true;
        }

        // 초기 상태: 연출 오브젝트와 플레이어 비활성화
        if (drawingObject) drawingObject.SetActive(false);
        if (realPlayer) realPlayer.SetActive(false);

        Transform originalFollow = virtualCamera.Follow;
        virtualCamera.Follow = spawnPoint;

        // 씬 전환 시 카메라 튐 없이 스폰 위치로 즉시 이동 (z축 유지)
        Vector3 targetcamPos = new Vector3(spawnPoint.position.x, spawnPoint.position.y, virtualCamera.transform.position.z);
        virtualCamera.OnTargetObjectWarped(virtualCamera.transform, targetcamPos - virtualCamera.transform.position);
        virtualCamera.transform.position = targetcamPos;

        // 카메라 줌인 (Orthographic Size 조절)
        float startZoom = virtualCamera.Lens.OrthographicSize;
        float elapsedTime = 0f;

        while (elapsedTime < 0.5f)
        {
            float t = elapsedTime / 0.5f;
            virtualCamera.Lens.OrthographicSize = Mathf.Lerp(startZoom, targetZoomSize, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        virtualCamera.Lens.OrthographicSize = targetZoomSize;

        // 확대 후 잠시 대기
        yield return new WaitForSeconds(preZoomDelay);

        // 만년필 그려지는 애니메이션 재생
        if (drawingObject)
        {
            drawingObject.SetActive(true);
            yield return new WaitForSeconds(drawingAnimationDuration);
            Destroy(drawingObject); // 다 그려진 그림 제거
        }

        // 진짜 플레이어 소환 및 활성화
        if (realPlayer)
        {
            realPlayer.transform.position = spawnPoint.position;
            realPlayer.SetActive(true);
        }

        // 등장 후 잠시 대기
        yield return new WaitForSeconds(postSpawnDelay);

        // 시네머신이 다시 '진짜 플레이어'를 따라가도록 설정 (Follow 연결)
        if (realPlayer)
        {
            virtualCamera.Follow = realPlayer.transform;
            virtualCamera.OnTargetObjectWarped(realPlayer.transform, Vector3.zero);
        }
        else
        {
            virtualCamera.Follow = originalFollow;
        }

        // 카메라 원래 게임 크기로 줌아웃
        elapsedTime = 0f;
        while (elapsedTime < 0.5f)
        {
            float t = elapsedTime / 0.5f;
            virtualCamera.Lens.OrthographicSize = Mathf.Lerp(targetZoomSize, defaultZoomSize, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        virtualCamera.Lens.OrthographicSize = defaultZoomSize;

        // 연출 종료 후 컷씬 해제 및 최초 이동 해금
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.isCutscenePlaying = false;
            TutorialManager.Instance.canMove = true;
        }

        Debug.Log("연출 종료, 게임 시작!");
        Destroy(this); // 이 연출 스크립트 삭제
    }
}