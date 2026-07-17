using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHp = 5;
    [SerializeField, HideInInspector] private PlayerHeartUI heartUI; // 머리 위 하트 UI 스크립트 참조 (Awake에서 자동 탐색)

    private int currentHp;
    private bool isDead;

    private void Awake()
    {
        currentHp = maxHp;
        isDead = false;

        if (heartUI == null)
        {
            heartUI = GetComponentInChildren<PlayerHeartUI>();
        }
    }

    private void Start()
    {
        // DataManager에 저장된 이전 체력이 있다면 불러오기 (느슨한 연동)
        if (DataManager.Instance != null)
        {
            currentHp = DataManager.Instance.CurrentPlayerStat.currentHp;
        }

        // 게임 시작 시 하트 UI에 현재 체력(100%)을 전송해 꽉 채워둡니다.
        if (heartUI != null)
        {
            heartUI.UpdateHeartFill(currentHp, maxHp);
        }
    }

    public void TakeDamage(
        int damage,
        Color attackColor,
        GameObject attacker,
        bool ignoreElement)
    {
        if (isDead) return;

        currentHp -= damage;

        Debug.Log($"플레이어 피격! HP : {currentHp}");

        // 피격당할 때마다 하트 UI의 물감 양을 갱신합니다.
        if (heartUI != null)
        {
            heartUI.UpdateHeartFill(currentHp, maxHp);
        }

        // DataManager 실시간 데이터 동기화 (기존 코드를 건드리지 않고 1줄 추가)
        if (DataManager.Instance != null)
        {
            DataManager.Instance.UpdatePlayerHp(currentHp, maxHp);
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("플레이어 사망");

        gameObject.SetActive(false);
    }
}