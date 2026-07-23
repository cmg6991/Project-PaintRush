using System.Collections;
using System.Runtime.CompilerServices;
using Unity.Cinemachine;
using UnityEngine;

public class TutorialZone : MonoBehaviour
{
    public enum TutorialStep {  Move, Jump, Shoot, Climb, Hang, ShowGun, Item, Monster}
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

            TutorialManager.Instance.currentRespawnPoint = this.transform;
        }

        // 시네머신 카메라가 지정되어 있지 않다면 자동 검색
        if (virtualCamera == null)
        {
            virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        }

        // 카메라가 비춰줄 타겟이 지정되어 있다면 카메라 이동 연출 수행
        if (virtualCamera != null && cameraFocusTarget != null)
        {
            // 시네머신의 Follow 컴포넌트 가져오기
            CinemachineFollow followComp = virtualCamera.GetComponent<CinemachineFollow>();
            Vector3 originalDamping = Vector3.zero;
            if (followComp != null)
            {
                // 평소 플레이어를 빠르게 따라가던 Damping 수치를 변수에 기억해둠
                originalDamping = followComp.TrackerSettings.PositionDamping;
                // FocusTarget으로 날아갈 때만 Damping을 느긋하게 변경
                followComp.TrackerSettings.PositionDamping = new Vector3(5f, 5f, 1f);
            }
            // FocusTarget으로 이동 (느긋하고 부드럽게 지이이잉~ 이동)
            virtualCamera.Follow = cameraFocusTarget;

            // 지형/장애물 보여주며 대기 (cameraHoldDuration 초 동안)
            yield return new WaitForSeconds(cameraHoldDuration);

            // 플레이어로 돌아오기 전에 Damping을 원래의 빠른 수치로 즉시 원복
            if (followComp != null)
            {
                followComp.TrackerSettings.PositionDamping = originalDamping;
            }
            // 플레이어로 카메라 복귀
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
                case TutorialStep.Hang:
                    TutorialManager.Instance.canHang = true;
                    break;
                case TutorialStep.ShowGun:
                    TutorialManager.Instance.canShowGun = true;
                    break;
                case TutorialStep.Item:
                    TutorialManager.Instance.canShowItem = true;
                    break;
                case TutorialStep.Monster:
                    TutorialManager.Instance.canMonsterMove = true;
                    break;
            }

            // 연출 종료 및 조작 해제
            TutorialManager.Instance.isCutscenePlaying = false;
        }

            Debug.Log($"[튜토리얼 해금] {targetStep}:{guideMessage}");
            // TODO : UI 텍스트 창에 guideMessage를 띄우는 함수

            // TutorialManager를 통한 가이드 UI 텍스트 화면 출력
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.ShowGuideUI(targetStep, guideMessage);
            }
       

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
