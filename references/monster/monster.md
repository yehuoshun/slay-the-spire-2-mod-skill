# 自定义敌怪 / 遭遇

> 参考：[杀戮尖塔2模组开发教程09 - 自定义敌怪 - 哔哩哔哩](https://www.bilibili.com/opus/1183380755590414377)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomEncounterModel.cs` + `CustomMonsterModel.cs`


## 章节导航

| 内容 | 文件 |
|------|------|
| 怪物类与模型 | [monster-core.md](monster-core.md) |
| 状态机与 AI 行为树 | [monster-ai.md](monster-ai.md) |
| 资源、遭遇与添加 | [monster-encounter.md](monster-encounter.md) |
| 纯原生注册辅助 | [monster-advanced.md](monster-advanced.md) |

## 概述

自定义敌人需要两个类：

1. **MonsterModel** — 怪物本身（血量、AI 状态机、资源）
2. **EncounterModel** — 遭遇（怪物池、站位、奖励档位）

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 怪物不攻击 | 检查 `GenerateMoveStateMachine`（protected + 初始状态对象） |
| 意图不显示 | `MoveState` 第三参传 Intent 实例（`new SingleAttackIntent(dmg)`） |
| 随机分支不生效 | `AddBranch(state, cooldown, MoveRepeatType, () => weight)` |
| 条件分支不触发 | `AddState(move, () => condition)`，按顺序判断，先精确后默认 |
| 遭遇不出现 | 检查 `GenerateAllEncounters` Patch（ref IEnumerable，非 getter） |
| 遭遇只在特定章节 | 章节有效性由章节配置控制（`IsValidForAct` 是 BaseLib 的） |
| 怪物不显示血条 | `IsHealthBarVisible = false`（宠物） |

---

## 演进路线

- 当前：手动 Patch 注入（保留为基准写法）
- 纯原生自动注册（Attribute 标记 + 反射统一注入）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`MoveState(Func<PlayerChoiceContext,Task>, List<IntentType>)`、`MonsterMoveStateMachine(字符串)`、`public GenerateMonsters`、`IsValidForAct/CustomScenePath`、`RepeatType`、`ref List` Patch）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖
## 调试

```
encounter <遭遇ID>
```

---

