using UnityEngine;

public class PaintableWall : MonoBehaviour,IPaintable
{
    public void Paint(Color color, Vector2 hitPoint)
    {
        PaintManager.instance.SpawnDefaultSplash(hitPoint, color);
    }
}
