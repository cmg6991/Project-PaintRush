using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class PaintableBrush : MonoBehaviour, IPaintable
{
    private float minScale = 0.2f;
    private float maxScale = 0.5f;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    public void Paint(Color color, Vector2 hitPoint)
    {
        int dotCount = Random.Range(2, 4);
        //bool brushSpawned = false;

        for (int i = 0; i < dotCount; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * 0.08f;
            Vector2 spawnPos = hitPoint + randomOffset;

            GameObject brushObj = PaintManager.instance.SpawnBrushSplash(spawnPos, color);

            if (brushObj != null)
            {
                //brushSpawned = true;

                float currentScale = brushObj.transform.localScale.x;
                float finalScale = currentScale * Random.Range(minScale, maxScale);
                brushObj.transform.localScale = Vector3.one * finalScale;

                SpriteRenderer brushSR = brushObj.GetComponent<SpriteRenderer>();
                if (brushSR != null)
                {
                    Color dotColor = color;
                    dotColor.a = Random.Range(0.6f, 0.8f);
                    brushSR.color = dotColor;

                    if (spriteRenderer != null)
                    {
                        brushSR.sortingOrder = spriteRenderer.sortingOrder + 1 + i;
                    }
                }

                brushObj.transform.SetParent(transform);

                brushObj.transform.localPosition = new Vector3(brushObj.transform.localPosition.x, brushObj.transform.localPosition.y, -0.01f);
            }
        }
        PaintManager.instance.SpawnDefaultSplash(hitPoint, color);
    }
}
