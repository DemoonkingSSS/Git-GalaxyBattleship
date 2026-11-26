using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;
    private Transform visualChild; 

    [Header("移动参数 / Movement Settings")]
    public float moveSpeed = 5f;
    private Vector2 moveInput;
    private bool isDead = false;

    [Header("翻滚设置 / Roll Settings")]
    public float rollDistance = 3f;
    public float rollDuration = 0.2f;
    public float rollCooldown = 1f;
    private float lastRollTime = -100f;
    private bool isRolling = false;

    [Header("受击设置 / Hit Settings")]
    public float hitCooldown = 0.5f; 
    private float lastHitTime = -100f; 

    [Header("武器设置 / Weapon Settings")]
    public GameObject Bulletprefab;     // 子弹预制体 / Bullet Prefab
    public Transform gunPivot;          // 武器枢轴 / Weapon Pivot
    public Vector2 bulletOffset;        // 子弹生成位置偏移 / Bullet spawn position offset
    public float rotationOffset = 0f;   // 手动指定的旋转偏移角度 / Manual rotation offset
    public float shootCooldown = 0.2f;  // 射击间隔 / Shooting cooldown
    private float lastShootTime = 0f;

    // --- 1. 初始化与生成广播 / Initialization and Spawn Broadcast ---
    void OnEnable() { StartCoroutine(DelayedSpawnBroadcast(0.5f)); }

    IEnumerator DelayedSpawnBroadcast(float delay)
    {
        yield return new WaitForSeconds(delay);
        // 广播玩家生成事件 / Broadcast player spawn event
        GameEvents.TriggerPlayerSpawn(this.gameObject);
        Debug.Log("玩家已生成，发送广播！ | Player spawned, sending broadcast!");
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // 获取子物体中的动画组件 / Get Animator from children
        anim = GetComponentInChildren<Animator>();
        if (anim != null) visualChild = anim.transform;
    }

    // --- 2. 输入与逻辑更新 / Input and Logic Update ---
    void Update()
    {
        if (isDead) return;

        // --- 调试按键检测 / Debug Keys Detection ---
        if (Input.GetKeyDown(KeyCode.J)) TakeDamage(10f); // 按J受伤 / Press J to take damage
        if (Input.GetKeyDown(KeyCode.K)) PlayerDie();     // 按K死亡 / Press K to die
        
        // 武器旋转处理 / Handle Weapon Rotation
        HandleWeaponRotation();

        if (isRolling) return;

        // 获取基础轴向输入 / Get raw axis input
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        // 动画状态切换 / Animation state switching
        bool isMoving = moveInput.sqrMagnitude > 0;
        if (anim != null)
        {
            anim.SetBool("IsRunning", isMoving);
            anim.SetBool("IsIdle", !isMoving);
        }

        // 处理角色水平翻转 / Handle character horizontal flip
        if (moveInput.x != 0) Flip(moveInput.x);

        // 翻滚指令检测 / Roll command detection
        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= lastRollTime + rollCooldown)
        {
            StartCoroutine(RollRoutine(moveInput));
            lastRollTime = Time.time;
        }

        // 射击指令检测 / Shooting input detection
        if (Input.GetMouseButton(0) && Time.time >= lastShootTime + shootCooldown)
        {
            Shoot();
            lastShootTime = Time.time;
        }
    }

    // --- 3. 武器逻辑 / Weapon Logic ---
    private void HandleWeaponRotation()
    {
        if (gunPivot == null) return;

        // 获取鼠标在世界空间的位置 / Get mouse world position
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // 计算方向向量 / Calculate direction vector
        Vector2 direction = (Vector2)(mousePos - gunPivot.position);
        
        // 计算基础角度并加上用户定义的偏移量 / Calculate angle and apply manual offset
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 仅修改旋转角度 / Apply rotation only
        gunPivot.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
    }

    private void Shoot()
    {
        if (Bulletprefab == null) 
        {
            Debug.LogWarning("未指定子弹预制体！ | Bullet prefab not assigned!");
            return;
        }
        // 计算带有偏移的生成位置 / Calculate spawn position with offset
        // 注意：gunPivot 的 transform.right 已经被 rotationOffset 转过 45°，直接用
        // TransformPoint 会把子弹甩到瞄准线旁边（表现为"打不中"）。这里改用真正的
        // 瞄准方向（gunPivot 角度减去偏移量）来算枪口位置。
        float aimAngle = gunPivot.eulerAngles.z - rotationOffset;
        float aimRad = aimAngle * Mathf.Deg2Rad;
        Vector3 aimDir = new Vector3(Mathf.Cos(aimRad), Mathf.Sin(aimRad), 0f);
        Vector3 spawnPos = gunPivot.position + aimDir * bulletOffset.x;

        // 子弹射击减去偏转角以修正飞行方向 / Subtract offset to correct bullet flight direction
        Quaternion bulletRotation = Quaternion.Euler(0, 0, aimAngle);
        GameObject Bullet = Instantiate(Bulletprefab, spawnPos, bulletRotation);
        Debug.Log("触发射击！ | Shoot triggered!");
    }

    // --- 4. 受伤逻辑 / Damage Logic ---
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead || this == null) return;

        // 检测是否碰到怪物层级的物体 / Check if hit a monster layer object
        if (collision.gameObject.layer == LayerMask.NameToLayer("Monster"))
        {
            TakeDamage(20f);
        }
    }

    public void TakeDamage(float amount)
    {
        // 死亡、翻滚中或受击冷却中则免疫伤害 / Immune if dead, rolling, or in cooldown
        if (isDead || isRolling || Time.time < lastHitTime + hitCooldown) return;

        lastHitTime = Time.time;
        Debug.Log($"玩家受到 {amount} 伤害！ | Player took {amount} damage!");

        if (anim != null) anim.SetTrigger("BeHit");

        // 更新生命值控制器数据 / Update HealthController data
        HealthController hc = GetComponent<HealthController>();
        if (hc != null) hc.TakeDamage(amount);
    }

    // --- 5. 死亡处理 / Death Handling ---
    public void PlayerDie()
    {
        if (isDead) return;
        isDead = true; 

        // 禁用碰撞体防止重复受击 / Disable collider to prevent further hits
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 死亡时隐藏武器 / Hide weapon on death
        if (gunPivot != null) gunPivot.gameObject.SetActive(false);

        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        if (anim != null)
        {
            anim.SetBool("IsRunning", false);
            anim.SetBool("IsIdle", false);
            anim.SetTrigger("IsDead"); 
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static; // 停止物理模拟 / Stop physics simulation
        }

        yield return new WaitForSeconds(0.5f);
        // 触发玩家死亡全局广播 / Trigger global player death broadcast
        GameEvents.TriggerPlayerDeath(gameObject);
    }

    // --- 6. 辅助功能 / Helper Functions ---
    void Flip(float horizontalInput)
    {
        if (visualChild != null)
        {
            // 通过修改缩放实现视觉翻转 / Handle visual flip via scale
            Vector3 currentScale = visualChild.localScale;
            if (horizontalInput > 0) currentScale.x = Mathf.Abs(currentScale.x); 
            else if (horizontalInput < 0) currentScale.x = -Mathf.Abs(currentScale.x); 
            visualChild.localScale = currentScale;
        }
    }

    void FixedUpdate()
    {
        // 物理更新移动速度 / Physical update for movement velocity
        if (!isDead && !isRolling && rb != null)
        {
            rb.velocity = moveInput.normalized * moveSpeed;
        }
    }

    IEnumerator RollRoutine(Vector2 input)
    {
        isRolling = true;
        // 确定翻滚方向 / Determine roll direction
        Vector2 rollDirection = (input.sqrMagnitude > 0) ? input.normalized : Vector2.right;

        if (anim != null) anim.SetTrigger("IsRolling");

        float speed = rollDistance / rollDuration;
        float timer = 0f;
        while (timer < rollDuration)
        {
            if (isDead) yield break; 
            if (rb != null) rb.velocity = rollDirection * speed;
            timer += Time.deltaTime;
            yield return null;
        }
        if (!isDead && rb != null) rb.velocity = Vector2.zero;
        isRolling = false;
    }

    // 可视化子弹生成点 / Visualize bullet spawn point
    private void OnDrawGizmosSelected()
    {
        if (gunPivot != null)
        {
            Gizmos.color = Color.red;
            Vector3 spawnPos = gunPivot.TransformPoint(bulletOffset);
            Gizmos.DrawSphere(spawnPos, 0.1f);
        }
    }
}