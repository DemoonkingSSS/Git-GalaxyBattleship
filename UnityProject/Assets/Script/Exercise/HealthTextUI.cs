using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 功能 4（上）：左上角生命值显示
/// 挂在 Canvas 下的 Text 上；自动找到场景里的玩家并订阅血量变化。
/// </summary>
public class HealthTextUI : MonoBehaviour
{
    [Header("绑定")]
    public PlayerHealth player;          // 留空则自动找

    [Header("显示")]
    public string prefix = "HP: ";

    private Text label;

    void Awake()
    {
        label = GetComponent<Text>();
    }

    void Start()
    {
        if (player == null) player = FindObjectOfType<PlayerHealth>();

        if (player != null) player.OnHealthChanged += OnChanged;

        // 订阅完先主动读一次，避免 Start 顺序不确定导致显示是空的
        if (player != null) OnChanged(player.hp, player.maxHp);
        else if (label != null) label.text = prefix + "-";
    }

    void OnDestroy()
    {
        if (player != null) player.OnHealthChanged -= OnChanged;
    }

    void OnChanged(int current, int max)
    {
        if (label != null) label.text = prefix + current + " / " + max;
    }
}
