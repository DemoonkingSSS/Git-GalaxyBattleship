using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 运行时搭建 UI。
/// 为什么放在运行时：批处理（-batchmode）下编辑器里创建 Text/TMP 组件会因字体与
/// TMP_Settings 未初始化而抛 NullReferenceException；运行时代码路径是正常初始化的，
/// 所以把界面放到 Awake 里建，稳妥且可复现。
///
/// 挂在场景里一个空物体上即可，字体/白图/音频由编辑器脚本在生成时赋值。
/// </summary>
public class RuntimeUIBuilder : MonoBehaviour
{
    [Header("资源引用（由生成器赋值）")]
    public Font uiFont;
    public Sprite whiteSprite;

    [Header("音频（由生成器赋值）")]
    public AudioClip coinPickupClip;
    public AudioClip monsterHitClip;
    public AudioClip monsterRushClip;

    [Header("模式")]
    public bool isMenuScene = false;

    [Header("外观")]
    public Color panelColor = new Color(0f, 0f, 0f, 0.78f);
    public Color buttonColor = new Color(0.20f, 0.20f, 0.26f, 1f);

    [Header("菜单装饰（可留空）")]
    public Sprite decoSlimeSprite;

    void Start()
    {
        if (isMenuScene) BuildMenu();
        else BuildGameHud();
    }

    // ---------------- 主场景 HUD ----------------

    void BuildGameHud()
    {
        var canvas = MakeCanvas("PlayerCanvas");
        EnsureEventSystem();

        var gfc = FindObjectOfType<GameFunctionalComponent>();

        // 金币计数
        var coinObj = MakeRect("CoinCount", canvas.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                               new Vector2(40f, -30f), new Vector2(560f, 80f), new Vector2(0f, 1f));
        var coinText = MakeText(coinObj, "Coin Count: 0", 46, new Color(1f, 0.85f, 0.25f), TextAnchor.UpperLeft);
        AddOutline(coinText, 3f, new Color(0.25f, 0.12f, 0f, 1f));
        var coinScript = coinObj.AddComponent<CoinCount>();
        coinScript.coinTextLegacy = coinText;

        // 右上角：剩余史莱姆
        var enemyObj = MakeRect("EnemyCount", canvas.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                                new Vector2(-40f, -30f), new Vector2(460f, 80f), new Vector2(1f, 1f));
        var enemyText = MakeText(enemyObj, "剩余史莱姆: -", 40, new Color(1f, 0.55f, 0.55f), TextAnchor.UpperRight);
        AddOutline(enemyText, 3f, new Color(0.2f, 0f, 0f, 1f));
        var counter = enemyObj.AddComponent<EnemyCounter>();
        counter.label = enemyText;

        // 面板
        var pause = MakePanel(canvas.transform, "PausePanel", "游戏暂停",
                              new[] { "继续 (Resume)", "重新开始 (Restart)", "退出 (Quit)" });
        if (gfc != null)
        {
            pause.buttons[0].onClick.AddListener(gfc.ResumeGame);
            pause.buttons[1].onClick.AddListener(gfc.RestartGame);
            pause.buttons[2].onClick.AddListener(gfc.QuitGame);
        }

        // 音量
        var volObj = MakeRect("VolumeControl", pause.root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                              new Vector2(0f, -330f), new Vector2(520f, 40f), new Vector2(0.5f, 0.5f));
        var slider = MakeSlider(volObj.transform);
        var vol = volObj.AddComponent<VolumeControl>();
        vol.volumeSlider = slider;

        var death = MakePanel(canvas.transform, "DeathPanel", "你死了  YOU DIED",
                              new[] { "重新开始 (Restart)", "退出 (Quit)" });
        if (gfc != null)
        {
            death.buttons[0].onClick.AddListener(gfc.RestartGame);
            death.buttons[1].onClick.AddListener(gfc.QuitGame);
        }

        var win = MakePanel(canvas.transform, "WinPanel", "VICTORY!  通关",
                            new[] { "再来一次 (Play Again)", "退出 (Quit)" });
        if (gfc != null)
        {
            win.buttons[0].onClick.AddListener(gfc.RestartGame);
            win.buttons[1].onClick.AddListener(gfc.QuitGame);
        }

        // 面板淡入 + 镜头震动
        AddFade(pause.root);
        AddFade(death.root);
        AddFade(win.root);

        var mainCam = Camera.main;
        if (mainCam != null && mainCam.GetComponent<CameraShake>() == null)
            mainCam.gameObject.AddComponent<CameraShake>();

        // 长按 R 的进度条
        var barRoot = MakeRect("RestartBar", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                               new Vector2(0f, 90f), new Vector2(420f, 26f), new Vector2(0.5f, 0f), new Color(0f, 0f, 0f, 0.55f));
        var fillObj = MakeRect("Fill", barRoot.transform, Vector2.zero, Vector2.one,
                               Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Color(1f, 0.55f, 0.2f, 0.95f));
        var fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        var fillImg = fillObj.GetComponent<Image>();
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 0f;

        // 黑屏：初始透明，避免 GFC 还没接管就先黑屏
        var fadeObj = MakeRect("FadeImage", canvas.transform, Vector2.zero, Vector2.one,
                               Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Color(0f, 0f, 0f, 0f));
        var fadeRt = fadeObj.GetComponent<RectTransform>();
        fadeRt.offsetMin = Vector2.zero;
        fadeRt.offsetMax = Vector2.zero;
        fadeObj.GetComponent<Image>().raycastTarget = false;

        // 接线
        if (gfc != null)
        {
            gfc.pausePanel = pause.root;
            gfc.deathPanel = death.root;
            gfc.winPanel = win.root;
            gfc.restartProgressRoot = barRoot;
            gfc.restartProgressBar = fillImg;
            gfc.fadeImage = fadeObj.GetComponent<Image>();

            // 音效表：ID 与音频文件的映射
            gfc.audioMappings = new List<GameFunctionalComponent.AudioMapping>();
            if (coinPickupClip != null)
                gfc.audioMappings.Add(new GameFunctionalComponent.AudioMapping { id = AudioID.CoinPickUp, clip = coinPickupClip });
            if (monsterHitClip != null)
                gfc.audioMappings.Add(new GameFunctionalComponent.AudioMapping { id = AudioID.MonsterHit, clip = monsterHitClip });
            if (monsterRushClip != null)
                gfc.audioMappings.Add(new GameFunctionalComponent.AudioMapping { id = AudioID.MonsterRush, clip = monsterRushClip });
        }

        pause.root.SetActive(false);
        death.root.SetActive(false);
        win.root.SetActive(false);
        barRoot.SetActive(false);
    }

