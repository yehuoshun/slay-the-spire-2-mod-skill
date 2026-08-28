# 设置界面（ModConfig）

> ⚠️ **本模块是整库唯一允许的第三方依赖（BaseLib）。**
> 设置界面（玩家可调的 mod 选项 UI）纯原生需手写整个 Godot UI（控件/双向绑定/持久化），成本极高且不属于玩法核心（卡牌/遗物/能力才是）。多次纯原生自建测试均出现大量 bug，故此处明确允许使用 BaseLib 的 `SimpleModConfig`。
> **不想装 BaseLib 的 mod，设置界面直接不做也不影响运行。**

> 参考：BaseLib 源码 `Config/` 目录


## 章节导航

| 内容 | 文件 |
|------|------|
| 基础使用 | [settings-core.md](settings-core.md) |
| Attribute、示例与本地化 | [settings-attributes.md](settings-attributes.md) |

## 概述

BaseLib 提供了一套完整的设置界面系统。通过 `SimpleModConfig` + 属性 Attribute，自动生成配置 UI，无需手动写 Godot 控件。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 设置不显示在菜单 | 检查 `ModConfigRegistry.Register` 是否调用 |
| 属性不显示 UI | 检查类型是否支持（bool/slider/string/color/enum） |
| 本地化不生效 | 检查 `settings_ui.json` 键格式 |
| 条件显示不生效 | 检查 `[ConfigVisibleIf]` 目标属性名 |
| 保存不生效 | 配置文件在 `user://mod_configs/<mod>.cfg` |

---

## 演进路线

- 当前方案：BaseLib `SimpleModConfig` + Attribute 自动 UI
- 纯原生方案：暂无成熟方案（需自建 Godot UI）
- 第三方方案：ModConfig API（需额外依赖）
