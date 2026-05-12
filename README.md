# 🗡️ 杀戮尖塔 2 Mod 开发技能

从零开发 Slay the Spire 2 模组的实用指南。

[![Version](https://img.shields.io/badge/version-v0.3.0-blue)](https://github.com/yehuoshun/slay-the-spire-2-mod-skill)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-purple)]()

## 能做什么

- **项目搭建** — .csproj 模板、mod_manifest.json、入口点
- **Harmony 补丁** — 固定目标 / 动态目标 / 方法体替换
- **自定义卡牌** — 链式配置基类 + 自动注册系统
- **自定义修正器** — SyncedModifierModel 模式 + 联机同步
- **Godot UI** — CanvasLayer 挂载、Canvas 绘制、主题字体捕获
- **联机适配** — Host/Client 检测、交互锁定、消息同步
- **实用工具** — 防重复处理、弱引用存储、反射操作

## 快速开始

```bash
cd ~/.openclaw/workspace/skills
git clone https://github.com/yehuoshun/slay-the-spire-2-mod-skill.git
```

对话触发：

```
帮我写一个 STS2 Mod，添加一张自定义卡牌
给我一个 STS2 Mod 的项目模板
怎么给 STS2 写 Harmony 补丁
```

## 文件结构

```
├── SKILL.md          # 技能核心 — 11 章节开发指南
├── README.md         # 本文件
└── .github/          # CI 工作流
```

## 技术参考

| 参考 Mod | 作者 | 重点 |
|---------|------|------|
| [STS2Plus](https://github.com/StephenSHorton/STS2Plus) | 4step | 修正器架构、UI 组件、联机同步 |
| [STS2-ShunMod](https://github.com/yehuoshun/STS2-ShunMod) | yehuoshun | 自动注册、自定义卡牌 |

## 许可证

MIT

## 作者

**yehuoshun** 和卷王龙虾，干就完了 🦞
