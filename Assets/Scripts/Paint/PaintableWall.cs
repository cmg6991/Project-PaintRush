using UnityEngine;

public class PaintableWall : MonoBehaviour,IPaintable
{
    public void Paint(Color color, Vector2 hitPoint)
    {
        PaintManager.instance.SpawnDefaultSplash(hitPoint, color,10f,0.1f, PaletteSpecialAttack.Instance.IsFeverActive);

        DoorOpen door = GetComponent<DoorOpen>();

        if (door != null)
        {
            Debug.Log("DoorOpen 찾음");
            door.AddPaintColor(color);
        }
    }
}
