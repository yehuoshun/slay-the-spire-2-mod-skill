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

## 鸣谢

- [STS2Plus](https://github.com/StephenSHorton/STS2Plus) — 修正器架构、UI 组件、联机同步参考实现
- [ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) — 官方模组模板（空/内容/角色三种）
- [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) — 官方 STS2 Mod 框架（CustomModel 系列、ModConfig 设置面板、图像路径工具、扩展方法）
- [Megadot](https://megadot.megacrit.com) — 杀戮尖塔 2 专用 Godot 编辑器
- [B站开发教程 01](https://www.bilibili.com/opus/1179300682687053826) — 环境搭建、PCK 导出、资源替换
- [B站开发教程 02](https://www.bilibili.com/opus/1179604439936270359) — 自定义遗物、DynamicVar、遗物池
- [B站开发教程 03](https://www.bilibili.com/opus/1179979923167641608) — 自定义卡牌、伤害链、选牌抽牌
- [B站开发教程 04](https://www.bilibili.com/opus/1180032536494997541) — 自定义药水、三种使用模式
- [B站开发教程 05](https://www.bilibili.com/opus/1180713881530531843) — 自定义附魔、附魔加成
- [B站开发教程 06](https://www.bilibili.com/opus/1180714323922649110) — 自定义事件、先古之民
- [B站开发教程 07](https://www.bilibili.com/opus/1181126133981118470) — 自定义能力 Buff/Debuff
- [B站开发教程 08](https://www.bilibili.com/opus/1182961747166756931) — 自定义角色、Spine 动画
- [B站开发教程 09](https://www.bilibili.com/opus/1183380755590414377) — 自定义怪物、遭遇定义

## 作者

**yehuoshun** 🦞
