using Project.Player;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    // Save Color
    public string currentWeaponColor = "None";

    private PlayerController2D playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController2D>();
    }

    void Update()
    {
        // Left Mouse Key
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if(playerController != null && playerController.IsClimbingOrHanging)
            {
                return;
            }
            Attack();
        }
    }

    void Attack()
    {
        // attackPoint 중심으로 attackRange 반경 안의 enemyLayer를 가진 콜라이더들을 감지
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            // Attack
            Debug.Log($"<color=cyan>[공격 성공]</color> {enemy.name}을(를) {currentWeaponColor} 색상으로 때렸습니다!");

            // take Damage
        }
    }

    // See AttackRange
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
