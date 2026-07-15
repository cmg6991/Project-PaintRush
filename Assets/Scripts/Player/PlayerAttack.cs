using Project.Player;
using System.Collections;
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
    [SerializeField] private float recoilDuration = 0.05f;      // 반동 속도

    [Header("--- Gun Sway(총기 흔들림) Settings ---")]
    [SerializeField] private float swaySpeed = 14f;             // 달릴때 흔들리는 속도
    [SerializeField] private float swayAmount = 0.05f;          // 달릴때 흔들리는 높이

    [Header("--- Gun Target Settings ---")]
    [SerializeField] private Transform gunPivot;                // 총 중심점

    public string currentWeaponColor = "White";

    [Header("--- Paint Inventory ---")]
    public int redInkCount = 0;
    public int greenInkCount = 0;
    public int blueInkCount = 0;

    private PlayerController2D playerController;
    private PlayerInputHandler inputHandler;
    private Vector3 gunOriginalLocalPos;
    private bool isRecoiling = false;

    private Camera mainCamera;

    private void Awake()
    {
        playerController = GetComponent<PlayerController2D>();
        inputHandler = GetComponent<PlayerInputHandler>();
        mainCamera = Camera.main;

        if (gunTransform != null)
        {
            gunOriginalLocalPos = gunTransform.localPosition;
        }
    }

    void Update()
    {
        HandleAiming();
        HandleGunSway();

        // 여기서는 오직 마우스 클릭 시 "총이 뒤로 밀리는 반동 연출" 수행
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (playerController != null && playerController.IsClimbingOrHanging) return;

            if (gunTransform != null && !isRecoiling)
            {
                StartCoroutine(ApplyRecoil());
            }
        }
    }

    private void HandleAiming()
    {
        if (playerController == null || gunPivot == null || mainCamera == null) { return; }

        // 마우스 화면 좌표 2D 월드 좌표로 변환
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = gunPivot.position.z; // 평면 축 일치

        // 캐릭터 회전 상태와 상관없이 월드상의 마우스 위치를 똑바로 쳐다보게 방향 벡터를 직주입
        Vector3 targetDirection = (mouseWorldPos - gunPivot.position).normalized;

        // 본체 회전으로 축이 뒤집히는것 고려, 방향벡터 주입
        gunPivot.right = targetDirection;

        // 인스펙터창에 위아래 30도 범위를 강제 제한(Clamp)하기 위해 로컬 Euler 각도만 정규화
        Vector3 localEuler = gunPivot.localRotation.eulerAngles;
        float currentAngle = localEuler.z;

        // 유니티는 내부적으로 각도를 0~360도로 처리하므로 -180~180도 범위로 보정
        if (currentAngle > 180f) currentAngle -= 360f;

        // 기획 오프셋 제한 적용 (-30도 ~ 30도)
        float clampedAngle = Mathf.Clamp(currentAngle, -30f, 30f);

        // 정렬 완료된 각도를 회전값에 대입합니다. (대각선 튐 및 상하 역전 현상 완치)
        gunPivot.localRotation = Quaternion.Euler(0f, 0f, clampedAngle);
    }

    private void HandleGunSway()
    {
        if (gunTransform == null || inputHandler == null) { return; }

        bool isMoving = Mathf.Abs(inputHandler.MoveInput.x) > 0.1f;

        if (isMoving && playerController != null && playerController.IsGroundedToAnim)
        {
            float swayY = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
            gunTransform.localPosition = new Vector3(gunOriginalLocalPos.x, gunOriginalLocalPos.y + swayY, gunOriginalLocalPos.z);
        }
        else
        {
            gunTransform.localPosition = Vector3.Lerp(gunTransform.localPosition, gunOriginalLocalPos, Time.deltaTime * 10f);
        }
    }

    private IEnumerator ApplyRecoil()
    {
        isRecoiling = true;

        float directionSign = playerController.IsFacingRight ? -1f : 1f;
        Vector3 recoilOffset = new Vector3(directionSign * recoilForce, 0f, 0f);
        Vector3 targetRecoilPos = gunOriginalLocalPos + recoilOffset;

        float elapsed = 0f;
        while (elapsed < recoilDuration)
        {
            gunTransform.localPosition = Vector3.Lerp(gunOriginalLocalPos, targetRecoilPos, elapsed / recoilDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        gunTransform.localPosition = targetRecoilPos;

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

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(attackPoint.position, attackPoint.position + attackPoint.right * attackRange);
    }

    public void AddInk(string colorName)
    {
        switch (colorName)
        {
            case "Red": redInkCount++; break;
            case "Green": greenInkCount++; break;
            case "Blue": blueInkCount++; break;
        }
    }
}