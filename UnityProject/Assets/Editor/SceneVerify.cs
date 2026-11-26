using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>主场景自检：核对教程成品的地图与装配。</summary>
public static class SceneVerify
{
    [MenuItem("Tools/教程成品 自检主场景")]
    public static void Verify()
    {
        var sb = new StringBuilder();
        int fail = 0;

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
        sb.AppendLine("SCENE=" + scene.name);

        // 瓦片地图
        var grid = GameObject.Find("Grid");
        Check(sb, ref fail, "Grid 存在", grid != null && grid.GetComponent<Grid>() != null);

        var ground = GameObject.Find("Ground");
        var wall = GameObject.Find("Wall");
        var deco = GameObject.Find("Decoration");
        var obstacle = GameObject.Find("Obstacle");

        Check(sb, ref fail, "Ground", ground != null);
        Check(sb, ref fail, "Wall", wall != null);
        Check(sb, ref fail, "Decoration", deco != null);
        Check(sb, ref fail, "Obstacle", obstacle != null);

        int groundTiles = CountCells(ground);
        int wallTiles = CountCells(wall);
        int obstacleTiles = CountCells(obstacle);

        sb.AppendLine($"  地上瓦片={groundTiles} 墙={wallTiles} 障碍={obstacleTiles}");
        Check(sb, ref fail, "地面瓦片 > 300", groundTiles > 300);
        Check(sb, ref fail, "墙瓦片 > 60", wallTiles > 60);
        Check(sb, ref fail, "障碍瓦片 >= 5", obstacleTiles >= 5);

        Check(sb, ref fail, "Wall 有 TilemapCollider2D", wall != null && wall.GetComponent<TilemapCollider2D>() != null);
        Check(sb, ref fail, "Wall 有 CompositeCollider2D", wall != null && wall.GetComponent<CompositeCollider2D>() != null);
        var wallRb = wall != null ? wall.GetComponent<Rigidbody2D>() : null;
        Check(sb, ref fail, "Wall 刚体为 Static", wallRb != null && wallRb.bodyType == RigidbodyType2D.Static);
        Check(sb, ref fail, "Obstacle 有复合碰撞", obstacle != null && obstacle.GetComponent<CompositeCollider2D>() != null);

        // 玩家
        var player = GameObject.Find("Player");
        Check(sb, ref fail, "Player 存在", player != null);
        if (player != null)
        {
            Check(sb, ref fail, "Player 层级 = Player", player.layer == LayerMask.NameToLayer("Player"));
            Check(sb, ref fail, "PlayerController", player.GetComponent<PlayerController>() != null);
            Check(sb, ref fail, "HealthController", player.GetComponent<HealthController>() != null);
            Check(sb, ref fail, "Rigidbody2D", player.GetComponent<Rigidbody2D>() != null);
            Check(sb, ref fail, "CapsuleCollider2D", player.GetComponent<CapsuleCollider2D>() != null);

            var ctrl = player.GetComponent<PlayerController>();
            Check(sb, ref fail, "子弹预制体已绑定", ctrl != null && ctrl.Bulletprefab != null);
            Check(sb, ref fail, "枪的枢轴已绑定", ctrl != null && ctrl.gunPivot != null);

            var anim = player.GetComponentInChildren<Animator>();
            Check(sb, ref fail, "角色 Animator 已挂", anim != null && anim.runtimeAnimatorController != null);
            if (anim != null && anim.runtimeAnimatorController != null)
            {
                int ps = anim.runtimeAnimatorController.animationClips.Length;
                sb.AppendLine($"  玩家动画 clip 数={ps}");
                Check(sb, ref fail, "玩家动画 >= 5 段", ps >= 5);
                int st = ((UnityEditor.Animations.AnimatorController)anim.runtimeAnimatorController).layers[0].stateMachine.states.Length;
                sb.AppendLine($"  玩家状态数={st}");
                Check(sb, ref fail, "状态机 >= 5 个状态", st >= 5);
            }
        }

        // 摄像机
        var cam = Camera.main;
        Check(sb, ref fail, "主摄像机", cam != null && cam.orthographic);
        var follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
        Check(sb, ref fail, "CameraFollow 已挂", follow != null);
        Check(sb, ref fail, "跟随目标已设", follow != null && follow.target != null);

        // 生成器与流程
        var ge = GameObject.Find("GameEvent");
        Check(sb, ref fail, "GameEvent 存在", ge != null);
        if (ge != null)
        {
            var spawn = ge.GetComponent<InitSpawn>();
            Check(sb, ref fail, "InitSpawn", spawn != null);
            Check(sb, ref fail, "生成组 = 2", spawn != null && spawn.spawnList.Count == 2);
            if (spawn != null)
            {
                foreach (var g in spawn.spawnList)
                    Check(sb, ref fail, $"生成组 {g.groupName} 预制体已绑定", g.prefab != null);
            }
            Check(sb, ref fail, "WinManager", ge.GetComponent<WinManager>() != null);
            Check(sb, ref fail, "GameFunctionalComponent", ge.GetComponent<GameFunctionalComponent>() != null);
        }

        // 预制体
        CheckPrefab(sb, ref fail, "Slime", "Assets/Prefabs/Slime.prefab");
        CheckPrefab(sb, ref fail, "Coin", "Assets/Prefabs/Coin.prefab");
        CheckPrefab(sb, ref fail, "Bullet", "Assets/Prefabs/Bullet.prefab");

        sb.AppendLine(fail == 0 ? "SCENEVERIFY_OK 全部通过" : "SCENEVERIFY_FAIL 失败=" + fail);
        Debug.Log(sb.ToString());
        if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 1);
    }

    static int CountCells(GameObject go)
    {
        if (go == null) return 0;
        var tm = go.GetComponent<Tilemap>();
        if (tm == null) return 0;
        int n = 0;
        foreach (var pos in tm.cellBounds.allPositionsWithin)
            if (tm.GetTile(pos) != null) n++;
        return n;
    }

    static void CheckPrefab(StringBuilder sb, ref int fail, string label, string path)
    {
        var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Check(sb, ref fail, "预制体 " + label, p != null);
    }

    static void Check(StringBuilder sb, ref int fail, string name, bool ok)
    {
        if (!ok) fail++;
        sb.AppendLine((ok ? "  [OK]   " : "  [FAIL] ") + name);
    }
}
