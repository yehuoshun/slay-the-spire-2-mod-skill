# 杀戮尖塔 2 Mod 开发 — AI 工作流

> 🦞 纯原生 Mod 开发指南。只依赖 `0Harmony.dll` + `sts2.dll`，不依赖任何第三方模组。

## 📂 文件结构

| 文件 | 内容 |
|------|------|
| `SKILL.md` | 主入口：总工作流、脚手架、API 速查、常见坑 |
| `references/modes-card.md` | 自定义卡牌 |
| `references/modes-relic.md` | 自定义遗物 |
| `references/modes-potion.md` | 自定义药水 |
| `references/modes-power.md` | 自定义能力（Buff/Debuff） |
| `references/modes-enchantment.md` | 自定义附魔 |
| `references/modes-event.md` | 自定义事件 & 先古之民 |
| `references/modes-harmony.md` | Harmony 补丁 |
| `references/modes-character.md` | 自定义角色 |
| `references/modes-monster.md` | 自定义怪物 & 遭遇 |
| `references/modes-serialization.md` | 序列化 & 注册 |
| `references/real-code-patterns.md` | 实战写法模式（YuWanCard/Natsuki/STS2Plus） |
| `references/project-scaffold.md` | 项目构建 & 部署 |
| `references/appendices.md` | 附录：API 速查、枚举、常见坑 |

## 鸣谢

### 🔥 活跃参考

- **[STS2Mod](https://github.com/s1f102500012/sts2mod)** — Natsuki 模组合集，多角色/符文/无尽模式等生产级参考（学习：2026-06-14）
- **[YuWanCard](https://github.com/YuWan886/Sts2-YuWanCard)** — 生产级角色 Mod，Builder 模式 + 反射注册 + 完整抽象基类体系（学习：2026-06-14）
- **[BaseLib](https://github.com/Alchyr/BaseLib-StS2)** — 基础库，Hooks 接口 + 反射/角色/怪物工具 + 异步 Patch 辅助（学习：2026-06-14）
- **[ModTemplate](https://github.com/Alchyr/ModTemplate-StS2)** — 模组脚手架模板，ContentMod / CharacterMod 双模板（学习：2026-06-14）
- **[ModConfig](https://github.com/xhyrzldf/ModConfig-STS2)** — 设置界面框架 v0.2.2，零 Harmony + 双向绑定 + 持久化 debounce，支持 Toggle/Slider/Dropdown/KeyBind/TextInput/ColorPicker/Button（学习：2026-06-14）

### 其他参考

- **[STS2Plus](https://github.com/StephenSHorton/STS2Plus)** — 修正器架构、联机同步参考
- **[sts2-res](https://github.com/yehuoshun/sts2-res)** — 反编译源码参考
- **[Megadot](https://megadot.megacrit.com)** — STS2 专用引擎
- [环境搭建 + 创建项目](https://www.bilibili.com/opus/1179300682687053826) — [烟汐忆梦_YM](https://space.bilibili.com/481430814)
- [自定义遗物](https://www.bilibili.com/opus/1179604439936270359) — [烟汐忆梦_YM](https://space.bilibili.com/481430814)
- [自定义卡牌](https://www.bilibili.com/opus/1179979923167641608) — [烟汐忆梦_YM](https://space.bilibili.com/481430814)
- [自定义药水](https://www.bilibili.com/opus/1180032536494997541) — [烟汐忆梦_YM](https://space.bilibili.com/481430814)
- [自定义附魔](https://www.bilibili.com/opus/1180713881530531843) — [烟汐忆梦_YM](https://space.bilibili.com/481430814)
- [自定义事件 + 先古之民](https://www.bilibili.com/opus/1180714323922649110) — [烟汐忆梦_YM](https://space.bilibili.com/481430814)
- [自定义能力 Buff/Debuff](https://www.bilibili.com/opus/1181126133981118470) — [烟汐忆梦_YM](https://space.bilibili.com/481430814)
- [自定义角色 + Spine 动画](https://www.bilibili.com/opus/1182961747166756931) — [烟汐忆梦_YM](https://space.bilibili.com/481430814)
- [自定义怪物 + 遭遇](https://www.bilibili.com/opus/1183380755590414377) — [烟汐忆梦_YM](https://space.bilibili.com/481430814)