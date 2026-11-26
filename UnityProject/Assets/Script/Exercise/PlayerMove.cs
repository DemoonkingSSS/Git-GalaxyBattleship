using UnityEngine;

/// <summary>
/// 功能 1：走路 + 碰撞
/// WASD 移动，速度 5。用 Rigidbody2D 做位移，BoxCollider2D 负责被墙挡住。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMove : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 input;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;        // 俯视角，不受重力
        rb.freezeRotation = true;    // 撞墙不翻滚
    }

    void Update()
    {
        // 读键盘：WASD 或方向键，返回 -1 ~ 1
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        // 斜向移动不该更快
        if (input.sqrMagnitude > 1f) input = input.normalized;
    }

    void FixedUpdate()
    {
        // 交给物理系统位移，这样才会与墙发生碰撞
        rb.velocity = input * moveSpeed;
    }
}
