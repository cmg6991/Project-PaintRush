using Project.Player;
using System.Collections;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("--- Gun Settings ---")]
    [SerializeField] private Transform gunTransform;            // 오브젝트
    [SerializeField] private Transform attackPoint;             // 여기서 레이캐스트 발사
    [SerializeField] private float attackRange = 15f;           // 레이캐스트 사정거리
    [SerializeField] private LayerMask enemyLayer;              // 몬스터 레이어

    [Header("--- Recoil (반동) Settings ---")]
    [SerializeField] private float recoilForce = 0.15f;         // 총이 뒤로 밀리는 거리
    [SerializeField] private float recoilDuration = 0.05f;      // 반동 속도 (얼마나 빨리 밀렸나)

    [Header("--- Gun Sway(총기 흔들림) Settings ---")]
    [SerializeField] private float swaySpeed = 14f;             // 달릴때 흔들리는 속도
    [SerializeField] private float swayAmount = 0.05f;          // 달릴때 흔들리는 높이

    [Header("--- Gun Target Settings ---")]
    [SerializeField] private Transform gunPivot;                // 총 중심점

    public string currentWeaponColor = "White";

    [Header("--- Paint Inventory ---")]                         // 플레이어가 보유한 색깔별 물감
    public int redInkCount = 0;
    public int greenInkCount = 0;
    public int blueInkCount = 0;

    private PlayerController2D playerController;
    private PlayerInputHandler inputHandler;                    // 입력 확인되야 총이 흔들림
    private Vector3 gunOriginalLocalPos;                        // 총의 원래 자리 기억용
    private bool isRecoiling = false;                           // 반동 연출용

    private Camera mainCamera;

    //public Shoot shoot;

    private void Awake()
    {
        playerController = GetComponent<PlayerController2D>();
        inputHandler = GetComponent<PlayerInputHandler>();

        //shoot = GetComponent<Shoot>();

        mainCamera = Camera.main;

        if(gunTransform != null)
        {
            // 게임 시작시 총 좌표 기억
            gunOriginalLocalPos = gunTransform.localPosition;
        }
    }

    void Update()
    {
        HandleAiming();

        HandleGunSway();

        // 마우스 왼쪽 클릭 감지
        if(Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 매달려 있을 때는 하지 않음
            if (playerController != null && playerController.IsClimbingOrHanging)
            {
                return;
            }

            AttackWithLaser();
        }
    }

    private void HandleAiming()
    {
        if (playerController == null || gunPivot == null || mainCamera == null) { return; }

        // 화면상 마우스 좌표를 게임 공간 좌표로 변환
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10f));

        // 총기 손잡이(pivot)에서 마우스를 향하는 화살표
        Vector2 targetDirection = (mouseWorldPos - gunPivot.position).normalized;
        // 플레이어 시선 방향에 따른 기준 정면 화살표
        Vector2 forwardDirection = playerController.IsFacingRight ? Vector2.right : Vector2.left;

        // 마우스가 정면보다 위에 있으면 +, 정면보다 아래에 있으면 -
        float rawAngle = Vector2.SignedAngle(forwardDirection, targetDirection);

        // Mathf.Clamp를 이용해 위아래 30도 이내로 각도를 강제 제한
        // 마우스가 50도 위로 올라가도, 이 함수를 거치면 무조건 30도에서 딱 멈춤
        float clampedAngle = Mathf.Clamp(rawAngle, -30f, 30f);

        if(!playerController.IsFacingRight)
        {
            clampedAngle *= -1f;
        }

        // 최종 가둔 각도(Z축)를 총기 손잡이(gunPivot)의 회전값에 부드럽게 주입
        gunPivot.localRotation = Quaternion.Euler(0f, 0f, clampedAngle);
    }

    private void HandleGunSway()
    {
        if (gunTransform == null || inputHandler == null){return;}

        // 플레이어가 좌우로 움직이고 있나 체크
        bool isMoving = Mathf.Abs(inputHandler.MoveInput.x) > 0.1f;

        // 플레이어가 땅에 붙어있고 걷는 중일때만 흔들림
        if (isMoving && playerController != null && playerController.IsGroundedToAnim)
        {
            // 흔들림 사인파로
            float swayY = Mathf.Sin(Time.time * swaySpeed) * swayAmount;

            gunTransform.localPosition = new Vector3(gunOriginalLocalPos.x, gunOriginalLocalPos.y + swayY, gunOriginalLocalPos.z);
        }
        else
        {
            gunTransform.localPosition = Vector3.Lerp(gunTransform.localPosition, gunOriginalLocalPos, Time.deltaTime * 10f);
        }
    }

    void AttackWithLaser()
    {
        // 총 반동 연출
        if(gunTransform != null && !isRecoiling)
        {
            StartCoroutine(ApplyRecoil());
        }

        // 플레이어가 바라보는 방향 벡터
        Vector2 fireDirection = attackPoint.right;

        //// 총구에서 레이캐스트 발사
        //RaycastHit2D hit = Physics2D.Raycast(attackPoint.position, fireDirection, attackRange, enemyLayer);

        //Debug.DrawRay(attackPoint.position, fireDirection * attackRange, Color.magenta, 0.3f);

        //if(hit.collider != null)
        //{
        //    // 몬스터 조준 감지 성공 확인
        //    Debug.Log($"<color=orange>[레이저 조준 완료]</color> {hit.collider.name}를 조준했습니다!");

        //        // 기타 색을 채우라고 지시하는 명령  <--------------- 여기에서 붙여서 사용하시면 되요
        //}

        //shoot.ShootPaint();
    }
    private IEnumerator ApplyRecoil()
    {
        isRecoiling = true;

        // 플레이어가 바라보는 방향의 반대 방향으로 총을 밈
        float directionSign = playerController.IsFacingRight ? -1f : 1f;
        Vector3 recoilOffset = new Vector3(directionSign * recoilForce, 0f, 0f);

        Vector3 targetRecoilPos = gunOriginalLocalPos + recoilOffset;

        // 총을 순식간에 뒤로 밀어냄
        float elapsed = 0f;
        while (elapsed < recoilDuration)
        {
            gunTransform.localPosition = Vector3.Lerp(gunOriginalLocalPos, targetRecoilPos, elapsed / recoilDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        gunTransform.localPosition = targetRecoilPos;


        // 원래 위치로 총을 복귀
        elapsed = 0f;
        while (elapsed < recoilDuration * 2f)
        {
            gunTransform.localPosition = Vector3.Lerp(targetRecoilPos, gunOriginalLocalPos, elapsed / (recoilDuration * 2f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        gunTransform.localPosition = gunOriginalLocalPos;

        isRecoiling = false;
    }

    // See AttackRange
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.cyan;

        Vector3 direction = Application.isPlaying && playerController != null
            && !playerController.IsFacingRight ? Vector3.left : Vector3.right;
        Gizmos.DrawLine(attackPoint.position, attackPoint.position + attackPoint.right * attackRange);
    }

    public void AddInk(string colorName)
    {
        switch (colorName)
        {
            case "Red":
                redInkCount++;
                Debug.Log($"<color=red>[물감 획득]</color> 빨간 물감 추가! 현재 개수 : {redInkCount}");
                break;
            case "Green":
                greenInkCount++;
                Debug.Log($"<color=red>[물감 획득]</color> 초록 물감 추가! 현재 개수 : {greenInkCount}");
                break;
            case "Blue":
                blueInkCount++;
                Debug.Log($"<color=red>[물감 획득]</color> 파란 물감 추가! 현재 개수 : {greenInkCount}");
                break;
            default:
                Debug.LogWarning($" 알 수 없는 색상의 물감입니다: {colorName}");
                break;
        }

        // 팔레트 UI 갱신
        // 다 모으면 펑 하고 전멸기 발동
    }
}
