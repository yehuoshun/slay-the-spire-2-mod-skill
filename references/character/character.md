# 自定义角色

> 参考：[杀戮尖塔2模组开发教程08 - 自定义角色 - 哔哩哔哩](https://www.bilibili.com/opus/1182961747166756931)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomCharacterModel.cs`


## 章节导航

| 内容 | 文件 |
|------|------|
| 卡池/遗物池/药水池 | [character-pools.md](character-pools.md) |
| 角色类与资源路径 | [character-core.md](character-core.md) |
| 场景、Spine、注册与本地化 | [character-assets.md](character-assets.md) |
| 纯原生一键注册 | [character-advanced.md](character-advanced.md) |

## 概述

自定义角色除了继承 `CharacterModel` 抽象类，还需要定义卡池、遗物池、药水池，以及大量场景/纹理/材质资源。

> 角色**没有 `Name` 属性**——名字走本地化（`characters` 表 `<ID>.title`）。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 角色不在选择界面 | 检查 `ModelDb.AllCharacters` Patch（ref IEnumerable） |
| 卡牌样式不对 | 检查 `CardFrameMaterialPath` 材质路径 + 着色器 |
| 能量图标不显示 | 检查 `EnergyColorName` 对应资源 |
| 角色场景报错 | 根节点必须继承 `NCreatureVisuals` |
| 能量计数器报错 | Label 必须使用 MegaLabel |
| 卡牌拖尾无效 | 根节点必须使用 `NCardTrailVfx` |
| Spine 动画不识别 | 后缀改为 `.spine-json` |
| 导出时报错 | 忽略 `addons/*` 防止插件重复定义 |
| 音效缺失 | 指定复用其他角色音效路径 |

---

## 演进路线

- 当前：手动 Patch 注入（保留为基准写法）
- 纯原生一键注册（Attribute 标记 + 反射）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`Name`、`Gender.Male/Female/Other`、`public GenerateAllCards/Relics/Potions`、`new MyCardPool()`、`ref List` Patch、`ScriptManagerBridge`）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖
