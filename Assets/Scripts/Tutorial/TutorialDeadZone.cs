using Unity.VisualScripting;
using UnityEngine;

public class TutorialDeadZone : MonoBehaviour
{
    [Header("--- 리스폰 위치 설정 ---")]
    [SerializeField] private Transform respawnPoint;    // 플레이어가 다시 돌아올 위치
    
    private void OnTriggerEnter2D(Collider2D collsion)
    {
        // 닿은 대상이 플레이어인지 확인
        if (collsion.CompareTag("Player"))
        {
            if (respawnPoint != null)
            {
                // 떨어질때 가속도와 물리속도 남는거 초기화
                Rigidbody2D rb = collsion.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;   // 낙하속도, 이동속도 초기화
                }

                collsion.transform.position = respawnPoint.position;
                Debug.Log("[DeadZone] 플레이어가 낙사하여 스폰 지점으로 이동");
            }
            else
            {
                Debug.LogWarning("[DeadZone] 리스폰 포인트가 지정되지 않았습니다.");
            }
        }
    }
}
