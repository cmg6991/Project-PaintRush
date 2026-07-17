using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 팔레트 피버의 입력, 지속시간, 데미지 강화, 연출을 담당한다.
///
/// 일반 사격 코드는 수정하지 않는다.
/// MonsterAI.TakeDamage가 이 컴포넌트의 피버 상태를 확인해
/// 데미지를 강화하고 속성 검사를 무시한다.
/// </summary>
public class PaletteSpecialAttack : MonoBehaviour
{
    public static PaletteSpecialAttack Instance { get; private set; }

    [Header("참조")]
    [SerializeField]
    private StagePaletteManager paletteManager;

    [Header("피버 설정")]
    [SerializeField, Min(0.1f)]
    private float feverDuration = 7f;

    [Tooltip("피버 중 일반 사격 한 발이 주는 최소 데미지")]
    [SerializeField, Min(1)]
    private int feverDamage = 100;

    [Header("직접 입력")]
    [Tooltip("PlayerInput 없이 이 컴포넌트가 직접 키 입력을 확인")]
    [SerializeField]
    private bool enableDirectInput = true;

    [SerializeField]
    private bool useQKey = true;

    [SerializeField]
    private bool useEKey = true;

    [SerializeField]
    private bool useRightMouseButton = true;

    [Header("연출")]
    [SerializeField]
    private GameObject feverEffectPrefab;

    [SerializeField]
    private Transform effectSpawnPoint;

    [Header("런타임 확인")]
    [SerializeField]
    private bool isFeverActive;

    [SerializeField]
    private float remainingTime;

    private Coroutine feverCoroutine;
    private GameObject spawnedEffect;

    public bool IsFeverActive => isFeverActive;
    public int FeverDamage => feverDamage;
    public float RemainingTime => remainingTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolvePaletteManager();
    }

    private void Update()
    {
        if (!enableDirectInput)
        {
            return;
        }

        bool requested = false;
        if (Keyboard.current != null)
        {
            requested =
                (useQKey &&
                 Keyboard.current.qKey.wasPressedThisFrame) ||
                (useEKey &&
                 Keyboard.current.eKey.wasPressedThisFrame);
        }

        if (!requested &&
            useRightMouseButton &&
            Mouse.current != null)
        {
            requested =
                Mouse.current.rightButton.wasPressedThisFrame;
        }

        if (requested)
        {
            TryActivate();
        }
    }

    public bool TryActivate()
    {
        ResolvePaletteManager();

        if (isFeverActive)
        {
            return false;
        }

        if (paletteManager == null)
        {
            Debug.LogWarning(
                "[피버] StagePaletteManager가 없습니다."
            );
            return false;
        }

        if (!paletteManager.TryStartSpecialAttack())
        {
            return false;
        }

        isFeverActive = true;
        remainingTime = feverDuration;

        SpawnEffect();

        feverCoroutine =
            StartCoroutine(FeverRoutine());

        Debug.Log(
            $"[피버] 시작! {feverDuration:F1}초 동안 " +
            $"일반 사격이 속성을 무시하고 최소 {feverDamage} 데미지"
        );

        return true;
    }

    /// <summary>
    /// PlayerInput의 PaletteAttack 액션과 연결할 수 있다.
    /// 직접 입력을 사용할 경우에는 연결하지 않아도 된다.
    /// </summary>
    public void OnPaletteAttack(InputValue value)
    {
        if (value.isPressed)
        {
            TryActivate();
        }
    }

    /// <summary>
    /// MonsterAI가 일반 피격값을 피버 공격값으로 변환할 때 사용한다.
    /// 피버가 아니면 아무 값도 변경하지 않는다.
    /// </summary>
    public bool ApplyMonsterDamageModifiers(
        ref int damage,
        ref bool ignoreElement)
    {
        if (!isFeverActive)
        {
            return false;
        }

        damage = Mathf.Max(damage, feverDamage);
        ignoreElement = true;
        return true;
    }

    private IEnumerator FeverRoutine()
    {
        while (remainingTime > 0f)
        {
            remainingTime =
                Mathf.Max(0f, remainingTime - Time.deltaTime);

            yield return null;
        }

        EndFever();
    }

    private void EndFever()
    {
        if (!isFeverActive)
        {
            return;
        }

        isFeverActive = false;
        remainingTime = 0f;
        feverCoroutine = null;

        if (spawnedEffect != null)
        {
            Destroy(spawnedEffect);
            spawnedEffect = null;
        }

        if (paletteManager != null)
        {
            paletteManager.CompleteSpecialAttack();
        }

        Debug.Log(
            "[피버] 종료. 팔레트 아이템과 수집 색 초기화"
        );
    }

    private void SpawnEffect()
    {
        if (feverEffectPrefab == null)
        {
            return;
        }

        Transform parent =
            effectSpawnPoint != null
                ? effectSpawnPoint
                : transform;

        spawnedEffect = Instantiate(
            feverEffectPrefab,
            parent.position,
            Quaternion.identity,
            parent
        );
    }

    private void ResolvePaletteManager()
    {
        if (paletteManager != null)
        {
            return;
        }

        paletteManager = StagePaletteManager.Instance;

        if (paletteManager == null)
        {
            paletteManager =
                FindAnyObjectByType<StagePaletteManager>();
        }
    }

    private void OnDisable()
    {
        if (!isFeverActive)
        {
            return;
        }

        if (feverCoroutine != null)
        {
            StopCoroutine(feverCoroutine);
        }

        EndFever();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}