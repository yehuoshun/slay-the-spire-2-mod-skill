# 自定义遗物：钩子与动态变量

## 修改角色初始遗物（Harmony 补丁）

```csharp
[HarmonyPatch(typeof(Ironclad), nameof(Ironclad.StartingRelics), MethodType.Getter)]
public static class IroncladStartingRelicsPatch
{
    private static void Postfix(Ironclad __instance, ref IReadOnlyList<RelicModel> __result)
    {
        var list = new List<RelicModel>(__result)
        {
            ModelDb.Relic<MyCustomRelic>()
        };
        __result = list;
    }
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
| `BeforeBlockGained` / `AfterBlockGained` | `Task ...(Creature, decimal, ValueProp, CardModel?)` | 获得格挡前后 |

> 判断持有者是否参与回合：`participants.Contains(Owner.Creature)`（真实遗物写法）。

---

## 实战回调代码片段

### 回合开始给能量

```csharp
protected override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
{
    if (!participants.Contains(Owner.Creature)) return;
    Flash();
    await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
}
```

### 卡牌打出时触发

```csharp
public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    if (cardPlay.Card.Type == CardType.Attack)
    {
        Flash();
        // 攻击牌触发效果
    }
}
```

### 战斗胜利时触发

```csharp
public override async Task AfterCombatVictory(CombatRoom room)
{
    Flash();
    await CreatureCmd.Heal(Owner.Creature, 6);
}
```

### 持有者受伤时触发

```csharp
public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
{
    if (target != Owner.Creature) return;
    Flash();
    // 受伤效果
}
```

### 每回合首次受伤减半

```csharp
private bool _alreadyUsedThisTurn;

public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
{
    if (participants.Contains(Owner.Creature))
        _alreadyUsedThisTurn = false;
}

public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
{
    if (target != Owner.Creature || _alreadyUsedThisTurn) return;
    _alreadyUsedThisTurn = true;
    Flash();
    // 减半伤害逻辑
}
```

