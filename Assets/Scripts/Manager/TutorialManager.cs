using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("--- 튜토리얼 입력 허용 플래그 ---")]
    public bool canMove = false;
    public bool canJump = false;
    public bool canClimb = false;
    public bool canShoot = false;
    public bool canShowGun = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}
