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

        // 현재 ColorMinus에 의해 _Progress가 하얀색인 상태
        float currentProgress = 1f;

        // 1에서 0으로 깎아내림, 원래 색상이 차오르게 역재생
        while(currentProgress > 0f)
        {
            // ColorMinus의 원래 속도로
            currentProgress -= Time.deltaTime * colorMinus.fillSpeed;

            // 셰이더 값 갱신
            mat.SetFloat("_Progress", currentProgress);
            yield return null;
        }

        // 오차 방지를 위해 0으로 세팅
        mat.SetFloat("_Progress", 0f);

        // 리플랙션으로 ColorMinus의 isAbsorbed를 false 변경
        if(absorbedField != null)
        {
            absorbedField.SetValue(colorMinus, false);
        }

        isRefilling = false;
        Debug.Log($"[{gameObject.name}] 동적 타일 색상 리스폰 완료!");
    }
}
