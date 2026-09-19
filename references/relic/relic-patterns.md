# 自定义遗物：实战回调代码片段

> 钩子签名表见 [relic-callbacks.md](relic-callbacks.md)。

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