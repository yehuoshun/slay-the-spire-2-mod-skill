# 已学仓库登记（LEARNED）

> 防止重复 clone 上游仓库扫代码。**学习新仓库前先查本表**，学过的只跟进增量（git fetch + diff），不重扫。

---

## 学习登记表

| 上游仓库 | 首次学习 | 最后跟进 | 学到版本/commit | 学习产出 |
|---------|---------|---------|----------------|---------|
| [Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) | 2026-08-28 | 2026-09-19 | v3.4.7（2026-09-11） | `references/baselib/`、`references/harmony/` 等全部模块的纯原生转译 |
| [Alchyr/ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) | 2026-09-19 | 2026-10-02 | 55ca2c6（2026-08-22，v2.5.1 后） | `references/setup/`（project-godot/mod-manifest/export-presets/template-pack/skeleton-build-targets 等） |
| [YuWan886/Sts2-YuWanCard](https://github.com/YuWan886/Sts2-YuWanCard) | 2026-10-02 | — | v0.5.12 / 324b396 | 融合进 card/power/relic/harmony/setup/serialization/baselib + 新建 `multiplayer/` 模块（真实 API 校验、多人、自定义稀有度、loader） |
| [yehuoshun/STS2-ShunMod](https://github.com/yehuoshun/STS2-ShunMod) | 2026-09-19 | — | main 分支 | `references/setup/ci-build.md`（GitHub Actions 流水线） |
| [godotengine/godot](https://github.com/godotengine/godot) | 2026-08-28 | — | 4.5.x 文档 | 环境搭建参考（Megadot 分支，非直接学习） |
| 烟汐忆梦_YM 教程（B站） | 2026-08-28 | — | 9 篇 | 各模块入门（card/relic/potion/enchantment/event/power/character/monster） |

---

## 学习流程（前置步骤）

在「读内容」之前，先：

1. **查本表** — 目标仓库/教程是否已登记
2. **已登记** → 只跟进增量：
   - `git fetch` 上游，对比上次学到版本 → 只学新增 commit
   - 参考上次产出文件，判断哪些 references 需要更新
3. **未登记** → 完整学习 + 学完登记本表（仓库名 + 日期 + 版本 + 产出）

## 登记规范

- 每次新学一个上游仓库/教程，学完必须在表中加一行
- 「学到版本」写上游仓库的 tag 或 commit hash（下次对比用）
- 「学习产出」写 references 对应目录
- 跟进增量后更新「最后跟进」日期和「学到版本」
