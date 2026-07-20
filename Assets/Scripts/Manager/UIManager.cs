using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : Singleton<UIManager>
{
    [Header("Setting")]
    [SerializeField] private Button settingButton;
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private Sprite settingOn;
    [SerializeField] private Sprite settingOff;

    bool isOpen = false;

    private FeverUI feverUI;
    private RestartUI restartUI;

    public override void Awake()
    {
        base.Awake();
    }
    private void Start()
    {
        settingButton.onClick.AddListener(PressSettingButton);
        UpdateToggleImage(settingButton, isOpen, settingOn, settingOff);
    }

    private void PressSettingButton()
    {
        isOpen = !isOpen;
        settingPanel.SetActive(isOpen);
        UpdateToggleImage(settingButton, isOpen, settingOn, settingOff);
        SoundManager.Instance.PlaySFX(SFXType.Click);
    }
    public void CloseSetting()
    {
        isOpen = false;
        settingPanel.SetActive(false);

        UpdateToggleImage(settingButton,isOpen,settingOn,settingOff);
    }
    public void UpdateToggleImage(Button button, bool isOn, Sprite onSprite, Sprite offSprite)
    {
        button.image.sprite = isOn ? onSprite : offSprite;
    }


    public void GameStart()
    {
        Debug.Log("게임 시작");
        SoundManager.Instance.PlaySFX(SFXType.Click);
    }

    public void Setting()
    {
        settingPanel.SetActive(true);
    }

    public void GameExit()
    {
        Debug.Log("게임 종료");
        SoundManager.Instance.PlaySFX(SFXType.Click);
    }
    public void RegisterFeverUI(FeverUI ui)
    {
        feverUI = ui;
    }

    public void ShowFeverUI()
    {
        feverUI?.FeverOn();
    }

    public void HideFeverUI()
    {
        feverUI?.FeverOff();
    }
    public void RegisterRestartUI(RestartUI ui)
    {
        restartUI = ui;
        Debug.Log("나오니 등록했니");
    }

    public void ShowRestartUI()
    {
        restartUI?.RestartOn();
    }
}
