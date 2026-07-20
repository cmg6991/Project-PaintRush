using UnityEngine;
using UnityEngine.SceneManagement;

public class DeadZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        UIManager.Instance.ShowRestartUI();
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
