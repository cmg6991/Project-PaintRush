using UnityEngine;

public enum SFXType
{
    Click,
    Shoot,
    PlayerWalk,
    PlayerJump,
    Paint
}
public class SoundManager : Singleton<SoundManager>
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM")]
    [SerializeField] private AudioClip bgmClip;

    [Header("SFX")]
    [SerializeField] private AudioClip[] sfxClips;
    public float BGMVolume { get; private set; } = 1f;
    public float SFXVolume { get; private set; } = 1f;

    private float prevBGMVolume = 1f;
    private float prevSFXVolume = 1f;

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

        PlayBGM();
    }

    public void PlayBGM()
    {
        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.Play();
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
