# 自定义卡牌：增幅实战案例（Token 增幅卡 + 动态增幅伤害）

> 实战验证（STS2_MarisaMod 2026-10-14 增量，TabooSpark）。增幅系统基类与 3 个 Patch 见 [card-amplify.md](card-amplify.md)。本节是增幅卡进阶用法：① **Token 卡池增幅卡**（不进角色卡池，由其他卡生成给队友）；② **增幅伤害动态化**——增幅态伤害 = 计算伤害（基础计算值 × 目标身上的崩坏层数乘数）。

## 案例：TabooSpark（Token 增幅卡）

```csharp
[Pool(typeof(TokenCardPool))]
public class TabooSpark : AbstractAmplifiedCard
{
    public TabooSpark() : base(0, 1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6, ValueProp.Move),
        new CalculationBaseVar(6),
        new ExtraDamageVar(1),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier((_, monster) => monster?.GetPowerAmount<HoukaiPower>() ?? 0),
    ];

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars.CalculationBase.UpgradeValueBy(3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.OnPlay(choiceContext, cardPlay);
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var damage = !AmplifiedInPlay ? DynamicVars.Damage.BaseValue : DynamicVars.CalculatedDamage.BaseValue;
        await DamageCmd.Attack(damage).FromCard(this).Targeting(cardPlay.Target)
            .WithHitVfxNode(t => SparkHitVfx.Create(NCombatRoom.Instance?.GetCreatureNode(t)!, "BurstSpark"))
            .BeforeDamage(async delegate
            {
                var beam = NSweepingBeamVfx.Create(Owner.Creature, [cardPlay.Target])!;
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(beam);
                await Cmd.Wait(0.5f);
            }).Execute(choiceContext);
    }
}
```

> `AbstractAmplifiedCard` 为 MarisaMod 自研基类（增幅判定 + 费用结算，BaseLib 系）；纯原生等价基类 + Patch 见 [card-amplify.md](card-amplify.md) §1-§3。

## 关键点

| 要点 | 说明 |
|------|------|
| Token 卡池 | `[Pool(typeof(TokenCardPool))]` —— 白卡不进角色卡池，只由其他卡生成（本例由四重火花给队友各发一张，见 card-api-effects.md） |
| 增幅伤害动态化 | `CalculatedDamageVar(ValueProp.Move).WithMultiplier((_, monster) => monster?.GetPowerAmount<HoukaiPower>() ?? 0)` —— 增幅态伤害 = 计算伤害，lambda 从目标身上实时读 Power 层数作乘数（目标崩坏越多，增幅越痛） |
| 增幅分支取伤害 | `!AmplifiedInPlay ? DynamicVars.Damage.BaseValue : DynamicVars.CalculatedDamage.BaseValue` —— 未增幅打基础 6，增幅后改打「计算伤害」 |
| 升级双变量 | `OnUpgrade` 同时 `UpgradeValueBy(3)` Damage 和 CalculationBase，普通与增幅伤害同步成长 |
| 自定义命中 VFX | `WithHitVfxNode(t => ...)` 按目标节点创建命中特效；`BeforeDamage` 委托里播扫射光束：`NSweepingBeamVfx.Create(Owner.Creature, [target])` + `CombatVfxContainer.AddChildSafely(beam)` + `Cmd.Wait(0.5f)` |
