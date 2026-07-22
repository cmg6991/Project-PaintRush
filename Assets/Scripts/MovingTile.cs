using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingTile : MonoBehaviour
{
    [SerializeField] private float moveDistance = 4f;
    [SerializeField] private float moveSpeed = 2f;

    private Rigidbody2D rb;
    private Vector2 startPos;
    private Vector2 lastPos;

    // 플레이어가 사용할 플랫폼 속도
    public Vector2 Velocity { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = rb.position;
        lastPos = startPos;
    }

    private void FixedUpdate()
    {
        float x = Mathf.Sin(Time.time * moveSpeed) * moveDistance;
        Vector2 target = startPos + Vector2.right * x;

        Velocity = (target - lastPos) / Time.fixedDeltaTime;

        rb.MovePosition(target);

        lastPos = target;
    }
}
