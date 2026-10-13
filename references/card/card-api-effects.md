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

## 攻击结果汇总（按实际造成伤害叠层）

> 实战验证（STS2_MarisaMod 2026-10-14，FourOfASpark）。`Execute()` 返回攻击命令对象，`Results` 里是**每个目标实际结算结果**——汇总未格挡伤害（`UnblockedDamage`），按真实造成伤害量给目标叠 debuff：

```csharp
var attackCommand = await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
    .Targeting(cardPlay.Target)
    .WithHitFx("vfx/vfx_attack_slash")
    .Execute(choiceContext);

// 实际造成伤害 = 所有目标未格挡伤害之和（格挡掉的、miss 的不算）
var amount = attackCommand.Results.SelectMany(r => r).Sum(r => r.UnblockedDamage);
if (amount > 0)
{
    await PowerCmd.Apply<HoukaiPower>(choiceContext, cardPlay.Target, amount, Owner.Creature, this);
}
```

- `attackCommand.Results` 结构：`IEnumerable<IEnumerable<结果条目>>`（外层按目标），`SelectMany(r => r)` 展平后 `Sum(r => r.UnblockedDamage)` 汇总
- 用途：**伤害联动类效果**（造成 X 伤害 → 叠 X 层、吸血、计数等）不依赖伤害变量，而是按实际结算值

## 队友全员发卡（生成 Token 卡入抽牌堆）

> 实战验证（STS2_MarisaMod 2026-10-14，FourOfASpark）。给每个存活玩家队友各生成一张 Token 卡，随机塞进抽牌堆（配合 [card-amplify-examples.md](card-amplify-examples.md) 的 TabooSpark）：

```csharp
var teammates = CombatState?.GetTeammatesOf(Owner.Creature)
    .Where(c => c is { IsAlive: true, IsPlayer: true });
foreach (var creature in teammates)
{
    if (creature.Player != null)
    {
        // 按玩家创建 Token 卡（CombatState.CreateCard<T> 第二参 = 卡牌归属玩家）
        var card = CombatState?.CreateCard<TabooSpark>(creature.Player);
        var result = await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, Owner, CardPilePosition.Random);

        // 本机玩家看到抽牌堆预览动画（非本机不播，防串扰）
        if (LocalContext.IsMe(creature))
        {
            CardCmd.PreviewCardPileAdd(result);
        }
    }
}
```

- `CombatState.CreateCard<T>(player)`：按归属玩家创建卡实例（本地化/所有权正确）
- `CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, owner, CardPilePosition.Random)`：生成的卡随机入抽牌堆，`owner` 为发卡方
- `LocalContext.IsMe(creature)`（命名空间 `MegaCrit.Sts2.Core.Context`，非 Multiplayer）：判断该玩家是否本机——多人关键，只有本机才播 `PreviewCardPileAdd` 预览