# 先古之民事件（Ancient）

> 参考：[杀戮尖塔2模组开发教程06 - 自定义事件 - 哔哩哔哩](https://www.bilibili.com/opus/1180714323922649110)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomAncientModel.cs`


## 章节导航

| 内容 | 文件 |
|------|------|
| 模板与可覆盖成员 | [ancient-core.md](ancient-core.md) |
| 对话、纹理与注册 | [ancient-register.md](ancient-register.md) |
| 纯原生注册辅助 | [ancient-advanced.md](ancient-advanced.md) |

## 概述

先古之民出现在每一章开头，继承 `AncientEventModel`。相比普通事件，额外要求实现对话系统（`DefineDialogues`）和选项列表（`AllPossibleOptions`）。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 先古之民不出现 | 检查 `AllAncients` Patch（ref IEnumerable）+ 5 种纹理资源 |
| 对话不显示 | `DefineDialogues` 对象初始化器正确 + `AncientDialogue` 每行 sfx 路径 |
| 选项不出现 | 检查 `AllPossibleOptions` 返回 |
| 音效不播放 | 检查 SFX 路径（每行一个，空行用 `""`） |

---

## 演进路线

- 当前：手动 Patch 注入（保留为基准写法）
- 纯原生自动注册（Attribute 标记 + 反射统一注入）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`new AncientDialogueSet(3参)`、`OptionPools/IsValidForAct/ShouldForceSpawn`、`ref List` Patch）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖
