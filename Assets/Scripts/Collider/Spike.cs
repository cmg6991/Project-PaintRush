using UnityEngine;

public class Spike : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerHealth player = other.GetComponent<PlayerHealth>();

        if (player == null || player.IsDead)
            return;

        player.TakeDamage(
            damage,
            Color.clear,
            gameObject,
            true
        );

        Debug.Log("가시 닿음");
    }
}
