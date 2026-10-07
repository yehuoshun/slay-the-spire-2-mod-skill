# 已学仓库登记（LEARNED）

> 防止重复 clone 上游仓库扫代码。**学习/增量扫描前先查这两个表**：`学习登记表` = 活跃仓库（只跟进增量 git fetch + diff，不重扫）；`非活跃来源` = 不扫描（直接跳过，防浪费时间）。

---

## 学习登记表

| 上游仓库 | 首次学习 | 最后跟进 | 学到版本/commit | 学习产出 |
|---------|---------|---------|----------------|---------|
| [Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) | 2026-08-28 | 2026-09-19 | v3.4.7（2026-09-11） | `references/baselib/`、`references/harmony/` 等全部模块的纯原生转译 |
| [Alchyr/ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) | 2026-09-19 | 2026-10-02 | 55ca2c6（2026-08-22，v2.5.1 后） | `references/setup/`（project-godot/mod-manifest/export-presets/template-pack/skeleton-build-targets 等） |
| [YuWan886/Sts2-YuWanCard](https://github.com/YuWan886/Sts2-YuWanCard) | 2026-10-02 | 2026-10-04 | 08165915（v0.5.12 后，2026-10-04） | 融合进 card/power/relic/harmony/setup/serialization/baselib + 新建 `multiplayer/` 模块（真实 API 校验、多人、自定义稀有度、loader）。**10-04 增量**：`multiplayer/multiplayer-netactions.md`（网络行动/PlayPhase 门控/交互状态防护）+ `harmony/harmony-transpiler.md`（泛型 operand 结构匹配、确定性排序）+ `resource-lifecycle.md` 图标预加载 + `settings-core.md` LocManager 就绪门控 |
| [lf201014/STS2_MarisaMod](https://github.com/lf201014/STS2_MarisaMod) | 2026-10-05 | 2026-10-05 | 637f17a（main，2026-10-05） | 实战角色 mod（BaseLib 依赖，170 cs/9.7k 行）。产出：新建 `card/card-amplify.md`（增幅/Kicker 系统：费用计算+标签+高亮三 Patch+悬停刷新）、`monster/monster-animator.md`（AnimState/CreatureAnimator 状态机）、`character/character-overrides.md`（Custom* 覆写点全清单+解锁屏蔽）、`harmony/harmony-async-local.md`（AsyncLocal 异步 Patch）、`card/card-hover-inject.md`（HoverTipFactory+TrashHeap 注入）；融合 event-core（ModifyNextEvent）、enchantment-advanced（EnergySpent 成长）、relic-callbacks（ModifyNextEvent 行） |
| [xhyrzldf/ModConfig-STS2](https://github.com/xhyrzldf/ModConfig-STS2) | 2026-10-07 | 2026-10-07 | v0.2.2（639eb97，2026-10-07） | 新建 `references/settings/modconfig.md`（导航/三种方案选型）+ `modconfig-integration.md`（反射桥模板/API/ConfigEntry 速查）+ `modconfig-internals.md`（Tab 注入 hack/控件渲染/持久化/KeyBind/i18n） |
| [yehuoshun/STS2-ShunMod](https://github.com/yehuoshun/STS2-ShunMod) | 2026-09-19 | — | main 分支 | `references/setup/ci-build.md`（GitHub Actions 流水线） |

---

## 非活跃来源（不参与增量扫描）

> 仅作历史来源登记，**一律不做 git fetch / 增量扫描**（无跟进价值 / 非仓库形式）。查表时看到它们 = 已学，直接跳过，别再花时间扫。

| 来源 | 首次学习 | 学到版本 | 学习产出 |
|------|---------|---------|---------|
| [godotengine/godot](https://github.com/godotengine/godot) | 2026-08-28 | 4.5.x 文档 | 环境搭建参考（Megadot 分支，非直接学习） |
| 烟汐忆梦_YM 教程（B站） | 2026-08-28 | 9 篇 | 各模块入门（card/relic/potion/enchantment/event/power/character/monster） |

---

## 学习流程（前置步骤）

在「读内容」之前，先：

1. **查表** — 目标仓库/教程是否已登记：
   - 命中「非活跃来源」→ 已学，直接跳过（不扫）
   - 命中「学习登记表」→ 走第 2 步
   - 都没命中 → 走第 3 步
2. **已登记（活跃）** → 只跟进增量：
   - `git fetch` 上游，对比上次学到版本 → 只学新增 commit
   - 参考上次产出文件，判断哪些 references 需要更新
3. **未登记** → 完整学习 + 学完登记本表（仓库名 + 日期 + 版本 + 产出）

> ⚠️ 增量扫描只针对「学习登记表」里的活跃仓库。「非活跃来源」列在这里就是为了**防止扫描扫到它们浪费时间**。

---

## 登记规范

- 每次新学一个上游仓库/教程，学完必须在表中加一行
- 「学到版本」写上游仓库的 tag 或 commit hash（下次对比用）
- 「学习产出」写 references 对应目录
- 跟进增量后更新「最后跟进」日期和「学到版本」
