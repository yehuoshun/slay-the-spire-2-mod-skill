# 自定义能力（Buff / Debuff）

> 参考：[杀戮尖塔2模组开发教程07 - 自定义能力（Buff） - 哔哩哔哩](https://www.bilibili.com/opus/1181126133981118470)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomPowerModel.cs`


## 章节导航

| 内容 | 文件 |
|------|------|
| 模板、图标命名与核心属性 | [power-core.md](power-core.md) |
| 真实回调与本地化 | [power-callbacks.md](power-callbacks.md) |
| 临时能力、调试与自动注册 | [power-advanced.md](power-advanced.md) |

## 概述

所有能力/Buff/Debuff 继承 `PowerModel` 抽象类。`Type`/`StackType` 是**抽象属性**（必须 override），行为通过真实回调（`ModifyDamageAdditive` 等 + AbstractModel 的 `After*` 钩子）实现。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 能力不生效 | 检查是否注册（attribute 自动注册 或 `ModelDb.Inject`） |
| 层数不减 | 用 `PowerCmd.Decrement` 或 `SetAmount(int, bool)`，不要直接改字段 |
| 图标不显示 | 检查 atlas 命名约定：`power_atlas.sprites/<能力ID小写>.tres` |
| 本地化不生效 | 使用 `smartDescription` 而非 `description` 显示动态变量 |
| Buff 不消失 | `AllowNegative = false`，层数归零自动移除 |
| 独立实例不生效 | `InstanceType = PowerInstanceType.Instanced` |
| 需要每次施加固定层数 | `PowerCmd.Apply<T>(choiceContext, target, amount, applier, cardSource)` |

---

## 演进路线

- 当前：手动注册（保留为基准写法）
- 纯原生自动注册（Attribute 标记 + ContentRegistry）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（PowerStackType.Stacks/Boolean、IsInstanced、OnCardPlayed/OnTurnEnd 等编造回调、DamageCmd.GainBlock、CustomPackedIconPath）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖
