using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 팔레트 피버의 입력, 지속시간, 게이지와 연출을 관리합니다.
/// 피버 중에는 몬스터 피해가 최소 피버 데미지로 보정되고 속성 판정을 무시합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class PaletteSpecialAttack : MonoBehaviour
{
    public static PaletteSpecialAttack Instance { get; private set; }

    [Header("참조")]
    [SerializeField] private StagePaletteManager paletteManager;

    [Header("피버 설정")]
    [SerializeField, Min(0.1f)] private float feverDuration = 7f;
    [SerializeField, Min(1)] private int feverDamage = 100;

    [Header("입력")]
    [SerializeField] private bool enableDirectInput = true;
    [Tooltip("켜면 Q키만 사용합니다.")]
    [SerializeField] private bool qKeyOnly = true;
    [SerializeField] private bool useQKey = true;
    [SerializeField] private bool useEKey;
    [SerializeField] private bool useRightMouseButton;

    [Header("연출")]
    [SerializeField] private GameObject feverEffectPrefab;
    [SerializeField] private Transform effectSpawnPoint;

    [Header("런타임 확인")]
    [SerializeField] private bool isFeverActive;
    [SerializeField] private float remainingTime;

    private Coroutine feverCoroutine;
    private GameObject spawnedEffect;
    private FillColor fillColor;

    public bool IsFeverActive => isFeverActive;
    public int FeverDamage => feverDamage;
    public float RemainingTime => remainingTime;

    private void Awake()
    {
        PaletteSpecialAttack duplicate =
            FindOtherInScene(gameObject.scene);

        if (duplicate != null)
        {
            Debug.LogError(
                $"[{nameof(PaletteSpecialAttack)}] 같은 씬에 컴포넌트가 " +
                $"둘 이상 있습니다. {name}을 비활성화합니다.",
                this);

            enabled = false;
            return;
        }

        Instance = this;
        ResolveReferences();
    }

    private void Update()
    {
        if (enableDirectInput && WasActivationRequested())
            TryActivate();
    }

    /// <summary>
    /// 전달된 오브젝트와 같은 씬에서 활성화된 피버 컴포넌트를 찾습니다.
    /// </summary>
    public static PaletteSpecialAttack FindForScene(
        GameObject context)
    {
        if (context == null)
            return Instance;

        Scene scene = context.scene;

        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        if (Instance != null &&
            Instance.isActiveAndEnabled &&
            Instance.gameObject.scene == scene)
        {
            return Instance;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            PaletteSpecialAttack[] attacks =
                root.GetComponentsInChildren<PaletteSpecialAttack>(true);

            foreach (PaletteSpecialAttack attack in attacks)
            {
                if (attack != null && attack.isActiveAndEnabled)
                    return attack;
            }
        }

        return null;
    }

    /// <summary>
    /// 게이지가 준비되어 있을 때 피버를 시작합니다.
    /// </summary>
    public bool TryActivate()
    {
        if (isFeverActive)
            return false;

        ResolveReferences();

        if (paletteManager == null)
        {
            Debug.LogWarning(
                "[피버] 현재 씬에 StagePaletteManager가 없습니다.",
                this);

            return false;
        }

        if (!paletteManager.TryStartSpecialAttack())
            return false;

        StartFever();
        return true;
    }

    /// <summary>
    /// 현재 피버가 활성화되어 있으면 최종 피버 데미지를 계산합니다.
    /// </summary>
    public bool TryGetFeverDamage(
        int baseDamage,
        out int resolvedDamage)
    {
        resolvedDamage = baseDamage;

        if (!isFeverActive || baseDamage <= 0)
            return false;

        resolvedDamage = Mathf.Max(baseDamage, feverDamage);
        return true;
    }

    /// <summary>
    /// 기존 코드와의 호환을 위한 래퍼입니다.
    /// 새 코드는 TryGetFeverDamage 사용을 권장합니다.
    /// </summary>
    public bool ApplyMonsterDamageModifiers(
        ref int damage,
        ref bool ignoreElement)
    {
        if (!TryGetFeverDamage(damage, out int resolvedDamage))
            return false;

        damage = resolvedDamage;
        ignoreElement = true;
        return true;
    }

    public void OnPaletteAttack(InputValue value)
    {
        if (value.isPressed)
            TryActivate();
    }

    private void StartFever()
    {
        isFeverActive = true;
        remainingTime = feverDuration;

        paletteManager.SetSpecialAttackGaugeRemaining(1f);
        fillColor?.FeverOn();

        SpawnEffect();
        PlayBgm(BGMType.Fever);

        feverCoroutine = StartCoroutine(FeverRoutine());

        Debug.Log(
            $"[피버] 시작! {feverDuration:F1}초 동안 " +
            $"속성을 무시하고 최소 {feverDamage} 데미지",
            this);
    }

    private IEnumerator FeverRoutine()
    {
        while (remainingTime > 0f)
        {
            remainingTime = Mathf.Max(
                0f,
                remainingTime - Time.deltaTime);

            float remainingRatio =
                feverDuration > 0f
                    ? remainingTime / feverDuration
                    : 0f;

            paletteManager?.SetSpecialAttackGaugeRemaining(
                remainingRatio);

            yield return null;
        }

        EndFever();
    }

    private void EndFever()
    {
        if (!isFeverActive)
            return;

        isFeverActive = false;
        remainingTime = 0f;
        feverCoroutine = null;

        paletteManager?.SetSpecialAttackGaugeRemaining(0f);
        paletteManager?.CompleteSpecialAttack();

        fillColor?.FeverOff();
        DestroySpawnedEffect();
        PlayBgm(BGMType.Normal);

        Debug.Log(
            "[피버] 종료. 설정에 따라 팔레트와 물감 진행도를 초기화합니다.",
            this);
    }

    private bool WasActivationRequested()
    {
        bool requested = false;

        if (Keyboard.current != null)
        {
            requested =
                useQKey &&
                Keyboard.current.qKey.wasPressedThisFrame;

            if (!qKeyOnly)
            {
                requested |=
                    useEKey &&
                    Keyboard.current.eKey.wasPressedThisFrame;
            }
        }

        if (!qKeyOnly &&
            !requested &&
            useRightMouseButton &&
            Mouse.current != null)
        {
            requested =
                Mouse.current.rightButton.wasPressedThisFrame;
        }

        return requested;
    }

    private void ResolveReferences()
    {
        if (paletteManager == null)
        {
            paletteManager =
                StagePaletteManager.FindForScene(this);
        }

        if (fillColor == null)
        {
            fillColor =
                GetComponentInChildren<FillColor>(true);
        }
    }

    private void SpawnEffect()
    {
        if (feverEffectPrefab == null)
            return;

        Transform parent =
            effectSpawnPoint != null
                ? effectSpawnPoint
                : transform;

        spawnedEffect = Instantiate(
            feverEffectPrefab,
            parent.position,
            Quaternion.identity,
            parent);
    }

    private void DestroySpawnedEffect()
    {
        if (spawnedEffect == null)
            return;

        Destroy(spawnedEffect);
        spawnedEffect = null;
    }

    private static void PlayBgm(BGMType bgmType)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBGM(bgmType);
    }

    private PaletteSpecialAttack FindOtherInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            PaletteSpecialAttack[] attacks =
                root.GetComponentsInChildren<PaletteSpecialAttack>(true);

            foreach (PaletteSpecialAttack attack in attacks)
            {
                if (attack != this && attack.enabled)
                    return attack;
            }
        }

        return null;
    }

    private void OnDisable()
    {
        if (!isFeverActive)
            return;

        if (feverCoroutine != null)
        {
            StopCoroutine(feverCoroutine);
            feverCoroutine = null;
        }

        EndFever();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
