using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Player
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerAnimation : MonoBehaviour
    {
        private Animator animator;
        private Rigidbody2D rb;
        private PlayerInputHandler inputHandler;
        private PlayerController2D playerController;
        private PlayerHealth playerHealth;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
            inputHandler = GetComponent<PlayerInputHandler>();
            playerController = GetComponent<PlayerController2D>();
            playerHealth = GetComponent<PlayerHealth>();
        }

        private void Update()
        {
            if (playerController == null) return;

            // 이미 사망했다면 애니메이션 노드를 Dead로 고정하기 위해 다른 상태값 업데이트 정지
            if (playerHealth != null && playerHealth.IsDead)
            {
                return;
            }

            // Check Running Condition (사다리 낙하 중일 때는 이동 입력을 해도 걷기 모션이 재생되지 않도록 방어)
            bool isMoving = Mathf.Abs(inputHandler.MoveInput.x) > 0.1f && !playerController.IsFallingFromLadder;
            animator.SetBool("isMoving", isMoving);

            // jump Condition 
            animator.SetBool("isGrounded", playerController.IsGroundedToAnim);

            // Climbing Ladder Condition 
            animator.SetBool("isClimbing", playerController.isClimbing);

            // Hanging Hanger Condition
            animator.SetBool("isHanging", playerController.isHanging);

            // Jump Animation finish
            if (playerController.isClimbing || playerController.isHanging || playerController.isInsideLadder)
            {
                animator.SetBool("isGrounded", true);
            }
            else
            {
                // In ground, In air, Send Check value
                animator.SetBool("isGrounded", playerController.IsGroundedToAnim);
            }

            // Check Attack LeftMouseButton
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (playerController.IsClimbingOrHanging)
                {
                    return;
                }
                //animator.SetTrigger("attack");  // 애니메이션 IDLE 사용
            }

            if (playerController.isHurtTriggered)
            {
                animator.SetTrigger("hurt");

                playerController.isHurtTriggered = false;
            }
        }
    }
}