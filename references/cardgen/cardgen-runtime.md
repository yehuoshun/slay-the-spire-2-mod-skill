# 运行时执行层（归属 / 身份 / 成长 / 升级）

> 来源：AutoAnthony v0.3.165 源码核对（`ChaosMode/ChaosCompositePower.cs`、`ChaosMode/ChaosOperationExecutor.cs`、`ChaosCardGenerator/ComponentAssemblyGenerator.cs`、`ChaosCardGenerator/OperationRuntimeSpec.cs`，HEAD 705dd61，STS2 0.113.x）。
> 这是「组件组装出来的卡真的在战斗里跑」的那一层：生成侧见 [cardgen-architecture.md](cardgen-architecture.md)，数值侧见 [cardgen-balance.md](cardgen-balance.md)。
> ⚠️ 定位红线同导航页：**生态参考 + 架构思想库，不是写普通卡的主路线**。

---

## 一、编译/估值入口的扩展点

内置 switch 不再垄断模板识别——**先问注册进来的组件包，再走内置映射**：

```csharp
// OperationRuntimeSpec.cs：编译入口最前面插一段
if (FoolComponentDefinitions.TryCompile(operation, out spec)) { failure = string.Empty; return true; }
spec = operation.Template switch { /* 内置模板 */ };
```

- 估值同理：`EffectBalanceModel` 里 `FoolComponentDefinitions.TryEstimate(atom.Template, spec, out v)` 排在 `ComponentValuationApi.TryEstimate` **之前**
- 新增 flag 走 `TemplateFlags` 表：如 `compact_hand_token_reference`（`joy`/`sadness` 这类生成到手牌的 token）
- 新增 opcode 只需一行 spec：`N:ReduceHandAttackCostUntilPlayed` → `("modify_cost","reduce_until_played","hand_cards")`；`CL:ForEachGoldThreshold` → `("template_modifier","gold_threshold")` + `RuntimeTriggerSpec("for_each_gold_threshold","immediate","threshold")`

> 教训：**扩展点要开在「数据路由」而不是「执行分支」**——组件包只负责把 opcode 编译成 RuntimeSpec，执行器仍然只认 RuntimeSpec。

## 二、多触发 Power 的操作归属（OwnsOperation）

一张卡的多条持久触发由一个 `ChaosCompositePower` 承载，而同一 slot/定义可能在场上存在两份战斗副本。做法：

- `[SavedProperty] ActivationRootIndex` 记「这个 Power 是从哪条 operation 激活的」
- `_ownedOperationMask` 沿 `triggerIndex` 父链求**最近的持久祖先**（`NearestPersistent`），首次使用构建、之后缓存
- 描述渲染、触发判定、修正符应用、成长累加全部先 `OwnsOperation(index)` 过滤（连 `FindOperationIndex` 都带这个谓词）
- 延迟监听器只在父触发时才安装，但**保留原 operation 索引**——捕获值、升级增量、卡身份、多人/存档 payload 都指向同一份定义

> 教训：多条触发共用一个 Power 时，「这条触发属于谁」必须存成可序列化的索引；靠定义相等去推断，一定会在同款副本上串改。

## 三、运行时身份的硬规则（定义相等 ≠ 卡身份）

| | 旧做法 | 现在的做法 |
|---|---|---|
| 找「施加效果的那张牌」 | `Definition.Slot + RuntimeProfileId + EffectiveDefinitionPayload` 全等 | ①`TriggerSources` 弱表里的**精确实例**（且未离场）②唯一匹配的 `DeckVersion` 副本；否则 `null` |
| 效果改动目标 | 找不到就退回定义匹配 | 找不到就**放弃这次改动**（静默不发生也比改错牌好） |
| 卡来源绑定 | 每次重新推断 | `PowerCmd.Apply(..., cardSource: parent?.SourceCombatCard ?? card)`，`SourceCombatCard` 移除/转移后仍保留 |

> 教训：给「效果改动」准备一个运行时唯一句柄（实例引用 / 版本号 / 显式索引），没有就**不做**；`DeckVersion` 在多份候选时必须唯一。
> 战斗内生成的卡没有 DeckVersion——不能靠「定义完全相等」当身份。

## 四、可累积状态存「增量」，不存「当前值」

