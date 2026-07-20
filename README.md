# Slay the Spire 2 Mod 开发 Skill

> 杀戮尖塔 2 纯原生 Mod 开发，零第三方依赖。

---

## 目录

- [SKILL.md](SKILL.md) — AI 工作流 + 15 条硬规则
- [LEARN.md](LEARN.md) — 学习流程

### references/

| 分类 | 文件 | 内容 |
|------|------|------|
| `setup/` | `environment-setup.md` | 环境搭建 & 创建项目 |
| `setup/` | `rider.md` | Rider 开发环境配置 |
| `relic/` | `relic.md` | 自定义遗物 |
| `card/` | `card.md` | 自定义卡牌 |
| `potion/` | `potion.md` | 自定义药水 |
| `enchantment/` | `enchantment.md` | 自定义附魔 |
| `event/` | `event.md` | 自定义事件 |
| `event/` | `ancient.md` | 先古之民事件 |
| `power/` | `power.md` | 自定义能力 |
| `character/` | `character.md` | 自定义角色 |
| `monster/` | `monster.md` | 自定义敌怪 & 遭遇 |
| `modifier/` | `modifier.md` | 自定义运行规则 |
| `orb/` | `orb.md` | 自定义球体 |
| `act/` | `act.md` | 自定义章节 |
| `pet/` | `pet.md` | 自定义宠物 |
| `harmony/` | `harmony.md` | Harmony 补丁模式 |
| `serialization/` | `serialization.md` | 序列化与注册 |
| `settings/` | `settings.md` | 设置界面 |
| `patterns/` | `code-patterns.md` | 实战写法模式 |
| `patterns/` | `api-reference.md` | API 附录 |
| `baselib/` | `baselib.md` | BaseLib 集成指南 |

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
- [x] Rider.md（保留）
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