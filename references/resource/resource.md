# 自定义资源（Custom Resource）

> 纯原生实现。灵感来源：BaseLib `CustomResource`。

## 概述

自定义资源是**能量之外的战斗资源**，比如「法力」「怒气」「连击点」。卡牌可以消耗这些资源代替或叠加能量费用，资源会在 UI 上实时显示。

核心组成：
1. **资源本身** — 基础值、最大值、每回合重置规则
2. **卡牌费用** — 卡牌消耗该资源才能打出
3. **UI 显示** — 战斗中显示资源数值/条/图标

## 章节导航

| 内容 | 文件 |
|------|------|
| 资源基类 + 注册 + 生命周期 | [resource-core.md](resource-core.md) |
| 资源费用 + UI 显示 | [resource-cost.md](resource-cost.md) |

## 常见问题

| 问题 | 解决 |
|------|------|
| 资源值不重置 | `StartOfTurnReset` 未被调用或实现不正确 |
| 卡牌不显示资源费用 | 检查 `CustomResources<T>.SetCanonicalCost` 是否调了 |
| UI 不显示 | 资源 UI 需要手动添加到战斗场景，或 Patch `ExtraCombatUi` |
| 多人不同步 | 资源数据需同步；BaseLib 有 `SpireField`，纯原生可用 `PlayerCombatState` 扩展字段