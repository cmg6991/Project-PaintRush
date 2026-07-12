using UnityEngine;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    //[SerializeField] private GameObject panel;

    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider effectSlider;

    [SerializeField] private Button exitButton;
    [SerializeField] private Button maxButton;
    [SerializeField] private Button exit2Button;

    [SerializeField] private RectTransform settingPanelTr;

    private Vector3 originalScale;
    private bool isMaximized = false;

    private void Start()
    {
        originalScale = settingPanelTr.localScale;

        exitButton.onClick.AddListener(Close);
        exit2Button.onClick.AddListener(Close);
        maxButton.onClick.AddListener(ToggleMaximize);

        //bgmSlider.onValueChanged.AddListener(ChangeBGMVolume);
        //effectSlider.onValueChanged.AddListener(ChangeEffectVolume);

        //bgmSlider.value = SoundManager.Instance.BGMVolume;
        //effectSlider.value = SoundManager.Instance.SFXVolume;
    }

    //private void ChangeBGMVolume(float value)
    //{
    //    SoundManager.Instance.SetBGMVolume(value);
    //}

    //private void ChangeEffectVolume(float value)
    //{
    //    SoundManager.Instance.SetSFXVolume(value);
    //}

    private void ToggleMaximize()
    {
        isMaximized = !isMaximized;

        if (isMaximized)
        {
            RectTransform parent = settingPanelTr.parent as RectTransform;

            float scaleX = parent.rect.width / settingPanelTr.rect.width;
            float scaleY = parent.rect.height / settingPanelTr.rect.height;

            // 비율 유지하면서 화면 안에 들어가는 최대 크기
            float scale = Mathf.Min(scaleX, scaleY);

            settingPanelTr.localScale = originalScale * scale;
        }
        else
        {
            settingPanelTr.localScale = originalScale;
        }
    }

    public void Close()
    {
        UIManager.Instance.CloseSetting();
    }
}
