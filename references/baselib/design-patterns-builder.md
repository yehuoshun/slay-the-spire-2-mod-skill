# 纯原生设计模式：链式辅助方法（转译 Builder）

> 从 design-patterns-core.md 拆出。学自 BaseLib `ConstructedCardModel` Builder 模式。

## BaseLib 的做法

`ConstructedCardModel` 用链式 Builder 一行定义卡牌：`.WithCost(1).WithDamage(6).WithUpgrade(...)`。

## 纯原生转译

原生 API 本身已经支持链式（`DamageCmd.Attack().FromCard().Targeting().Execute()`），可以再包一层静态辅助，把高频重复操作收敛成一行：

```csharp
public static class CardFx
{
    // 攻击：来源是当前卡牌，打指定目标
    public static Task DealDamage(this CardModel card, PlayerChoiceContext ctx,
        CardPlay play, int damage, int hits = 1, string? hitFx = null)
    {
        ArgumentNullException.ThrowIfNull(play.Target, "play.Target");
        var cmd = DamageCmd.Attack(damage).FromCard(card).Targeting(play.Target)
            .WithHitCount(hits);
        if (hitFx != null) cmd = cmd.WithHitFx(hitFx);
        return cmd.Execute(ctx);
    }

    // 攻击所有敌人
    public static Task DealDamageAll(this CardModel card, PlayerChoiceContext ctx,
        CardPlay play, int damage)
        => DamageCmd.Attack(damage).FromCard(card)
            .TargetingAllOpponents(card.CombatState).Execute(ctx);

    // 抽牌（真实签名：Draw(PlayerChoiceContext, decimal, Player, bool)）
    public static Task Draw(this CardModel card, PlayerChoiceContext ctx, int count)
        => CardPileCmd.Draw(ctx, count, card.Owner, false);
}

// 用法：一行完成攻击+抽牌
protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
{
    ArgumentNullException.ThrowIfNull(play.Target, "play.Target");
    await Task.WhenAll(
        this.DealDamage(ctx, play, 6),
        this.DealDamage(ctx, play, 3));
}
```

## 关键点

- 辅助方法只是收敛重复，**不改变原生行为**
- 扩展方法命名要带模块前缀（`CardFx` / `RelicFx` / `PowerFx`），避免与其他 Mod 冲突
- 保留原生链式能力，辅助方法内部仍走 `DamageCmd` / `CardPileCmd` 等原生命令
