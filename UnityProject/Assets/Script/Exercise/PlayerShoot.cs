using UnityEngine;

/// <summary>
/// 功能 3（上）：射击
/// 按住鼠标左键，每 0.3 秒发射一发子弹，朝鼠标方向飞。
/// </summary>
public class PlayerShoot : MonoBehaviour
{
    [Header("射击参数")]
    public float shootInterval = 0.3f;   // 射速：0.3 秒一发
    public GameObject bulletPrefab;      // 子弹预制体
    public float bulletSpeed = 12f;

    [Header("朝向")]
    public bool aimAtMouse = true;       // 朝鼠标；关掉则朝右

    private float nextShootTime;

    void Update()
    {
        if (bulletPrefab == null) return;
        if (!Input.GetMouseButton(0)) return;      // 左键
        if (Time.time < nextShootTime) return;     // 冷却中

        nextShootTime = Time.time + shootInterval;

        Vector2 dir = aimAtMouse
            ? (Vector2)(MouseWorld() - transform.position)
            : (Vector2)transform.right;

        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        // 从玩家前方一点生成，避免子弹一出生就撞到自己
        GameObject go = Instantiate(bulletPrefab, transform.position + (Vector3)(dir * 0.6f), Quaternion.identity);
        Bullet bullet = go.GetComponent<Bullet>();
        if (bullet != null) bullet.Launch(dir, bulletSpeed);
    }

    Vector3 MouseWorld()
    {
        Camera cam = Camera.main;
        if (cam == null) return transform.position;
        Vector3 p = cam.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0f;
        return p;
    }
}
