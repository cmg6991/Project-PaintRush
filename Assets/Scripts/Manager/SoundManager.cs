using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    public float BGMVolume { get; private set; } = 1f;
    public float SFXVolume { get; private set; } = 1f;

    private float prevBGMVolume = 1f;
    private float prevSFXVolume = 1f;

    public bool IsSoundOn => BGMVolume > 0f || SFXVolume > 0f;

    public override void Awake()
    {
        base.Awake();
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
        // BGM AudioSource에 적용
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = value;
        // 효과음 AudioSource에 적용
    }
}
