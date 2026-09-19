# 自定义模组徽章（Badge）

> 纯原生实现。灵感来源：BaseLib `CustomBadge`。

## 概述

徽章在游戏结束画面显示，代表玩家完成特定成就（如「首次通关」「无伤 Boss」等）。自定义徽章允许添加新的成就图标到集勋章界面。

## 章节导航

| 内容 | 文件 |
|------|------|
| 创建徽章 + 图标 + 注册 | [badge-core.md](badge-core.md) |

## 常见问题

| 问题 | 解决 |
|------|------|
| 徽章不显示 | 检查 `BadgePool.CreateAll` Postfix 是否注册 |
| 图标不显示 | 检查 `IconPath` 路径 + `NBadge.Create` Transpiler |
| 徽章条件不触发 | `IsObtained` 方法逻辑有误 |

## 演进路线

- 当前：手动继承 + Postfix 注册（纯原生）
- 更优：BaseLib 的 `CustomBadge` + `[CustomEnum]` 自动扫
- 纯原生缺点：`Badge` 构造函数分支版本不同（BaseLib 用 IL Emit 处理）