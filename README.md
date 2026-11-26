# Git-GalaxyBattleship

> 这个仓库里，游戏工程在 **`UnityProject/`** 子目录。根目录下的 `111`、`3434.txt`、`9178.txt`、`haha` 是仓库早期留下的文件，与游戏无关，请忽略。

## 游戏

2D 俯视角动作小游戏：骑士在房间里边打史莱姆边捡金币。

**操作**

| 按键 | 功能 |
|---|---|
| `WASD` | 移动 |
| `Shift` | 翻滚（无敌帧，有冷却） |
| 鼠标左键 | 射击（朝鼠标方向） |
| `Esc` | 暂停 |
| 长按 `R` | 重开本局 |
| `T` | 自己扣一点血（调试用） |

**通关条件**：打光所有史莱姆，或捡光所有金币。

## 环境要求

- **Unity 2022.3.62f3c1**（中国版）
- Windows 10/11

> 版本号必须一致。中国版和官方版混用，Unity Hub 会提示升级，可能引发包解析问题。

## 怎么跑起来

1. 克隆仓库
2. Unity Hub → Add → 选择 `UnityProject/` 文件夹
3. 打开后进 `Assets/Scenes/Intro.unity`，点 Play

**第一次打开会导入几分钟**，因为 `Library/` 不进仓库，需要本地重建。这是正常现象。

## 目录结构

```
UnityProject/
├── Assets/
│   ├── Scenes/       Intro（开始菜单）/ MainScene（主游戏）/ Exercise（练习场景）
│   ├── Script/       游戏逻辑
│   │   ├── PlayerController.cs        玩家移动、翻滚、射击、受伤
│   │   ├── Item/MonsterTrack.cs       史莱姆 AI（追踪 + 冲刺）
│   │   ├── GameEvent/                 事件系统、生成器、UI 流程控制、音量
│   │   └── UI/                        金币计数、运行时 UI 构建、各种手感脚本
│   ├── Editor/       编辑器工具（地图/场景/UI 生成器、自检脚本）
│   ├── Animation/    动画 clip 与状态机
│   ├── Sprite/       精灵图
│   ├── Audio/        音效
│   └── Prefabs/      Slime / Coin / Bullet
├── Packages/
└── ProjectSettings/
```

## 给组员的协作规矩（重要）

1. **`Library/`、`Temp/` 绝对不要提交**。已在 `.gitignore` 里排除，别手动加回来。
2. **`.meta` 文件必须提交**。Unity 靠它认资源引用，漏了就是一片粉色丢失贴图。
3. **`.unity` 和 `.prefab` 几乎无法合并**。两个人同时改同一个场景，git 冲突解不干净。**约定分工：谁负责哪个场景，别人别动。**
4. 改了 C# 脚本先自己跑一遍，`Tools` 菜单里有自检工具可以核对场景完整性。

## 可运行版本

不想装 Unity 的，去 Releases 下载 `KnightAndSlimes-win64.zip`，解压后运行 `KnightAndSlimes.exe`。

> 注意：必须解压整个文件夹再运行，只拿 `.exe` 是打不开的。
