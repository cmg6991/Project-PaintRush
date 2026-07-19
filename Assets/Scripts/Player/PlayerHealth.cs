using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("--- 체력 설정 ---")]
    [SerializeField] private int maxHp = 5;
    private int currentHp;
    private bool isDead;

    public bool IsDead => isDead;

    [Header("--- 머리 위 하트 UI 설정 ---")]
    [SerializeField] private Transform heartUiTransform; // 머리 위 하트 UI 전체 오브젝트 (회전 고정용)
    [SerializeField] private Transform maskTransform;    // 물감을 채워줄 SpriteMask의 Transform
    [SerializeField] private float minYOffset = -0.5f;   // 체력 0일 때 마스크의 로컬 Y축 좌표
    [SerializeField] private float maxYOffset = 0f;      // 체력 100%일 때 마스크의 로컬 Y축 좌표

    private Quaternion heartInitialRotation;

    private void Awake()
    {
        currentHp = maxHp;
        isDead = false;
    }

    private void Start()
    {
        // DataManager에 저장된 이전 체력이 있다면 불러오기 (느슨한 연동)
        if (DataManager.Instance != null)
        {
            currentHp = DataManager.Instance.CurrentPlayerStat.currentHp;
        }

        // 머리 위 하트 UI 초기 회전값 기억 (본체가 뒤집혀도 머리 위 하트는 회전되지 않게 고정)
        if (heartUiTransform != null)
        {
            heartInitialRotation = heartUiTransform.rotation;
        }

        // 게임 시작 시 하트 게이지 즉시 갱신
        UpdateHeartFill();
    }

    private void LateUpdate()
    {
        // 본체 회전(Flip)이 적용되어도 머리 위 UI는 회전 없이 똑바로 고정
        if (heartUiTransform != null)
        {
            heartUiTransform.rotation = heartInitialRotation;
        }
    }

    public void TakeDamage(
        int damage,
        Color attackColor,
        GameObject attacker,
        bool ignoreElement)
    {
        if (isDead) return;

        currentHp = Mathf.Max(0, currentHp - damage);

        Debug.Log($"플레이어 피격! HP : {currentHp}");

        // 하트 UI 물감 양 실시간 갱신
        UpdateHeartFill();

        // 피격당할 때 플레이어 본체 넉백 및 스케일 팽창 연출 호출
        Project.Player.PlayerController2D controller = GetComponent<Project.Player.PlayerController2D>();
        if (controller != null && attacker != null)
        {
            controller.ApplyKnockback(attacker.transform.position);
        }

        // DataManager 실시간 데이터 동기화
        if (DataManager.Instance != null)
        {
            DataManager.Instance.UpdatePlayerHp(currentHp, maxHp);
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    // 머리 위 하트의 물감 마스크 높이를 보간해 연출하는 메서드
    public void UpdateHeartFill()
    {
        if (maskTransform == null) return;

        float hpRatio = (float)currentHp / maxHp;
        hpRatio = Mathf.Clamp01(hpRatio);

        float targetY = Mathf.Lerp(minYOffset, maxYOffset, hpRatio);

        maskTransform.localPosition = new Vector3(
            maskTransform.localPosition.x,
            targetY,
            maskTransform.localPosition.z
        );
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("플레이어 사망");
        SoundManager.Instance.PlaySFX(SFXType.PlayerDead);

        // 피격 깜빡이 코루틴 정지 및 색상 원복
        Project.Player.PlayerController2D controller = GetComponent<Project.Player.PlayerController2D>();
        if (controller != null)
        {
            controller.StopInvincibleBlink();
        }

        // 총기 숨김 연동
        PlayerAttack attackComponent = GetComponent<PlayerAttack>();
        if (attackComponent != null)
        {
            attackComponent.HideWeapon();
        }

        // 사망 애니메이션 트리거 작동 ("die")
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("die");
        }

        // 지형과는 부딪혀 안착하되 몬스터/투사체 충돌은 방어하도록 레이어를 임시 변경
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        // 제자리 정지 및 자연스러운 수직 낙하 물리 처리
        Rigidbody2D playerRb = GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero; // 넉백 비행력 완전 제거
            playerRb.gravityScale = 3f;             // 수직 하강만 하도록 기본 중력 강도로 리셋
        }

        // 사망 모션을 안전하게 다 연출한 뒤 게임오브젝트 비활성화
        StartCoroutine(DieDelayRoutine());
    }

    private IEnumerator DieDelayRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        isDead = false;

        // 리스폰 시 플레이어 레이어 원상 복원
        gameObject.layer = LayerMask.NameToLayer("Player");

        // 리스폰 시 혹시 꺼져있을 콜라이더 복구
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D col in colliders)
        {
            col.enabled = true;
        }

        // 리스폰 시 리지드바디 시뮬레이션 복구
        Rigidbody2D playerRb = GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.simulated = true;
            playerRb.linearVelocity = Vector2.zero;
        }
    }
}