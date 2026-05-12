<h1 align="center">🗡️ 杀戮尖塔 2 Mod 开发技能</h1>

<p align="center">
  Slay the Spire 2 模组开发实用指南 — 可复用的模式、API、坑点。
</p>

<p align="center">
  <img alt="Version" src="https://img.shields.io/badge/version-v0.3.0-blue" />
  <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-green" />
  <img alt=".NET 9.0" src="https://img.shields.io/badge/.NET-9.0-purple" />
</p>

<p align="center">
  <a href="#能做什么">能做什么</a> ·
  <a href="#快速开始">快速开始</a> ·
  <a href="#项目结构">项目结构</a> ·
  <a href="#参考实现">参考实现</a> ·
  <a href="#许可证">许可证</a>
</p>

## 能做什么

- **从零搭建 Mod** — 项目结构、.csproj 模板、构建、安装
- **Harmony 补丁** — 固定目标 / 动态目标 / 替换方法体 三种模式
- **自动内容注册** — PoolAttribute + 扫描器，加新卡牌无需改入口
- **自定义卡牌** — ShunCard 链式配置基类 + 完整实现模板
- **自定义修正器** — SyncedModifierModel 模式 + 多规则状态管理
- **Godot UI** — Overlay 挂载、独立 CanvasLayer、Canvas 绘制、主题字体捕获
- **联机适配** — Host/Client 角色检测 + UI 交互锁定
- **实用工具** — AppliedTracker、ConditionalWeakTable、反射+事件触发

## 快速开始

### 1. 安装技能

```bash
cd ~/.openclaw/workspace/skills
git clone https://github.com/yehuoshun/slay-the-spire-2-mod-skill.git
```

### 2. 对话触发

```
帮我写一个 STS2 Mod，添加一张自定义卡牌
```

```
给我一个 STS2 Mod 的项目模板
```

AI 会读取本技能后给出代码。

## 项目结构

```
slay-the-spire-2-mod-skill/
├── SKILL.md           # 技能核心文档（AI 读取）
├── README.md          # 本文件
└── .github/
    └── workflows/     # 钉钉通知
```

## 参考实现

| Mod | 作者 | 重点参考 |
|-----|------|---------|
| [STS2Plus](https://github.com/StephenSHorton/STS2Plus) | 4step | 修正器架构、UI 组件、联机同步 |
| [STS2-ShunMod](https://github.com/yehuoshun/STS2-ShunMod) | yehuoshun | 自动注册、自定义卡牌、高级补丁 |

## 许可证

MIT

## 作者

**yehuoshun** 和卷王龙虾，干就完了 🦞
