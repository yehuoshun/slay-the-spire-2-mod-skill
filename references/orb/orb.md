# 自定义球体（Orb）

> 参考：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomOrbModel.cs`


## 章节导航

| 内容 | 文件 |
|------|------|
| 模板与可覆盖成员 | [orb-core.md](orb-core.md) |
| 雷电球完整示例 | [orb-example.md](orb-example.md) |
| 注册、本地化与自动注册 | [orb-register.md](orb-register.md) |

## 概述

球体是故障机器人（以及部分 Mod 角色）的充能球系统。继承 `OrbModel` 抽象类。每个球体有**被动效果**（回合结束触发）和**激发效果**（释放时触发）。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 球体不显示 | 检查命名约定 `orbs/<id小写>.png` + `orb_visuals/<id小写>` 场景 |
| 被动不触发 | 重写 `BeforeTurnEndOrbTrigger` → `Passive(PlayerChoiceContext, Creature?)` |
| 激发无效果 | 重写 `Evoke(PlayerChoiceContext)` → 返回 `IEnumerable<Creature>` |
| 音效不播放 | 检查 `PassiveSfx`/`EvokeSfx`/`ChannelSfx` 路径 |
| 随机球池 | 原生用 `OrbModel.GetRandomOrb(Rng)`（无 IncludeInRandomPool 属性） |
| 伤害没打出来 | 用 `CombatState.GetOpponentsOf(Owner.Creature)` + `CreatureCmd.Damage` |

---

## 演进路线

- 当前：手动注册（保留为基准写法）
- 纯原生自动注册（Attribute 标记 + ContentRegistry）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`Passive/Evoke(CombatState)`、`IconPath/SpritePath` override、`IncludeInRandomPool`、`.Random()` 扩展、`DamageCmd.Attack().FromCard(this)`）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖
