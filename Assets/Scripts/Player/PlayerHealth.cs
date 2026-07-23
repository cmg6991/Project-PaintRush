using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("--- 체력 설정 ---")]
    [SerializeField] private int maxHp = 5;
    private int currentHp;
    private bool isDead;

    public bool IsDead => isDead;
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool CanHeal => !isDead && currentHp < maxHp;
    public bool IsFullHealth => currentHp >= maxHp;

    [Header("--- 머리 위 하트 UI 설정 ---")]
    [SerializeField] private Transform heartUiTransform; // 머리 위 하트 UI 전체 오브젝트 (회전 고정용)
    [SerializeField] private HeartHp heartUiScript; // 🌟 마스크 Transform 대신 PlayerHeartUI 스크립트 연결!

    private Quaternion heartInitialRotation;

    private void Awake()
    {
        currentHp = maxHp;
        isDead = false;
    }

    private void Start()
    {
        if (DataManager.Instance != null)
        {
            currentHp = DataManager.Instance.CurrentPlayerStat.currentHp;
        }

        if (heartUiTransform != null)
        {
            heartInitialRotation = heartUiTransform.rotation;
        }

        // 게임 시작 시 하트 게이지 즉시 갱신
        UpdateHeartFill();
    }

    private void LateUpdate()
    {
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

        Project.Player.PlayerController2D controller = GetComponent<Project.Player.PlayerController2D>();
        
        // 무적 상태라면 체력 차감, 넉백, 데이터 동기ㅗ하 스킵하고 탈출
        if (controller != null && controller.IsInvincible) return;

        currentHp = Mathf.Max(0, currentHp - damage);

        Debug.Log($"플레이어 피격! HP : {currentHp}");

        // 🌟 하트 UI 실시간 갱신 호출
        UpdateHeartFill();

        // 피격당할 때 플레이어 본체 넉백 및 스케일 팽창 연출 호출
        if (controller != null && attacker != null)
        {
            controller.ApplyKnockback(attacker.transform.position);
        }

        if (DataManager.Instance != null)
        {
            DataManager.Instance.UpdatePlayerHp(currentHp, maxHp);
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 플레이어 체력을 회복합니다.
    /// 실제로 회복된 양을 반환합니다.
    /// </summary>
    public int Heal(int amount)
    {
        if (amount <= 0 || !CanHeal)
            return 0;

        int previousHp = currentHp;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        int healedAmount = currentHp - previousHp;

        if (healedAmount <= 0)
            return 0;

        Debug.Log($"플레이어 HP 회복! +{healedAmount}, HP : {currentHp}/{maxHp}");

        UpdateHeartFill();

        if (DataManager.Instance != null)
        {
            DataManager.Instance.UpdatePlayerHp(currentHp, maxHp);
        }

        return healedAmount;
    }

    // 🌟 마스크 이동 대신 PlayerHeartUI의 UpdateHeartFill을 불러주도록 수정!
    public void UpdateHeartFill()
    {
        if (heartUiScript != null)
        {
            heartUiScript.UpdateHeartFill(currentHp, maxHp);
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("플레이어 사망");
        SoundManager.Instance.PlaySFX(SFXType.PlayerDead);

        Project.Player.PlayerController2D controller = GetComponent<Project.Player.PlayerController2D>();
        if (controller != null)
        {
            controller.StopInvincibleBlink();
        }

        PlayerAttack attackComponent = GetComponent<PlayerAttack>();
        if (attackComponent != null)
        {
            attackComponent.HideWeapon();
        }

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("death");
        }

        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        Rigidbody2D playerRb = GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.gravityScale = 3f;
        }

        StartCoroutine(DieDelayRoutine());
    }

    private IEnumerator DieDelayRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        gameObject.SetActive(false);
        UIManager.Instance.ShowRestartUI();
        if (TutorialManager.Instance != null && TutorialManager.Instance.currentRespawnPoint != null)
        {
            RespawnAtTutorialCheckpoint(TutorialManager.Instance.currentRespawnPoint.position);
        }
        else
        {
            gameObject.SetActive(false);
            UIManager.Instance.ShowRestartUI();
        }
    }

    // 튜토리얼 전용 부활
    public void RespawnAtTutorialCheckpoint(Vector3 respawnPosition)
    {
        isDead = false;
        currentHp = maxHp;  // 체력 100퍼 복구
        UpdateHeartFill();  // 하트 UI 복구

        // 사망할 때 바뀐 레이어 및 위치 복구
        gameObject.layer = LayerMask.NameToLayer("Player");
        transform.position = respawnPosition;

        // 사망할 때 멈췄던 물리 복구
        Rigidbody2D playerRb = GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero; // 남아있는 물리 속도 제거
            playerRb.gravityScale = 1.5f;           // 중력 원래대로
        }

        // 사망 모션에서 기본 상태로 애니메이터 리셋
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }
    }

    private void OnEnable()
    {
        isDead = false;

        gameObject.layer = LayerMask.NameToLayer("Player");

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D col in colliders)
        {
            col.enabled = true;
        }

        Rigidbody2D playerRb = GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.simulated = true;
            playerRb.linearVelocity = Vector2.zero;
        }
    }
}