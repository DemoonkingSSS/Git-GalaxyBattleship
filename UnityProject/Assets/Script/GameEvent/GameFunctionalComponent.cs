using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/*
 * [脚本功能概览 / Script Overview]
 * 本脚本是游戏的全局功能控制器（单例模式），主要负责以下核心逻辑：
 * 1. UI 面板管理：控制暂停、死亡、胜利面板的显示与隐藏。
 * 2. 状态管理：处理游戏的暂停(Esc)、长按重启(R)以及全局时间缩放控制。
 * 3. 视觉反馈：实现场景切换时的全屏图片淡入淡出（Fade In/Out）效果。
 * 4. 音频系统：通过事件中心(GameEvents)接收并播放全局单次音效(PlayOneShot)。
 * 5. 事件监听：订阅并处理来自 GameEvents 的玩家死亡(OnPlayerDeath)与游戏胜利(OnGameVictory)信号。
 */

public class GameFunctionalComponent : MonoBehaviour
{
    // 单例模式：允许其他脚本通过 GameFunctionalComponent.Instance 访问
    // Singleton: Allows other scripts to access via GameFunctionalComponent.Instance
    public static GameFunctionalComponent Instance;

    [System.Serializable]
    public struct AudioMapping // 音频映射结构体 / Audio Mapping Structure
    {
        public AudioID id;
        public AudioClip clip;
    }

    [Header("UI 面板 / UI Panels")]
    public GameObject pausePanel;            // 暂停面板 (Esc)
    public GameObject deathPanel;            // 死亡面板 (玩家死亡时弹出)
    public GameObject winPanel;              // 胜利面板 (游戏胜利时弹出)
    public GameObject restartProgressRoot;  // 重启进度条父物体 (R)
    public Image restartProgressBar;        // 重启进度条图片 (Filled 模式)

    [Header("淡入淡出设置 / Fade Settings")]
    public Image fadeImage;                 // 指定的全屏淡入淡出图片
    public float fadeDuration = 0.5f;       // 淡入淡出的持续时间

    [Header("重启设置 / Restart Settings")]
    public float restartHoldTime = 2f;      // 需要长按 R 的秒数
    public float restartCooldown = 1f;       // 重启后的强制冷却时间 (防止连续触发)
    private static float lastRestartTime = -100f; // 静态变量，跨场景保留时间戳

    [Header("音频库 / Audio Library")]
    [Range(0, 1)] public float masterVolume = 1f;
    public List<AudioMapping> audioMappings = new List<AudioMapping>(); // ID 与音频文件的映射列表
    private AudioSource globalSource;       // 全局音效播放器

    private float restartTimer = 0f;
    private bool isPaused = false;
    private bool isFading = false;         // 状态锁：淡入淡出期间禁用部分输入
    private bool isGameOver = false;       // 游戏结束状态锁 (死亡或胜利)

    void Awake()
    {
        // 初始化单例 / Initialize Singleton
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // 动态添加全局音效播放器
        globalSource = gameObject.AddComponent<AudioSource>();
        globalSource.playOnAwake = false;
    }

    void OnEnable()
    {
        // 订阅音频播放事件 / Subscribe to Audio Event
        GameEvents.OnRequestPlaySound += HandlePlaySoundRequest;
        
        // 订阅玩家死亡事件 / Subscribe to Player Death Event
        GameEvents.OnPlayerDeath += HandlePlayerDeath;

        // 订阅游戏胜利事件 / Subscribe to Game Victory Event
        GameEvents.OnGameVictory += HandleGameVictory;
    }

    void OnDisable()
    {
        // 取消订阅，防止内存泄漏 / Unsubscribe
        GameEvents.OnRequestPlaySound -= HandlePlaySoundRequest;
        GameEvents.OnPlayerDeath -= HandlePlayerDeath;
        GameEvents.OnGameVictory -= HandleGameVictory;
    }

