using UnityEditor;
using UnityEngine;

public class PaintManager : MonoBehaviour
{
    public static PaintManager instance;

    [Header("기본 스플래시 매니저 (벽/문)")]
    [SerializeField] private RandomSplash defaultSplashManager;

    [Header("브러시 스플래시 매니저 (사다리/몬스터/타일)")]
    [SerializeField] private RandomSplash brushSplashManager;

    [Header("피버 스플래시 매니저")]
    [SerializeField] private RandomSplash feverSplashManager;


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

    //public void SpawnDefaultSplash(Vector2 position, Color color)
    //{
    //    if (defaultSplashManager != null)
    //    {
    //        defaultSplashManager.Spawn(position, color,false);
    //    }
    //}
    public void SpawnDefaultSplash(Vector2 position,Color color, float fadeTime,float targetAlpha)
    {
        if (defaultSplashManager != null)
        {
            GameObject splash = defaultSplashManager.Spawn(position,color,false);

            PaintFade fade = splash.GetComponent<PaintFade>();

            if (fade != null)
            {
                fade.StartFade( fadeTime,targetAlpha);
            }
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

    public GameObject SpawnFeverSplash(Vector2 position, Color color)
    {
        if (feverSplashManager != null)
        {
            return feverSplashManager.Spawn(position, color, true);
        }
        return null;
    }
}
