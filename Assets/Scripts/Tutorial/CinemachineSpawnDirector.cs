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

    private void Start()
    {
        StartCoroutine(CinemachineSpawnRoutine());
    }

    private IEnumerator CinemachineSpawnRoutine()
    {
        // 초기 상태: 연출 오브젝트와 플레이어 비활성화
        if (drawingObject) drawingObject.SetActive(false);
        if (realPlayer) realPlayer.SetActive(false);

        // ※ 시네머신이 문 앞을 비추게 하려면, 
        // 연출 동안 시네머신의 Follow를 문 앞(SpawnPoint)에 있는 빈 오브젝트로 지정해두거나
        // 혹은 SpawnPoint 위치로 카메라를 잠시 고정하는 방식
        // 여기서는 가장 깔끔하게 연출 동안 Follow를 해제(null)하고 스폰 위치로 포커스를 맞춥니다.
        Transform originalFollow = virtualCamera.Follow;
        virtualCamera.Follow = null;

        // 카메라 위치를 스폰 위치로 즉시 이동 (z축 유지)
        Vector3 camPos = virtualCamera.transform.position;
        camPos.x = spawnPoint.position.x;
        camPos.y = spawnPoint.position.y;
        virtualCamera.transform.position = camPos;

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

        Debug.Log("연출 종료, 게임 시작!");
        Destroy(this); // 이 연출 스크립트 삭제
    }
}