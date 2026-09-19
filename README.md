# Slay the Spire 2 Mod 开发 Skill

> 杀戮尖塔 2 纯原生 Mod 开发，零第三方依赖。

---

## 目录

- [SKILL.md](SKILL.md) — AI 工作流 + 15 条硬规则
- [LEARN.md](LEARN.md) — 学习流程

### references/

> 📌 每个模块的 `xx.md` 为导航页（概述 + 常见问题 + **章节导航表**），正文按章节拆分在 `xx-*.md` 子文件中。读模块时先开导航页，再按需读子文件。

| 分类 | 文件 | 内容 |
|------|------|------|
| `setup/` | [environment-setup.md](references/setup/environment-setup.md) | 环境搭建、创建项目、PCK 打包、调试（教程） |
| `setup/` | [project-skeleton.md](references/setup/project-skeleton.md) | 生产级项目骨架（目录规范 + 路径检测 + 自动打 PCK） |
| `setup/` | [rider.md](references/setup/rider.md) | Rider 开发环境配置（代码检查、Harmony 抑制规则） |
| `relic/` | [relic.md](references/relic/relic.md) | 自定义遗物（代码模板、稀有度、池、图标、本地化） |
| `card/` | [card.md](references/card/card.md) | 自定义卡牌（构造函数、API 速查、卡池、肖像、本地化） |
| `potion/` | [potion.md](references/potion/potion.md) | 自定义药水（属性、回调、图标、池、本地化） |
| `enchantment/` | [enchantment.md](references/enchantment/enchantment.md) | 自定义附魔（回调、附魔、本地化、图标） |
| `event/` | [event.md](references/event/event.md) | 自定义事件（选项、多页、先古之民、Patch、本地化） |
| `power/` | [power.md](references/power/power.md) | 自定义能力（Buff/Debuff、属性、回调、本地化） |
| `character/` | [character.md](references/character/character.md) | 自定义角色（卡池/遗物池/药水池、场景资源、注册） |
| `monster/` | [monster.md](references/monster/monster.md) | 自定义敌怪 & 遭遇（状态机、AI 行为树、注册） |
| `modifier/` | [modifier.md](references/modifier/modifier.md) | 自定义 Modifier（运行规则、Alignment、注册、效果实现） |
| `orb/` | [orb.md](references/orb/orb.md) | 自定义球体（被动/激发、图标、精灵、随机池） |
| `act/` | [act.md](references/act/act.md) | 自定义章节（地图背景、音乐、宝箱、房间配置） |
| `pet/` | [pet.md](references/pet/pet.md) | 自定义宠物（固定不行动 AI、血条、场景） |
| `resource/` | [resource.md](references/resource/resource.md) | 自定义资源（法力/怒气等 + 卡牌费用 + UI） |
| `badge/` | [badge.md](references/badge/badge.md) | 自定义模组徽章（Badge 继承、图标、注册） |
| `rest-site/` | [rest-site.md](references/rest-site/rest-site.md) | 自定义休息点选项（RestSiteOption 继承、图标、注入） |
| `pile/` | [pile.md](references/pile/pile.md) | 自定义牌堆（PileType注入、定位、动画） |
| `reward/` | [reward.md](references/reward/reward.md) | 自定义奖励（RewardType 注入、序列化、示例） |
| `energy/` | [energy.md](references/energy/energy.md) | 自定义能量（图标、类型、视觉效果） |
| `harmony/` | [harmony.md](references/harmony/harmony.md) | Harmony 补丁模式（PatchCategory、安全、组织规范） |
| `harmony/` | [harmony-custom-power-sfx.md](references/harmony/harmony-custom-power-sfx.md) | Transpiler 实战：自定义能力音效（零第三方依赖） |
| `serialization/` | [serialization.md](references/serialization/serialization.md) | 序列化与注册（ModelDb、SavedProperty、InjectTypeIntoCache） |
| `settings/` | [settings.md](references/settings/settings.md) | 设置界面（BaseLib SimpleModConfig、Attribute、本地化） |
| `baselib/` | [design-patterns.md](references/baselib/design-patterns.md) | 纯原生设计模式总纲（从 BaseLib 提炼，零第三方依赖） |

---

## 设计原则

1. **硬规则驱动**：所有行为由硬规则约束，不靠"建议"
2. **知识注入**：代码模板和 API 参考在 references 中持续积累
3. **Actions 通知**：提交后由 GitHub Actions 发钉钉通知；本仓库为**文档型**（无 C# 代码可编译），编译验证需在本地 Rider / 游戏环境完成，agent 负责静态检查 + push
4. **零第三方依赖**：只靠 `0Harmony.dll` + `sts2.dll`

---

## 鸣谢

### 活跃仓库

- [Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) — 官方模组标准库（Custom*Model 基类、[Pool]、Builder、工具）
- [Alchyr/ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) — 官方模组脚手架模板（工程化思想：骨架自动化、路径检测、目录规范）

### 项目仓库

- [yehuoshun/slay-the-spire-2-mod-skill-archive](https://github.com/yehuoshun/slay-the-spire-2-mod-skill-archive) — 旧版本 references 存档（含 v1/v2 版本记录）

### 不活跃仓库

- [烟汐忆梦_YM](https://space.bilibili.com/481430814) — 9 篇教程（环境搭建、遗物、卡牌、药水、附魔、事件&先古之民、能力、角色、敌怪&遭遇）

