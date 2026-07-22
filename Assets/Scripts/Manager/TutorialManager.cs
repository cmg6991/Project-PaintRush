using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("--- 튜토리얼 상태 플래그 ---")]
    public bool isCutscenePlaying = false;          // 컷씬, 카메라 안내 연출 진행 중 여부 

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

    // 튜토리얼 상태 초기화 함수
    public void ResetTutorialFlags()
    {
        isCutscenePlaying = false;
        canMove = false;
        canJump = false;
        canClimb = false;
        canShoot = false;
        canShowGun = false;
    }
}
