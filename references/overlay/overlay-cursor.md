# PRTSCursor 案例精读：动态光标 overlay


> 上游：`s1f102500012/sts2mod/PRTS动态光标 PRTSCursor`，v1.0.3
> 功能：把默认鼠标箭头替换为 P.R.T.S 风格 60 FPS 动态光标，常态与点击均为 PRTS 风格
> 已验证：`NCursorManager`（`MegaCrit.Sts2.Core.Nodes.CommonUi`）`UpdateCursor()` 确为 private instance 方法

## 整体架构

```
系统光标（全透明 1x1） ← 原版想画什么都被 透明图 挡住
        ↕  Harmony prefix 抑制 UpdateCursor()
游戏内 overlay：CanvasLayer(4096) + Sprite2D  ← SceneTree.ProcessFrame 信号逐帧驱动
```

## Harmony 补丁点（全部独立 try-catch）

| 目标 | 时机 | 作用 |
|------|------|------|
| `NGame._Ready` | postfix | 启动 overlay（窗口就绪） |
| `NCursorManager._EnterTree` | postfix | 启动 overlay |
| `NCursorManager._Ready` | postfix | 启动 overlay |
| `NCursorManager.UpdateCursor`（private） | prefix → 返回 false | 抑制原版刷新系统光标 |

- 目标方法用 `Type.GetMethod(name, flags, binder:null, parameters, modifiers:null)` 精确查找，找不到抛异常被 TryPatch 吞掉（只 Warn 不断加载）。
- 私有方法定位：`BindingFlags.Instance | BindingFlags.NonPublic`。

## 透明系统光标

```csharp
Image image = Image.CreateEmpty(1, 1, useMipmaps: false, Image.Format.Rgba8);
image.Fill(new Color(0f, 0f, 0f, 0f));   // 全透明
foreach (Input.CursorShape shape in AllCursorShapes)  // 17 种形状全覆盖
    Input.SetCustomMouseCursor(image, shape, Vector2.Zero);
```

原版 `NCursorManager` 只驱动 Arrow（外加 Help 检查光标），但任何控件可请求其他形状 —— 全覆盖保证屏幕只有 PRTS 光标。每次 `UpdateCursor` prefix 里 `force:true` 重设，防引擎重置（手柄→鼠标切换后）。

## 动画驱动

```csharp
// 帧推进：Time.GetTicksMsec + 累加器，delta clamp 0~0.25s，60fps 步进
_frameAccumulator += delta;
int steps = (int)(_frameAccumulator / (1.0 / 60.0));
if (steps > 0) { _frameAccumulator -= steps * (1.0 / 60.0); _frameIndex = (_frameIndex + steps) % _frames.Length; }
_sprite.Texture = _frames[_frameIndex];
```

- 帧纹理数组从 `res://PRTSCursor/cursors/default/default_NN.png` 顺序加载（`ResourceLoader.Load<Texture2D>(CacheMode.Reuse)`，失败 fallback `Image.LoadFromFile` + `ImageTexture.CreateFromImage`；`MaxFrameCount=4096` 上限，断档 break）。
- 位置：`_sprite.GetGlobalMousePosition() - Hotspot`（Hotspot = (29,37)），NaN/Infinity 直接隐藏；`Input.MouseMode == Hidden`（手柄）时隐藏。

## 原版公共 API 对照（先想清楚再选路）

`NCursorManager` 其实有公共方法：

```csharp
public void OverrideCursor(Image cursorTilted, Image cursorNotTilted, Vector2 hotspot)
public void StopOverridingCursor()
```

| 方案 | 适用 | 限制 |
|------|------|------|
| `OverrideCursor` | 静态图片替换 Arrow（tilted/未tilted 两态） | 只有 Arrow 形态；静态图，动画需每帧刷新系统光标 |
| 全 overlay 方案 | 动画光标 | 需自建 CanvasLayer + 信号驱动（本案例） |

> 作者 1.0.1 试过「每帧刷新系统光标」，部分机器闪烁、macOS 掉帧 → 最终全 overlay。**做动画光标直接上 overlay，别走系统光标逐帧刷新。**

## 资源打包（PCK 侧差异点）

- csproj：`net9.0` + `Microsoft.NET.Sdk`，引 sts2.dll / GodotSharp.dll / 0Harmony.dll 全 `Private=false`；构建后校验 bin 里**只有主 DLL**，多 DLL 直接报错退出。
- 资源导入：独立最小 import 工程（`project.godot` + rsync assets）→ `Godot --headless --path <import> --import` 生成 `.godot/imported` 缓存。
- 打包：**用游戏本体二进制**跑 `pack_mod.gd`（`"$GAME_BIN" --headless --path tools -s res://pack_mod.gd -- manifest out.pck import_root`），`PCKPacker` 打包，`.md5` 跳过，路径前缀 `res://{mod_id}/`。
- manifest：`has_pck:true` `has_dll:true` `affects_gameplay:false` `min_game_version:"0.107.1"`；发布 zip 校验恰好 3 个条目（dll/json/pck）。
