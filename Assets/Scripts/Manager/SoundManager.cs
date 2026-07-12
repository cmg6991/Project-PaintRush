using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    public bool IsSoundOn { get; private set; } = true;
    public override void Awake()
    {
        base.Awake();

        ApplySound();
    }
    public void ToggleSound()
    {
        IsSoundOn = !IsSoundOn;
        ApplySound();
    }

    private void ApplySound()
    {
        AudioListener.volume = IsSoundOn ? 1f : 0f;
    }
}
