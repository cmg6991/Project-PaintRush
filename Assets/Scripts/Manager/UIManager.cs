using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [Header("Setting")]
    [SerializeField] private Button settingButton;
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private Sprite settingOn;
    [SerializeField] private Sprite settingOff;

    bool isOpen = false;

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
    }

    public void GameExit()
    {
        Debug.Log("게임 종료");
    }
}
