# 设置界面（ModConfig）— 纯原生方案

> **2026-10-02 升级：本模块已从「BaseLib 例外」转正为纯原生方案**（零第三方依赖，真编译验证）。
> 方案来源：精读 BaseLib `Config/` 源码（`SimpleModConfig`/`ModConfigRegistry`/`NModConfigSubmenu`）后提炼——核心思想是「静态属性 + Attribute 描述 + 反射动态生成 UI + 文件持久化」，全部用 Godot 原生控件与游戏原生 Patch 点实现。

## 章节导航

| 内容 | 文件 |
|------|------|
| 配置声明与持久化 | [settings-core.md](settings-core.md) |
| Attribute、UI 生成与主菜单注入 | [settings-attributes.md](settings-attributes.md) |

## 方案组成

1. **声明**：静态类 + 自研 `[ConfigSection]`/`[ConfigSlider]`/`[ConfigIgnore]` Attribute 描述配置项
2. **持久化**：Godot 原生 `ConfigFile` → `user://mod_configs/<ModId>/config.cfg`（首次运行用默认值）
3. **UI**：继承游戏原生 `NSubmenu` 的自研子菜单，反射遍历属性动态生成 `CheckButton`（bool）/`HSlider`（double）+ 分组标题
4. **注入**：`NMainMenuSubmenuStack.GetSubmenuType` Prefix 注册子菜单 + `NMainMenu._Ready` Postfix 复制 Settings 按钮加入主菜单

> 可编译完整示例：`sts2-mod-examples/Sts2ModExamplesCode/Settings/`（ModConfig/NModConfigSubmenu/ModConfigPatches）。

## 常见问题

| 问题 | 解决 |
|------|------|
| 设置不显示在菜单 | 检查 `GetSubmenuType` Patch 是否生效 + 按钮本地化键存在 |
| 属性不显示 UI | 检查类型是否支持（bool→CheckButton，double→HSlider） |
| 本地化不生效 | 按钮键放 `main_menu_ui` 表（`SetLocalization` 查该表） |
| 保存不生效 | `ConfigFile.Save` 路径 `user://mod_configs/<ModId>/config.cfg`；加载在 ModEntry 初始化 |
| 重复设置按钮 | `NMainMenu._Ready` Postfix 每次进主菜单都跑——用 `GetNodeOrNull` 判重 |

## 演进路线

- 旧方案：BaseLib `SimpleModConfig`（唯一第三方例外，已废弃）
- **当前（2026-10-02）：纯原生方案**（自研 Attribute + ConfigFile + NSubmenu UI，真编译验证）
- 后续：enum 下拉、颜色选择、条件显示（`[ConfigVisibleIf]`）可按需扩展
