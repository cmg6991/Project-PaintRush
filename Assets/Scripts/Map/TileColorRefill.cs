using System.Collections;
using System.Reflection;
using UnityEngine;

public class TileColorRefill : MonoBehaviour
{
    [Header("--- 리스폰 설정 ---")]
    [Tooltip("색상이 완전히 빨려나간 뒤 다시 차오르기까지 대기하는 시간")]
    public float refillDelay = 5f;

    private ColorMinus colorMinus;
    private Material mat;
    private bool isRefilling = false;

    // 페인트 private 변수 가지고 옴
    private FieldInfo absorbedField;
    
    void Awake()
    {
        colorMinus = GetComponent<ColorMinus>();

        // ColorMinus의 private 변수인 'isAbsorbed' 에 접근 권한 획득
        absorbedField = typeof(ColorMinus).GetField("isAbsorbed", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    // Update is called once per frame
    void Update()
    {
        // colorminus가 색을 모두 흡수하고
        // 리필 코루틴이 돌고 있지 않다면
        if (colorMinus != null && colorMinus.IsAbsorbed && !isRefilling)
        {
            StartCoroutine(RefillRoutine());
        }
    }

    IEnumerator RefillRoutine()
    {
        isRefilling = true;

        // 지정된 시간만큼 쿨타임 대기
        yield return new WaitForSeconds(refillDelay);

        // ColorMinus가 Awake()에서 생성해둔 복사 매태리얼을 가져옴
        mat = GetComponent<SpriteRenderer>().material;

        if (mat != null)
        {
            // 완전히 하얀 상태에서 원래 색이 차오르는 상태로 부드럽게 보간
            float duration = 1f / Mathf.Max(colorMinus.fillSpeed, 0.001f);
            float elapsed = 0f;

            // 현재 셰이더의 _Progress 값(보통 완전히 빠져있다면 1 근처)에서 시작
            float startProgress = mat.GetFloat("_Progress");

            while(elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // startProgress에서 완충까지 부드럽게 보간
                float currentProgress = Mathf.Lerp(startProgress, 0f, t);
                mat.SetFloat("_Progress", currentProgress);

                yield return null;
            }

            // 확실하게 완충 상태로 고정
            mat.SetFloat("_Progress", 0f);
        }

        // 리플랙션으로 ColorMinus의 isAbsorbed를 false 변경
        if(absorbedField != null)
        {
            absorbedField.SetValue(colorMinus, false);
        }

        isRefilling = false;
        Debug.Log($"[{gameObject.name}] 동적 타일 색상 리스폰 완료!");
    }
}
