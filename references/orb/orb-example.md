# 自定义球体：雷电球完整示例

## 完整示例：雷电球（对照真实 LightningOrb）

```csharp
public class LightningOrb : OrbModel
{
    public override Color DarkenedColor => new("796606");
    public override decimal PassiveVal => ModifyOrbValue(3m);
    public override decimal EvokeVal => ModifyOrbValue(8m);

    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext)
    {
        await Passive(choiceContext, null);
    }

    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        Trigger();
        await ApplyLightningDamage(PassiveVal, target, choiceContext);
    }

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext choiceContext)
    {
        return await ApplyLightningDamage(EvokeVal, null, choiceContext);
    }

    private async Task<IEnumerable<Creature>> ApplyLightningDamage(decimal value, Creature? target, PlayerChoiceContext choiceContext)
    {
        var enemies = CombatState.GetOpponentsOf(Owner.Creature).Where(e => e.IsHittable).ToList();
        if (enemies.Count == 0) return [];
        var targets = target == null
            ? new List<Creature> { Owner.RunState.Rng.CombatTargets.NextItem(enemies) }
            : new List<Creature> { target };
        VfxCmd.PlayOnCreature(targets[0], "vfx/vfx_attack_lightning");
        PlayEvokeSfx();
        return await CreatureCmd.Damage(choiceContext, targets, value, ValueProp.Unpowered, Owner.Creature);
    }
}
```

