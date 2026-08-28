# 实战写法模式：能力

## 能力

### 伤害修正（力量式）

```csharp
public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
{
    if (Owner != dealer) return 0m;
    return props.IsPoweredAttack() ? Amount : 0m;
}
```

### 回合结束递减层数（临时能力）

```csharp
public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
{
    if (participants.Contains(Owner))
    {
        await PowerCmd.Decrement(this);
    }
}
```

### 卡牌打出时获得格挡

```csharp
public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, cardPlay);
}
```

### 临时能力（完整类）

```csharp
public class MyTempPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;   // 归零自动移除

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Decrement(this);
        }
    }
}
```

