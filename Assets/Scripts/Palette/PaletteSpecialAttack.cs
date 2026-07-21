using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Q키 입력, 피버 지속시간, 데미지 강화와 연출을 담당합니다.
/// 같은 씬의 StagePaletteManager만 사용합니다.
/// </summary>
public class PaletteSpecialAttack : MonoBehaviour
{
    public static PaletteSpecialAttack Instance { get; private set; }

    [Header("참조")]
    [SerializeField]
    private StagePaletteManager paletteManager;

    [Header("피버 설정")]
    [SerializeField, Min(0.1f)]
    private float feverDuration = 5f;

    [SerializeField, Min(1)]
    private int feverDamage = 100;

    [Header("입력")]
    [SerializeField]
    private bool enableDirectInput = true;

    [Tooltip("켜면 Q키만 사용합니다.")]
    [SerializeField]
    private bool qKeyOnly = true;

    [SerializeField]
    private bool useQKey = true;

    [SerializeField]
    private bool useEKey = false;

    [SerializeField]
    private bool useRightMouseButton = false;

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
                $"[{nameof(PaletteSpecialAttack)}] 같은 씬에 컴포넌트가 둘 이상 있습니다. " +
                $"{gameObject.name}을 비활성화합니다.");

            enabled = false;
            return;
        }

        if (Instance == null ||
            Instance.gameObject.scene == gameObject.scene)
        {
            Instance = this;
        }

        ResolvePaletteManager();
        fillColor =
            GetComponentInChildren<FillColor>();
    }

    private void Update()
    {
        if (!enableDirectInput)
            return;

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

        if (requested)
            TryActivate();
    }

    public static PaletteSpecialAttack FindForScene(
        GameObject context)
    {
        if (context == null)
            return Instance;

        Scene scene = context.scene;

        if (!scene.IsValid() || !scene.isLoaded)
            return Instance;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            PaletteSpecialAttack attack =
                root.GetComponentInChildren<PaletteSpecialAttack>(true);

            if (attack != null && attack.enabled)
                return attack;
        }

        return null;
    }

    public bool TryActivate()
    {
        SoundManager.Instance.PlayBGM(BGMType.Fever);
        ResolvePaletteManager();

        if (isFeverActive)
            return false;

        if (paletteManager == null)
        {
            Debug.LogWarning(
                "[피버] 현재 씬에 StagePaletteManager가 없습니다.");

            return false;
        }

        if (!paletteManager.TryStartSpecialAttack())
            return false;

        isFeverActive = true;
        remainingTime = feverDuration;
        paletteManager.SetSpecialAttackGaugeRemaining(1f);

        fillColor?.FeverOn();
        SpawnEffect();

        feverCoroutine =
            StartCoroutine(FeverRoutine());

        Debug.Log(
            $"[피버] 시작! {feverDuration:F1}초 동안 " +
            $"속성을 무시하고 최소 {feverDamage} 데미지");

        return true;
    }

    public void OnPaletteAttack(InputValue value)
    {
        if (value.isPressed)
            TryActivate();
    }

    public bool ApplyMonsterDamageModifiers(
        ref int damage,
        ref bool ignoreElement)
    {
        if (!isFeverActive)
            return false;

        damage =
            Mathf.Max(damage, feverDamage);

        ignoreElement = true;
        return true;
    }

    private IEnumerator FeverRoutine()
    {
        while (remainingTime > 0f)
        {
            remainingTime =
                Mathf.Max(
                    0f,
                    remainingTime - Time.deltaTime);

            float remaining01 =
                feverDuration <= 0f
                    ? 0f
                    : remainingTime / feverDuration;

            paletteManager?.SetSpecialAttackGaugeRemaining(
                remaining01);

            yield return null;
        }

        paletteManager?.SetSpecialAttackGaugeRemaining(0f);
        EndFever();
    }

    private void EndFever()
    {
        if (!isFeverActive)
            return;

        isFeverActive = false;
        remainingTime = 0f;
        feverCoroutine = null;

        fillColor?.FeverOff();

        if (spawnedEffect != null)
        {
            Destroy(spawnedEffect);
            spawnedEffect = null;
        }

        paletteManager?.CompleteSpecialAttack();
        SoundManager.Instance.PlayBGM(BGMType.Normal);
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

    private void ResolvePaletteManager()
    {
        if (paletteManager != null)
            return;

        paletteManager =
            StagePaletteManager.FindForScene(this);
    }

    private PaletteSpecialAttack FindOtherInScene(
        Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (PaletteSpecialAttack attack in
                     root.GetComponentsInChildren<PaletteSpecialAttack>(true))
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
            StopCoroutine(feverCoroutine);

        EndFever();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
