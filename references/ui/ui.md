# 手牌 / 战斗 UI 覆写

> 实战验证：sts2mod 手牌上限解除 RemoveHandLimit v0.3.1（2026-10-07）。

## 章节导航

| 内容 | 文件 |
|------|------|
| 手牌 UI 布局覆写（双排/上限/快捷路径） | [ui-hand-layout.md](ui-hand-layout.md) |
| Async 状态机 Transpiler + Prefix 短路协同 | [../harmony/harmony-transpiler-async.md](../harmony/harmony-transpiler-async.md) |

## 概述

战斗/手牌 UI 覆写 = 游戏内节点级 patch（NPlayerHand/NHandCardHolder/HandPosHelper 等）。与内容型 mod 不同，UI 覆写的关键是：

1. **布局数据**：能偷原版查表就别手写曲线（HandPosHelper 扇形表）
2. **边界接管**：原版逻辑假定 10 张手牌（快捷槽/焦点/布局），超限必须 prefix 短路接管
3. **多 mod 撞车**：Priority.High + 返回 false 抢占；transpiler 加 expectedCount 断言防静默失效

## 常见问题

| 问题 | 解决 |
|------|------|
| ZIndex 不生效 | 容器 `YSortEnabled` 关掉（否则覆盖 ZIndex） |
| 上排卡点不到 | 上排 hitbox 高度压缩（×0.56）+ 原 rect 按 GetInstanceId 缓存 |
| 快捷键越界 | `StartCardPlay` prefix 全接管（手柄玩家放行） |
| 与其他 UI mod 冲突 | 关键 prefix 一律 `Priority.High/First` + 返回 false 短路 |
| async 方法 transpiler 不生效 | 打 `<X>d__N.MoveNext`（GetAsyncStateMachineTarget），不是方法本体 |
| 版本更新后常量调用点变化 | expectedCount 断言启动报错显形 |