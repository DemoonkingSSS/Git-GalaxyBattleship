using System.Collections;
using UnityEngine;

/// <summary>
/// 面板出现时淡入 + 轻微放大，避免"啪"地一下弹出来。
/// 用 unscaledDeltaTime，暂停时（Time.timeScale = 0）也能正常播放。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PanelFade : MonoBehaviour
{
    public float duration = 0.22f;
    public float fromAlpha = 0f;
    public float fromScale = 0.96f;

    private CanvasGroup group;
    private Coroutine routine;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group == null) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        group.alpha = fromAlpha;
        transform.localScale = Vector3.one * fromScale;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - k, 3f);   // ease-out

            group.alpha = Mathf.Lerp(fromAlpha, 1f, eased);
            transform.localScale = Vector3.one * Mathf.Lerp(fromScale, 1f, eased);
            yield return null;
        }

        group.alpha = 1f;
        transform.localScale = Vector3.one;
        routine = null;
    }
}
