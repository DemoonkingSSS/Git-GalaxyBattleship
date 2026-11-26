using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WinManager : MonoBehaviour
{
    public static WinManager Instance;

    // 使用字典：Key 是预制体名字，Value 是当前存活数量
    // Use Dictionary: Key is prefab name, Value is current alive count
    public Dictionary<string, int> liveCounts = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 注册：增加某种名字的物体计数
    public void Register(string groupName)
    {
        if (!liveCounts.ContainsKey(groupName)) liveCounts[groupName] = 0;
        liveCounts[groupName]++;
    }

    // 注销：减少计数，并检查胜利
    public void Unregister(string groupName)
    {
        if (liveCounts.ContainsKey(groupName))
        {
            liveCounts[groupName]--;
            CheckVictory();
        }
    }

    private void CheckVictory()
    {
        // 扩展性：只要字典里有任何一个 Key 的 Value 归零，就触发胜利
        // Any group reaches zero -> Victory
        foreach (var count in liveCounts.Values)
        {
            if (count <= 0) 
            {
                GameEvents.TriggerGameVictory();
                break; 
            }
        }
    }
}