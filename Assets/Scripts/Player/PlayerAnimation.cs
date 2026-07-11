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

        private void Awake()
        {
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
            inputHandler = GetComponent<PlayerInputHandler>();
            playerController = GetComponent<PlayerController2D>();
        }

        private void Update()
        {
            // Check Running Condition
            bool isMoving = Mathf.Abs(inputHandler.MoveInput.x) > 0.1f;
            animator.SetBool("isMoving", isMoving);

            // jump Condition 
            animator.SetBool("isGrounded", playerController.IsGroundedToAnim);

            // Climbing Ladder Condition 
            animator.SetBool("isClimbing", playerController.isClimbing);

            // Hanging Hanger Condition
            animator.SetBool("isHanging", playerController.isHanging);

            // Jump Animation finish
            if (playerController.isClimbing || playerController.isHanging)
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
                if(playerController.IsClimbingOrHanging)
                {
                    return;
                }
                animator.SetTrigger("attack");
            }
        }
    }
}