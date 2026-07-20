using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvas;

    private void Awake()
    {
        canvas.alpha = 0f;
        canvas.interactable = false;
        canvas.blocksRaycasts = false;
    }
    private void Start()
    {
        UIManager.Instance.RegisterRestartUI(this);
    }

    public void RestartOn()
    {
        canvas.alpha = 1f;
        canvas.interactable = true;
        canvas.blocksRaycasts = true;

        Time.timeScale = 0f;
    }

    public void Restart() 
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void Main()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameStart");
    }
    public void Exit()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}
