using UnityEngine;
using System.Collections.Generic;

/*
 * [脚本功能概览 / Script Overview]
 * 1. 自动飞行：物体生成时沿 Transform.right 高速直线飞出。
 * 2. 距离销毁：超过最大飞行距离后自动销毁。
 * 3. 触发销毁 (Trigger)：当进入碰撞箱时，若该物体不在“忽略层级列表”中，则销毁子弹。
 * 4. 层级过滤：可以通过 LayerMask 指定哪些层级不会导致子弹销毁（如玩家自身、其他子弹等）。
 */

[RequireComponent(typeof(Rigidbody2D))]
public class BulletShoot : MonoBehaviour
{
    [Header("飞行设置 / Flight Settings")]
    public float speed = 20f;           // 飞行速度 / Projectile speed
    public float maxDistance = 50f;     // 最大飞行距离 / Maximum travel distance

    [Header("碰撞过滤 / Collision Filtering")]
    [Tooltip("子弹碰到这些层级的物体时【不会】被销毁 | Bullet will NOT be destroyed when hitting these layers")]
    public LayerMask ignoreLayers;      

    private Vector3 startPosition;      // 记录起始位置 / Starting position
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        // 使用 Trigger 时，建议仍保持 Continuous 以提高检测精度
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // 强制确保 Rigidbody 的 IsKinematic 不会影响触发
        rb.isKinematic = false; 
    }

    void Start()
    {
        startPosition = transform.position;
        rb.velocity = transform.right * speed;
    }

    void Update()
    {
        float traveledDistance = Vector3.Distance(startPosition, transform.position);

        if (traveledDistance >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    // --- 触发逻辑 / Trigger Logic ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 检查碰到的物体层级是否在 ignoreLayers 中
        // Check if the hit object's layer is in the ignoreLayers mask
        if (((1 << other.gameObject.layer) & ignoreLayers) != 0)
        {
            // 如果在忽略列表中，直接跳过逻辑
            return;
        }

        // 2. 如果不在忽略列表中，无论对方是否为 Trigger，均销毁子弹
        Debug.Log($"子弹触发销毁。击中: {other.gameObject.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        Destroy(gameObject);
    }
}