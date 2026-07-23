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

        currentHp = Mathf.Max(0, currentHp - damage);

        Debug.Log($"플레이어 피격! HP : {currentHp}");

        // 🌟 하트 UI 실시간 갱신 호출
        UpdateHeartFill();

        Project.Player.PlayerController2D controller = GetComponent<Project.Player.PlayerController2D>();
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
            anim.SetTrigger("die");
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