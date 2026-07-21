using UnityEngine;

public class TutorialZone : MonoBehaviour
{
    public enum TutorialStep {  Move, Jump, Shoot, Climb, ShowGun}
    public TutorialStep targetStep;

    [TextArea]
    public string guideMessage;     // 화면에 띄울 가이드

    public bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTriggered) return;

        if (collision.CompareTag("Player"))
        {
            isTriggered = true;

            // 단계별로 입력 잠금 해제
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

            Debug.Log($"[튜토리얼] {guideMessage}");
            // TODO : UI 텍스트 창에 guideMessage를 띄우는 함수 호출

            // 구역 통과 완료 후 트리거 비활성화
            gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmos()
    {
        // 1. 투명한 노란색/초록색 네모 박스 그리기 (지나가지 전에는 노란색으로 표시)
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
