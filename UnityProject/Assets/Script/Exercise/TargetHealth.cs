using System.Collections;
using UnityEngine;

/// <summary>
/// 功能 3（下）：靶子受击
/// HP = 3，每挨一枪闪红一下并扣 1 点，血量归零后销毁。
/// </summary>
public class TargetHealth : MonoBehaviour
{
    [Header("血量")]
    public int maxHp = 3;
    public int hp = 3;

    [Header("受击表现")]
    public Color flashColor = new Color(1f, 0.25f, 0.25f);
    public float flashDuration = 0.12f;

    private SpriteRenderer sr;
    private Color baseColor = Color.white;
    private Coroutine flashRoutine;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    public void TakeDamage(int amount)
    {
        if (hp <= 0) return;          // 已经死了，忽略后续子弹

        hp -= amount;

        if (sr != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashAndMaybeDie(hp <= 0));
        }
        else if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }

    IEnumerator FlashAndMaybeDie(bool die)
    {
        sr.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        sr.color = baseColor;

        if (die) Destroy(gameObject);
    }
}
