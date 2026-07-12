using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [Header("Start 씬 SoundOn/OFF")]
    [SerializeField] private Button soundButton;
    [SerializeField] private Sprite soundOn;
    [SerializeField] private Sprite soundOff;

    public override void Awake()
    {
        base.Awake();
    }
    private void Start()
    {
        soundButton.onClick.AddListener(PressSoundButton);
        //UpdateToggleImage(soundButton, SoundManager.Instance.IsSoundOn, soundOn, soundOff);
    }

    private void PressSoundButton()
    {
        SoundManager.Instance.ToggleSound();
        UpdateToggleImage(soundButton, SoundManager.Instance.IsSoundOn, soundOn, soundOff);
    }
    private void UpdateToggleImage(Button button, bool isOn, Sprite onSprite, Sprite offSprite)
    {
        button.image.sprite = isOn ? onSprite : offSprite;
    }


    public void GameStart()
    {
        Debug.Log("게임 시작");
    }

    public void GameExit()
    {
        Debug.Log("게임 종료");
    }
}
