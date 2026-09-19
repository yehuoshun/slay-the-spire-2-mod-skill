# 自定义能量

> 能量图标、能量类型、能量视觉效果的自定义。

## 章节导航

| 内容 | 文件 |
|------|------|
| 自定义能量图标（导航页） | [energy-custom-icon.md](energy-custom-icon.md) |
| ├ 接口、辅助类与使用 | [energy-custom-icon-core.md](energy-custom-icon-core.md) |
| ├ 大/文本图标 Patch | [energy-custom-icon-patches.md](energy-custom-icon-patches.md) |
| └ 资源准备与常见问题 | [energy-custom-icon-resources.md](energy-custom-icon-resources.md) |

## 概述

卡牌的左上角显示能量图标（默认是一个带颜色的圆，颜色由所属卡池的 `EnergyColorName` 决定）。需要自定义图标时（如紫球能量、元素标记等），通过 Harmony Patch 替换图标解析逻辑即可。

BaseLib 提供了 `ICustomEnergyIconPool` 接口 + 两个 Patch 实现。**纯原生完全等价实现**——自己写接口 + 自己写 Patch。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 图标不显示 | 检查 `.tres` / `.png` 路径是否正确，游戏是否读取到 |
| 大图标换了，文本图标没换 | 两个 Patch（`GetPath` + `TryEvaluateFormat`）都要写 |
| 图标颜色不对 | 自定义图标不受 `EnergyColorName` 着色影响 |
| 只影响自己的池 | 通过自定义分隔符编码 ModelId，只匹配自己的池 |