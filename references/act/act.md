# 自定义章节（Act）

> 参考：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomActModel.cs`


## 章节导航

| 内容 | 文件 |
|------|------|
| 模板与可覆盖成员 | [act-core.md](act-core.md) |
| 注册、本地化与自动注册 | [act-register.md](act-register.md) |

## 概述

章节（Act）控制游戏每章的地图背景、音乐、宝箱、房间数量等。继承 `ActModel` 抽象类。

原版章节：Act 1（密林 Overgrowth）、Act 2（蜂巢 Hive）、Act 3（荣耀 Glory）等。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 自定义章节不生效 | 检查 `ModelDb.Inject` 是否调用 |
| 地图背景不显示 | 资源放 `packed/map/map_bgs/<id小写>/`（命名约定，非 override） |
| 音乐不播放 | 检查 `BgMusicOptions` / `MusicBankPaths` 路径 |
| 宝箱不显示 | 检查 `ChestSpineSkinName*` 与 `ChestSpineResourcePath` |
| 房间数量不对 | 重写 `BaseNumberOfRooms` |
| 先古之民不出现 | 在 `AllAncients` 中添加 |
| 缺抽象成员报错 | 必填：`Index`/`IsDefault`/`Map*Color`/`BgMusicOptions`/`ChestSpine*`/`BossDiscoveryOrder`/`AllAncients`/`GenerateAllEncounters` |

---

## 演进路线

- 当前：手动注册（保留为基准写法）
- 纯原生自动注册（Attribute 标记 + ContentRegistry）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`MapTopBgPath` 等非 virtual override、缺 `IsDefault`/`BossDiscoveryOrder`/`GenerateAllEncounters` 必填、本地化 `name`）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖
