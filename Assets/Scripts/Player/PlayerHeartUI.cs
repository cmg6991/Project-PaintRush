using UnityEngine;
using UnityEngine.UI;

public class PlayerHeartUI : MonoBehaviour, IDamageable
{
    [Header("--- UI References ---")]
    [SerializeField] private Image heartFillImage;      // 빨간색 물감으로 채워진 Filled 이미지

    private Quaternion initialRotation;
    private PlayerHealth playerHealth;                  // 본체 체력 컴포넌트 캐싱

    private void Awake()
    {
        if (heartFillImage == null)
        {
            heartFillImage = GetComponent<Image>();
        }

        if (heartFillImage != null)
        {
            heartFillImage.type = Image.Type.Filled;
            heartFillImage.fillMethod = Image.FillMethod.Vertical;
            heartFillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        }

        // 상위 부모 오브젝트(new Player)에서 PlayerHealth 컴포넌트를 캐싱
        playerHealth = GetComponentInParent<PlayerHealth>();
    }

    private void Start()
    {
        initialRotation = transform.rotation;
    }

    private void LateUpdate()
    {
        transform.rotation = initialRotation;
    }

    // 하트 자체가 피격당했을 때, 동료분의 PlayerHealth로 데미지
    public void TakeDamage(int damage, Color attackColor, GameObject attacker, bool ignoreElement)
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, attackColor, attacker, ignoreElement);
        }
        else
        {
            Debug.LogWarning("[PlayerHeartUI] 상위 부모에게서 PlayerHealth 컴포넌트를 찾을 수 없습니다.");
        }
    }

    // 동료분의 PlayerHealth에서 체력이 깎인 뒤 이 함수를 호출해 줍니다.
    public void UpdateHeartFill(int currentHp, int maxHp)
    {
        if (heartFillImage == null) return;

        float hpRatio = (float)currentHp / maxHp;
        heartFillImage.fillAmount = Mathf.Clamp01(hpRatio);

        // 물감이 다 빠질수록 색상도 흐려짐
        heartFillImage.color = Color.Lerp(Color.white, Color.red, hpRatio);
    }
}