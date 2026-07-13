using UnityEngine;
using UnityEngine.UI;

public class SoundButton : MonoBehaviour
{
    [SerializeField] private Sprite soundOn;
    [SerializeField] private Sprite soundOff;

    private Button button;


    private void Start()
    {
        button = GetComponent<Button>();

        button.onClick.AddListener(PressSoundButton);
    }


    private void PressSoundButton()
    {
        SoundManager.Instance.ToggleSound();
        UIManager.Instance.UpdateToggleImage(button, SoundManager.Instance.IsSoundOn, soundOn, soundOff);
    }
}
