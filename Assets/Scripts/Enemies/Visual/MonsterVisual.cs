using UnityEngine;

public enum MonsterVisualState
{
    Idle,
    Move,
    VerticalMove,
    Attack
}

public class MonsterVisual : MonoBehaviour
{
    [Header("렌더러")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("스프라이트")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite moveSprite1;
    [SerializeField] private Sprite moveSprite2;
    [SerializeField] private Sprite attackSprite;
    [SerializeField] private Sprite hitSprite;
    [SerializeField] private Sprite deadSprite;

    [Header("애니메이션")]
    [SerializeField, Min(0.02f)]
    private float moveFrameInterval = 0.2f;

    [Header("기본 방향")]
    [Tooltip("원본 스프라이트가 오른쪽을 바라보면 체크")]
    [SerializeField] private bool facesRightByDefault = true;

    private MonsterVisualState currentState = MonsterVisualState.Idle;

    private int verticalDirection = 1;
    private float moveFrameTimer;
    private bool useFirstMoveFrame = true;

    private float hitEndTime;
    private bool isDead;

    private Color elementTint = Color.white;

    public void SetVerticalDirection(int direction)
    {
        if (direction == 0)
        {
            return;
        }

        verticalDirection = direction;
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            Debug.LogError(
                $"{gameObject.name}: SpriteRenderer를 찾지 못했습니다."
            );
        }
    }

    private void Start()
    {
        ApplyIdleSprite();
    }

    private void Update()
    {
        if (spriteRenderer == null || isDead)
        {
            return;
        }

        // 피격 시간이 남아 있으면 피격 스프라이트 우선
        if (Time.time < hitEndTime)
        {
            spriteRenderer.color = Color.white;

            SetSprite(
                hitSprite != null
                    ? hitSprite
                    : normalSprite
            );

            return;
        }

        switch (currentState)
        {
            case MonsterVisualState.Idle:
                ApplyIdleSprite();
                break;

            case MonsterVisualState.Move:
                UpdateMoveAnimation();
                break;

            case MonsterVisualState.VerticalMove:
                ApplyVerticalMoveSprite();
                break;

            case MonsterVisualState.Attack:
                ApplyAttackSprite();
                break;
        }
    }
    private void ApplyVerticalMoveSprite()
    {
        if (verticalDirection > 0)
        {
            // 위로 올라가는 이미지
            SetSprite(
                moveSprite2 != null
                    ? moveSprite2
                    : normalSprite
            );
        }
        else
        {
            // 아래로 내려오는 이미지
            SetSprite(
                moveSprite1 != null
                    ? moveSprite1
                    : normalSprite
            );
        }
    }

    public void SetState(MonsterVisualState newState)
    {
        if (isDead)
        {
            return;
        }

        if (currentState == newState)
        {
            return;
        }

        currentState = newState;

        moveFrameTimer = 0f;
        useFirstMoveFrame = true;
    }

    public void PlayHit(float duration)
    {
        if (isDead)
        {
            return;
        }

        hitEndTime = Mathf.Max(
            hitEndTime,
            Time.time + duration
        );

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = Color.white;

        SetSprite(
            hitSprite != null
                ? hitSprite
                : normalSprite
        );
    }

    public void PlayDead()
    {
        isDead = true;

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = Color.white;

        if (deadSprite != null)
        {
            spriteRenderer.sprite = deadSprite;
        }
        else if (hitSprite != null)
        {
            spriteRenderer.sprite = hitSprite;
        }
        else if (normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }
    }

    public void SetDirection(int direction)
    {
        if (spriteRenderer == null || direction == 0)
        {
            return;
        }

        spriteRenderer.flipX = facesRightByDefault
            ? direction < 0
            : direction > 0;
    }

    public void SetElementTint(Color color)
    {
        elementTint = color;

        if (spriteRenderer == null ||
            isDead ||
            Time.time < hitEndTime)
        {
            return;
        }

        spriteRenderer.color = elementTint;
    }

    private void ApplyIdleSprite()
    {
        SetSprite(normalSprite);
    }

    private void ApplyAttackSprite()
    {
        if (attackSprite != null)
        {
            SetSprite(attackSprite);
        }
        else if (moveSprite2 != null)
        {
            SetSprite(moveSprite2);
        }
        else
        {
            SetSprite(normalSprite);
        }
    }

    private void UpdateMoveAnimation()
    {
        Sprite firstFrame = moveSprite1 != null
            ? moveSprite1
            : normalSprite;

        Sprite secondFrame = moveSprite2 != null
            ? moveSprite2
            : firstFrame;

        moveFrameTimer -= Time.deltaTime;

        if (moveFrameTimer <= 0f)
        {
            useFirstMoveFrame = !useFirstMoveFrame;
            moveFrameTimer = moveFrameInterval;
        }

        SetSprite(
            useFirstMoveFrame
                ? firstFrame
                : secondFrame
        );
    }

    private void SetSprite(Sprite sprite)
    {
        if (spriteRenderer == null || sprite == null)
        {
            return;
        }

        spriteRenderer.sprite = sprite;
    }
}
