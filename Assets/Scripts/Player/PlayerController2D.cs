using System.Collections;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;

namespace Project.Player
{
    // 이 스크립트가 붙은 오브젝트는 Rigidbody와 PlayerInput이 필수적
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("--- Physics Control Settings --- ")]
        [SerializeField, Range(1f, 20f)] private float moveSpeed = 8f;        // 지상 이동속도
        [SerializeField, Range(5f, 25f)] private float jumpForce = 12f;       // 점프력

        [Header("--- Floor Check Settings ---")]
        [SerializeField] private Transform groundCheckPoint;                  // 발밑 바닥 체크 레이캐스트
        [SerializeField] private LayerMask groundLayer;                       // 땅으로 인식할 타겟
        [SerializeField] private float groundCheckRadius = 0.2f;              // 바닥체크 원 크기
        [SerializeField] private float climbSpeed = 4f;                       // 사다리 등반 속도

        [Header("--- Hanger Settings ---")]
        [SerializeField, Range(-3f, 3f)] private float hangerYOffset = 1.2f;  // 행거 잡았을 때의 Y축 
        [SerializeField, Range(-3f, 3f)] private float hangerXOffset = 0f;    // 행거 잡았을 때의 X축

        [Header("--- Invicible (무적 깜빡이) Settings ---")]
        [SerializeField] private float invincibleDuration = 1.5f;             // 맞는 순간 무적되는 시간
        [SerializeField] private float blinkInterval = 0.1f;                  // 깜빡이는 간격


        private Rigidbody2D rb;
        private PlayerInputHandler inputHandler;
        private SpriteRenderer spriteRenderer;                                // 스프라이트를 투명하게 만들기 위함
        private bool isGrounded;                                              // 땅에 붙어있나
        private float originalGravityScale;                                   // 사다리에서 혹은 행거에서 떨어질때 중력 조절           

        // Character Look
        public bool IsFacingRight { get; private set; } = true;               // 플레이어 시선 방향
        public bool IsGroundedToAnim => isGrounded;                           // 애니메이션에게 땅 착지 여부 전달

        // Ladder State
        public bool isInsideLadder = false;                                   // 사다리 충돌범위안 판별
        public bool isClimbing = false;                                       // 사다리 오르는 중인가 판별

        // Hanging State
        public bool isInsideHanger = false;                                   // 손잡이 잡고 있는가
        public bool isHanging = false;                                        // 손잡이 잡고있는중인가

        private bool isFallingFromObject = false;                             // 점프로 탈출했을때 공중에 떠있나

        private Collider2D currentLadderCollider;                             // 충돌한 사다리와 행거의 중심점 좌표등을 빼옴
        private Collider2D currentHangerCollider;

        public bool IsClimbingOrHanging => isClimbing || isHanging;           // 둘중에 하나라도 하고있는가

        [HideInInspector] public bool isHurtTriggered = false;                // 피격당한 첫 프레임
        private bool isInvincible = false;                                    // 무적 발동 여부

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            inputHandler = GetComponent<PlayerInputHandler>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            originalGravityScale = rb.gravityScale;
        }

        void Update()
        {
            // 원을 계속 그려서 ground 있는지 검사
            isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);

            if (isGrounded)                         // 땅에 발이 닿으면 오브젝트 탈출 후 낙하상태 해제
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

            // 땅에 있고, 사다리/행거 안 탈 때 점프 키 누르면 Y축위로 속도를 줌
            if (inputHandler.JumpTriggered && isGrounded && !isClimbing && !isHanging)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }

            // 위아래 키입력이 있고 사다리 충돌 범위 안이라면
            if (isInsideLadder && currentLadderCollider != null && Mathf.Abs(inputHandler.MoveInput.y) > 0.1f)
            {

                // In Ladder
                if (!isClimbing)
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

                    if (isGrounded && inputHandler.MoveInput.y < -0.1f && transform.position.y > currentLadderCollider.bounds.center.y)
                    {
                        transform.position = new Vector3(transform.position.x, transform.position.y - 0.2f, transform.position.z);
                    }
                }

                // 사다리 타는중
                isClimbing = true;
                isHanging = false;
                isFallingFromObject = false;
            }

            // 행거 범위 안이고 윗키를 눌렀을시
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

                    // 행거 오프셋 지정 (바라보는 방향에따라)
                    float dynamicXOffset = IsFacingRight ? -hangerXOffset : hangerXOffset;

                    // 매달린 좌표 계산해서 행거를 사용할때는 플레이어 고정
                    float targetX = colliderCenter.x + dynamicXOffset;
                    float targetY = colliderCenter.y - hangerYOffset;
                    transform.position = new Vector3(targetX, targetY, transform.position.z);

                    rb.linearVelocity = Vector2.zero;
                }
                
                // 손잡이에 매달리는중
                isHanging = true;
                isClimbing = false;
                isFallingFromObject = false;
            }

            // 사다리 타는 도중 벗어날때
            if (isClimbing && currentLadderCollider != null)
            {
                float ladderMaxY = currentLadderCollider.bounds.max.y;      // 사다리 꼭대기 Y높이
                float ladderCenterY = currentLadderCollider.bounds.center.y;// 사다리 중앙 Y높이

                // 꼭대기 탈출
                if (groundCheckPoint.position.y > ladderMaxY && inputHandler.MoveInput.y > 0.1f)
                {
                    isClimbing = false;
                    rb.gravityScale = originalGravityScale;

                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

                    transform.rotation = Quaternion.Euler(0f, IsFacingRight ? 0f : 180f, 0f);   // 회전 : 틸트(떨림) 리셋
                }

                // 바닥 탈출
                else if (isGrounded && transform.position.y < ladderCenterY 
                    && inputHandler.MoveInput.y <= 0.1f 
                    && (inputHandler.MoveInput.y < -0.1f || Mathf.Abs(inputHandler.MoveInput.x) > 0.1f))
                {
                    isClimbing = false;
                    rb.gravityScale = originalGravityScale;
                    // 회전 오른쪽을 보고 있나 아닌가에 따라 y 변경
                    transform.rotation = Quaternion.Euler(0f, IsFacingRight ? 0f : 180f, 0f);
                }
            }

            // 사다리, 행거 도중 점프 눌러서 이탈
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
            // 사다리 타는 중일 때 물리 연산
            if (isClimbing)
            {
                rb.gravityScale = 0f;

                // 오직 위아래로만 움직이도록 속도 제어
                float moveY = inputHandler.MoveInput.y;
                rb.linearVelocity = new Vector2(0f, moveY * climbSpeed);

                if(Mathf.Abs(moveY) > 0.1f)
                {
                    // sin 파로 사다리로 움직일때만 흔들어줌
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
                transform.rotation = Quaternion.Euler(0f, IsFacingRight ? 0f : 180f, 0f);

                // 떨어지는 중이면 입력 무시
                if (isFallingFromObject)
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
            if (collision.GetComponent<ColorDropItem>() != null) return;

            if (collision.CompareTag("Enemy") && collision.isTrigger) return;

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

            else if (collision.CompareTag("Enemy"))
            {
                if (isInvincible) return;

                Debug.Log("<color=red>[플레이어 피격]</color>");

                isHurtTriggered = true;

                StartCoroutine(InvincibleBlinkRoutine());
            }
        }
        private void OnTriggerExit2D(Collider2D collision)
        {
            if(collision.GetComponent<ColorDropItem>() != null)
            {
                return;
            }

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

        private IEnumerator InvincibleBlinkRoutine()
        {
            isInvincible = true;
            float timer = 0f;

            while (timer < invincibleDuration)
            {
                if(spriteRenderer != null)
                {
                    float currentAlpha = spriteRenderer.color.a == 1f ? 0.2f : 1f;
                    spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, currentAlpha);
                }

                // 0.1초 대기후 다음 루프 실행
                yield return new WaitForSeconds(blinkInterval);
                timer += blinkInterval;
            }

            if (spriteRenderer != null)
            {
                Color normalColor = spriteRenderer.color;
                normalColor.a = 1f;                         // 알파값 보통 색깔 지정
                spriteRenderer.color = normalColor;
            }

            isInvincible = false;
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