- 成长型伤害（Rolling Boulder）：`[SavedProperty] int[] RollingDamageGrowth` 存 `(index, bonus)` 交替对；旧绝对字段 `RollingDamage` 只作一次迁移（`EnsureLegacyRollingGrowth`）
- 描述渲染把 growth 临时加进 RuntimeSpec 的 damage 槽再 render（`EffectiveDescriptionOperations`），描述与执行共用同一份数
- 多人校验算 growth 数组；绝对兼容值不参与新存档语义
- 选择上下文：`CaptureCombatSelection(player)` 记 `(CombatState 引用, CurrentCombatId)`，每次 `await` 之后用 `CanContinueCombatSelection` 再判一次；战斗结束/换场就丢弃结果，**不要把空结果当「部分选择」补齐**

> 教训：任何会累积的状态（成长、层数、预算）存「基准 + 增量对」，读的时候投影；这样迁移、复制、多人校验都不会出现「两份真相」。

## 五、升级只能有一个入口

- 旧：执行期逐条 `ApplyUpgradeDelta`，还要为 schema 1-7 猜 `slotId`
- 新：`ExecuteSpec` 一律来自 `CardUpgradeGenerator.GetUpgradedOperations(card.Generated)` 后取 `RequireStructured(operations[operationIndex])`
- 描述、变量、执行三处同源，升级不再出现「描述加了、执行没加」
- 数值 cap 与负面升级取 `Math.Max(1 - negativeUpgradeDelta, randomized)` 式下限保护，避免 cap 把升级增量反向吃掉

## 六、组装器的「排他形状」要先决定再组装

稀有 Shoot 卡一度「组装不出来」：先选普通组件、再决定要不要加 Shoot 修饰符，结果多效果组装总是先占满预算。

```csharp
// 开组装前，按原生 稀有度×卡型×目标 的覆盖率抽一次要不要这个独占形状
var wantsShoot = shoot is not null && shell.Type == GeneratedCardType.Attack
    && shell.Target == TargetMode.SingleEnemy && eligibleShootShells > 0
    && _random.NextDouble() < Math.Min(1d,
        shootRecipes.Where(r => r.Atoms.Any(FoolComponentRules.IsShootModifier))
            .Sum(CharacterComponentCatalogs.NativeOccurrenceWeight) / (double)eligibleShootShells);
```

- 命中后只允许「1 个 Shoot 修饰 + 1 个未链接的选中敌人伤害操作（无额外效果/触发/修饰）」，由 `FoolComponentRules.HasValidShootAssembly` 校验
- 组装循环里第二个修饰符直接 `continue`

> 教训：**「必须有某类修饰符」的形状，要在组件选择之前决定，并且按原生覆盖率抽**；放到之后就是被概率挤掉。

## 七、v0.113 运行期契约（生成卡执行侧踩到的）

| 行为 | 旧（≤0.111） | 0.113 |
|---|---|---|
| 卡牌选择器 | `CardSelectCmd.Selector`（全局单例） | `RunManager.Instance.CardSelectorStack.GetSelector(player)`（**每玩家**） |
| 选择取消 | 无此概念 | owner 死亡 / 战斗结束时原生选择被**取消**：空结果 ≠ 部分选择 |
| 击杀 | `CreatureCmd.Kill(creature)` | `CreatureCmd.Kill(creature, card)` |
| 伤害 cap 钩子 | `ValueProp.Unpowered \| Move` 走 `ModifyDamage` | `ValueProp.Move` + 显式 `ModifyDamageHookType.Cap` |
| 战斗内洗牌 | `RunState.Rng.Shuffle` | `RunState.Rng.CombatCardSelection` |
| 动态关键词 | `CardCmd.ApplyKeyword`（永久写卡） | `PowerModel.TryModifyKeywordsInCombat(card, keywords)`：Power 在则生效、移除即失效 |
| 打出后移动 | 直接 `CardPileCmd.Add` | `card.RequestPostPlayMove(destination)` |
| 旋律 / 箭矢 | — | `CardTag.Wah/Cho/Ki`、`MelodyCmd.MelodiesThisTurn/PlayedWah/Cho/KiThisTurn`、`ArrowCmd.ShootArrowAtTarget/CreatureIsImpaledByArrow/PlayerHasAnyArrows` |

> 通用迁移流程与其它 0.113 断点（动画器、伤害钩子 6 参、`FromCard` 必带 `cardPlay` 等）见 [setup/game-version-migration.md](../setup/game-version-migration.md)。
