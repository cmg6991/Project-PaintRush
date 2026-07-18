using UnityEngine;
using UnityEngine.EventSystems;

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
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            ShootPaint();
            SoundManager.Instance.PlaySFX(SFXType.Shoot);
        }
    }

    public void ShootPaint()
    {
        if (!fillColor.HasColor)
            return;

        // 마우스 월드 좌표 변환
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 mouse = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouse.z = 0;

        RaycastHit2D[] hits = Physics2D.RaycastAll(mouse, Vector2.zero);

        foreach (RaycastHit2D hit in hits)
        {
            //DoorOpen door = hit.collider.GetComponent<DoorOpen>();

            //if (door != null)
            //{
            //    IPaintable paintable = door.GetComponent<IPaintable>();

            //    if (paintable != null)
            //    {
            //        paintable.Paint(fillColor.ShootColor, mouse);

            //        fillColor.Consume(0.1f);

            //        if (smoke != null)
            //            smoke.PlayParticle(fillColor.ShootColor);

            //        return; // 문만 칠하고 끝
            //    }
            //}
            DoorOpen door = hit.collider.GetComponent<DoorOpen>();

            if (door != null)
            {
                // 문 색 저장
                door.AddPaintColor(fillColor.ShootColor);


                // 문 전용 물감 (천천히 유지)
                PaintManager.instance.SpawnDefaultSplash(
                    mouse,
                    fillColor.ShootColor,
                    20f,
                    0.5f
                );


                fillColor.Consume(0.1f);


                if (smoke != null)
                    smoke.PlayParticle(fillColor.ShootColor);


                return; // 문이면 여기서 종료
            }

        }
        bool didPaint = false;

        foreach (var hit in hits)
        {
            IPaintable paintable = hit.collider.GetComponent<IPaintable>();

            if (paintable == null)
                continue;

            //// 문이 겹쳐 있으면 일반 벽은 무시
            //if (hitDoor && hit.collider.GetComponent<DoorOpen>() == null)
            //    continue;

            paintable.Paint(fillColor.ShootColor, mouse);
            didPaint = true;
        }

        if (didPaint)
            fillColor.Consume(0.3f);

        if (smoke != null)
        {
            smoke.PlayParticle(fillColor.ShootColor);
        }
    }
}