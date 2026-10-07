# 游戏内 Overlay 渲染模式（光标/UI 覆写）


> 参考：`s1f102500012/sts2mod` → `PRTS动态光标 PRTSCursor`（作者 Natsuki，v1.0.3，适配 0.107.1）
> 场景：替换系统光标、渲染自定义 UI 层、在游戏 framebuffer 内做逐帧动画


## 章节导航

| 内容 | 文件 |
|------|------|
| PRTSCursor 案例精读：光标 overlay 全流程 | [overlay-cursor.md](overlay-cursor.md) |

## 概述

在游戏自己的 framebuffer 里挂一个顶层 `CanvasLayer` + `Sprite2D`，每帧跟随鼠标/位置刷新 —— 替代「逐帧刷新系统光标」的方案。

适合：光标动画、HUD 元素、截图可见的 UI 覆写。

## 核心坑（本模块最重要的一条）

**标准单 DLL mod 布局（`Microsoft.NET.Sdk`）下，Godot C# 源生成器不运行 → 自定义 `Node` 子类的 `_Process` / `_Ready` / `_EnterTree` 回调根本不会被调用。**

- 症状：节点进了场景树，但从不逐帧更新（动画不动、甚至不显示）。
- 原因：Godot C# 绑定靠源生成器为覆写方法生成注册代码；非 Godot SDK 编译时生成器不跑，引擎不知道你的回调存在。
- 解法：**只用内置节点类型**（`CanvasLayer`、`Sprite2D`，无需覆写任何回调），逐帧驱动改挂 `SceneTree.ProcessFrame` 信号（普通 `Callable`，不依赖源生成器）。

## 常见问题

| 问题 | 解决 |
|------|------|
| 覆写了 `_Process` 却不执行 | 项目是 `Microsoft.NET.Sdk` 单 DLL → 改 `SceneTree.ProcessFrame` 信号方案（见上） |
| overlay 初始化时挂不上树 | 初始化期 `tree.Root.AddChild` 包 try-catch，失败 `CallDeferred(Node.MethodName.AddChild, layer)` 兜底 |
| 逐帧刷新系统光标闪烁/丢失 | macOS 上尤其明显 → 改游戏内 overlay + 透明系统光标（见 overlay-cursor.md） |
| 其他控件请求别的光标形态露出系统指针 | 用同一张 1x1 透明图覆盖**全部** `Input.CursorShape`（17 种），不止 Arrow |
| HiDPI / 内容缩放下坐标偏移 | 用 `Sprite2D.GetGlobalMousePosition()`（与 GlobalPosition 经同一 canvas transform，自洽），不要手算屏幕坐标 |
| CI/无头环境 | 检测 `DisplayServer.GetName()=="headless"` 或命令行 `--headless` → 跳过 overlay 与光标应用，放行原版逻辑 |

## 设计要点

- **补丁独立性**：每个 hook 单独 try-catch，单个签名变化只禁用该功能，不让整个 initializer 抛异常导致 mod 加载失败。
- **多时机 EnsureStarted**：`NGame._Ready` + `NCursorManager._EnterTree/_Ready` 多个 postfix 都触发启动，容错初始化时序。
- **抑制原版行为**：hook 原版私有方法用 prefix 返回 `false` 跳过方法体（如 `NCursorManager.UpdateCursor`）。
- **截图可见**：渲染在游戏 framebuffer 内 → 截图/录屏能捕获光标（硬件光标不行）。
