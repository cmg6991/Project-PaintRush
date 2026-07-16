using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaletteSpecialAttack : MonoBehaviour
{
    public static PaletteSpecialAttack Instance
    {
        get;
        private set;
    }

    [Header("참조")]
    [SerializeField]
    private StagePaletteManager paletteManager;

    [Header("피버 설정")]
    [SerializeField, Min(0.1f)]
    private float feverDuration = 7f;

    [SerializeField, Min(1)]
    private int feverDamage = 100;

    [Header("Q키 테스트")]
    [SerializeField]
    private bool enableKeyboardInput = true;

    [SerializeField]
    private Key feverKey = Key.Q;

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

    public bool IsFeverActive =>
        isFeverActive;

    public int FeverDamage =>
        feverDamage;

    public float RemainingTime =>
        remainingTime;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolvePaletteManager();
    }

    private void Update()
    {
        if (!enableKeyboardInput ||
            Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[feverKey]
            .wasPressedThisFrame)
        {
            TryActivate();
        }
    }

    public bool TryActivate()
    {
        ResolvePaletteManager();

        if (isFeverActive)
        {
            Debug.Log(
                "[피버] 이미 피버 타임이 진행 중입니다."
            );

            return false;
        }

        if (paletteManager == null)
        {
            Debug.LogWarning(
                "[피버] StagePaletteManager가 없습니다."
            );

            return false;
        }

        if (!paletteManager.CanUseSpecialAttack)
        {
            Debug.Log(
                "[피버] 발동 조건 부족. " +
                $"색상 완료={paletteManager.HasAllRequiredColors}, " +
                $"팔레트 장착={paletteManager.HasPaletteItem}"
            );

            return false;
        }

        feverCoroutine =
            StartCoroutine(FeverRoutine());

        return true;
    }

    public void OnPaletteAttack(
        InputValue value)
    {
        if (value.isPressed)
        {
            TryActivate();
        }
    }

    private IEnumerator FeverRoutine()
    {
        isFeverActive = true;
        remainingTime = feverDuration;

        SpawnEffect();

        Debug.Log(
            $"[피버] 피버 타임 시작! {feverDuration:F1}초"
        );

        while (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        EndFever();
    }

    private void EndFever()
    {
        isFeverActive = false;
        remainingTime = 0f;
        feverCoroutine = null;

        if (spawnedEffect != null)
        {
            Destroy(spawnedEffect);
            spawnedEffect = null;
        }

        /*
         * 피버 종료 시:
         * - 팔레트 아이템 초기화
         * - 수집한 색 전부 초기화
         */
        if (paletteManager != null)
        {
            paletteManager.ResetPaletteProgress(
                true,
                true
            );
        }

        Debug.Log(
            "[피버] 피버 종료. 팔레트와 수집 색 초기화"
        );
    }

    private void SpawnEffect()
    {
        if (feverEffectPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition =
            effectSpawnPoint != null
                ? effectSpawnPoint.position
                : transform.position;

        spawnedEffect = Instantiate(
            feverEffectPrefab,
            spawnPosition,
            Quaternion.identity
        );

        if (effectSpawnPoint != null)
        {
            spawnedEffect.transform.SetParent(
                effectSpawnPoint
            );
        }
    }

    private void ResolvePaletteManager()
    {
        if (paletteManager != null)
        {
            return;
        }

        paletteManager =
            StagePaletteManager.Instance;

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