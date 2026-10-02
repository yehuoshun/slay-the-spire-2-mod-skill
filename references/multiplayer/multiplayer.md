# 多人模式（Multiplayer）

> 实战项目验证（YuWanCard）。STS2 原生支持多人（host/join），mod 内容需显式声明多人行为，否则联机下可能出 bug。本模块为横切主题（卡牌/遗物/能力/网络消息都会涉及）。

## 章节导航

| 内容 | 文件 |
|------|------|
| 约束、身份检查与网络消息 | [multiplayer-core.md](multiplayer-core.md) |

## 概述

多人下同一段代码在所有客户端执行，三件事必须处理：

1. **声明约束**：内容是否允许出现在多人（`MultiplayerConstraint`）
2. **身份检查**：只对本地玩家生效的副作用包 `LocalContext.IsMe`
3. **跨端同步**：自定义数据用 `INetMessage` 网络消息

本地化还有第四个点：能力 `remoteDescription`（多人视角描述）。

## 常见问题

| 问题 | 解决 |
|------|------|
| 多人下效果执行多次 | 副作用没包 `LocalContext.IsMe` |
| 跨端数据不同步 | 用 `INetMessage` + `NetTransferMode.Reliable` 定向发送 |
| 卡牌只在单人/多人出现 | 覆写 `MultiplayerConstraint` |
| 对方看能力描述乱 | 补 `remoteDescription` 字段 |

## 演进路线

- 2026-10-02 新增本模块（学自 YuWanCard 多人实现）
- 后续：角色多人起始遗物、多人专属内容注册可继续补充
