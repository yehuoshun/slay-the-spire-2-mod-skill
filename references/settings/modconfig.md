# 设置界面（第三方框架 ModConfig）— 通用模组配置框架

> **2026-10-07 学习**：精读 [xhyrzldf/ModConfig-STS2](https://github.com/xhyrzldf/ModConfig-STS2)（v0.2.2，commit 639eb97，MIT）源码提炼。作者「皮一下就很凡」。与「纯原生方案」并列的**第三种设置方案**，适合「多配置项 + 零开发成本 + 玩家生态成熟」的场景。

## 章节导航

| 内容 | 文件 |
|------|------|
| 零依赖反射接入（ModConfigBridge 模板） | [modconfig-integration.md](modconfig-integration.md) |
| 内部机制（Tab 注入 / 控件渲染 / 持久化 / KeyBind / i18n） | [modconfig-internals.md](modconfig-internals.md) |

## 三种设置方案选型

| 方案 | 依赖 | 适用场景 |
|------|------|---------|
| 纯原生（settings-core/attributes） | 零依赖 | 少量配置项、要求完全自控、不依赖生态 |
| BaseLib SimpleModConfig | BaseLib | 已有 BaseLib 依赖的 mod（旧例外，已废弃） |
| **ModConfig（本文）** | **零代码依赖**（反射桥） | 多配置项（9 种控件开箱即用）、玩家生态成熟（Nexus #27）、双语自动 |

## 核心特性

1. **零 Harmony、跨平台**（AnyCPU：Win/macOS/Linux）
2. **零依赖接入**：你的 mod 通过**反射**调用 ModConfig，不引用 DLL；玩家没装 ModConfig 时 mod 照常运行（GetValue 返回 fallback）
3. **9 种控件**：Toggle / Slider / Dropdown / KeyBind / TextInput / Button / ColorPicker / Header / Separator
4. 设置界面自动注入游戏 Settings 的 **「Mods」标签页**（克隆原生 tab + panel）
5. 自动持久化 `user://ModConfig/<modId>.json`（防抖保存，无需手动 Save）
6. 双语标签（en/zhs）自动检测语言，语言切换实时刷新
7. 上游已接入的开源 mod：Skada（Damage Meter）、SpeedX（22 设置）、Rewind、QuickLink
8. 安装（玩家侧）：`ModConfig.dll` + `ModConfig.pck` → `<Game>/mods/ModConfig/`

## 常见问题

| 问题 | 解决 |
|------|------|
| 配置项不显示 | 用延迟注册（`DeferredRegister()` 等 1 帧）——你的 mod 可能比 ModConfig 先加载（字母序） |
| `OnChanged` 不触发 | 必须通过反射 `SetProp` 设置 `OnChanged` 属性（模板 `Set(cfg, "OnChanged", ...)`） |
| KeyBind 取值类型 | `long`（Godot keycode + 修饰键位组合），从 object 强转 long |
| 配置没保存 | 自动保存 `user://ModConfig/<modId>.json`，无需手动 Save |
| Slider 格式不对 | 设 `Format`：`"F0"` 整数 / `"F1"` 一位小数 / `"P0"` 百分比 |
| mod 改了设置不同步 | 调 `ModConfigBridge.SetValue(key, value)` 同步回框架 |
| ModConfig 没装 | `IsAvailable == false`，所有 GetValue 返回 fallback，照常运行 |

## 与纯原生方案的关系

- 二者不冲突：ModConfig 框架本身就是个 mod；你的 mod 通过反射桥接
- 桥接模板只有 1 个文件（`examples/ModConfigBridge.cs`，319 行），复制即用
- 若你的 mod 已内建纯原生设置，无需迁移；ModConfig 适合「设置项很多、想省 UI 工作量」的场景
