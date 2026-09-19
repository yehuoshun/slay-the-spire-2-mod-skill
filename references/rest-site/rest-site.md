# 自定义休息点（RestSite）

> 篝火休息点，玩家在 RestSite 可以选择操作：休息（回血）、锻造（升级卡牌）、回忆（拿回消耗牌）等。

## 概述

RestSite 的每个操作是一个 `RestSiteOption` 子类。游戏原生有：

| 原生选项 | 类 | 说明 |
|---------|-----|------|
| 休息 | `RestOption` | 回 30% HP |
| 锻造 | `SmithOption` | 升级一张卡牌 |
| 回忆 | `RecallOption` | 从消耗堆拿回一张牌 |
| 挖矿 | `TokeOption` | 移除一张牌 |

自定义休息点选项就是继承 `RestSiteOption` 添加新的操作类型，比如「交易」「祝福」「特殊强化」。

## 章节导航

| 内容 | 文件 |
|------|------|
| 创建自定义选项 + 图标 | [rest-site-options.md](rest-site-options.md) |

## 常见问题

| 问题 | 解决 |
|------|------|
| 选项不显示 | 检查 `room.AddRestSiteOption()` 是否调用 |
| 图标不显示 | `IconPath` 返回空或路径错误 |
| 选项点了没反应 | `OnOptionSelected` 回调未实现或抛出异常 |
| 选项不触发你自定义的model | 检查 ModelId, 你要确保继承了 RestSiteOption