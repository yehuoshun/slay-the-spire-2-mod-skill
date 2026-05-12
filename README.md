<h1 align="center">🗡️ 杀戮尖塔 2 Mod 开发技能</h1>

<p align="center">
  从零开发 Slay the Spire 2 模组的实用指南 — 项目搭建、Harmony 补丁、自定义卡牌、修正器、Godot UI、联机适配。
</p>

<p align="center">
  <img alt="Version" src="https://img.shields.io/badge/version-v0.3.0-blue" />
  <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-green" />
  <img alt=".NET 9.0" src="https://img.shields.io/badge/.NET-9.0-purple" />
</p>

<p align="center">
  <a href="#能做什么">能做什么</a> ·
  <a href="#快速开始">快速开始</a> ·
  <a href="#文件结构">文件结构</a> ·
  <a href="#技术参考">技术参考</a> ·
  <a href="#许可证">许可证</a>
</p>

## 能做什么

- **项目搭建** — .csproj 模板、mod_manifest.json、入口点，复制即用
- **Harmony 补丁** — 固定目标 / 动态目标 / 方法体替换，三种模式覆盖所有场景
- **自定义卡牌** — ShunCard 链式配置基类 + PoolAttribute 自动注册，添卡不改入口
- **自定义修正器** — SyncedModifierModel 模式 + 多规则状态管理 + 联机同步
- **Godot UI** — CanvasLayer 挂载、Canvas 自定义绘制、主题字体捕获、Overlay 检测
- **联机适配** — Host/Client 角色检测、UI 交互锁定、修正器消息同步
- **实用模式** — AppliedTracker 防重复、ConditionalWeakTable 弱引用、反射+事件触发
- **全套参考** — 整合 STS2Plus（10 修正器 + 6 UI 组件）和 STS2-ShunMod（卡牌 + 注册系统）

## 快速开始

```bash
cd ~/.openclaw/workspace/skills
git clone https://github.com/yehuoshun/slay-the-spire-2-mod-skill.git
```

对话中触发：

```
帮我写一个 STS2 Mod，添加一张自定义卡牌
给我一个 STS2 Mod 的项目模板
怎么给 STS2 写 Harmony 补丁
```

## 文件结构

```
slay-the-spire-2-mod-skill/
├── SKILL.md          # 技能核心文档 — 11 章节实用指南
├── README.md         # 本文件
└── .github/
    └── workflows/    # CI（钉钉通知）
```

## 技术参考

本技能的知识提炼自以下开源 Mod：

| Mod | 作者 | 版本 | 参考价值 |
|-----|------|------|---------|
| [STS2Plus](https://github.com/StephenSHorton/STS2Plus) | 4step | v0.2.0 | 修正器架构、Godot UI 组件、联机同步 |
| [STS2-ShunMod](https://github.com/yehuoshun/STS2-ShunMod) | yehuoshun | v0.0.8 | 自动内容注册、自定义卡牌、高级补丁模式 |

## 许可证

MIT

## 作者

**yehuoshun** 和卷王龙虾，干就完了 🦞
