using UnityEngine;

public class UISplash : PoolableObject
{
    [SerializeField] private Sprite[] splashSprites;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public override void OnSpawn()
    {
        SetRandomSprite();
    }

    private void SetRandomSprite()
    {
        if (splashSprites == null || splashSprites.Length == 0)
            return;

        int index = Random.Range(0, splashSprites.Length);
        sr.sprite = splashSprites[index];

        Debug.Log($"º±≈√µ» Splash: {splashSprites[index].name}");
    }

    public void SetColor(Color color)
    {
        sr.color = color;
    }
}
