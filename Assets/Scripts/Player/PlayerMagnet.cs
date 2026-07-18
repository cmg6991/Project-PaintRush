using Unity.VisualScripting;
using UnityEngine;

public class PlayerMagnet : MonoBehaviour
{
    [Header("--- 흡수 설정 ---")]
    [SerializeField] private float initialPullSpeed = 5f;

    private void Awake()
    {
        // 자식 센서 오브젝트에 Rigidbody2D가 있는지 검사
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        // 만약 없다면 동적으로 추가하여 물리 이벤트를 격리
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // 중력의 영향을 받지 않고 충돌 이벤트만 격리하기 위해 Kinematic으로 강제 설정
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
    }

    private void Start()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        int itemLayer = LayerMask.NameToLayer("Item");

        if (playerLayer != -1 && itemLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, itemLayer, true);
            Debug.Log("<color=cyan>[자석 시스템]</color> 코드로 Player와 Item 레이어 간의 충돌 차단");
        }
    }


    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.CompareTag("Item") || collision.GetComponent<ColorDropItem>())
        {
            ColorDropItem item = collision.GetComponent<ColorDropItem>();

            if(item != null)
            {
                item.StartMagnet(transform.parent, initialPullSpeed);
            }
        }
    }
}
