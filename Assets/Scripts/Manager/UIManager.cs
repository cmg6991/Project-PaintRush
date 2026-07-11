using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    public override void Awake()
    {
        base.Awake();
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
