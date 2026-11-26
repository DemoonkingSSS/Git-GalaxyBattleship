using System;
using UnityEngine;

public static class GameEvents
{
    // --- 1. 玩家生成事件 | Player Spawn Event ---
    // 广播刚生成的玩家物体 / Broadcast the newly spawned player object
    public static event Action<GameObject> OnPlayerSpawn;
    public static void TriggerPlayerSpawn(GameObject playerObject)
    {
        OnPlayerSpawn?.Invoke(playerObject);
    }

    // --- 2. 玩家死亡事件 | Player Death Event ---
    // 广播死亡的玩家物体 / Broadcast the dead player object
    public static event Action<GameObject> OnPlayerDeath;
    public static void TriggerPlayerDeath(GameObject playerObject)
    {
        OnPlayerDeath?.Invoke(playerObject);
    }

    // --- 3. 游戏胜利事件 | Game Victory Event ---
    public static event Action OnGameVictory;
    public static void TriggerGameVictory()
    {
        OnGameVictory?.Invoke();
    }

    // --- 4. 物品拾取事件 | Item Pickup Event ---
    // 传递物品 ID 以区分不同物体 / Pass an ID to distinguish different items
    public static event Action<ItemID> OnItemPickedUp;
    public static void TriggerItemPickedUp(ItemID itemId)
    {
        OnItemPickedUp?.Invoke(itemId);
    }

    // --- 5. 音频播放事件 | Audio Play Event ---
    // 传递音频 ID 以区分不同音频 / Pass an ID to distinguish different Audio    
    public static System.Action<AudioID> OnRequestPlaySound;

    public static void TriggerPlaySound(AudioID audioId)
    {
        OnRequestPlaySound?.Invoke(audioId);
    }
}

// 定义物品类型列表
// Define Item Type Enum
/// <summary>
/// 物品 ID 及其描述表 | Item ID & Description Table
/// </summary>
public enum ItemID
{
    /// <summary> 默认占位符 </summary>
    None = 0,

    /// <summary> 基础货币：每枚增加 1 计数 (Base currency: +1 count) </summary>
    Coin = 101,

}

// 音频 ID 枚举，方便在 Inspector 中选择
// Audio ID Enum for easy selection in Inspector
public enum AudioID
{
    /// <summary> 默认占位符 </summary>
    None = 0,
    /// <summary> 金币拾取音效 </summary>
    CoinPickUp = 101,  

    /// <summary> 怪物冲刺音效 </summary>
    MonsterRush = 201,  

    /// <summary> 怪物受击音效 </summary>
    MonsterHit = 202,    
}