using UnityEngine;

/// <summary>
/// 功能 3（中）：子弹
/// 沿发射方向匀速飞行；打到靶子扣血，撞到墙就消失；超时自动销毁。
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("子弹参数")]
    public int damage = 1;
    public float lifeTime = 3f;

    private Vector2 direction = Vector2.right;
    private float speed = 12f;
    private float dieAt;

    /// <summary>由发射方调用，设置方向与速度。</summary>
    public void Launch(Vector2 dir, float spd)
    {
        direction = dir.normalized;
        speed = spd;
        transform.right = direction;   // 让长条朝向飞行方向（方块无所谓）
    }

    void Start()
    {
        dieAt = Time.time + lifeTime;
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        if (Time.time >= dieAt) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TargetHealth target = other.GetComponent<TargetHealth>();
        if (target != null) target.TakeDamage(damage);

        // 打到东西就消失（靶子或墙都算）
        Destroy(gameObject);
    }
}
