# 自定义遗物：钩子与动态变量

## 修改角色初始遗物（Harmony 补丁）

```csharp
[HarmonyPatch(typeof(Ironclad), nameof(Ironclad.StartingRelics), MethodType.Getter)]
static class IroncladStartingRelicsPatch
{
    static void Postfix(Ironclad __instance, ref IReadOnlyList<RelicModel> __result)
        => __result = __result.Append(ModelDb.Relic<MyCustomRelic>()).ToList();
}
```

---

## 动态变量

`CanonicalVars` 返回 `IEnumerable<DynamicVar>`，用于代码和描述同步。**注意构造参数类型**：能量/金币/抽牌是 `int`，治疗/伤害/格挡是 `decimal`。

| 类型 | 构造 | 说明 |
|------|------|------|
| `EnergyVar` | `new EnergyVar(1)` | 能量（int） |
| `GoldVar` | `new GoldVar(10)` | 金币（int） |
| `HealVar` | `new HealVar(5m)` | 治疗（decimal） |
| `DamageVar` | `new DamageVar(10m, ValueProp.Move)` | 伤害（decimal + props） |
| `BlockVar` | `new BlockVar(5m, ValueProp.Unpowered)` | 格挡（decimal + props） |
| `CardsVar` | `new CardsVar(2)` | 抽牌数（int） |
| `DynamicVar` | `new DynamicVar("Count", 3m)` | 自定义（name + decimal） |

取值：`DynamicVars.<字段名>.BaseValue` 或 `.IntValue`

---

## 生命周期钩子（真实签名，来自 AbstractModel）

> 玩家受伤/击杀等用 `AfterDamageReceived`/`AfterDamageGiven` 实现（旧版 `OnPlayerDamaged`/`OnPlayerKill` 不存在）。

| 钩子 | 签名 | 触发时机 |
|------|------|---------|
| `AfterSideTurnStart` | `Task AfterSideTurnStart(CombatSide, IReadOnlyList<Creature>, ICombatState)` | 某方回合开始 |
| `BeforeSideTurnStart` | `Task BeforeSideTurnStart(PlayerChoiceContext, CombatSide, IReadOnlyList<Creature>, ICombatState)` | 某方回合开始前 |
| `AfterSideTurnEnd` | `Task AfterSideTurnEnd(PlayerChoiceContext, CombatSide, IEnumerable<Creature>)` | 某方回合结束 |
| `AfterPlayerTurnStart` | `Task AfterPlayerTurnStart(PlayerChoiceContext, Player)` | 玩家回合开始 |
| `AfterPlayerTurnEnd` | `Task AfterPlayerTurnEnd(PlayerChoiceContext, Player)` | 玩家回合结束 |
| `AfterCardPlayed` | `Task AfterCardPlayed(PlayerChoiceContext, CardPlay)` | 打出卡牌后 |
| `AfterCombatEnd` | `Task AfterCombatEnd(CombatRoom)` | 战斗结束 |
| `AfterCombatVictory` | `Task AfterCombatVictory(CombatRoom)` | 战斗胜利（推荐） |
| `AfterDamageReceived` | `Task AfterDamageReceived(PlayerChoiceContext, Creature target, DamageResult, ValueProp, Creature? dealer, CardModel?)` | 持有者受伤（target == Owner.Creature） |
| `AfterDamageGiven` | `Task AfterDamageGiven(PlayerChoiceContext, Creature? dealer, DamageResult, ValueProp, Creature target, CardModel?)` | 造成伤害（含击杀判断） |
| `ModifyNextEvent` | `EventModel ModifyNextEvent(EventModel)` | 进下个事件前替换（见 event-core.md） |
| `BeforeBlockGained` / `AfterBlockGained` | `Task ...(Creature, decimal, ValueProp, CardModel?)` | 获得格挡前后 |

> 判断持有者是否参与回合：`participants.Contains(Owner.Creature)`（真实遗物写法）。

---

## 数值 / 奖励修改钩子

> 实战项目验证（YuWanCard 真实代码）。除事件钩子外，遗物还可覆写以下数值/奖励修改钩子。

| `ModifyDamageMultiplicative` | `decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)` | 伤害倍率 |
| `ModifyBlockMultiplicative` | `decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)` | 格挡倍率 |
| `ModifyMaxEnergy` | `decimal ModifyMaxEnergy(Player player, decimal amount)` | 最大能量 |
| `ModifyHandDraw` | `decimal ModifyHandDraw(Player player, decimal count)` | 抽牌数（⚠️ decimal 非 int） |
| `ModifyRestSiteHealAmount` | `decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)` | 休息处回复（⚠️ 第一参 Creature） |
| `ModifyGoldGained` | `decimal ModifyGoldGained(Player player, decimal amount)` | 金币获得量 |
| `ModifyPowerAmountGivenAdditive` | `decimal ModifyPowerAmountGivenAdditive(PowerModel, Creature giver, decimal, Creature? target, CardModel?)` | 施加量加减 |
| `ModifyPowerAmountGivenMultiplicative` | 同上 | 施加量乘除 |
| `TryModifyRewards` | `bool TryModifyRewards(Player, List<Reward>, AbstractRoom?)` | 战斗奖励（⚠️ 3 参） |
| `TryModifyCardRewardOptions` | `bool TryModifyCardRewardOptions(Player, List<CardCreationResult>, CardCreationOptions)` | 替换奖励卡牌 |
| `AfterModifyingGoldGained` | `Task AfterModifyingGoldGained(Player player, decimal amount)` | 金币修改后副作用 |

> 金币修改防递归（`_modifyingGold` 守卫）见 [relic-patterns.md](relic-patterns.md)。

