using System.Collections;
using UnityEngine;

/// <summary>
/// 摄像机轻微震动。订阅事件系统的"玩家死亡 / 游戏胜利"广播，
/// 不需要改动 PlayerController 或任何已有脚本。
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraShake : MonoBehaviour
{
    public float duration = 0.35f;
    public float magnitude = 0.28f;

    private Vector3 basePos;
    private Coroutine routine;

    void Awake()
    {
        basePos = transform.localPosition;
    }

    void OnEnable()
    {
        GameEvents.OnPlayerDeath += OnPlayerDeath;
        GameEvents.OnGameVictory += OnVictory;
    }

    void OnDisable()
    {
        GameEvents.OnPlayerDeath -= OnPlayerDeath;
        GameEvents.OnGameVictory -= OnVictory;
    }

    void OnPlayerDeath(GameObject _) { Shake(duration, magnitude * 1.6f); }
    void OnVictory() { Shake(duration * 0.6f, magnitude * 0.5f); }

    public void Shake(float dur, float amp)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Routine(dur, amp));
    }

    IEnumerator Routine(float dur, float amp)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / dur);          // 衰减
            Vector2 offset = Random.insideUnitCircle * amp * k;
            transform.localPosition = basePos + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }
        transform.localPosition = basePos;
        routine = null;
    }
}
