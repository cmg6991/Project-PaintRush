using UnityEngine;
using System.Collections;

public enum SFXType
{
    Click,
    Shoot,
    PlayerWalk,
    PlayerJump,
    Item,
    Hit,
    MonsterDead,
    PlayerDead,
    Paint
}

public enum BGMType
{
    Normal,
    Fever
}
public class SoundManager : Singleton<SoundManager>
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM")]
    [SerializeField] private AudioClip[] bgmClip;
    [SerializeField] private float fadeTime = 0.7f;


    [Header("SFX")]
    [SerializeField] private AudioClip[] sfxClips;
    public float BGMVolume { get; private set; } = 1f;
    public float SFXVolume { get; private set; } = 1f;

    private float prevBGMVolume = 1f;
    private float prevSFXVolume = 1f;

    private Coroutine bgmCoroutine;
    private BGMType currentBGM = (BGMType)(-1);

    public bool IsSoundOn => BGMVolume > 0f || SFXVolume > 0f;

    public override void Awake()
    {
        base.Awake();
    }
    private void Start()
    {
        BGMVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        bgmSource.volume = BGMVolume;
        sfxSource.volume = SFXVolume;

        PlayBGM(BGMType.Normal, false);
        //bgmSource.clip = bgmClip[0];
        //bgmSource.Play();
    }

    public void PlayBGM(BGMType type, bool fade =true)
    {
        //bgmSource.clip = bgmClip;
        //bgmSource.loop = true;
        //bgmSource.Play();
        if (bgmSource.clip == bgmClip[(int)type] && bgmSource.isPlaying)
            return;

        currentBGM = type;

        if (bgmCoroutine != null)
            StopCoroutine(bgmCoroutine);

        if (fade)
            bgmCoroutine = StartCoroutine(ChangeBGM(type));
        else
        {
            bgmSource.clip = bgmClip[(int)type];
            bgmSource.loop = true;
            bgmSource.volume = BGMVolume;
            bgmSource.Play();
        }
    }

    private IEnumerator ChangeBGM(BGMType type)
    {
        float startVolume = bgmSource.volume;

        // Fade Out
        while (bgmSource.volume > 0)
        {
            bgmSource.volume -= Time.deltaTime / fadeTime * startVolume;
            yield return null;
        }

        bgmSource.Stop();

        bgmSource.clip = bgmClip[(int)type];
        bgmSource.loop = true;
        bgmSource.Play();

        // Fade In
        while (bgmSource.volume < BGMVolume)
        {
            bgmSource.volume += Time.deltaTime / fadeTime * BGMVolume;
            yield return null;
        }

        bgmSource.volume = BGMVolume;
    }

    public void PlayFeverBGM()
    {
        PlayBGM(BGMType.Fever);
    }

    public void PlayNormalBGM()
    {
        PlayBGM(BGMType.Normal);
    }

    public void PlaySFX(SFXType type)
    {
        sfxSource.PlayOneShot(sfxClips[(int)type]);
    }

    public void ToggleSound()
    {
        if (IsSoundOn)
        {
            prevBGMVolume = BGMVolume;
            prevSFXVolume = SFXVolume;

            SetBGMVolume(0f);
            SetSFXVolume(0f);
        }
        else
        {
            SetBGMVolume(prevBGMVolume);
            SetSFXVolume(prevSFXVolume);
        }
    }

    public void SetBGMVolume(float value)
    {
        BGMVolume = value;
        bgmSource.volume = value;

        PlayerPrefs.SetFloat("BGMVolume", value);
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = value;
        sfxSource.volume = value;

        PlayerPrefs.SetFloat("SFXVolume", value);
    }
}
