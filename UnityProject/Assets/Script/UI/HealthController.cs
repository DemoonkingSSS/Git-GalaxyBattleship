using UnityEngine;
using UnityEngine.UI;

public class HealthController : MonoBehaviour
{
    [Header("血量设置 / Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    private float visualHealth; 

    [Header("显示设置 / Display Settings")]
    public float smoothSpeed = 2f; 
    private Image healthBarImage;

    void Start()
    {
        currentHealth = maxHealth;
        visualHealth = maxHealth; 

        healthBarImage = GetComponentInChildren<Image>();

        if (healthBarImage != null)
        {
            healthBarImage.type = Image.Type.Filled;
        }
        else
        {
            Debug.LogError($"{gameObject.name}: 子物体中未找到 Image 组件！");
        }
    }

    void Update()
    {
        if (healthBarImage != null && !Mathf.Approximately(visualHealth, currentHealth))
        {
            visualHealth = Mathf.MoveTowards(visualHealth, currentHealth, smoothSpeed * maxHealth * Time.deltaTime);
            healthBarImage.fillAmount = visualHealth / maxHealth;
        }

        //受伤调试
        // if (Input.GetKeyDown(KeyCode.R))
        // {
        //     TakeDamage(20f);
        // }
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return; // 如果已经死亡，不再处理

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"{gameObject.name} 剩余血量: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} 血量归零，触发死亡流程 | HP zero, triggering death flow");

        // 1. 尝试获取怪物脚本 / Try to get Monster script
        MonsterTrack monsterAI = GetComponent<MonsterTrack>();
        // 2. 尝试获取玩家脚本 / Try to get Player script
        PlayerController playerCtrl = GetComponent<PlayerController>();

        if (monsterAI != null)
        {
            // 触发怪物死亡逻辑
            monsterAI.HandleDeath();
        }
        else if (playerCtrl != null)
        {
            // 触发玩家死亡逻辑
            playerCtrl.PlayerDie();
        }
        else
        {
            // 3. 兜底逻辑：如果两个脚本都没有，直接销毁
            // Fallback: If neither script exists, destroy immediately
            Destroy(gameObject);
        }
    }
}