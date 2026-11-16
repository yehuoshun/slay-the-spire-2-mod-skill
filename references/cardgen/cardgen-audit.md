# 组件化卡牌的回归与审计工程

> 来源：AutoAnthony `tests/` 目录（20 个独立工程，走 `dotnet run` 的控制台回归，不启 Godot 编辑器）。为什么值得单独学：一个生成式卡池 mod 的正确性**无法靠手测**——每次生成都是新的组合，只能靠「构建期门禁 + 结构化作弊检测 + 跨版本对账」三层兜住。

## 一、三层门禁

| 层 | 跑什么 | 失败意味着 |
|---|---|---|
| 构建期 | 结构化目录验证、RuntimeSpec 注册表验证、执行路由覆盖率、本地化边界（`Verify-LocalizedTextBoundaries.ps1`） | 数据自相矛盾，**根本不该发布** |
| 生成器自检 | `--self-test`、`--native-decomposition-audit`、`--baseline` 微基准 | 生成逻辑或性能退化 |
| 真引擎回归 | 隔离 Godot 进程加载真原生卡，比 base/首升级的费用、变量、关键词，再格式化中英双语描述 | 与游戏本体行为脱节 |

## 二、20 个回归工程（按关注点分组）

| 关注点 | 工程 |
|---|---|
| 原生卡版本对账 | `NativeCardVersionRegression`、`LibraryActivationCompatibility` |
| 生成卡热路径与成本 | `GeneratedCardHotPathRegression`、`GeneratedStarCostRegression`、`CounterModifierRegression` |
| 升级 / 降级生命周期 | `UpgradeLifecycleRegression`、`UnifiedXUpgrade`、`StartingRepairRegression` |
| 卡自身状态变异 | `SelfMutationRegression`、`DrawGrowthAudit` |
| 愚者（Fool）系统 | `FoolComponentRegression`（4 个 `.cs`）、`FoolCombatRegression` |
| 战斗内选择与随机域 | `CombatSelectionLifetimeRegression`、`OccurrenceProbe`、`TransformationPoolRegression` |
| 存档与读档 | `RunLoadRegression`、`AncientRelicBindingRegression` |
| 多人准备 / 自动出牌 / 复杂交互 | `MultiplayerPreparationRegression`、`ModifierAndAutoplayAudit`、`TeaOfDiscourtesyRegression` |

每个工程都有一份 README，**第一件事是写清「这套测什么、不测什么」**：不启 Godot 渲染、不读写用户存档、不替代真机双人局。这条「显式声明不测什么」比多写十个断言更有价值 —— 它防止后来的人把绿灯当成「全都对」。

## 三、四条可抄的断言纪律

1. **失败样本留档**：修复前先跑出失败输出，存进 `audits/*.txt`，回归工程里引用它做「这个坑不许回来」的证据。
2. **固定种子 digest**：固定种子生成 170 张卡 × 六池，跨版本比对摘要 `952C035827E0B6AD035A53BA2B166FBA5FE079EFBA2F2161BE86A8C89B3D4EA1`——数据改动一眼可见。
3. **按分配字节数做微基准**（GC 分配是最容易悄悄退化的指标）：语义标记 200→40、block flag 32→0、描述缓存 504→0、portrait key 224→0、空 tooltip 2320→0、派生 tooltip 4094→1077。`GeneratedCardHotPathRegression` 的 `--baseline` **只关掉分配断言、保留正确性断言**，所以能既当基准又当回归。
4. **零副作用判据**：读档往返后哈希一致还不够 —— 再加一条「Continue 期间禁止任何池生成」，否则白跑一次兼容生成也看不出来。

## 四、真引擎回归怎么跑（v113 实测口径）

- 在**隔离 Godot 进程**里加载真原生卡：1,146 个语言/卡用例 + 2,212 条 base/升级描述全过（中英双语各格式化一遍）。
- 482 条 exact-upgrade 重建全过；六池生成 + Colorless 52→53 快照恢复全过（BaseLib 3.4.8 / RitsuLib 0.6.11，恢复保留 514 条旧定义）。
- 0.3.165：659 记录 / 566 精确可执行升级；8 个 start-save-continue 用例各校验 7 池 / 606 卡；最终冷 Continue 指纹一致且**零池生成**。
- 全程用隔离 profile / fixture，不读也不改用户存档；测试结束关掉测试清单，再核对产出的 DLL 哈希与发布包一致。

## 五、这些测试**不**能替代什么

真机双人局、原生战斗程序的每一条分支、玩家实际手感与难度曲线。审计能保证「结构没坏、数值口径没漂」，「好不好玩」只能靠人玩。

## 相关

- 门禁背后的结构约束（schema、append-only、签名缓存）：[cardgen-architecture.md](cardgen-architecture.md)
- 运行时状态为什么难测（归属掩码、活动根、读档水合）：[cardgen-runtime.md](cardgen-runtime.md)
- 跨游戏版本怎么对账：[../setup/game-version-migration-semantics.md](../setup/game-version-migration-semantics.md)