    void Start()
    {
        // 初始 UI 状态设置
        if (pausePanel != null) pausePanel.SetActive(false);
        if (deathPanel != null) deathPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (restartProgressRoot != null) restartProgressRoot.SetActive(false);
        if (restartProgressBar != null) restartProgressBar.fillAmount = 0;

        // 场景启动时自动淡入 (从不透明到透明)
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            Color c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
            StartCoroutine(FadeRoutine(0f)); 
        }
    }

    void Update()
    {
        // 如果游戏已经结束（死亡或胜利），只允许处理重启逻辑
        if (isGameOver)
        {
            HandleRestartInput();
            return;
        }

        HandlePauseInput();
        HandleRestartInput();
    }

    // --- 1. 暂停逻辑 / Pause Logic ---
    private void HandlePauseInput()
    {
        // 结束状态下禁止通过 Esc 切换暂停状态
        if (Input.GetKeyDown(KeyCode.Escape) && !isFading && !isGameOver)
        {
            isPaused = !isPaused;
            if (pausePanel != null) pausePanel.SetActive(isPaused);

            // Time.timeScale = 0 会冻结物理和所有 Time.deltaTime 相关逻辑
            Time.timeScale = isPaused ? 0f : 1f;
            Debug.Log(isPaused ? "游戏暂停 | Paused" : "游戏恢复 | Resumed");
        }
    }

    // --- 2. 玩家死亡处理 / Player Death Handling ---
    private void HandlePlayerDeath(GameObject Player)
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log("监听到玩家死亡事件 | Player death event detected.");

        // 延迟弹出死亡面板
        StartCoroutine(ShowEndPanelRoutine(deathPanel, 0.6f));
    }

    // --- 3. 游戏胜利处理 / Game Victory Handling ---
    private void HandleGameVictory()
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log("监听到游戏胜利事件 | Game Victory event detected.");

        // 立即或延迟弹出胜利面板
        StartCoroutine(ShowEndPanelRoutine(winPanel, 0.2f));
    }

    // 通用的面板显示协程 (支持死亡或胜利)
    IEnumerator ShowEndPanelRoutine(GameObject panel, float delay)
    {
        // 使用 Realtime 等待，不受 TimeScale 影响
        yield return new WaitForSecondsRealtime(delay);

        if (panel != null) panel.SetActive(true);
        
        // 停止物理和逻辑更新
        Time.timeScale = 0f; 
    }

    // --- 4. 长按 R 重启逻辑 / Restart Logic ---
    private void HandleRestartInput()
    {
        if (Time.unscaledTime < lastRestartTime + restartCooldown || isFading) return;

        if (Input.GetKey(KeyCode.R))
        {
            if (restartProgressRoot != null) restartProgressRoot.SetActive(true);
            
            restartTimer += Time.unscaledDeltaTime; 

            if (restartProgressBar != null)
                restartProgressBar.fillAmount = restartTimer / restartHoldTime;

            if (restartTimer >= restartHoldTime)
            {
                RestartGame();
            }
        }
        else
        {
            restartTimer = 0f;
            if (restartProgressRoot != null) restartProgressRoot.SetActive(false);
        }
    }

    // --- 5. 场景与退出函数 / Scene & Quit Functions ---
    public void RestartGame()
    {
        if (isFading) return;
        lastRestartTime = Time.unscaledTime; 
        StartCoroutine(RestartSequence());
    }

    IEnumerator RestartSequence()
    {
        isFading = true; 
        yield return StartCoroutine(FadeRoutine(1f)); 

        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Debug.Log("退出游戏 | Quitting Game");
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; 
    #else
        Application.Quit(); 
    #endif
    }

    // --- 6. 淡入淡出核心 / Fade Core ---
    IEnumerator FadeRoutine(float targetAlpha)
    {
        if (fadeImage == null) yield break;

        float startAlpha = fadeImage.color.a;
        float timer = 0f;
        Color tempColor = fadeImage.color;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            tempColor.a = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            fadeImage.color = tempColor;
            yield return null; 
        }

        tempColor.a = targetAlpha;
        fadeImage.color = tempColor;

        fadeImage.raycastTarget = (targetAlpha > 0f);
        isFading = false; 
    }

    // --- 7. 音频系统处理 / Audio System Handling ---
    private void HandlePlaySoundRequest(AudioID id)
    {
        AudioMapping mapping = audioMappings.Find(m => m.id == id);
        if (mapping.clip != null)
        {
            globalSource.PlayOneShot(mapping.clip, masterVolume);
        }
        else
        {
            Debug.LogWarning($"[Audio] 未找到 AudioID: {id} 的音频配置！");
        }
    }

    /// <summary>供暂停面板的「继续」按钮调用 / Called by the Resume button</summary>
    public void ResumeGame()
    {
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        globalSource.volume = masterVolume;
    }
}