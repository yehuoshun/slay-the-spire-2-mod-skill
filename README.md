# Slay the Spire 2 Mod 开发 Skill

> 杀戮尖塔 2 纯原生 Mod 开发，零第三方依赖。

---

## 目录

- [SKILL.md](SKILL.md) — AI 工作流 + 15 条硬规则
- [LEARN.md](LEARN.md) — 学习流程
- [LEARNED.md](LEARNED.md) — 已学仓库登记（防重复学习）

### references/

> 📌 每个模块的 `xx.md` 为导航页（概述 + 常见问题 + **章节导航表**），正文按章节拆分在 `xx-*.md` 子文件中。读模块时先开导航页，再按需读子文件。

> 模块级索引见 [references/index.md](references/index.md)，子文件由各模块导航页章节导航表索引。


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
- [YuWan886/Sts2-YuWanCard](https://github.com/YuWan886/Sts2-YuWanCard) — 实战大型 mod（真实 API 用法样本：多人、自定义稀有度、多版本 loader、生命条预测）

### 项目仓库

- [yehuoshun/slay-the-spire-2-mod-skill-archive](https://github.com/yehuoshun/slay-the-spire-2-mod-skill-archive) — 旧版本 references 存档（含 v1/v2 版本记录）

### 不活跃仓库

- [烟汐忆梦_YM](https://space.bilibili.com/481430814) — 9 篇教程（环境搭建、遗物、卡牌、药水、附魔、事件&先古之民、能力、角色、敌怪&遭遇）

