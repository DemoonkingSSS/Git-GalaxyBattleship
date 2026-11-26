using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 教程成品搭建 —— 第二步：地图与主场景
/// 菜单：Tools / 教程成品 2/4 地图与场景
/// 批处理：-executeMethod ChapterBuilder.BuildMainScene
/// </summary>
public static class SceneAssembler
{
    const string ScenePath = "Assets/Scenes/MainScene.unity";
    const string TileDir = "Assets/Tiles";
    const string PrefabDir = "Assets/Prefabs";
    const string SpriteDir = "Assets/Sprite";

    // 房间尺寸（瓦片数）
    const int RoomW = 26;
    const int RoomH = 15;

    [MenuItem("Tools/教程成品 2/4 地图与场景")]
    public static void BuildFromMenu()
    {
        BuildMainScene();
        EditorUtility.DisplayDialog("完成", "MainScene 已生成。", "好");
    }

    public static void BuildMainScene()
    {
        try
        {
            EnsureFolder(TileDir);

            Tile floor = MakeTile("Floor", new Color(0.30f, 0.29f, 0.33f), false);
            Tile wall = MakeTile("Wall", new Color(0.55f, 0.53f, 0.58f), true);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var grid = new GameObject("Grid").AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            var ground = MakeLayer(grid.transform, "Ground", 0, floor, false);
            var wallTm = MakeLayer(grid.transform, "Wall", 2, wall, true);
            var deco = MakeLayer(grid.transform, "Decoration", 1, floor, false);
            var obstacle = MakeLayer(grid.transform, "Obstacle", 3, wall, true);

            // --- 铺地面 ---
            int halfW = RoomW / 2;
            int halfH = RoomH / 2;
            for (int x = -halfW; x < halfW; x++)
                for (int y = -halfH; y < halfH; y++)
                    ground.SetTile(new Vector3Int(x, y, 0), floor);

            // --- 围墙 ---
            for (int x = -halfW; x < halfW; x++)
            {
                wallTm.SetTile(new Vector3Int(x, halfH - 1, 0), wall);
                wallTm.SetTile(new Vector3Int(x, -halfH, 0), wall);
            }
            for (int y = -halfH; y < halfH; y++)
            {
                wallTm.SetTile(new Vector3Int(-halfW, y, 0), wall);
                wallTm.SetTile(new Vector3Int(halfW - 1, y, 0), wall);
            }

            // --- 几处障碍（柱子） ---
            int[,] pillars = { { -6, 2 }, { 6, 2 }, { -6, -3 }, { 6, -3 }, { 0, 4 } };
            for (int i = 0; i < pillars.GetLength(0); i++)
                obstacle.SetTile(new Vector3Int(pillars[i, 0], pillars[i, 1], 0), wall);

            // --- 房间外的装饰圈（避免边界看起来像悬空） ---
            for (int x = -halfW - 1; x <= halfW; x++)
            {
                deco.SetTile(new Vector3Int(x, halfH, 0), floor);
                deco.SetTile(new Vector3Int(x, -halfH - 1, 0), floor);
            }

            AddStaticCollider(wallTm.gameObject);
            AddStaticCollider(obstacle.gameObject);

            // --- 摄像机 ---
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.10f, 0.13f);
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<CameraFollow>();

            // --- 玩家 ---
            var player = BuildPlayer();

            // --- 生成器 / 流程 ---
            var gameEvent = new GameObject("GameEvent");
            var spawn = gameEvent.AddComponent<InitSpawn>();
            spawn.spawnAreaMin = new Vector2(-halfW + 3, -halfH + 2);
            spawn.spawnAreaMax = new Vector2(halfW - 3, halfH - 2);
            spawn.spawnList = new List<InitSpawn.SpawnGroup>
            {
                new InitSpawn.SpawnGroup { groupName = "Slime", prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Slime.prefab"), count = 4 },
                new InitSpawn.SpawnGroup { groupName = "Coin", prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Coin.prefab"), count = 8 },
            };
            gameEvent.AddComponent<WinManager>();
            gameEvent.AddComponent<GameFunctionalComponent>();

            // 摄像机跟随目标
            follow.target = player.transform;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("SCENE_OK " + ScenePath);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError("SCENE_FAIL " + e);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    // ---------------- 瓦片 ----------------

    static Tile MakeTile(string name, Color baseColor, bool drawBrick)
    {
        string png = TileDir + "/" + name + ".png";
        string tilePath = TileDir + "/" + name + ".asset";

        const int size = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var rnd = new System.Random(drawBrick ? 7 : 13);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float v = (float)rnd.NextDouble() * 0.07f - 0.035f;   // 细碎噪点，避免纯色死板
                Color c = baseColor + new Color(v, v, v);

                if (drawBrick)
                {
                    bool mortar = (y % 8 == 0) || ((x + (y / 8) * 8) % 16 == 0);
                    if (mortar) c *= 0.72f;
                    else if (y % 8 == 1) c *= 1.10f;                  // 砖块上沿高光
                }
                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
        }
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        File.WriteAllBytes(png, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(png, ImportAssetOptions.ForceUpdate);
        var imp = AssetImporter.GetAtPath(png) as TextureImporter;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 16f;
        imp.filterMode = FilterMode.Point;
        imp.mipmapEnabled = false;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(png);

        AssetDatabase.DeleteAsset(tilePath);
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = Color.white;
        AssetDatabase.CreateAsset(tile, tilePath);
        return tile;
    }

    static Tilemap MakeLayer(Transform parent, string name, int order, Tile floorOrWall, bool collider)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tm = go.AddComponent<Tilemap>();
        var tr = go.AddComponent<TilemapRenderer>();
        tr.sortingOrder = order;
        return tm;
    }

    static void AddStaticCollider(GameObject go)
    {
        var tmCol = go.AddComponent<TilemapCollider2D>();
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        var comp = go.AddComponent<CompositeCollider2D>();
        comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
        tmCol.usedByComposite = true;
    }

    // ---------------- 玩家 ----------------

    static GameObject BuildPlayer()
    {
        var root = new GameObject("Player");
        root.layer = LayerMask.NameToLayer("Player");
        root.transform.position = new Vector3(-8f, -3f, 0f);

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var cap = root.AddComponent<CapsuleCollider2D>();
        cap.size = new Vector2(0.62f, 1.15f);
        cap.direction = CapsuleDirection2D.Vertical;

        var ctrl = root.AddComponent<PlayerController>();
        root.AddComponent<HealthController>();

        // 角色贴图（子物体，只负责动画）
        var knight = new GameObject("knight_0");
        knight.transform.SetParent(root.transform, false);
        knight.transform.localScale = Vector3.one * 5f;
        var sr = knight.AddComponent<SpriteRenderer>();
        sr.sprite = FirstSprite(SpriteDir + "/knight.png");
        sr.sortingOrder = 5;
        var anim = knight.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/PlayerAnimation.controller");

        // 枪：跟随鼠标旋转
        var gun = new GameObject("GunPivot");
        gun.transform.SetParent(root.transform, false);
        gun.transform.localScale = Vector3.one * 3f;
        var gunSr = gun.AddComponent<SpriteRenderer>();
        gunSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "/Handgun.png");
        gunSr.sortingOrder = 6;

        ctrl.gunPivot = gun.transform;
        ctrl.rotationOffset = -45f;
        ctrl.bulletOffset = new Vector2(0.6f, 0f);
        ctrl.Bulletprefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Bullet.prefab");
        ctrl.moveSpeed = 5f;

        // 玩家血条（世界空间）
        var barGo = new GameObject("HealthBar");
        barGo.transform.SetParent(root.transform, false);
        barGo.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        barGo.transform.localScale = Vector3.one * 0.012f;
        var canvas = barGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        barGo.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 12f);

        var bar = new GameObject("Bar");
        bar.transform.SetParent(barGo.transform, false);
        var img = bar.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.35f, 0.85f, 0.45f);
        img.type = UnityEngine.UI.Image.Type.Filled;
        img.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        img.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
        bar.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 12f);

        return root;
    }

    // ---------------- 杂项 ----------------

    static Sprite FirstSprite(string pngPath)
    {
        foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(pngPath))
            if (o is Sprite s) return s;
        return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
    }

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
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
