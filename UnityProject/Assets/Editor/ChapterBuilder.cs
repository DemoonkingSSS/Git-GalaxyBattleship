using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 教程成品搭建 —— 第一步：地基（分层 / 切图 / 动画 / 预制体）
/// 菜单：Tools / 教程成品 1/4 地基
/// 批处理：-executeMethod ChapterBuilder.BuildFoundation
/// </summary>
public static class ChapterBuilder
{
    const string AnimDir = "Assets/Animation";
    const string PrefabDir = "Assets/Prefabs";
    const string SpriteDir = "Assets/Sprite";

    [MenuItem("Tools/教程成品 1/4 地基")]
    public static void BuildFromMenu()
    {
        BuildFoundation();
        EditorUtility.DisplayDialog("完成", "地基已就绪：分层、切图、动画、预制体。", "好");
    }

    public static void BuildFoundation()
    {
        try
        {
            EnsureLayers("Player", "Monster", "Bullet");
            EnsureFolder(PrefabDir);

            // ---- 1. 切图 ----
            SliceGrid(SpriteDir + "/slime_green.png", 24, 24, 4, 3, "slime_", 24);
            SliceGrid(SpriteDir + "/coin.png", 16, 16, 12, 1, "coin_", 16);
            SliceGrid(SpriteDir + "/Tilemap Dungeon original size.png", 16, 16, 30, 20, "tile_", 16);

            // ---- 2. 动画 ----
            Sprite[] knight = LoadSprites(SpriteDir + "/knight.png");
            Sprite[] slime = LoadSprites(SpriteDir + "/slime_green.png");
            Sprite[] coin = LoadSprites(SpriteDir + "/coin.png");

            var playerIdle = MakeClip("PlayerIdle", Sub(knight, 0, 4), 8f, true);
            var playerRun = MakeClip("PlayerRun", Sub(knight, 9, 16), 12f, true);
            var playerRoll = MakeClip("PlayerRoll", Sub(knight, 28, 8), 14f, false);
            var playerHit = MakeClip("PlayerHit", Sub(knight, 36, 4), 10f, false);
            var playerDeath = MakeClip("PlayerDeath", Sub(knight, 43, 4), 8f, false);

            var slimeRush = MakeClip("SlimeRush", Sub(slime, 0, 4), 12f, true);
            var slimeIdle = MakeClip("SlimeIdle", Sub(slime, 4, 4), 8f, true);
            var slimeDead = MakeClip("SlimeDead", Sub(slime, 8, 4), 8f, false);

            var coinSpin = MakeClip("CoinSpin", coin, 16f, true);

            // ---- 3. 状态机 ----
            var playerCtrl = BuildPlayerController(playerIdle, playerRun, playerRoll, playerHit, playerDeath);
            var slimeCtrl = BuildSlimeController(slimeIdle, slimeRush, slimeDead);
            var coinCtrl = BuildCoinController(coinSpin);

            // ---- 4. 预制体 ----
            CreateSlimePrefab(slimeCtrl, slime[4]);
            CreateCoinPrefab(coinCtrl, coin[0]);
            CreateBulletPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("FOUNDATION_OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError("FOUNDATION_FAIL " + e);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    // ---------------- 分层 ----------------

    static void EnsureLayers(params string[] names)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;

        var so = new SerializedObject(assets[0]);
        var layers = so.FindProperty("layers");

        foreach (var name in names)
        {
            bool exists = false;
            for (int i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == name) { exists = true; break; }
            if (exists) continue;

            for (int i = 8; i < layers.arraySize; i++)
            {
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                {
                    layers.GetArrayElementAtIndex(i).stringValue = name;
                    break;
                }
            }
        }
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    // ---------------- 切图 ----------------

    static void SliceGrid(string path, int cellW, int cellH, int cols, int rows, string prefix, int ppu)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { Debug.LogWarning("找不到图：" + path); return; }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        int texH = cellH * rows;
        var metas = new List<SpriteMetaData>();
        int index = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                metas.Add(new SpriteMetaData
                {
                    name = prefix + index,
                    rect = new Rect(c * cellW, texH - (r + 1) * cellH, cellW, cellH),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                });
                index++;
            }
        }

        importer.spritesheet = metas.ToArray();
        importer.SaveAndReimport();
    }

