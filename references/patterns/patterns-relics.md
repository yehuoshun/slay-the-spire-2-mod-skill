# 实战写法模式：遗物与初始遗物修改

## 遗物

### 回合开始给能量

```csharp
protected override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
{
    if (!participants.Contains(Owner.Creature)) return;
    Flash();
    await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);  // (decimal, Player)
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

---

## 修改角色初始遗物

```csharp
[HarmonyPatch(typeof(Ironclad), nameof(Ironclad.StartingRelics), MethodType.Getter)]
public static class IroncladStartingRelicsPatch
{
    private static void Postfix(Ironclad __instance, ref IReadOnlyList<RelicModel> __result)
    {
        var list = new List<RelicModel>(__result) { ModelDb.Relic<MyCustomRelic>() };
        __result = list;
    }
}
```

