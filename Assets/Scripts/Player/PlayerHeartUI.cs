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
        // 같은 오브젝트에 붙어있는 Image 컴포넌트를 자동으로 확인
        if (heartFillImage == null)
        {
            heartFillImage = GetComponent<Image>();
        }

        // 에디터 설정을 잊었더라도 코드로 강제 Filled 타입 및 수직 깎임 세팅을 적용
        if (heartFillImage != null)
        {
            heartFillImage.type = Image.Type.Filled;
            heartFillImage.fillMethod = Image.FillMethod.Vertical;
            heartFillImage.fillOrigin = (int)Image.OriginVertical.Bottom; // 아래에서부터 채움
        }

        // 상위 부모 오브젝트에서 PlayerHealth 컴포넌트를 캐싱해 둡니다.
        playerHealth = GetComponentInParent<PlayerHealth>();
    }

    private void Start()
    {
        // 플레이어가 뒤집혀도 머리 위 UI는 회전 없음
        initialRotation = transform.rotation;
    }

    private void LateUpdate()
    {
        // 플레이어 본체가 좌우 Flip 등으로 회전해도 UI는 회전없음
        transform.rotation = initialRotation;
    }

    // IDamageable 인터페이스 구현: 머리 위 하트 피격 시 본체로 데미지 토스
    public void TakeDamage(int damage, Color attackColor, GameObject attacker, bool ignoreElement)
    {
        if (playerHealth != null)
        {
            // 본체 PlayerHealth로 데미지 처리를 위임합니다.
            playerHealth.TakeDamage(damage, attackColor, attacker, ignoreElement);
        }
        else
        {
            Debug.LogWarning("[PlayerHeartUI] 상위 부모에게서 PlayerHealth 컴포넌트를 찾을 수 없습니다.");
        }
    }

    // PlayerHealth에서 체력 변화가 완료된 후 호출하여 UI를 갱신하는 함수
    public void UpdateHeartFill(int currentHp, int maxHp)
    {
        if (heartFillImage == null) return;

        // 체력 비율 계산 Filled Image의 fillAmount에 대입
        float hpRatio = (float)currentHp / maxHp;
        heartFillImage.fillAmount = Mathf.Clamp01(hpRatio);

        // 물감이 다 빠질수록 색상도 흐려짐 (체력 0에 가까워질수록 원래 흰색 이미지로 돌아가거나 연해짐)
        heartFillImage.color = Color.Lerp(Color.white, Color.red, hpRatio);
    }
}
