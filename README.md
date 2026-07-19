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
| `content/` | `relic.md` | 自定义遗物 |

---

## 设计原则

1. **硬规则驱动**：所有行为由硬规则约束，不靠"建议"
2. **知识注入**：代码模板和 API 参考在 references 中持续积累
3. **CI 验证**：本地编译在 CI 完成，agent 负责静态检查 + push
4. **零第三方依赖**：只靠 `0Harmony.dll` + `sts2.dll`

---

## 鸣谢

- **环境搭建 + 创建项目教程** — [烟汐忆梦_YM](https://space.bilibili.com/481430814)

---

## 当前状态

- [x] SKILL.md（重写完成）
- [x] Rider.md（保留）
- [x] 环境搭建 & 创建项目（from 烟汐忆梦_YM 教程）
- [ ] 卡牌模式
- [x] 遗物模式（from 烟汐忆梦_YM 教程）
- [ ] 药水模式
- [ ] 能力模式
- [ ] 附魔模式
- [ ] 事件模式
- [ ] Harmony 补丁模式
- [ ] 角色模式
- [ ] 怪物模式
- [ ] 序列化与注册
- [ ] 设置界面
- [ ] 实战写法模式
- [ ] API 附录