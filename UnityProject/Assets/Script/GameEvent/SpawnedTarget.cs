using UnityEngine;

public class SpawnedTarget : MonoBehaviour
{
    private string myGroupName;

    public void Initialize(string name)
    {
        myGroupName = name;
        WinManager.Instance.Register(myGroupName);
    }

    void OnDestroy()
    {
        // 场景卸载时不触发，防止切关报错
        if (WinManager.Instance != null && gameObject.scene.isLoaded)
        {
            WinManager.Instance.Unregister(myGroupName);
        }
    }
}