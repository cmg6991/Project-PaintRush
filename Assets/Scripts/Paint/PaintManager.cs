using UnityEditor;
using UnityEngine;

public class PaintManager : MonoBehaviour
{
    public static PaintManager instance;

    [Header("기본 스플래시 매니저 (벽/문)")]
    [SerializeField] private RandomSplash defaultSplashManager;

    [Header("브러시 스플래시 매니저 (사다리/몬스터/타일)")]
    [SerializeField] private RandomSplash brushSplashManager;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SpawnDefaultSplash(Vector2 position, Color color)
    {
        if (defaultSplashManager != null)
        {
            defaultSplashManager.Spawn(position, color,false);
        }
    }

    // 사다리, 몬스터, 타일이 호출할 함수
    public GameObject SpawnBrushSplash(Vector2 position, Color color)
    {
        if (brushSplashManager != null)
        {
            return brushSplashManager.Spawn(position, color,true);
        }
        return null;
    }
}
