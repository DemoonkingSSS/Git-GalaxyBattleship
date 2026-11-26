using UnityEditor;
using UnityEngine;

/// <summary>
/// 修一个手感问题：子弹撞到金币会被"吃掉"（BulletShoot 碰到任何非忽略层级都自毁）。
/// 做法：新建 Pickup 层给可拾取物，并把 Pickup 加进子弹的忽略列表。
/// 菜单：Tools / 修复 子弹被金币拦截
/// </summary>
public static class FixBulletPassPickup
{
    const string CoinPrefab = "Assets/Prefabs/Coin.prefab";
    const string BulletPrefab = "Assets/Prefabs/Bullet.prefab";

    [MenuItem("Tools/修复 子弹被金币拦截")]
    public static void Apply()
    {
        EnsureLayer("Pickup");

        int pickup = LayerMask.NameToLayer("Pickup");
        if (pickup < 0) { Debug.LogError("Pickup 层创建失败"); return; }

        // 1. 金币改到 Pickup 层
        var coin = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefab);
        if (coin == null) { Debug.LogError("找不到金币预制体"); return; }

        var coinRoot = PrefabUtility.LoadPrefabContents(CoinPrefab);
        coinRoot.layer = pickup;
        PrefabUtility.SaveAsPrefabAsset(coinRoot, CoinPrefab);
        PrefabUtility.UnloadPrefabContents(coinRoot);

        // 2. 子弹忽略 Pickup 层
        var bulletRoot = PrefabUtility.LoadPrefabContents(BulletPrefab);
        if (bulletRoot == null) { Debug.LogError("找不到子弹预制体"); return; }

        var bs = bulletRoot.GetComponent<BulletShoot>();
        if (bs == null) { Debug.LogError("子弹上没有 BulletShoot"); return; }

        bs.ignoreLayers = LayerMask.GetMask("Player", "Bullet", "Pickup", "Ignore Raycast");
        Debug.Log("子弹忽略层 = " + bs.ignoreLayers.value);

        PrefabUtility.SaveAsPrefabAsset(bulletRoot, BulletPrefab);
        PrefabUtility.UnloadPrefabContents(bulletRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PICKUP_FIX_OK");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    static void EnsureLayer(string name)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;

        var so = new SerializedObject(assets[0]);
        var layers = so.FindProperty("layers");

        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == name) return;

        for (int i = 8; i < layers.arraySize; i++)
        {
            if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
            {
                layers.GetArrayElementAtIndex(i).stringValue = name;
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                return;
            }
        }
    }
}
