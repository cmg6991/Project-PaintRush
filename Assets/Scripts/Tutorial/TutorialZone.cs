using System.Collections;
using System.Runtime.CompilerServices;
using Unity.Cinemachine;
using UnityEngine;

public class TutorialZone : MonoBehaviour
{
    public enum TutorialStep {  Move, Jump, Shoot, Climb, ShowGun}
    public TutorialStep targetStep;

    [Header("--- 가이드 연출 설정 ---")]
    [Tooltip("카메라가 비춰줄 목표 타겟 (없으면 카메라 이동 스킵)")]
    public Transform cameraFocusTarget;

    [Tooltip("카메라가 타겟을 비추고 대기하는 시간")]
    public float cameraHoldDuration = 1.5f;

    [Tooltip("시네머신 버츄얼 카메라 (비워두면 자동 탐색")]
    public CinemachineCamera virtualCamera;

    [TextArea]
    public string guideMessage;     // 화면에 띄울 가이드

    public bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTriggered) return;

        if (collision.CompareTag("Player"))
        {
            isTriggered = true;
            StartCoroutine(TriggerTutorialRoutine(collision.transform));
        }
    }

    private IEnumerator TriggerTutorialRoutine(Transform playerTransform)
    {
        // 조작 및 물리 잠금
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.isCutscenePlaying = true;
        }

        // 시네머신 카메라가 지정되어 있지 않다면 자동 검색
        if (virtualCamera == null)
        {
            virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        }

        // 카메라가 비춰줄 타겟이 지정되어 있다면 카메라 이동 연출 수행
        if (virtualCamera != null && cameraFocusTarget != null)
        {
            Transform originalFollow = virtualCamera.Follow;

            // 카메라 타겟을 장애물 / 기믹 위치로 변경
            virtualCamera.Follow = cameraFocusTarget;

            // 지정된 시간만큼 대기 (기믹 안내)
            yield return new WaitForSeconds(cameraHoldDuration);

            // 카메라를 다시 플레이어로 복귀
            virtualCamera.Follow = playerTransform;
            yield return new WaitForSeconds(0.3f);
        }

        // 단계별 입력 기능 해금
        if (TutorialManager.Instance != null)
        {
            switch (targetStep)
            {
                case TutorialStep.Move:
                    TutorialManager.Instance.canMove = true;
                    break;
                case TutorialStep.Jump:
                    TutorialManager.Instance.canJump = true;
                    break;
                case TutorialStep.Shoot:
                    TutorialManager.Instance.canShoot = true;
                    break;
                case TutorialStep.Climb:
                    TutorialManager.Instance.canClimb = true;
                    break;
                case TutorialStep.ShowGun:
                    TutorialManager.Instance.canShowGun = true;
                    break;
            }

            // 연출 종료 및 조작 해제
            TutorialManager.Instance.isCutscenePlaying = false;
        }

            Debug.Log($"[튜토리얼 해금] {targetStep}:{guideMessage}");
            // TODO : UI 텍스트 창에 guideMessage를 띄우는 함수

            // 구역 통과 완료 후 트리거 비활성화
            gameObject.SetActive(false);
    }
    
    private void OnDrawGizmos()
    {
        // 투명한 노란색/초록색 네모 박스 그리기 (지나가기 전에는 노란색으로 표시)
        Gizmos.color = isTriggered ? new Color(0f, 1f, 0f, 0.3f) : new Color(1f, 0.92f, 0.016f, 0.35f);

        // 오브젝트에 붙어있는 BoxCollider2D 크기를 자동으로 가져와서 기즈모 박스 크기로 반영
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            // 콜라이더의 로컬 오프셋과 크기를 월드 좌표 기준으로 변환해서 그리기
            Vector3 center = transform.TransformPoint(box.offset);
            Vector3 size = new Vector3(box.size.x * transform.lossyScale.x, box.size.y * transform.lossyScale.y, 1f);

            Gizmos.DrawCube(center, size); // 내부가 채워진 네모

            // 테두리 선은 진하게 그리기
            Gizmos.color = isTriggered ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(center, size);
        }
        else
        {
            // BoxCollider2D가 없을 경우 기본 오브젝트 위치에 사각형 표시
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
    }
}
