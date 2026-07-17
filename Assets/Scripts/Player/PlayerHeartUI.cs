using UnityEngine;

public class PlayerHeartUI : MonoBehaviour, IDamageable
{
    [Header("--- Mask Reference ---")]
    [SerializeField] private Transform maskTransform; // 움직여서 물감을 가려줄 SpriteMask의 Transform

    [Header("--- Y Offset Config ---")]
    [SerializeField] private float minYOffset = -0.5f; // 체력 0일 때 마스크의 로컬 Y축 좌표 (다 가려지는 높이)
    [SerializeField] private float maxYOffset = 0f;    // 체력 100%일 때 마스크의 로컬 Y축 좌표 (꽉 채워지는 높이)

    private Quaternion initialRotation;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        // 상위 부모(new Player)에게서 PlayerHealth 컴포넌트
        playerHealth = GetComponentInParent<PlayerHealth>();
    }

    private void Start()
    {
        // 플레이어가 뒤집혀도 머리 위 UI는 회전 없음
        initialRotation = transform.rotation;
    }

    private void LateUpdate()
    {
        // 본체 회전 고정
        transform.rotation = initialRotation;
    }

    // IDamageable 구현: 머리 위 하트 피격 시 본체로 데미지 위임
    public void TakeDamage(int damage, Color attackColor, GameObject attacker, bool ignoreElement)
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, attackColor, attacker, ignoreElement);
        }
        else
        {
            Debug.LogWarning("[PlayerHeartUI] PlayerHealth 컴포넌트를 찾을 수 없습니다.");
        }
    }

    // PlayerHealth에서 체력이 변하면 실시간으로 마스크 좌표 보정
    public void UpdateHeartFill(int currentHp, int maxHp)
    {
        if (maskTransform == null) return;

        // 체력 비율 (0.0f ~ 1.0f)
        float hpRatio = (float)currentHp / maxHp;
        hpRatio = Mathf.Clamp01(hpRatio);

        // 체력 비율에 맞춰 minYOffset ~ maxYOffset 사이로 마스크 Y좌표를 보간
        float targetY = Mathf.Lerp(minYOffset, maxYOffset, hpRatio);

        // 마스크의 로컬 Y좌표만 실시간으로 수정
        maskTransform.localPosition = new Vector3(
            maskTransform.localPosition.x,
            targetY,
            maskTransform.localPosition.z
        );
    }
}