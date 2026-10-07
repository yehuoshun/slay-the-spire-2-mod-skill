# Run 生命周期扩展（无尽循环 / Interop / 配置）

> 实战验证：sts2mod 无尽模式 EndlessMode v0.4.1（2026-10-07）。通关后继续深入的「无尽模式」——建筑师事件进入、按轮发放遗物、跨轮重建 RunState。

## 章节导航

| 内容 | 文件 |
|------|------|
| 无尽循环：确定性种子 / RunState 重建 / 多人同步 | [run-endless-loop.md](run-endless-loop.md) |
| 公共 Interop + JSON 配置（位标志同步） | [run-interop-config.md](run-interop-config.md) |

## 概述

无尽模式 = 在终点事件追加「进入无尽」选项 → 新一轮（loop）：保留玩家进度，重建随机数/地图/章节，发放轮次遗物。要点：

- **确定性种子**：轮次种子从 当前 Acts+轮次+玩家数 哈希派生，多人两端一致
- **RunState 重建**：反射设置 init-only 属性（Rng/Odds/Map/Acts）+ 清历史
- **多人配置同步**：遗物开关用位标志经 PlayerChoiceSynchronizer 同步；同步不可用回退编译期默认（防分叉）

## 常见问题

| 问题 | 解决 |
|------|------|
| 轮次种子两端不一致 | 种子派生只用确定性输入（Acts Id/轮次/玩家数/基础种子），禁读本地配置 |
| init-only 属性改不动 | 反射 PropertyInfo.SetMethod（RunState.Rng/Odds 是 `{ get; init; }`） |
| 多人遗物发放分叉 | 主客本地配置不同 → 位标志经 choice 同步；不可用用编译期默认 |
| 重复进入无尽 | transitionKey 防重入 + 失败 30s 可重试 + 10min 远程等待上限 |