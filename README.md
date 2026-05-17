# 杀戮尖塔2 Mod 开发

帮你写 Slay the Spire 2 模组 — 从项目搭建到 Harmony 补丁、自定义卡牌/遗物/药水/能力/事件/怪物/角色、Godot UI、联机适配。

## 能做什么

- **环境与项目** — Megadot + .NET 9 + .csproj + mod_manifest.json
- **Harmony 补丁** — 固定目标、动态目标、方法体替换
- **自定义卡牌** — 基类 + 自动注册 + 伤害链 + 抽牌选牌
- **自定义遗物** — RelicModel + DynamicVar + 事件钩子 + 初始遗物修改
- **自定义药水** — 伤害/效果/自动消耗（复活）三种模式
- **自定义能力** — PowerModel + Buff/Debuff + 层数叠加
- **自定义附魔** — EnchantmentModel + 伤害/格挡加成
- **自定义事件** — EventModel + 多选 + 添加到地图
- **自定义怪物** — MonsterModel + 状态机 + 遭遇定义
- **自定义角色** — CharacterModel + 卡池/遗物池/药水池 + Spine
- **Godot UI / 联机** — CanvasLayer、交互锁定
- **避坑手册** — 7 个常见坑及对策

## 快速开始

```bash
cd ~/.openclaw/workspace/skills
git clone https://github.com/yehuoshun/slay-the-spire-2-mod-skill.git
```

## 参考

本 Skill 已将以下来源的核心知识内化，无需翻阅外部链接即可编写 Mod：

- STS2Plus · ModTemplate-StS2 · BaseLib-StS2 · Sts2-YuWanCard · Megadot · B站开发教程 01-09

## 作者

**yehuoshun** 🦞
