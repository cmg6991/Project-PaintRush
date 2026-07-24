using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 현재 총 색으로 클릭 지점의 문 또는 IPaintable 대상을 칠합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class Shoot : MonoBehaviour
{
    [SerializeField] private ShootParticle smoke;

    [Header("물감 소비량")]
    [SerializeField, Min(0f)] private float doorPaintCost = 0.1f;
    [SerializeField, Min(0f)] private float normalPaintCost = 0.2f;

    private FillColor fillColor;

    private void Awake()
    {
        fillColor = GetComponent<FillColor>();
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;
        // 튜토리얼 진행 중이면서 컷씬 재생 중이거나 사격이 해금되지 않았다면 사격 차단
        if(TutorialManager.Instance != null && (TutorialManager.Instance.isCutscenePlaying || !TutorialManager.Instance.canShoot))
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            ShootPaint();
		}
    }

    public void ShootPaint()
    {
        if (fillColor == null ||
            !fillColor.HasColor ||
            Camera.main == null)
        {
            return;
        }

        Color shotColor =
            fillColor.ShootColor;

        SoundManager.Instance?.PlaySFX(
            SFXType.Shoot);

        Vector3 mouseScreenPosition =
            Input.mousePosition;

        mouseScreenPosition.z =
            Mathf.Abs(
                Camera.main.transform.position.z);

        Vector3 mouseWorldPosition =
            Camera.main.ScreenToWorldPoint(
                mouseScreenPosition);

        mouseWorldPosition.z = 0f;

        RaycastHit2D[] hits =
            Physics2D.RaycastAll(
                mouseWorldPosition,
                Vector2.zero);

        if (TryPaintDoor(
                hits,
                mouseWorldPosition,
                shotColor))
        {
            return;
        }

        bool didPaint =
            PaintNormalTargets(
                hits,
                mouseWorldPosition,
                shotColor);

        if (didPaint)
            fillColor.Consume(normalPaintCost);

        smoke?.PlayParticle(shotColor);
    }

    private bool TryPaintDoor(
        RaycastHit2D[] hits,
        Vector2 hitPoint,
        Color shotColor)
    {
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            DoorOpen door =
                hit.collider.GetComponentInParent<DoorOpen>();

            if (door == null)
                continue;

            door.AddPaintColor(shotColor);

            PaintManager.instance?.SpawnDefaultSplash(
                hitPoint,
                shotColor,
                20f,
                0.5f,
                PaletteSpecialAttack.Instance.IsFeverActive);

            fillColor.Consume(doorPaintCost);
            smoke?.PlayParticle(shotColor);
            return true;
        }

        return false;
    }

    private static bool PaintNormalTargets(
        RaycastHit2D[] hits,
        Vector2 hitPoint,
        Color shotColor)
    {
        HashSet<IPaintable> paintedTargets = new();
        bool didPaint = false;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            IPaintable paintable =
                hit.collider
                    .GetComponentInParent<IPaintable>();

            if (paintable == null ||
                !paintedTargets.Add(paintable))
            {
                continue;
            }

            paintable.Paint(
                shotColor,
                hitPoint);

            didPaint = true;
        }

        return didPaint;
    }
}
