using Unity.VisualScripting;
using UnityEngine;

public class ColorDropItem : MonoBehaviour
{

    [Header("--- 둥둥 떠다니는 연출 설정---")]
    [SerializeField] private float floatSpeed = 3f;         // 위아래 움직이는 속도
    [SerializeField] private float floatAmount = 0.2f;      // 위아래 움직이는 범위

    public string itemColor = "Red";

    private bool isBeingPulled = false;                     // 플레이어의 당김여부
    private Transform playerTarget;                         // 플레이어 추적용
    private float pullSpeed;                                // 실시간 흡수 속도
    private Vector3 startLocalPos;                          // 처음 위치 기억

    private void Awake()
    {
        startLocalPos = transform.position; 
    }

    private void Update()
    {
        if (!isBeingPulled)
        {
            float newY = startLocalPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        else
        {
            if (playerTarget == null) return;

            pullSpeed += 15f * Time.deltaTime;

            transform.position = Vector2.MoveTowards(       // 아이템 플레이어에게로
                transform.position,
                playerTarget.position,
                pullSpeed * Time.deltaTime
            );
        }
    }

    public void StartMagnet(Transform target, float initialSpeed)
    {
        if (isBeingPulled) return;

        playerTarget = target;
        pullSpeed = initialSpeed;
        isBeingPulled = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerAttack playerAttack = collision.GetComponent<PlayerAttack>();

            if(playerAttack != null)
            {
                playerAttack.AddInk(itemColor);

                Destroy(gameObject);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 4f);
    }
}
