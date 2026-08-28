# 实战写法模式：卡牌

## 卡牌

### 单目标攻击

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
    await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
        .FromCard(this)
        .Targeting(cardPlay.Target)
        .Execute(choiceContext);
}
```

### 多段攻击（3 次）

```csharp
await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
    .FromCard(this).Targeting(cardPlay.Target)
    .WithHitCount(3)
    .Execute(choiceContext);
```

### AOE 攻击（所有敌人）

```csharp
await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
    .FromCard(this)
    .TargetingAllOpponents(CombatState)
    .Execute(choiceContext);
```

### 攻击 + 施加能力

```csharp
ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
    .FromCard(this).Targeting(cardPlay.Target)
    .Execute(choiceContext);

await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, 1, Owner.Creature, this);
```

### 格挡

```csharp
// 真实签名：CreatureCmd.GainBlock(Creature, decimal, ValueProp, CardPlay?, bool)
await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);
```

### 抽牌

```csharp
// 真实签名：CardPileCmd.Draw(PlayerChoiceContext, decimal, Player, bool)
await CardPileCmd.Draw(choiceContext, 2, Owner);
```

### 升级

```csharp
protected override void OnUpgrade()
{
    DynamicVars.Damage.UpgradeValueBy(3m);
}
```

### 选择手牌消耗

```csharp
var selected = await CardSelectCmd.FromHand(choiceContext, Owner,
    new CardSelectorPrefs("选择一张牌消耗", selectCount: 1),
    card => card != this, this);
foreach (var card in selected) await CardCmd.Exhaust(choiceContext, card);
```

### 选择手牌升级

```csharp
// 真实签名：FromHandForUpgrade(context, player, source)（无 filter 参数）
var card = await CardSelectCmd.FromHandForUpgrade(choiceContext, Owner, this);
if (card != null) CardCmd.Upgrade(card);
```

### 使用条件（属性，不是方法）

```csharp
// IsPlayable / ShouldGlowGoldInternal 都是 protected virtual 属性
protected override bool IsPlayable => Owner.Gold >= 100;

protected override bool ShouldGlowGoldInternal => Owner.Gold >= 100;
```

