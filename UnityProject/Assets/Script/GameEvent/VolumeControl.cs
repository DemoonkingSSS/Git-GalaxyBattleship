using UnityEngine;
using UnityEngine.UI; // 必须引用 UI 命名空间 / Required for UI elements

public class VolumeControl : MonoBehaviour
{
    [Header("UI 组件 / UI Components")]
    public Slider volumeSlider; // 手动拖入你的 Slider 组件 / Drag your Slider here

    void Start()
    {
        // 1. 检查 Slider 是否已赋值
        // 1. Check if Slider is assigned
        if (volumeSlider == null)
        {
            volumeSlider = GetComponent<Slider>();
        }

        if (volumeSlider != null)
        {
            // 2. 初始化 Slider 的值为当前系统的音量
            // 2. Initialize slider value to current master volume
            if (GameFunctionalComponent.Instance != null)
            {
                volumeSlider.value = GameFunctionalComponent.Instance.masterVolume;
            }

            // 3. 监听滑动条数值改变事件
            // 3. Add listener for slider value changes
            volumeSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
        else
        {
            Debug.LogError($"{gameObject.name}: 未找到 Slider 组件！ | Slider component not found!");
        }
    }

    // 当滑动条滑动时执行的函数
    // Function executed when the slider is moved
    private void OnSliderValueChanged(float value)
    {
        if (GameFunctionalComponent.Instance != null)
        {
            // 调用管理器的全局音量设置函数
            // Call the master volume setting in your manager
            GameFunctionalComponent.Instance.SetMasterVolume(value);
        }
    }

    private void OnDestroy()
    {
        // 移除监听，防止内存泄漏
        // Remove listener to prevent memory leaks
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }
}