using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingESC : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("GameStart");
        }
    }
}