    // ---------------- 开始菜单 ----------------

    void BuildMenu()
    {
        var canvas = MakeCanvas("StartMenu");
        EnsureEventSystem();

        var start = canvas.gameObject.AddComponent<StartMenu>();

        var titleObj = MakeRect("Title", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                new Vector2(0f, -240f), new Vector2(1400f, 160f), new Vector2(0.5f, 0.5f), new Color(0f, 0f, 0f, 0f));
        var titleText = MakeText(titleObj, "骑士与史莱姆", 120, new Color(1f, 0.93f, 0.72f), TextAnchor.MiddleCenter);
        AddOutline(titleText, 4f, new Color(0.35f, 0.15f, 0.05f, 1f));
        titleObj.AddComponent<TitleBob>();

        var subObj = MakeRect("Subtitle", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                              new Vector2(0f, -390f), new Vector2(1400f, 70f), new Vector2(0.5f, 0.5f), new Color(0f, 0f, 0f, 0f));
        var subText = MakeText(subObj, "WASD 移动 · Shift 翻滚 · 左键射击 · 长按 R 重开", 34,
                               new Color(1f, 1f, 1f, 0.72f), TextAnchor.MiddleCenter);
        AddOutline(subText, 2f, new Color(0f, 0f, 0f, 0.8f));

        // 角落的史莱姆装饰（有图才画）
        if (decoSlimeSprite != null)
        {
            Vector2[] spots =
            {
                new Vector2(-620f, 260f),
                new Vector2(640f, 180f),
                new Vector2(-540f, -260f),
                new Vector2(560f, -300f),
            };
            for (int i = 0; i < spots.Length; i++)
            {
                var deco = MakeRect("DecoSlime" + i, canvas.transform,
                                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                    spots[i], new Vector2(140f, 140f), new Vector2(0.5f, 0.5f));
                var img = deco.GetComponent<Image>();
                img.sprite = decoSlimeSprite;
                img.color = new Color(1f, 1f, 1f, 0.55f);
                img.preserveAspect = true;
                img.raycastTarget = false;

                var bob = deco.AddComponent<TitleBob>();
                bob.amplitude = 18f;
                bob.speed = Random.Range(0.8f, 1.4f);
                bob.scaleAmount = 0.05f;
            }
        }

        var startBtn = MakeButton(canvas.transform, "StartButton", "开始游戏", new Vector2(0f, -40f), new Vector2(440f, 100f));
        var quitBtn = MakeButton(canvas.transform, "QuitButton", "退出", new Vector2(0f, -180f), new Vector2(440f, 100f));

        startBtn.onClick.AddListener(start.LoadNextScene);
        quitBtn.onClick.AddListener(start.QuitGame);
    }

