using UnityEngine;
using UnityEngine.SceneManagement; // 必须引用场景管理命名空间 / Required for scene management

public class StartMenu : MonoBehaviour
{
    // --- 1. 切换到下一场景 / Switch to Next Scene ---
    // 可在 Inspector 的 Button 组件中通过 OnClick 事件引用
    public void LoadNextScene()
    {
        // 获取当前活跃场景的索引并加 1 / Get current scene index and add 1
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        // 检查索引是否超出总场景数范围 / Check if index is within range
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
            Debug.Log("正在加载下一场景... | Loading next scene...");
        }
        else
        {
            Debug.LogWarning("没有更多场景可以加载了！ | No more scenes in Build Settings!");
        }
    }

    // --- 2. 退出游戏 / Quit Game ---
    // 可被外部按钮或逻辑调用
    public void QuitGame()
    {
        Debug.Log("退出游戏... | Quitting game...");

        #if UNITY_EDITOR
            // 如果在编辑器模式下，停止运行 / Stop playing in Editor
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // 如果是打包后的游戏，关闭程序 / Quit the application
            Application.Quit();
        #endif
    }
}