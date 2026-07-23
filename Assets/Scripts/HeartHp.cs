using UnityEngine;

public class HeartHp : MonoBehaviour, IDamageable
{
    [Header("--- Target Sprite ---")]
    [SerializeField] private SpriteRenderer heartFillRenderer; // 빨간색 하트의 SpriteRenderer

    private Quaternion initialRotation;
    private PlayerHealth playerHealth;
    private float maxHeartHeight; // 원본 하트의 높이 저장

    private void Awake()
    {
        // 1. 상위 부모(Player)에게서 PlayerHealth 컴포넌트 가져오기
        playerHealth = GetComponentInParent<PlayerHealth>();

        // 2. 초기 하트 높이 저장 (Awake 시점에 기억)
        if (maxHeartHeight <= 0 && heartFillRenderer != null)
        {
            maxHeartHeight = heartFillRenderer.size.y;
        }
        else
        {
            Debug.LogError("[PlayerHeartUI] heartFillRenderer가 연결되지 않았습니다!");
        }
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

    public void UpdateHeartFill(int currentHp, int maxHp)
    {
        if (heartFillRenderer == null) return;

        // 체력이 0일 때는 완전히 비움 (0.0)
        if (currentHp <= 0)
        {
            Vector2 zeroSize = heartFillRenderer.size;
            zeroSize.y = 0f;
            heartFillRenderer.size = zeroSize;
            return;
        }

        // 1. 순수 체력 비율 계산 (0.0 ~ 1.0)
        float rawRatio = (float)currentHp / maxHp;
        rawRatio = Mathf.Clamp01(rawRatio);

        // 2. [핵심] 체력이 1이라도 남아있다면, 최소 높이를 0.35(35%)로 보정!
        // rawRatio가 0~1로 변할 때, 실제 적용 비율은 0.35~1.0 사이로 매핑됨
        float minVisualRatio = 0.35f; // 체력이 1 남았을 때 보일 최소 비율 (원하는 만큼 0.3~0.4 조절 가능)
        float mappedRatio = Mathf.Lerp(minVisualRatio, 1.0f, rawRatio);

        // 3. 보정된 비율로 Size Y 적용
        Vector2 currentSize = heartFillRenderer.size;
        currentSize.y = maxHeartHeight * mappedRatio;
        heartFillRenderer.size = currentSize;

        //Debug.Log($"[Heart] 실제 체력비율: {rawRatio:F2} -> 연출용 비율: {mappedRatio:F2}, 최종 size.y: {currentSize.y}");
    }
}