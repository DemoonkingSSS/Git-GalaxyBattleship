using System;
using UnityEngine;

/// <summary>
/// 功能 4（下）：玩家生命
/// HP = 3，按 T 扣 1 点血。血量变化用事件通知 UI，UI 不用每帧去查。
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("血量")]
    public int maxHp = 3;
    public int hp = 3;

    /// <summary>(当前血量, 最大血量)</summary>
    public event Action<int, int> OnHealthChanged;

    void Start()
    {
        hp = maxHp;
        RaiseChanged();
    }

    void Update()
    {
        // 按 T 扣血（作业要求的调试键）
        if (Input.GetKeyDown(KeyCode.T)) TakeDamage(1);
    }

    public void TakeDamage(int amount)
    {
        if (hp <= 0) return;

        hp = Mathf.Max(0, hp - amount);
        RaiseChanged();

        if (hp == 0) Debug.Log("玩家血量归零");
    }

    public void Heal(int amount)
    {
        hp = Mathf.Min(maxHp, hp + amount);
        RaiseChanged();
    }

    void RaiseChanged()
    {
        if (OnHealthChanged != null) OnHealthChanged(hp, maxHp);
    }
}
