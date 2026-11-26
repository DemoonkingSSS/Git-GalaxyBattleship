using UnityEngine;
using System.Collections;

public class MonsterTrack : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;
    private HealthController health; 

    [Header("视觉翻转设置 / Visual Flip Settings")]
    public SpriteRenderer monsterSprite;

    [Header("目标设置 / Target Settings")]
    public Transform player;
    public float moveSpeed = 5f;
    public float stoppingDistance = 0.5f; 
    private Vector2 targetOffset; 

    [Header("分散逻辑 / Separation Logic")]
    public float separationRadius = 1.0f;
    public float separationWeight = 1.5f; 
    public LayerMask monsterLayer;

    [Header("攻击设置 / Attack Settings")]
    public float attackRange = 4f;
    public float dashSpeed = 20f;
    public float dashDuration = 0.3f;
    public float attackCooldown = 2f;

    [Header("碰撞检测设置 / Combat Detection")]
    public string bulletLayerName = "Bullet"; 
    // 这里设为 20，确保 TakeDamage 调用时默认就是这个值
    public float bulletDamage = 20f;  

    private bool isAttacking = false;
    private bool isDead = false; 
    private float lastAttackTime = -100f;
    private float randomCooldownOffset;

    // --- 监听玩家生成事件 / Listen for player spawn events ---
    void OnEnable() { GameEvents.OnPlayerSpawn += SetTargetPlayer; }
    void OnDisable() { GameEvents.OnPlayerSpawn -= SetTargetPlayer; }

    void SetTargetPlayer(GameObject playerObject)
    {
        if (playerObject != null)
        {
            player = playerObject.transform;
            // 为每个怪物设置随机偏移，防止完全重叠 / Set random offset to prevent overlapping
            targetOffset = Random.insideUnitCircle * 2.0f; 
        }  
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        health = GetComponent<HealthController>();
        
        if (monsterSprite == null) monsterSprite = GetComponentInChildren<SpriteRenderer>();

        // 随机化冷却偏移，增加行为自然度 / Randomize cooldown offset for natural behavior
        randomCooldownOffset = Random.Range(0f, 1.5f);
        if (monsterLayer == 0) monsterLayer = 1 << gameObject.layer;
    }

    void Update()
    {
        if (isDead) return;

        // 调试用：Q键扣 20 血 / Debug: Press Q to deduct 20 HP
        if (Input.GetKeyDown(KeyCode.Q)) TakeDamage(20f);
        // 调试用：E键死亡 / Debug: Press E to trigger death
        if (Input.GetKeyDown(KeyCode.E)) HandleDeath();

        if (player == null || isAttacking) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // 攻击范围检测与冷却判定 / Attack range and cooldown check
        if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown + randomCooldownOffset)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    // --- 关键：受击判定 / Critical: Hit Detection ---
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        // 检测子弹层级碰撞 / Detect collision with bullet layer
        if (collision.gameObject.layer == LayerMask.NameToLayer(bulletLayerName))
        {
            // 每次碰撞固定扣 20 血 / Fixed 20 damage per hit
            TakeDamage(20f);

            // 销毁子弹防止穿透多段伤害 / Destroy bullet to prevent piercing damage
            Destroy(collision.gameObject);
        }
    }
    void FixedUpdate()
    {
        if (isDead || player == null || isAttacking) return;

        // 目标位置包含随机偏移 / Target position includes random offset
        Vector2 targetPos = (Vector2)player.position + targetOffset;
        float distToTarget = Vector2.Distance(transform.position, targetPos);
        Vector2 finalVelocity = Vector2.zero;

        if (distToTarget > stoppingDistance)
        {
            // 追踪逻辑：朝目标移动 / Tracking: move towards target
            Vector2 moveDir = (targetPos - (Vector2)transform.position).normalized;
            Vector2 trackVelocity = moveDir * moveSpeed;
            Vector2 sepDir = CalculateSeparation();
            // 结合追踪速度与分散速度 / Combine tracking and separation velocity
            finalVelocity = trackVelocity + (sepDir * separationWeight);
            if (anim != null) anim.SetBool("isMove", true);
        }
        else
        {
            // 停止距离内仅保留分散逻辑 / Only separation logic within stopping distance
            Vector2 sepDir = CalculateSeparation();
            if (sepDir.magnitude < 0.1f)
            {
                finalVelocity = Vector2.zero;
                if (anim != null) anim.SetBool("isMove", false);
            }
            else
            {
                finalVelocity = sepDir * (moveSpeed * 0.5f);
            }
        }

        // 平滑速度过渡 / Smooth velocity transition
        rb.velocity = Vector2.Lerp(rb.velocity, finalVelocity, 0.1f);

        // 根据移动方向翻转图片 / Flip sprite based on movement direction
        if (monsterSprite != null && rb.velocity.x != 0 && distToTarget > stoppingDistance)
        {
            monsterSprite.flipX = rb.velocity.x < 0; 
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        // 播放受击音效 / Play hit sound effect
        GameEvents.TriggerPlaySound(AudioID.MonsterHit);

        if (health != null)
        {
            health.TakeDamage(amount);
            // 检测是否达到死亡条件 / Check for death condition
            if (health.currentHealth <= 0) HandleDeath();
        }
    }

    public void HandleDeath()
    {
        if (isDead) return; 
        isDead = true;
        rb.velocity = Vector2.zero; 
        rb.simulated = false; // 禁用物理模拟 / Disable physics simulation

        if (anim != null) anim.SetTrigger("isDead");

        // 如果需要掉落或得分，可以在这里触发 / Trigger drops or scoring here
        // GameEvents.TriggerDeath(this.gameObject);

        // 延迟销毁物体以便播放动画 / Delay destruction for animation
        Destroy(gameObject, 0.8f); 
    }

    // 计算怪物之间的排斥力，防止重叠 / Calculate repulsion between monsters
    Vector2 CalculateSeparation()
    {
        Vector2 separation = Vector2.zero;
        Collider2D[] neighbors = Physics2D.OverlapCircleAll(transform.position, separationRadius, monsterLayer);
        int count = 0;
        foreach (var n in neighbors)
        {
            if (n.gameObject == gameObject) continue;
            Vector2 diff = (Vector2)transform.position - (Vector2)n.transform.position;
            float dist = diff.magnitude;
            if (dist < 0.01f) continue;
            // 距离越近，排斥力越大 / Closer distance, stronger repulsion
            separation += diff.normalized * (separationRadius - dist);
            count++;
        }
        return count > 0 ? separation / count : Vector2.zero;
    }

    // 怪物攻击协程：蓄力后冲刺 / Monster attack routine: charge then dash
    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        // 随机前摇增加不确定性 / Random pre-delay for uncertainty
        yield return new WaitForSeconds(Random.Range(0f, 0.4f));
        if (isDead) yield break; 
        if (anim != null) anim.SetTrigger("isRush"); 
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(0.4f); // 蓄力阶段 / Charging phase

        if (player != null && !isDead)
        {
            GameEvents.TriggerPlaySound(AudioID.MonsterRush); 
            Vector2 dashDir = (player.position - transform.position).normalized;
            if (monsterSprite != null) monsterSprite.flipX = dashDir.x < 0;

            float timer = 0f;
            while (timer < dashDuration && !isDead)
            {
                rb.velocity = dashDir * dashSpeed; // 执行冲刺 / Execute dash
                timer += Time.deltaTime;
                yield return null;
            }
        }
        if (!isDead) rb.velocity = Vector2.zero;
        lastAttackTime = Time.time;
        isAttacking = false;
    }
}