    // ---------------- 控件 ----------------

    Canvas MakeCanvas(string name)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    GameObject MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                        Vector2 pos, Vector2 size, Vector2 pivot, Color? color = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.sprite = whiteSprite;
        img.color = color ?? new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = img.color.a > 0.01f;

        return go;
    }

    Text MakeText(GameObject host, string text, int size, Color color, TextAnchor anchor)
    {
        // 注意：host 身上已经有 Image（Graphic）。同一个 GameObject 上再挂 Text（也是 Graphic）
        // 会共用同一个 CanvasRenderer，赋值 font 时抛 NullReferenceException。
        // 所以文本一律挂在子物体上，占满父级区域。
        var go = new GameObject("Label");
        go.transform.SetParent(host.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var t = go.AddComponent<Text>();

        Font f = uiFont;
        if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (f == null) { Debug.LogError("RuntimeUIBuilder: 找不到可用字体"); return t; }

        t.font = f;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void AddOutline(Text t, float distance, Color color)
    {
        if (t == null) return;
        var o = t.gameObject.AddComponent<Outline>();
        o.effectColor = color;
        o.effectDistance = new Vector2(distance, -distance);
        o.useGraphicAlpha = false;
    }

    static void AddFade(GameObject go)
    {
        if (go == null) return;
        if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();
        if (go.GetComponent<PanelFade>() == null) go.AddComponent<PanelFade>();
    }

    class Panel
    {
        public GameObject root;
        public List<Button> buttons = new List<Button>();
    }

    Panel MakePanel(Transform parent, string name, string title, string[] labels)
    {
        var p = new Panel();
        p.root = MakeRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                          new Vector2(0.5f, 0.5f), panelColor);
        var rt = p.root.GetComponent<RectTransform>();
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var titleObj = MakeRect(name + "_Title", p.root.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                new Vector2(0f, -90f), new Vector2(1400f, 140f), new Vector2(0.5f, 0.5f), new Color(0f, 0f, 0f, 0f));
        MakeText(titleObj, title, 88, Color.white, TextAnchor.MiddleCenter);

        for (int i = 0; i < labels.Length; i++)
            p.buttons.Add(MakeButton(p.root.transform, name + "_Btn" + i, labels[i],
                                     new Vector2(0f, -70f - i * 130f), new Vector2(480f, 100f)));

        return p;
    }

    Button MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        var go = MakeRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          pos, size, new Vector2(0.5f, 0.5f), buttonColor);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();

        var colors = btn.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = new Color(0.32f, 0.32f, 0.40f, 1f);
        colors.pressedColor = new Color(0.48f, 0.48f, 0.58f, 1f);
        btn.colors = colors;

        MakeText(go, label, 40, Color.white, TextAnchor.MiddleCenter);
        return btn;
    }

    Slider MakeSlider(Transform parent)
    {
        var go = new GameObject("VolumeSlider");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(460f, 26f);

        var bg = go.AddComponent<Image>();
        bg.sprite = whiteSprite;
        bg.color = new Color(0.25f, 0.25f, 0.30f, 1f);

        var slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        faRt.offsetMin = Vector2.zero;
        faRt.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fRt = fill.AddComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;
        fRt.offsetMin = Vector2.zero;
        fRt.offsetMax = Vector2.zero;

        var fImg = fill.AddComponent<Image>();
        fImg.sprite = whiteSprite;
        fImg.color = new Color(0.42f, 0.78f, 1f, 1f);

        slider.fillRect = fRt;
        slider.targetGraphic = bg;
        return slider;
    }
}
