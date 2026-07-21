using UnityEngine;

public class Spike : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerHealth player =
            collision.collider.GetComponentInParent<PlayerHealth>();

        if (player == null || player.IsDead)
            return;

        player.TakeDamage(
            damage,
            Color.clear,   // 가시는 속성색이 없으므로 아무 색이나 전달
            gameObject,    // 공격자 = 가시
            true           // 속성 무시
        );
        Debug.Log("가시 닿음");
    }
}
