using Project.Player;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("--- Gun Settings ---")]
    [SerializeField] private Transform gunTransform;            // 오브젝트
    [SerializeField] private Transform attackPoint;             // 여기서 레이캐스트 발사
    [SerializeField] private float attackRange = 14f;           // 레이캐스트 사정거리
    [SerializeField] private LayerMask enemyLayer;              // 몬스터 레이어

    [Header("--- Recoil (반동) Settings ---")]
    [SerializeField] private float recoilForce = 0.14f;         // 총이 뒤로 밀리는 거리
    [SerializeField] private float recoilDuration = 0.05f;      // 반동 속도

    [Header("--- Gun Sway(총기 흔들림) Settings ---")]
    [SerializeField] private float swaySpeed = 13f;             // 달릴때 흔들리는 속도
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
    private Vector3 swayOffset = Vector3.zero;
    private Vector3 recoilOffset = Vector3.zero;

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

        // 달리며 쏠 때 Sway(흔들림)와 Recoil(반동) 오프셋이 상호 충돌하지 않도록 최종 합산 처리
        if (gunTransform != null)
        {
            gunTransform.localPosition = gunOriginalLocalPos + swayOffset + recoilOffset;
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

        // 캐릭터 회전 상태와 상관없이 월드상의 마우스 위치를 똑바로 쳐다보게 방향 벡터 연산
        Vector3 targetDirection = (mouseWorldPos - gunPivot.position).normalized;

        // 왼쪽/오른쪽 조준을 일관성있게 처리하기 위해 X축 부호에 플레이어 바라보는 방향 반영
        float rawAngle = Mathf.Atan2(targetDirection.y, playerController.IsFacingRight ? targetDirection.x : -targetDirection.x) * Mathf.Rad2Deg;

        // 기획 오프셋 제한 적용 (-30도 ~ 30도)
        float clampedAngle = Mathf.Clamp(rawAngle, -30f, 30f);

        // 정렬 완료된 로컬 Z축 각도를 회전값에 직주입 (부모 뒤집힘 짐벌락 현상 완치)
        gunPivot.localRotation = Quaternion.Euler(0f, 0f, clampedAngle);
    }

    private void HandleGunSway()
    {
        if (gunTransform == null || inputHandler == null) { return; }

        bool isMoving = Mathf.Abs(inputHandler.MoveInput.x) > 0.1f;

        if (isMoving && playerController != null && playerController.IsGroundedToAnim)
        {
            float swayY = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
            swayOffset = new Vector3(0f, swayY, 0f);
        }
        else
        {
            swayOffset = Vector3.Lerp(swayOffset, Vector3.zero, Time.deltaTime * 10f);
        }
    }

    private IEnumerator ApplyRecoil()
    {
        isRecoiling = true;

        float directionSign = playerController.IsFacingRight ? -1f : 1f;
        Vector3 targetRecoilOffset = new Vector3(directionSign * recoilForce, 0f, 0f);

        float elapsed = 0f;
        while (elapsed < recoilDuration)
        {
            recoilOffset = Vector3.Lerp(Vector3.zero, targetRecoilOffset, elapsed / recoilDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        recoilOffset = targetRecoilOffset;

        elapsed = 0f;
        while (elapsed < recoilDuration * 2f)
        {
            recoilOffset = Vector3.Lerp(targetRecoilOffset, Vector3.zero, elapsed / (recoilDuration * 2f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        recoilOffset = Vector3.zero;

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

    // --- 무기 비활성화 (사망 시 호출) ---
    public void HideWeapon()
    {
        if (gunTransform != null) gunTransform.gameObject.SetActive(false);
        if (gunPivot != null) gunPivot.gameObject.SetActive(false);
    }

    // --- 무기 활성화 (리스폰/부활 시 호출) ---
    public void ShowWeapon()
    {
        if (gunTransform != null) gunTransform.gameObject.SetActive(true);
        if (gunPivot != null) gunPivot.gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        // 다시 살아날 때 자동으로 총을 보이게 설정
        ShowWeapon();
    }
}