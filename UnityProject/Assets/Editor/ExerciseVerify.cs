using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 练手场景自检：加载 Exercise.unity，逐条核对作业的四项要求。
/// 批处理：-executeMethod ExerciseVerify.Verify
/// </summary>
public static class ExerciseVerify
{
    [MenuItem("Tools/自检练习场景")]
    public static void Verify()
    {
        var sb = new StringBuilder();
        int fail = 0;

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Exercise.unity", OpenSceneMode.Single);
        sb.AppendLine("SCENE=" + scene.name + " path=" + scene.path);

        // ---- 功能 1：玩家移动 + 碰撞 ----
        var player = GameObject.Find("Player");
        Check(sb, ref fail, "玩家存在", player != null);
        if (player != null)
        {
            var move = player.GetComponent<PlayerMove>();
            var rb = player.GetComponent<Rigidbody2D>();
            var col = player.GetComponent<BoxCollider2D>();

            Check(sb, ref fail, "PlayerMove 脚本", move != null);
            Check(sb, ref fail, "移动速度 = 5", move != null && Mathf.Approximately(move.moveSpeed, 5f));
            Check(sb, ref fail, "Rigidbody2D", rb != null);
            Check(sb, ref fail, "重力关闭(俯视角)", rb != null && Mathf.Approximately(rb.gravityScale, 0f));
            Check(sb, ref fail, "BoxCollider2D", col != null);
        }

        // ---- 功能 2：房子外围四面墙 ----
        int walls = 0;
        foreach (var n in new[] { "Wall_Top", "Wall_Bottom", "Wall_Left", "Wall_Right" })
        {
            var w = GameObject.Find(n);
            bool ok = w != null && w.GetComponent<BoxCollider2D>() != null;
            if (ok) walls++;
            Check(sb, ref fail, "墙 " + n, ok);
        }
        Check(sb, ref fail, "墙数量 = 4", walls == 4);

        // ---- 功能 3：射击 + 靶子受击 ----
        if (player != null)
        {
            var shoot = player.GetComponent<PlayerShoot>();
            Check(sb, ref fail, "PlayerShoot 脚本", shoot != null);
            Check(sb, ref fail, "射速 0.3s", shoot != null && Mathf.Approximately(shoot.shootInterval, 0.3f));
            Check(sb, ref fail, "子弹 Prefab 已绑定", shoot != null && shoot.bulletPrefab != null);

            if (shoot != null && shoot.bulletPrefab != null)
            {
                var bc = shoot.bulletPrefab.GetComponent<Bullet>();
                var bcol = shoot.bulletPrefab.GetComponent<Collider2D>();
                Check(sb, ref fail, "子弹带 Bullet 脚本", bc != null);
                Check(sb, ref fail, "子弹碰撞体是 Trigger", bcol != null && bcol.isTrigger);
            }
        }

        int targets = 0;
        for (int i = 1; i <= 3; i++)
        {
            var t = GameObject.Find("Target_" + i);
            var th = t != null ? t.GetComponent<TargetHealth>() : null;
            bool ok = th != null && th.maxHp == 3 && t.GetComponent<Collider2D>() != null;
            if (ok) targets++;
            Check(sb, ref fail, "靶子 Target_" + i + " (HP=3 + 碰撞体)", ok);
        }
        Check(sb, ref fail, "靶子数量 = 3", targets == 3);

        // ---- 功能 4：玩家生命 + UI ----
        if (player != null)
        {
            var ph = player.GetComponent<PlayerHealth>();
            Check(sb, ref fail, "PlayerHealth 脚本", ph != null);
            Check(sb, ref fail, "玩家 HP = 3", ph != null && ph.maxHp == 3);
        }

        var hpUi = GameObject.Find("HealthText");
        Check(sb, ref fail, "左上角 HealthText 存在", hpUi != null);
        if (hpUi != null)
        {
            var txt = hpUi.GetComponent<Text>();
            var ui = hpUi.GetComponent<HealthTextUI>();
            Check(sb, ref fail, "HealthText 是 UI.Text", txt != null);
            Check(sb, ref fail, "HealthText 用了内置字体", txt != null && txt.font != null);
            Check(sb, ref fail, "健康值 UI 脚本", ui != null);

            var rt = hpUi.GetComponent<RectTransform>();
            Check(sb, ref fail, "锚点固定在左上角", rt != null && Mathf.Approximately(rt.anchorMin.x, 0f) && Mathf.Approximately(rt.anchorMin.y, 1f));

            if (ui != null)
            {
                var ph = player != null ? player.GetComponent<PlayerHealth>() : null;
                Check(sb, ref fail, "HealthTextUI 能自动找到玩家", ui.player != null || ph != null);
            }
        }

        // ---- 摄像机 ----
        var cam = Camera.main;
        Check(sb, ref fail, "主摄像机存在且为正交", cam != null && cam.orthographic);

        sb.AppendLine(fail == 0 ? "VERIFY_OK 全部通过" : "VERIFY_FAIL 失败项=" + fail);
        Debug.Log(sb.ToString());

        if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 1);
    }

    static void Check(StringBuilder sb, ref int fail, string name, bool ok)
    {
        if (!ok) fail++;
        sb.AppendLine((ok ? "  [OK]   " : "  [FAIL] ") + name);
    }
}
