# 杀戮尖塔2 Mod 开发

帮助开发杀戮尖塔2（Slay the Spire 2）Mod，提供模板、API、最佳实践指导。

## 快速开始

### 1. 环境准备

- 安装 .NET SDK
- 安装 Visual Studio 或 Rider
- 安装杀戮尖塔2游戏

### 2. 下载依赖

- [BaseLib](https://github.com/Alchyr/BaseLib-StS2/releases) - 前置依赖 mod
- 解压到 `Slay the Spire 2/mods` 目录

### 3. 创建项目

使用 [ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) 模板：

| 模板 | 用途 |
|-----|------|
| Slay the Spire 2 Mod | 空 mod 框架 |
| Slay the Spire 2 Content | 添加卡牌、遗物 |
| Slay the Spire 2 Character | 自定义角色 |

⚠️ 创建 solution 时勾选 **"Put solution and project in same directory"**

### 4. 开始开发

参考 [YuWanCard](https://github.com/YuWan886/Sts2-YuWanCard) 项目学习实现方式。

## 常见问题

**游戏日志位置**：`%AppData%\SlayTheSpire2\logs\`

**如何调试**：查看日志输出，或使用 IDE 断点调试

**API 在哪**：查看 [BaseLib Wiki](https://alchyr.github.io/BaseLib-Wiki/)

## 资源链接

- [BaseLib Wiki](https://alchyr.github.io/BaseLib-Wiki/)
- [ModTemplate Wiki](https://github.com/Alchyr/ModTemplate-StS2/wiki/Setup)
- [YuWanCard 参考项目](https://github.com/YuWan886/Sts2-YuWanCard)

## 社区

- QQ: 752913553
- Discord: https://discord.gg/tJT3a95Y8y
- PigHub: https://www.pighub.top/

---

## 更新日志

### v1.1 (2026-04-16)

**更新内容**：
- 更新 YuWanCard 项目信息（大量新卡牌、遗物、敌人、事件）
- 新增依赖项目链接（七咒之戒、Spire Codex）
- 新增游戏日志路径说明
- 新增多人模式支持说明
- 新增卡牌关键词参考
- 新增充能球、敌人、事件开发说明
- 完善资源链接汇总

### v1.0 (2026-04-14)

初始版本。