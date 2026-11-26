using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 教程成品搭建 —— 第三步：把 UI 生成器挂进场景。
/// 真正的界面在运行时由 RuntimeUIBuilder 搭建（批处理下编辑器里建 UI 组件会 NRE）。
/// 菜单：Tools / 教程成品 3/4 UI 与流程
/// </summary>
public static class UIAssembler
{
    const string MainScene = "Assets/Scenes/MainScene.unity";
    const string IntroScene = "Assets/Scenes/Intro.unity";
    const string WhitePath = "Assets/Tiles/White.png";

    [MenuItem("Tools/教程成品 3/4 UI 与流程")]
    public static void BuildFromMenu()
    {
        BuildUI();
        EditorUtility.DisplayDialog("完成", "UI 生成器已挂进 MainScene 与 Intro。", "好");
    }

    public static void BuildUI()
    {
        try
        {
            Sprite white = EnsureWhiteSprite();
            Font font = FindFont();

            Debug.Log("UI 字体：" + (font != null ? font.name : "未找到") + " / 白图：" + (white != null));

            BuildScene(MainScene, false, font, white);
            BuildIntro(font, white);
            SetBuildScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("UI_OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError("UI_FAIL " + e);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    static void BuildScene(string path, bool isMenu, Font font, Sprite white)
    {
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        var existing = GameObject.Find("UIRoot");
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject("UIRoot");
        var builder = root.AddComponent<RuntimeUIBuilder>();
        builder.isMenuScene = isMenu;
        builder.uiFont = font;
        builder.whiteSprite = white;
        builder.coinPickupClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/CoinPickup.wav");
        builder.monsterHitClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/MonsterHit.wav");
        builder.monsterRushClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/MonsterRush.wav");
        builder.decoSlimeSprite = FindSubSprite("Assets/Sprite/slime_green.png", "slime_4");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, path);
    }

    static void BuildIntro(Font font, Sprite white)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.11f);
        camGo.AddComponent<AudioListener>();

        var root = new GameObject("UIRoot");
        var builder = root.AddComponent<RuntimeUIBuilder>();
        builder.isMenuScene = true;
        builder.uiFont = font;
        builder.whiteSprite = white;
        builder.decoSlimeSprite = FindSubSprite("Assets/Sprite/slime_green.png", "slime_4");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, IntroScene);
    }

    static void SetBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(IntroScene, true),
            new EditorBuildSettingsScene(MainScene, true),
        };
    }

    static Sprite FindSubSprite(string pngPath, string spriteName)
    {
        foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(pngPath))
            if (o is Sprite s && s.name == spriteName) return s;

        foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(pngPath))
            if (o is Sprite s) return s;

        return null;
    }

    static Font FindFont()    {
        foreach (var guid in AssetDatabase.FindAssets("t:Font", new[] { "Assets/UI/Font" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            var f = AssetDatabase.LoadAssetAtPath<Font>(p);
            if (f != null) return f;
        }
        foreach (var guid in AssetDatabase.FindAssets("t:Font", new[] { "Assets" }))
        {
            var f = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guid));
            if (f != null) return f;
        }
        return null;
    }

    static Sprite EnsureWhiteSprite()
    {
        if (!File.Exists(WhitePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(WhitePath));
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            File.WriteAllBytes(WhitePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(WhitePath, ImportAssetOptions.ForceUpdate);
        }

        var imp = AssetImporter.GetAtPath(WhitePath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 8f;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
    }
}
