# 自定义事件

> 参考：[杀戮尖塔2模组开发教程06 - 自定义事件 - 哔哩哔哩](https://www.bilibili.com/opus/1180714323922649110)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomEventModel.cs`


## 章节导航

| 内容 | 文件 |
|------|------|
| 模板、资源与选项构造 | [event-core.md](event-core.md) |
| 本地化、背景图与添加 | [event-register.md](event-register.md) |
| 多页选项与注册辅助 | [event-advanced.md](event-advanced.md) |

## 概述

所有事件继承 `EventModel` 抽象类。事件是玩家进入问号房间后随机发生的，核心由 `GenerateInitialOptions` 返回选项列表；选项回调是无参 `async Task` 方法，通过 `SetEventState`/`SetEventFinished` 控制流程。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 事件不触发 | 检查 `Overgrowth.AllEvents` Patch 是否生效（ref IEnumerable） |
| 选项不可点击 | 检查回调是否为 null / `disableOnChosen` 设置 |
| 肖像图不显示 | 检查命名约定 `images/events/<事件ID小写>.png` |
| 本地化不显示 | 检查 events.json 键格式（`pages.<STATE>.options.<KEY>`），必须 Publish |
| 背景图不显示 | PNG 命名与事件 ID 小写一致 |
| 多页切换不生效 | 回调里调 `SetEventState(LocString, 新选项)` 双参 |
| 拿玩家/金币 | 事件用 `Owner`（Player?），加金币 `PlayerCmd.GainGold` |

---

## 演进路线

- 当前：手动 Patch 注入（保留为基准写法）
- 纯原生自动注册（Attribute 标记 + 反射统一注入）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`Task<List<EventOption>>`、`EventOption(LocString, Func<...>)`、`SetEventState/Finished(string)`、`GetStateOptions`、`ref List` Patch）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖
