# 自定义 Modifier（运行规则）

> 参考：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomModifierModel.cs` + `STS2Plus` 源码


## 章节导航

| 内容 | 文件 |
|------|------|
| 模板与可覆盖成员 | [modifier-core.md](modifier-core.md) |
| 本地化、注册与序列化 | [modifier-register.md](modifier-register.md) |
| 效果实现与完整示例 | [modifier-effects.md](modifier-effects.md) |
| 纯原生注册工厂 | [modifier-advanced.md](modifier-advanced.md) |

## 概述

Modifier 是 Custom Run 界面中可勾选的运行规则（如"敌人 HP 翻倍"、"开局 1 血"）。继承 `ModifierModel` 抽象类。

Modifier 定义**元数据**（标题/描述/图标，全部自动从 `modifiers` 本地化表生成），效果通过 **override 钩子**（`AfterRunCreated` + AbstractModel 的 `Modify*` 系列）直接实现——**不需要 Harmony Patch**。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| Modifier 不显示在 Custom Run | 检查 `GetModifiersTickedOn` Patch |
| Modifier 效果不生效 | 检查 override 的 `Modify*`/`AfterRunCreated` 钩子签名 |
| 标题/描述不显示 | 检查 `modifiers.json` 键与类名一致，必须 Publish |
| 想限制同组互斥 | 原生无互斥分组（`MutuallyExclusiveGroup` 是 BaseLib 的），需自行实现 |
| 多人不同步 | 实现自定义网络消息同步规则选择 |

---

## 演进路线

- 当前：手动 Patch 注入（保留为基准写法）
- 纯原生自动注册工厂（Attribute 标记 + 反射收集）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`ModifierAlignment`/`Alignment`/`MutuallyExclusiveGroup`/`SortOrder`、效果用 Harmony Patch + IsModifierActive、Patch `FromSerializable`）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖
