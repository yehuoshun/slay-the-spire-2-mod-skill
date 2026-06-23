# 杀戮尖塔 2 Mod 开发 — AI 工作流

> 🦞 纯原生 Mod 开发指南。零第三方依赖，只靠 `0Harmony.dll` + `sts2.dll`。
> 从 STS2Plus / YuWanCard / 海克斯符文 / ShunMod / ModTemplate / BaseLib 六个生产级模组中提炼的最佳实践。

## 设计理念

- **纯原生**：不依赖任何第三方模组框架
- **三种写法**：每个模式提供最简 → Builder → 生产级三种写法，按需选用
- **属性扫描注册**：`[CardPool]` / `[RelicPool]` 属性自动注册，零手动
- **Try-Install 补丁**：每个 Hook 组独立 try-catch，一个挂了不影响其他
- **零 Harmony 设置**：自研设置界面方案（可选依赖 ModConfig 框架）

## 📂 文件结构

| 文件 | 内容 |
|------|------|
| `SKILL.md` | 主入口：总工作流、模式速查、API 速查、常见坑 |
| `references/project-scaffold.md` | 项目搭建：目录结构、csproj、ModEntry、ContentRegistry、构建部署 |
| `references/modes-card.md` | 自定义卡牌：最简 / Builder / 生产级三种写法 |
| `references/modes-relic.md` | 自定义遗物：Rarity 获取途径、生命周期钩子、计数器 |
| `references/modes-potion.md` | 自定义药水：Usage 场景、目标型 |
| `references/modes-power.md` | 自定义能力：PowerType / StackType / 生命周期钩子 |
| `references/modes-enchantment.md` | 自定义附魔：ModifyCard / CanEnchant |
| `references/modes-event.md` | 自定义事件 & 先古之民：条件选项、对话树 |
| `references/modes-harmony.md` | Harmony 补丁：分组 / Try-Install / ModPatcher 三种组织方式 |
| `references/modes-character.md` | 自定义角色：卡池/遗物池/场景/视觉资源 |
| `references/modes-monster.md` | 自定义怪物 & 遭遇：状态机、条件转移 |
| `references/modes-serialization.md` | 序列化 & 注册：属性扫描 / 集中注册表 / ModelDb.Inject |
| `references/modes-settings-ui.md` | 设置界面：ModConfig 框架 / 自研零 Harmony 方案 |
| `references/real-code-patterns.md` | 实战写法模式：五种写法对比 + 选型建议 |
| `references/appendices.md` | 附录：完整 API 速查、枚举表、常见坑 |

## 鸣谢

### 🔥 活跃参考

- **[STS2Mod](https://github.com/s1f102500012/sts2mod)** — Natsuki 模组合集，多角色/符文/无尽模式/集成战略事件等生产级参考（学习：2026-06-23）
- **[YuWanCard](https://github.com/YuWan886/Sts2-YuWanCard)** — 生产级角色 Mod，Builder 模式 + 反射注册 + 完整抽象基类体系 + 确定性随机 + ModelId 去重（学习：2026-06-23）
- **[BaseLib](https://github.com/Alchyr/BaseLib-StS2)** — 基础库 v3.3.2，Hooks 接口 + MoveBuilder + MultiPileCardSelect + CardTransformReward + ITrashHeap + CustomCharacterUtils + CustomActModel + Pet 目标类型（学习：2026-06-23）
- **[ModTemplate](https://github.com/Alchyr/ModTemplate-StS2)** — 模组脚手架模板，ContentMod / CharacterMod 双模板，更新 .NET 9.0 兼容（学习：2026-06-23）
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