using Unity.VisualScripting;
using UnityEngine;

public class PlayerMagnet : MonoBehaviour
{
    [Header("--- 흡수 설정 ---")]
    [SerializeField] private float initialPullSpeed = 5f;

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
