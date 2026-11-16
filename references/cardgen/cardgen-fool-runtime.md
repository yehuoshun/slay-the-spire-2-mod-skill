# 愚者词条 · 运行期与生命周期

> 配套 [cardgen-fool.md](cardgen-fool.md)（机制与 API）与 [cardgen-runtime.md](cardgen-runtime.md)（通用运行时执行层）。本文件只讲 Fool 特有的运行期细节。

## 一、一张牌从 JSON 到战斗的完整链路

1. **定义**：`fool_components.json` → `FoolComponentDefinitions.TryCompile` 生成 RuntimeSpec。数字 token 只在**离线编译边界**匹配（`FoolComponentDefinitions.cs:42`–`:53`），运行期不再解析文本。
2. **生成**：`ComponentAssemblyGenerator` 调 `CanAppend`（`:2960`）、`HasValidShootAssembly`（`:2681`）、`FoolMelodySubset.WithMask(atom, _random.Next(1, 8))`（`:2761`）组卡；旋律命名/打标走 `FoolMelodyNames.Compose`（`:28`）+ `ApplyNameTags`（`:854` / `:961`）。
3. **入池**：`ChaosFoolCardPool`（`ChaosMode/ChaosCardPool.cs:71`）由 Harmony 前缀替换原生 `Fool.CardPool` / `Fool.StartingDeck`（`ChaosMode/Patches/FoolPoolPatches.cs:9`–`:21`）。
4. **战斗**：`ExecuteFoolOperation` 分派 → `PowerCmd.Apply<AimPower/StretchPower/…>`、`ArrowCmd.GainArrowSlot`；条件子句走 `ExecuteImmediateConditionChildren`（`:46`），需要持续挂载的走 `ArmCompositePower`（`:63`）。
5. **触发边界**：见下节。
6. **存档 / 多人**：见第三节。
7. **卡面**：`FoolHoverTips`（`ChaosFoolPresentation.cs:12`）按「出现了哪些模板」推导瞄准 / 拉伸 / 扎穿 / 箭矢 / 旋律 / Tarot / 悲伤提示，`melody_mask` 反查 `FoolMelodySubset.Tags(mask)`（`:35`）。

## 二、卡钩子 vs 复合 Power：按「谁能感知事件」划边界

| 事件 | 归属 | 位置 |
|---|---|---|
| 卡自身位置相关（本牌在弃牌堆、本牌松脱、本牌一次性闸门） | **卡钩子** | `ChaosFoolCardHooks.cs:42`（`AfterArrowShot`）→ `FireFoolLifecycle:26` |
| 全局事件（箭矢扎穿 / 敌人死亡 / 旋律全局获得） | **复合 Power** | `ChaosCompositePower.AfterArrowImpaled / AfterArrowDislodged / AfterDeath`（`ChaosFoolPowerHooks.cs:55` / `:62` / `:70`） |

- executor 只做「模板 → 原生命令」的分派，**不持有生命周期状态**；状态放卡或 Power 上，否则读档必丢。
- 有次数上限的递归触发要显式建模：`ChaosAbilityTriggerLimiter.TryConsume`（`ChaosAbilityTriggerLimiter.cs:24`）按「时点纪元 + 单效果配额」限流，**每时点每效果上限 20**，否则链式卡互拉会打不穿。
- 优先 **patch 原生返回值**而不是重写管线：瞄准倍率没有重写伤害计算，而是 postfix `AimPower.ModifyDamageAdditive`（`Patches/FoolAimModifierPatch.cs:9`–`:18`）—— 预览、嵌套攻击、实际命中天然对齐。

## 三、存档与多人

- 永久瞄准层数存 `[SavedProperty] ExtraAim`（`ChaosFoolCardState.cs:15`），setter 里同步 `F:Aim` 的 `DynamicVars` 增量（`:22`–`:29`）—— 同一份数值既是显示值又是 `BaseValue`，用 setter 做 delta 同步，别在两处各存一份。
- 一次性闸门状态同样 `[SavedProperty]`：`ConsumedFoolDeathTriggers` / `FoolFirstArrowDislodged` / `FoolAllMelodiesTriggered`（`ChaosFoolPowerHooks.cs:16`–`:22`），跨回合在 `BeforeSideTurnStart` 重置（`:24`）。
- 多人：走原生 `PlayerChoiceContext` + 命令，由权威端结算；和原生一致，别自己发网络消息。
- 冷快照水合要能**原样恢复**具名计数模板（两张「隐式单卡 Transform」的计数），否则 Continue 会判定需要重建整池 —— 判据是「Continue 期间零池生成」，比只比指纹更硬。

## 四、为什么拆成 6 个 `ChaosFool*` 文件

它们都寄宿在既有 `partial class`（`ChaosCardModel` / `ChaosOperationExecutor` / `ChaosCompositePower`），所以按职责分文件：定义（`ChaosFoolCards.cs`）、状态（`ChaosFoolCardState.cs`）、钩子（`ChaosFoolCardHooks.cs` / `ChaosFoolPowerHooks.cs`）、执行（`ChaosFoolExecutor.cs`）、表现（`ChaosFoolPresentation.cs`）。

`ChaosFoolCards.cs:5`–`:100` 是 91 个只覆盖 `Slot` 的空壳 —— 给池子编址用，不承载逻辑（理由见 [cardgen-fool.md](cardgen-fool.md) 常见问题）。

## 五、坑与取舍

- **永久奖励是硬约束**：任何跨战斗收益必须 `Restricted` + 保留 `Exhaust`，禁止靠 recurring 祖先或计数乘子绕过，并且要校验**整条触发祖先链**（`docs/fool-integration-plan.md:70`–`:81`）。旋律掩码不算可升级数值。
- **状态必须单独存**：`ExtraAim` 既是显示值又是 `F:Aim` 的 BaseValue，两边各存一份迟早不同步。
- **别让 executor 记状态**：它被所有卡共享，一旦存了「某张牌射过箭」这类事实，读档/多人就崩。
- **数量封顶写进限流器**：手写 `if (count < 20)` 会散落在各处；统一进 `ChaosAbilityTriggerLimiter` 才能保证「同效果同时点」语义。

## 可复用的通用经验（与 Fool 无关也成立）

1. 原卡拆成「语义组件 + 命名槽位」，数值走 RuntimeSpec，别做整卡代理；共享组件复用而非复制。
2. 关键词 / 标签必须是**显式元数据**，命名语法只负责展示；从牌名反解析行为迟早出错。
3. 生成期校验（可追加、锚点、消耗依赖）比运行时兜底便宜得多，而且错误能在自检里暴露。
4. 有次数上限的递归触发要显式建模「时点纪元 + 单效果配额」。
5. 新角色接入 = 池替换 + 起始牌组替换 + 枚举值**追加**（保证旧存档身份不漂移），三件事都要有回归。
