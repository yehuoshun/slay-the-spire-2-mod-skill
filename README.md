# Slay the Spire 2 Mod 开发 Skill

> 杀戮尖塔 2 纯原生 Mod 开发，零第三方依赖。

---

## 目录

- [SKILL.md](SKILL.md) — AI 工作流 + 15 条硬规则
- [LEARN.md](LEARN.md) — 学习流程

### references/

> 📌 每个模块的 `xx.md` 是**版本入口**（含 v1/v2 历史），当前使用 **v2**（纯原生进阶版）。

| 分类 | 文件 | 内容 |
|------|------|------|
| `setup/` | [environment-setup.md](references/setup/environment-setup.md) | 环境搭建、项目创建、PCK 打包、C# 入口 |
| `setup/` | [rider.md](references/setup/rider.md) | Rider 开发环境配置（代码检查、Harmony 抑制规则） |
| `relic/` | [relic.md](references/relic/relic.md) | 自定义遗物（代码模板、稀有度、池、图标、本地化） |
| `card/` | [card.md](references/card/card.md) | 自定义卡牌（构造函数、API 速查、卡池、肖像、本地化） |
| `potion/` | [potion.md](references/potion/potion.md) | 自定义药水（属性、回调、图标、池、本地化） |
| `enchantment/` | [enchantment.md](references/enchantment/enchantment.md) | 自定义附魔（回调、附魔、本地化、图标） |
| `event/` | [event.md](references/event/event.md) | 自定义事件（选项、多页、Patch、本地化） |
| `event/` | [ancient.md](references/event/ancient.md) | 先古之民事件（对话、纹理、注册） |
| `power/` | [power.md](references/power/power.md) | 自定义能力（Buff/Debuff、属性、回调、本地化） |
| `character/` | [character.md](references/character/character.md) | 自定义角色（卡池/遗物池/药水池、场景资源、注册） |
| `monster/` | [monster.md](references/monster/monster.md) | 自定义敌怪 & 遭遇（状态机、AI 行为树、注册） |
| `modifier/` | [modifier.md](references/modifier/modifier.md) | 自定义 Modifier（运行规则、Alignment、注册、效果实现） |
| `orb/` | [orb.md](references/orb/orb.md) | 自定义球体（被动/激发、图标、精灵、随机池） |
| `act/` | [act.md](references/act/act.md) | 自定义章节（地图背景、音乐、宝箱、房间配置） |
| `pet/` | [pet.md](references/pet/pet.md) | 自定义宠物（固定不行动 AI、血条、场景） |
| `harmony/` | [harmony.md](references/harmony/harmony.md) | Harmony 补丁模式（PatchCategory、安全、组织规范） |
| `serialization/` | [serialization.md](references/serialization/serialization.md) | 序列化与注册（ModelDb、SavedProperty、InjectTypeIntoCache） |
| `settings/` | [settings.md](references/settings/settings.md) | 设置界面（BaseLib SimpleModConfig、Attribute、本地化） |
| `patterns/` | [code-patterns.md](references/patterns/code-patterns.md) | 实战写法模式（卡牌/遗物/能力/事件代码片段） |
| `patterns/` | [api-reference.md](references/patterns/api-reference.md) | API 附录（命名空间、命令类、回调签名、注册点） |
| `baselib/` | [baselib.md](references/baselib/baselib.md) | 纯原生设计模式总纲（从 BaseLib 提炼，零第三方依赖） |

---

## 设计原则

1. **硬规则驱动**：所有行为由硬规则约束，不靠"建议"
2. **知识注入**：代码模板和 API 参考在 references 中持续积累
3. **CI 验证**：本地编译在 CI 完成，agent 负责静态检查 + push
4. **零第三方依赖**：只靠 `0Harmony.dll` + `sts2.dll`

---

## 鸣谢

### 活跃仓库

- [Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) — 官方模组标准库（Custom*Model 基类、[Pool]、Builder、工具）

### 不活跃仓库

- [烟汐忆梦_YM](https://space.bilibili.com/481430814) — 9 篇教程（环境搭建、遗物、卡牌、药水、附魔、事件&先古之民、能力、角色、敌怪&遭遇）

---

## 当前状态

- [x] SKILL.md（重写完成）
- [x] Rider 环境配置（保留）
- [x] 环境搭建 & 创建项目（from 烟汐忆梦_YM 教程）
- [x] 卡牌模式（from 烟汐忆梦_YM 教程03）
- [x] 遗物模式（from 烟汐忆梦_YM 教程）
- [x] 药水模式（from 烟汐忆梦_YM 教程04）
- [x] 能力模式（from 烟汐忆梦_YM 教程07）
- [x] 附魔模式（from 烟汐忆梦_YM 教程05）
- [x] 事件模式（from 烟汐忆梦_YM 教程06）
- [x] Harmony 补丁模式
- [x] 角色模式（from 烟汐忆梦_YM 教程08）
- [x] 怪物模式（from 烟汐忆梦_YM 教程09）
- [x] 序列化与注册
- [x] 设置界面
- [x] 实战写法模式
- [x] API 附录
- [x] BaseLib 集成指南（更新至 3.4.5：CustomResource 系统、CustomCalculatedVar.Create、CustomLargeImagePath）
- [x] 14 个子项已升级 **v2**（纯原生进阶，导航模式：`xx.md` 入口 + `xx-v1.md` 存档）
- [x] baselib.md 重写为纯原生设计模式总纲