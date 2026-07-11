using UnityEngine;

public class PaintManager : MonoBehaviour
{
    public static PaintManager instance;
    public GameObject paintPrefab;

    public Sprite[] paintSprites;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void SpawnPoint(Vector2 position, Color color, Transform parent, float scale)
    {

    }
}
