using UnityEngine;

/// <summary>让标题轻微上下浮动，别像块贴纸。挂在标题所在的 GameObject 上。</summary>
public class TitleBob : MonoBehaviour
{
    public float amplitude = 10f;     // 上下幅度（像素）
    public float speed = 1.6f;        // 速度
    public float scaleAmount = 0.02f; // 轻微呼吸缩放

    private RectTransform rt;
    private Vector2 basePos;
    private Vector3 baseScale;
    private float phase;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (rt != null) basePos = rt.anchoredPosition;
        baseScale = transform.localScale;
        phase = Random.Range(0f, 6.28f);
    }

    void Update()
    {
        float t = Time.unscaledTime * speed + phase;

        if (rt != null)
            rt.anchoredPosition = basePos + new Vector2(0f, Mathf.Sin(t) * amplitude);

        float s = 1f + Mathf.Sin(t * 0.7f) * scaleAmount;
        transform.localScale = baseScale * s;
    }
}
