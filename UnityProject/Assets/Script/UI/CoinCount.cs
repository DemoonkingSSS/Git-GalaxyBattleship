using UnityEngine;
using TMPro; // 引用 TextMeshPro 命名空间 / Required for TextMeshPro

public class CoinCount : MonoBehaviour
{
    [Header("UI 组件 / UI Component")]
    [Tooltip("拖入显示金币数量的 TMP 文本 / Drag the TMP text object here")]
    public TextMeshProUGUI coinText; 

    [Tooltip("备用：内置 UI.Text（批处理环境没有 TMP 时使用）")]
    public UnityEngine.UI.Text coinTextLegacy;

    [Header("设置 / Settings")]
    [Tooltip("指定哪种 ItemID 属于金币 / Define which ItemID counts as a Coin")]
    public ItemID coinType = ItemID.Coin; 

    private int currentCoinCount = 0;

    private void OnEnable()
    {
        // 1. 订阅事件：监听 ItemID 类型的广播
        // 1. Subscribe: Listen to ItemID type broadcasts
        GameEvents.OnItemPickedUp += HandleItemPickedUp;
    }

    private void OnDisable()
    {
        // 2. 取消订阅：防止物体销毁后的引用错误
        // 2. Unsubscribe: Prevent reference errors after destruction
        GameEvents.OnItemPickedUp -= HandleItemPickedUp;
    }

    private void Start()
    {
        // 初始化 UI 显示
        UpdateCoinUI();
    }

    private void HandleItemPickedUp(ItemID id)
    {
        // 3. 逻辑判断：如果拾取的物品类型匹配金币类型
        // 3. Logic: If the picked item type matches the coin type
        if (id == coinType)
        {
            currentCoinCount++;
            UpdateCoinUI();
        }
    }

    private void UpdateCoinUI()
    {
        string text = $"Coin Count: {currentCoinCount}";

        if (coinText != null)
        {
            // 4. 同步文本内容 / Synchronize text content
            coinText.text = text;
        }
        else if (coinTextLegacy != null)
        {
            // 备用通道：内置 UI.Text / Fallback to legacy UI.Text
            coinTextLegacy.text = text;
        }
    }
}