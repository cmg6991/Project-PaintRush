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

        //bool didPaint = false;

        //foreach (var hit in hits)
        //{
        //    IPaintable paintable = hit.collider.GetComponent<IPaintable>();

        //    Debug.Log("IPaintable 찾음: " + hit.collider.name);


        //    if (paintable != null)
        //    {
        //        paintable.Paint(fillColor.CurrentColor, hit.point);

        //        didPaint = true;
        //    }
        //}

        //if (didPaint)
        //{
        //    fillColor.Consume(0.3f);
        //}

        //smoke.PlayParticle(fillColor.CurrentColor);
        bool hitDoor = false;

        foreach (var hit in hits)
        {
            if (hit.collider.GetComponent<DoorOpen>() != null)
            {
                hitDoor = true;
                break;
            }
        }

        bool didPaint = false;

        foreach (var hit in hits)
        {
            IPaintable paintable = hit.collider.GetComponent<IPaintable>();

            if (paintable == null)
                continue;

            // 문이 겹쳐 있으면 일반 벽은 무시
            if (hitDoor && hit.collider.GetComponent<DoorOpen>() == null)
                continue;

            paintable.Paint(fillColor.CurrentColor, mouse);
            didPaint = true;
        }

        if (didPaint)
            fillColor.Consume(0.1f);

        smoke.PlayParticle(fillColor.CurrentColor);
    }
}