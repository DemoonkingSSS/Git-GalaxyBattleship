using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 练习场景自动搭建器。
/// 菜单：Tools / 生成练习场景 Exercise
/// 批处理：Unity.exe -batchmode -quit -projectPath ... -executeMethod ExerciseBuilder.Build
/// </summary>
public static class ExerciseBuilder
{
    const string ScenePath = "Assets/Scenes/Exercise.unity";
    const string ArtDir = "Assets/Exercise/Art";
    const string PrefabDir = "Assets/Exercise/Prefabs";

    [MenuItem("Tools/生成练习场景 Exercise")]
    public static void BuildFromMenu()
    {
        Build();
        EditorUtility.DisplayDialog("完成",
            "练习场景已生成：\n" + ScenePath + "\n\n直接点 Play 就能试。", "好");
    }

    /// <summary>批处理入口。成功退出码 0，失败 1。</summary>
    public static void Build()
    {
        try
        {
            EnsureFolder(ArtDir);
            EnsureFolder(PrefabDir);

            Sprite square = CreateSquareSprite("Square", ArtDir);

            // 先做子弹预制体，玩家身上要引用它
            GameObject bulletPrefab = CreateBulletPrefab(square);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateWalls(square);
            GameObject player = CreatePlayer(square, bulletPrefab);
            CreateTargets(square);
            CreateUI();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("BUILD_OK " + ScenePath);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError("BUILD_FAIL " + e);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    // ---------------- 资源 ----------------

    static Sprite CreateSquareSprite(string name, string dir)
    {
        string pngPath = dir + "/" + name + ".png";

        if (!File.Exists(pngPath))
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();

            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);
        }

        var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64f;   // 64px = 1 个世界单位
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
    }

    static GameObject CreateBulletPrefab(Sprite square)
    {
        string path = PrefabDir + "/Bullet.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return AssetDatabase.LoadAssetAtPath<GameObject>(path);

        var go = new GameObject("Bullet");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = new Color(1f, 0.85f, 0.25f);
        sr.sortingOrder = 12;

        go.transform.localScale = Vector3.one * 0.22f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;   // 位置由脚本推，不参与物理模拟
        rb.gravityScale = 0f;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        go.AddComponent<Bullet>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ---------------- 场景内容 ----------------

    static void CreateCamera()
    {
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.4f;                                   // 竖向可见 10.8 单位
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.11f, 0.11f, 0.14f);

        camGo.AddComponent<AudioListener>();
    }

    static void CreateWalls(Sprite square)
    {
        // 房间内空 16 x 9，墙厚 0.5
        const float halfW = 8f;
        const float halfH = 4.5f;
        const float t = 0.5f;

        MakeWall("Wall_Top", new Vector2(0f, halfH), new Vector2(halfW * 2f + t * 2f, t), square);
        MakeWall("Wall_Bottom", new Vector2(0f, -halfH), new Vector2(halfW * 2f + t * 2f, t), square);
        MakeWall("Wall_Left", new Vector2(-halfW, 0f), new Vector2(t, halfH * 2f), square);
        MakeWall("Wall_Right", new Vector2(halfW, 0f), new Vector2(t, halfH * 2f), square);
    }

    static void MakeWall(string name, Vector2 center, Vector2 size, Sprite square)
    {
        var go = new GameObject(name);
        go.transform.position = center;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = new Color(0.34f, 0.31f, 0.44f);

        go.AddComponent<BoxCollider2D>();   // sprite 本身就是 1x1，缩放即碰撞体大小
    }

    static GameObject CreatePlayer(Sprite square, GameObject bulletPrefab)
    {
        var go = new GameObject("Player");
        go.transform.position = new Vector3(-5f, 0f, 0f);
        go.transform.localScale = Vector3.one * 0.9f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = new Color(0.36f, 0.74f, 1f);
        sr.sortingOrder = 10;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        go.AddComponent<BoxCollider2D>();
        go.AddComponent<PlayerMove>();
        go.AddComponent<PlayerHealth>();

        var shoot = go.AddComponent<PlayerShoot>();
        shoot.bulletPrefab = bulletPrefab;
        shoot.shootInterval = 0.3f;

        return go;
    }

    static void CreateTargets(Sprite square)
    {
        Vector2[] spots =
        {
            new Vector2(4.5f, 2.5f),
            new Vector2(6f, -1.5f),
            new Vector2(3f, -3f),
        };

        for (int i = 0; i < spots.Length; i++)
        {
            var go = new GameObject("Target_" + (i + 1));
            go.transform.position = spots[i];
            go.transform.localScale = Vector3.one * 1.2f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = new Color(0.42f, 0.82f, 0.45f);
            sr.sortingOrder = 8;

            go.AddComponent<BoxCollider2D>();

            var hp = go.AddComponent<TargetHealth>();
            hp.maxHp = 3;
            hp.hp = 3;
        }
    }

    static void CreateUI()
    {
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasGo.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 左上角：血量
        var hpGo = new GameObject("HealthText");
        hpGo.transform.SetParent(canvasGo.transform, false);
        var hpText = hpGo.AddComponent<Text>();
        hpText.font = font;
        hpText.fontSize = 42;
        hpText.color = Color.white;
        hpText.alignment = TextAnchor.UpperLeft;
        hpText.text = "HP: 3 / 3";
        PlaceTopLeft(hpText.rectTransform, new Vector2(40f, -30f), new Vector2(500f, 80f));
        hpGo.AddComponent<HealthTextUI>();

        // 左下角：操作提示
        var tipGo = new GameObject("TipText");
        tipGo.transform.SetParent(canvasGo.transform, false);
        var tipText = tipGo.AddComponent<Text>();
        tipText.font = font;
        tipText.fontSize = 26;
        tipText.color = new Color(1f, 1f, 1f, 0.65f);
        tipText.alignment = TextAnchor.LowerLeft;
        tipText.text = "WASD 移动    左键 射击    T 键 扣血";
        PlaceBottomLeft(tipText.rectTransform, new Vector2(40f, 30f), new Vector2(900f, 60f));
    }

    static void PlaceTopLeft(RectTransform rt, Vector2 offset, Vector2 size)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = size;
    }

    static void PlaceBottomLeft(RectTransform rt, Vector2 offset, Vector2 size)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = size;
    }

    // ---------------- 杂项 ----------------

    static void AddToBuildSettings(string path)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!list.Exists(s => s.path == path))
        {
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path) ?? "Assets";
        parent = parent.Replace("\\", "/");
        string leaf = Path.GetFileName(path);

        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
