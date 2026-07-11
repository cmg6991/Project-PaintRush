using UnityEngine;
using UnityEngine.InputSystem;

public class TestShooter : MonoBehaviour
{
    [Header("총알")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("현재 속성")]
    public ElementType currentBulletElement = ElementType.Red;

    [Header("공격 타입")]
    public AttackType currentAttackType = AttackType.Normal;

    private void Update()
    {
        // 색상 변경
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            currentBulletElement = ElementType.Red;
            Debug.Log("총알 색상 : RED");
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            currentBulletElement = ElementType.Blue;
            Debug.Log("총알 색상 : BLUE");
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            currentBulletElement = ElementType.Yellow;
            Debug.Log("총알 색상 : YELLOW");
        }

        // 공격 타입 변경
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            if (currentAttackType == AttackType.Normal)
            {
                currentAttackType = AttackType.Palette;
                Debug.Log("팔레트 공격 ON");
            }
            else
            {
                currentAttackType = AttackType.Normal;
                Debug.Log("일반 공격 ON");
            }
        }

        // 발사
        if (Keyboard.current.zKey.wasPressedThisFrame)
        {
            Debug.Log("Z 입력됨 - 총알 발사 시도");
            Shoot();
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("Bullet Prefab이 연결 안 됨");
            return;
        }

        if (firePoint == null)
        {
            Debug.LogError("Fire Point가 연결 안 됨");
            return;
        }

        GameObject bulletObj = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.identity
        );

        TestBullet bullet = bulletObj.GetComponent<TestBullet>();

        if (bullet != null)
        {
            bullet.SetElement(currentBulletElement);
            bullet.SetAttackType(currentAttackType);
        }

        Debug.Log("총알 생성됨");
    }
}