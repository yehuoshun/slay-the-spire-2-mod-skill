# 自定义能力：真实回调与本地化

## 真实回调

> 所有 `After*` 钩子定义在 `AbstractModel`（能力/遗物/卡牌通用），`Modify*`/`BeforeApplied` 等由 PowerModel 提供。

### 伤害 / 格挡修正（核心）

| 回调 | 签名 | 说明 |
|------|------|------|
| `ModifyDamageAdditive` | `decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)` | 伤害加减（力量） |
| `ModifyDamageMultiplicative` | 同左 5 参 | 伤害乘除（易伤） |
| `ModifyBlockAdditive` | `decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)` | 格挡加减 |
| `ModifyBlockMultiplicative` | 同左 5 参 | 格挡乘除 |

### 生命周期 / 事件钩子（AbstractModel）

| 回调 | 签名 | 说明 |
|------|------|------|
| `AfterCardPlayed` | `Task AfterCardPlayed(PlayerChoiceContext, CardPlay)` | 持有者打出任意牌后 |
| `AfterSideTurnEnd` | `Task AfterSideTurnEnd(PlayerChoiceContext, CombatSide, IEnumerable<Creature>)` | 某方回合结束后 |
| `AfterSideTurnStart` | `Task AfterSideTurnStart(CombatSide, IReadOnlyList<Creature>, ICombatState)` | 某方回合开始后 |
| `BeforeSideTurnStart` | `Task BeforeSideTurnStart(PlayerChoiceContext, CombatSide, IReadOnlyList<Creature>, ICombatState)` | 某方回合开始前 |

### PowerModel 自带

| 回调 | 签名 | 说明 |
|------|------|------|
| `BeforeApplied` | `Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)` | 施加前 |
| `AfterApplied` | `Task AfterApplied(Creature? applier, CardModel? cardSource)` | 施加后 |
| `AfterRemoved` | `Task AfterRemoved(Creature oldOwner)` | 移除后 |
| `ShouldPowerBeRemovedAfterOwnerDeath` | `bool` | 持有者死亡时是否移除 |

## 实战回调代码片段

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

### 回合开始获得格挡

```csharp
public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
{
    if (participants.Contains(Owner))
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
}
```

