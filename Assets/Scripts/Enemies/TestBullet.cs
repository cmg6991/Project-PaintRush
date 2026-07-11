using UnityEngine;

public enum AttackType
{
    Normal,
    Palette
}

public class TestBullet : MonoBehaviour
{
    [Header("공격 타입")]
    public AttackType attackType = AttackType.Normal;

    [Header("데미지")]
    public int normalDamage = 1;
    public int paletteDamage = 3;

    [Header("속성")]
    public ElementType attackElement = ElementType.Red;

    [Header("이동")]
    public float speed = 8f;
    public float lifeTime = 3f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Translate(Vector2.left * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어 자신은 무시
        if (other.CompareTag("Player"))
        {
            return;
        }

        // 적 공격 범위 Trigger는 무시
        if (other.gameObject.layer == LayerMask.NameToLayer("EnemyAttack"))
        {
            return;
        }

        IDamageable target = other.GetComponent<IDamageable>();

        if (target == null)
        {
            target = other.GetComponentInParent<IDamageable>();
        }

        if (target == null)
        {
            return;
        }

        bool ignoreElement = attackType == AttackType.Palette;

        int finalDamage = attackType == AttackType.Palette
            ? paletteDamage
            : normalDamage;

        target.TakeDamage(
            finalDamage,
            attackElement,
            gameObject,
            ignoreElement
        );

        Destroy(gameObject);
    }

    public void SetElement(ElementType element)
    {
        attackElement = element;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr == null) return;

        switch (attackElement)
        {
            case ElementType.Red:
                sr.color = Color.red;
                break;

            case ElementType.Blue:
                sr.color = Color.blue;
                break;

            case ElementType.Yellow:
                sr.color = Color.yellow;
                break;

            default:
                sr.color = Color.white;
                break;
        }
    }

    public void SetAttackType(AttackType newAttackType)
    {
        attackType = newAttackType;
    }
}