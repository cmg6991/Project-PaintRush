using UnityEngine;

public class Shoot : MonoBehaviour
{
    private FillColor fillColor;
    [SerializeField] private ShootParticle smoke;

    private void Awake()
    {
        fillColor = GetComponent<FillColor>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ShootPaint();
        }
    }

    public void ShootPaint()
    {
        if (!fillColor.HasColor)
            return;

        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0;
        RaycastHit2D[] hits = Physics2D.RaycastAll(mouse, Vector2.zero);

        bool didPaint = false;

        foreach (var hit in hits)
        {
            IPaintable paintable = hit.collider.GetComponent<IPaintable>();

            if (paintable != null)
            {
                paintable.Paint(fillColor.CurrentColor, hit.point);

                didPaint = true;
            }
        }

        if (didPaint)
        {
            fillColor.Consume(0.3f);
        }

        smoke.PlayParticle(fillColor.CurrentColor);
    }
}