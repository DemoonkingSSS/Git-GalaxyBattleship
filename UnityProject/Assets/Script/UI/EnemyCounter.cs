using UnityEngine;
using UnityEngine.UI;

/// <summary>右上角"剩余史莱姆"计数，让玩家知道还差多少能通关。</summary>
public class EnemyCounter : MonoBehaviour
{
    public Text label;
    public string format = "剩余史莱姆: {0}";

    private float nextCheck;
    private int lastCount = -1;

    void Update()
    {
        if (label == null) return;
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 0.25f;      // 每 0.25 秒数一次，别每帧遍历

        int count = FindObjectsOfType<MonsterTrack>().Length;
        if (count != lastCount)
        {
            lastCount = count;
            label.text = string.Format(format, count);
        }
    }
}
