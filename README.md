# 🗡️ 杀戮尖塔 2 Mod 开发技能

从零开发 Slay the Spire 2 模组的实用指南。

[![Version](https://img.shields.io/badge/version-v0.4.0-blue)](https://github.com/yehuoshun/slay-the-spire-2-mod-skill)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-purple)]()

## 能做什么

- **项目搭建** — Megadot 环境、PCK 导出、DLL 构建、安装调试
- **Harmony 补丁** — 固定目标 / 动态目标 / 方法替换
- **自定义卡牌与修正器** — 链式基类 + 自动注册系统
- **Godot UI** — CanvasLayer 挂载、Overlay 阻塞检测
- **联机适配** — Host/Client 检测、交互锁定
- **实用工具** — 反射操作、防重复处理、弱引用存储、本地化

## 快速开始

```bash
cd ~/.openclaw/workspace/skills
git clone https://github.com/yehuoshun/slay-the-spire-2-mod-skill.git
```

对话触发（STS2 = 杀戮尖塔2）：

```
帮我写一个杀戮尖塔2 Mod
给我一个 STS2 Mod 的项目模板
怎么给 STS2 写 Harmony 补丁
```

## 文件结构

```
├── SKILL.md      # 10 章节开发指南
├── README.md     # 本文件
└── .github/      # CI 工作流
```

## 技术参考

[STS2Plus](https://github.com/StephenSHorton/STS2Plus) — 修正器架构、UI 组件、联机同步

## 许可证

MIT
