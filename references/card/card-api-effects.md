# 自定义卡牌：卡牌效果完整示例

> 接 [card-api.md](card-api.md) 主文。

---

## 攻击

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

## 攻击 + 施加能力

```csharp
ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
    .FromCard(this).Targeting(cardPlay.Target)
    .Execute(choiceContext);

await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, 1, Owner.Creature, this);
```

## 格挡

```csharp
// 真实签名：CreatureCmd.GainBlock(Creature, decimal, ValueProp, CardPlay?, bool)
await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);
```

## 抽牌

```csharp
// 真实签名：CardPileCmd.Draw(PlayerChoiceContext, decimal, Player, bool)
await CardPileCmd.Draw(choiceContext, 2, Owner);
```

## 升级

```csharp
protected override void OnUpgrade()
{
    DynamicVars.Damage.UpgradeValueBy(3m);
}
```

## 攻击 + 抽牌（常用组合）

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
    await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
        .Targeting(cardPlay.Target).Execute(choiceContext);
    await CardPileCmd.Draw(choiceContext, 1, Owner);
}
```