    static Sprite[] LoadSprites(string pngPath)
    {
        var list = new List<Sprite>();
        foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(pngPath))
            if (o is Sprite s) list.Add(s);
        return list.ToArray();
    }

    static Sprite[] Sub(Sprite[] all, int start, int count)
    {
        var outArr = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            int idx = start + i;
            outArr[i] = idx < all.Length ? all[idx] : all[all.Length - 1];
        }
        return outArr;
    }

    // ---------------- 动画 ----------------

    static AnimationClip MakeClip(string name, Sprite[] frames, float fps, bool loop)
    {
        string path = AnimDir + "/" + name + ".anim";
        var clip = new AnimationClip { frameRate = fps };

        var binding = new EditorCurveBinding
        {
            path = "",
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite",
        };

        var keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // ---------------- 状态机 ----------------

    static AnimatorController BuildPlayerController(params AnimationClip[] clips)
    {
        string path = AnimDir + "/PlayerAnimation.controller";
        AssetDatabase.DeleteAsset(path);

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        ctrl.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsIdle", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsRolling", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("BeHit", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;

        var idle = sm.AddState("PlayerIdle");
        var run = sm.AddState("PlayerRun");
        var roll = sm.AddState("PlayerRoll");
        var hit = sm.AddState("PlayerHit");
        var dead = sm.AddState("PlayerDeath");

        idle.motion = clips[0];
        run.motion = clips[1];
        roll.motion = clips[2];
        hit.motion = clips[3];
        dead.motion = clips[4];
        sm.defaultState = idle;

        Link(idle, run, 0f, false, AnimatorConditionMode.If, "IsRunning");
        Link(run, idle, 0f, false, AnimatorConditionMode.If, "IsIdle");

        FromAny(sm, roll, AnimatorConditionMode.If, "IsRolling");
        FromAny(sm, hit, AnimatorConditionMode.If, "BeHit");
        FromAny(sm, dead, AnimatorConditionMode.If, "IsDead");

        Link(roll, idle, 0f, true);
        Link(hit, idle, 0f, true);

        return ctrl;
    }

    static AnimatorController BuildSlimeController(AnimationClip idleClip, AnimationClip rushClip, AnimationClip deadClip)
    {
        string path = AnimDir + "/SlimeAnimation.controller";
        AssetDatabase.DeleteAsset(path);

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        ctrl.AddParameter("isMove", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("isRush", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("isDead", AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;
        var idle = sm.AddState("SlimeIdle");
        var rush = sm.AddState("SlimeRush");
        var dead = sm.AddState("SlimeDead");

        idle.motion = idleClip;
        rush.motion = rushClip;
        dead.motion = deadClip;
        sm.defaultState = idle;

        Link(idle, rush, 0f, false, AnimatorConditionMode.If, "isMove");
        Link(rush, idle, 0f, false, AnimatorConditionMode.IfNot, "isMove");

        FromAny(sm, rush, AnimatorConditionMode.If, "isRush");
        FromAny(sm, dead, AnimatorConditionMode.If, "isDead");

        return ctrl;
    }

    static AnimatorController BuildCoinController(AnimationClip spin)
    {
        string path = AnimDir + "/CoinAnimation.controller";
        AssetDatabase.DeleteAsset(path);

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = ctrl.layers[0].stateMachine;
        var s = sm.AddState("CoinSpin");
        s.motion = spin;
        sm.defaultState = s;
        return ctrl;
    }

    static void Link(AnimatorState from, AnimatorState to, float duration, bool exitTime,
                     AnimatorConditionMode mode = AnimatorConditionMode.If, string param = null)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = exitTime;
        t.exitTime = exitTime ? 1f : 0f;
        t.duration = duration;
        t.hasFixedDuration = true;
        if (param != null) t.AddCondition(mode, 0f, param);
    }

    static void FromAny(AnimatorStateMachine sm, AnimatorState target, AnimatorConditionMode mode, string param)
    {
        var t = sm.AddAnyStateTransition(target);
        t.hasExitTime = false;
        t.duration = 0f;
        t.canTransitionToSelf = false;
        t.AddCondition(mode, 0f, param);
    }

    // ---------------- 预制体 ----------------

    static void CreateSlimePrefab(AnimatorController ctrl, Sprite idleFrame)
    {
        var go = new GameObject("Slime");
        go.layer = LayerMask.NameToLayer("Monster");
        go.transform.localScale = Vector3.one;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = idleFrame;
        sr.sortingOrder = 5;

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.85f, 0.85f);

        var track = go.AddComponent<MonsterTrack>();
        track.monsterSprite = sr;
        track.monsterLayer = LayerMask.GetMask("Monster");
        track.bulletLayerName = "Bullet";

        var hp = go.AddComponent<HealthController>();
        hp.maxHealth = 100f;

        // 血条：世界空间画布 + 一张填充图片
        var barGo = new GameObject("HealthBar");
        barGo.transform.SetParent(go.transform, false);
        barGo.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        barGo.transform.localScale = Vector3.one * 0.012f;

        var canvas = barGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = barGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, 12f);

        var bar = new GameObject("Bar");
        bar.transform.SetParent(barGo.transform, false);
        var img = bar.AddComponent<Image>();
        img.color = new Color(0.85f, 0.2f, 0.2f);
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        var barRt = bar.GetComponent<RectTransform>();
        barRt.sizeDelta = new Vector2(100f, 12f);
        barRt.anchoredPosition = Vector2.zero;

        barGo.SetActive(true);

        SavePrefab(go, PrefabDir + "/Slime.prefab");
    }

    static void CreateCoinPrefab(AnimatorController ctrl, Sprite frame)
    {
        var go = new GameObject("Coin");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frame;
        sr.sortingOrder = 3;

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        col.isTrigger = true;

        var pick = go.AddComponent<CoinPickUp>();
        pick.itemId = ItemID.Coin;
        pick.pickUpAudioId = AudioID.CoinPickUp;

        SavePrefab(go, PrefabDir + "/Coin.prefab");
    }

    static void CreateBulletPrefab()
    {
        var go = new GameObject("Bullet");
        go.layer = LayerMask.NameToLayer("Bullet");

        var bulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "/bullet.png");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = bulletSprite;
        sr.sortingOrder = 6;
        go.transform.localScale = Vector3.one * 0.5f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // 拉长碰撞箱，避免高速子弹一帧穿过目标
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.2f, 0.4f);
        col.isTrigger = true;

        var bs = go.AddComponent<BulletShoot>();
        bs.speed = 20f;
        bs.ignoreLayers = LayerMask.GetMask("Player", "Bullet", "Ignore Raycast");

        SavePrefab(go, PrefabDir + "/Bullet.prefab");
    }

    static void SavePrefab(GameObject go, string path)
    {
        AssetDatabase.DeleteAsset(path);
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    // ---------------- 杂项 ----------------

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
