# 自定义能量图标

> 纯原生实现（零第三方依赖）。灵感来源：BaseLib v3.4.7 `ICustomEnergyIconPool`。

## 概述

默认卡池的 `EnergyColorName` 决定了图标颜色和纹理路径。如果想用完全不同的图标（图案、特效图标、自定义绘制），通过 Harmony Patch 替换图标解析逻辑即可。

## 原理

游戏通过 `EnergyIconHelper.GetPath(string prefix)` 查找图标资源路径，`prefix` 就是 `EnergyColorName` 的值。文本中通过 `EnergyIconsFormatter` 渲染内联图标。

拦截策略：让池的 `EnergyColorName` 返回一个编码了 ModelId 的唯一字符串（`Category∴Entry`），Patch 看到是自己的编码就返回自定义路径，否则放行。

## 章节导航

| 内容 | 文件 |
|------|------|
| 接口、辅助类与使用 | [energy-custom-icon-core.md](energy-custom-icon-core.md) |
| 大图标 Patch & 文本图标 Transpiler | [energy-custom-icon-patches.md](energy-custom-icon-patches.md) |
| 资源准备、常见问题与演进 | [energy-custom-icon-resources.md](energy-custom-icon-resources.md) |

## 参见

- [energy.md](energy.md) — 能量模块导航
- [harmony-basics.md](../harmony/harmony-basics.md) — Prefix / Transpiler 语法