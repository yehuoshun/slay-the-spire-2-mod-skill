# 自定义药水

> 参考：[杀戮尖塔2模组开发教程04 - 自定义药水 - 哔哩哔哩](https://www.bilibili.com/opus/1180032536494997541)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomPotionModel.cs`


## 章节导航

| 内容 | 文件 |
|------|------|
| 模板、图标与属性 | [potion-core.md](potion-core.md) |
| 回调 | [potion-callbacks.md](potion-callbacks.md) |
| 池、图标、本地化与自动注册 | [potion-register.md](potion-register.md) |

## 概述

所有药水继承 `PotionModel` 抽象类。`Rarity`/`Usage`/`TargetType` 是**抽象属性**（必须 override），使用效果在 `OnUse` 回调实现。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 药水不显示 | 检查手动注册或 Attribute 是否生效 |
| 图标不显示 | 检查 atlas 命名约定：`atlases/potion_atlas.sprites/<药水ID小写>.tres` |
| 本地化不生效 | 必须 Publish 而非 Build |
| 药水无法使用 | 检查 `Usage` 属性是否匹配当前场景 |
| 自动药水不触发 | `Usage = PotionUsage.Automatic`，只能代码触发 |
| 随机生成不想要 | 重写 `CanBeGeneratedInCombat` 返回 `false` |

---

## 演进路线

- 当前：手动注册（保留为基准写法）
- **纯原生自动注册**（见上方「进阶」章节）——Attribute 标记 + ContentRegistry / 反射注册辅助，免手动注册
- v3（本版）：API 全量校正，弃用 v2 的错误签名（PotionTarget/ShouldDie/DamageCmd.FromCard）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖
