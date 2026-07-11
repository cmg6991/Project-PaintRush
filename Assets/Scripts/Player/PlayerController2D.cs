using System.Security.Cryptography;
using UnityEngine;

namespace Project.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("--- Physics Control Settings --- ")]
        [SerializeField, Range(1f, 20f)] private float moveSpeed = 8f;
        [SerializeField, Range(5f, 25f)] private float jumpForce = 12f;

        [Header("--- Floor Check Settings ---")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private float climbSpeed = 4f;

        [Header("--- Hanger Settings ---")]
        [SerializeField, Range(-3f, 3f)] private float hangerYOffset = 1.2f;
        [SerializeField, Range(-3f, 3f)] private float hangerXOffset = 0f;


        private Rigidbody2D rb;
        private PlayerInputHandler inputHandler;
        private bool isGrounded;
        private float originalGravityScale; // In ladder, gravity 0

        // Character Look
        public bool IsFacingRight { get; private set; } = true;
        public bool IsGroundedToAnim => isGrounded;

        // Ladder State
        public bool isInsideLadder = false;
        public bool isClimbing = false;

        // Hanging State
        public bool isInsideHanger = false;
        public bool isHanging = false;

        // 사다리/행잉 탈출 후 낙하기억
        private bool isFallingFromObject = false;

        // 기타 값 조절
        private Collider2D currentLadderCollider;
        private Collider2D currentHangerCollider;

        public bool IsClimbingOrHanging => isClimbing || isHanging;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            inputHandler = GetComponent<PlayerInputHandler>();

            originalGravityScale = rb.gravityScale;
        }

        void Update()
        {
            // Floor Check
            isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);

            if (isGrounded)
            {
                isFallingFromObject = false;
            }

            // Print Debug Log
            //if (inputHandler.JumpTriggered)
            //{
            //    Collider2D hitCollider = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
            //    string detectedObjectName = hitCollider != null ? hitCollider.name : "없음 (Null)";

            //    Debug.Log($"<color=cyan>[Jump 시스템 디버그]</color> " +
            //              $"스페이스바 입력: {inputHandler.JumpTriggered} | " +
            //              $"isGrounded(땅인가?): {isGrounded} | " +
            //              $"감지된 바닥 오브젝트: {detectedObjectName}");
            //}

            // Jump
            if (inputHandler.JumpTriggered && isGrounded && !isClimbing && !isHanging)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }

            // Check Ladder
            if (isInsideLadder && currentLadderCollider != null && Mathf.Abs(inputHandler.MoveInput.y) > 0.1f)
            {

                // In Ladder
                if (!isClimbing && currentLadderCollider != null)
                {
                    float playerX = transform.position.x;
                    float ladderCenterX = currentLadderCollider.bounds.center.x;

                    // 사다리를 마주 보도록 조준 시선 변경 
                    if (playerX < ladderCenterX)
                    {
                        // 사다리보다 오른쪽에 있었다면 무조건 '왼쪽'을 바라봐야 함
                        if (IsFacingRight) Flip();
                    }
                    else
                    {
                        // 사다리보다 왼쪽에 있었다면 무조건 '오른쪽'을 바라봐야 함
                        if (!IsFacingRight) Flip();
                    }
                }

                isClimbing = true;
                isHanging = false;
                isFallingFromObject = false;
            }

            if (isInsideHanger && currentHangerCollider != null && inputHandler.MoveInput.y > 0.1f)
            {
                if(!isHanging)
                {
                    float playerX = transform.position.x;
                    Vector3 colliderCenter = currentHangerCollider.bounds.center;

                    // 행거를 마주 보도록 조준 시선 변경 
                    if (playerX < colliderCenter.x)
                    {
                        // 행거보다 오른쪽에 있었다면 무조건 '왼쪽'을 바라봐야 함
                        if (IsFacingRight) Flip();
                    }
                    else
                    {
                        // 행거보다 왼쪽에 있었다면 무조건 '오른쪽'을 바라봐야 함
                        if (!IsFacingRight) Flip();
                    }
                    float dynamicXOffset = IsFacingRight ? -hangerXOffset : hangerXOffset;

                    float targetX = colliderCenter.x + dynamicXOffset;
                    float targetY = colliderCenter.y - hangerYOffset;
                    transform.position = new Vector3(targetX, targetY, transform.position.z);

                    rb.linearVelocity = Vector2.zero;
                }
                isHanging = true;
                isClimbing = false;
                isFallingFromObject = false;
            }

            // 사다리 Exit Condition
            if (isClimbing && currentLadderCollider != null)
            {
                // Y height calculation
                float ladderCenterY = currentLadderCollider.bounds.center.y;

                if (groundCheckPoint.position.y > ladderCenterY && inputHandler.MoveInput.y > 0.1f && isGrounded)
                {
                    isClimbing = false;
                    rb.gravityScale = originalGravityScale;

                    transform.rotation = Quaternion.Euler(0f, IsFacingRight ? 0f : 180f, 0f);
                }
                else if (isGrounded && (inputHandler.MoveInput.y < -0.1f || Mathf.Abs(inputHandler.MoveInput.x)> 0.1f))
                {
                    isClimbing = false;
                    rb.gravityScale = originalGravityScale;

                    transform.rotation = Quaternion.Euler(0f, IsFacingRight ? 0f : 180f, 0f);
                }
            }

            // In Haning state, Press Jump
            if ((isClimbing || isHanging) && inputHandler.JumpTriggered)
            {
                isClimbing = false;
                isHanging = false;
                rb.gravityScale = originalGravityScale;

                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.7f);
                isFallingFromObject = true;

                transform.rotation = Quaternion.Euler(0f, IsFacingRight ? 0f : 180f, 0f);
            }
        }

        private void FixedUpdate()
        {
            if (isClimbing)
            {
                rb.gravityScale = 0f;

                // In Ladder, only can move Y
                float moveY = inputHandler.MoveInput.y;
                rb.linearVelocity = new Vector2(0f, moveY * climbSpeed);

                if(Mathf.Abs(moveY) > 0.1f)
                {
                    float angle = Mathf.Sin(Time.time * 15f) * 10f;
                    transform.rotation = Quaternion.Euler(0f, IsFacingRight ? 0f : 180f, angle);
                }
                else
                {
                    transform.rotation = Quaternion.Euler(0f, IsFacingRight ? 0f : 180f, 0f);
                }
            }
            else if (isHanging)
            {
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                rb.gravityScale = originalGravityScale;

                // 떨어지는 중이면 입력 무시
                if(isFallingFromObject)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
                else
                {
                    // Velocity Control
                    float moveX = inputHandler.MoveInput.x;
                    rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);

                    // Character Flip
                    if (moveX > 0 && !IsFacingRight) Flip();
                    else if (moveX < 0 && IsFacingRight) Flip();
                }
            }
        }

        private void Flip()
        {
            IsFacingRight = !IsFacingRight;
            transform.Rotate(0f, 180f, 0f);
        }

        // Check Ladder Collision 
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Ladder"))
            {
                isInsideLadder = true;
                currentLadderCollider = collision;
            }
            else if (collision.CompareTag("Hanger"))
            {
                isInsideHanger = true;
                currentHangerCollider = collision;
            }
        }
        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Ladder"))
            {
                isInsideLadder = false;
                isClimbing = false;
                currentLadderCollider = null;
            }
            else if (collision.CompareTag("Hanger"))
            {
                isInsideHanger = false;
                isHanging = false;
                currentHangerCollider = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheckPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
            }
        }
    }
}