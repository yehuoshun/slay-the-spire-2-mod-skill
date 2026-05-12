# 杀戮尖塔2 Mod 开发

帮你写 Slay the Spire 2 模组 — 从项目搭建到 Harmony 补丁、自定义卡牌/遗物/修正器、Godot UI 开发、联机适配，一站式指导。

## 能做什么

- **环境与项目** — Megadot + .NET 9 SDK + .csproj 模板 + mod_manifest.json，复制即用
- **Harmony 补丁** — 固定目标、动态目标、方法体替换，三种模式覆盖所有场景
- **自定义卡牌** — ShunCard 链式配置基类 + PoolAttribute 自动注册，添卡不改入口
- **自定义遗物** — RelicModel 继承、DynamicVar、事件钩子、图标裁切、初始遗物修改
- **自定义修正器** — SyncedModifierModel 模式 + 双路状态检查 + 联机同步
- **Godot UI** — CanvasLayer 独立挂载、Overlay 阻塞检测、游戏字体捕获
- **联机适配** — Host/Client 角色检测、UI 交互锁定、修正器消息同步
- **本地化** — JSON 文件结构（中/英） + LocManager 动态合并
- **实用工具** — 防重复处理、ConditionalWeakTable 弱引用、反射+手动事件触发
- **避坑手册** — 6 个常见坑及原因/对策

## 快速开始

```bash
cd ~/.openclaw/workspace/skills
git clone https://github.com/yehuoshun/slay-the-spire-2-mod-skill.git
```

对话中直接提需求：

```
帮我写一个杀戮尖塔2的模组，加一张自定义卡牌
给我一个 STS2 Mod 的项目模板
怎么给 STS2 的卡牌打 Harmony 补丁
```

AI 会自动读取 SKILL.md 并基于其中的模式给出代码。

## 文件结构

```
├── SKILL.md          # 10 章节开发指南（AI 读取的核心文档）
├── README.md         # 本文件
└── .github/          # 钉钉通知 CI
```

## 鸣谢

- [STS2Plus](https://github.com/StephenSHorton/STS2Plus) — 修正器架构、Godot UI 组件、联机同步机制参考实现

## 作者

**yehuoshun** 🦞
