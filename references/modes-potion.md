# 模式三：自定义药水

## 最简示例（伤害药水）

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;

namespace MyMod.Potions;

public class DamagePotion : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;

    protected override string BigIconPath => "res://MyMod/images/potions/damage_potion.png";
    public override string PackedIconPath => BigIconPath;
    protected override string PackedIconOutlinePath => BigIconPath;

    protected override async Task OnUse(PlayerChoiceContext ctx, PotionUse use)
    {
        await DamageCmd.Attack(20)
            .FromCard(null) // 药水没有来源卡牌
            .TargetingAllOpponents(ctx.CombatState)
            .Execute();
    }
}
```

## PotionUsage 枚举

| 值 | 说明 |
|----|------|
| `CombatOnly` | 仅战斗中使用 |
| `AnyTime` | 随时可用（如鲜血药水） |
| `Triggered` | 代码触发（如瓶中精灵） |

## PotionRarity 枚举

`Common` / `Uncommon` / `Rare`

## 效果药水示例（自伤换 BUFF）

```csharp
public class BuffPotion : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;

    protected override async Task OnUse(PlayerChoiceContext ctx, PotionUse use)
    {
        await PlayerCmd.Damage(ctx.Player, 10, null);
        await CreatureCmd.ApplyPower(ctx.Player.Creature, typeof(IntangiblePower), 2,
            ctx.Player.Creature, null);
    }
}
```

## 自动触发药水示例（死亡时复活）

```csharp
public class RevivePotion : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.Triggered;
    public override bool CanBeGeneratedInCombat => false; // 不在随机药水中出现

    protected override async Task OnUse(PlayerChoiceContext ctx, PotionUse use) { /* 手动使用无效 */ }

    // 阻止死亡
    public override bool ShouldDie(Creature creature) => creature == Owner?.Creature ? false : true;

    // 阻止死亡后触发
    public override async Task AfterPreventingDeath(Creature creature)
    {
        await PlayerCmd.Heal(Owner, 10);
        // 消耗药水：从药水栏移除
    }
}
```

## 注册到药水池

```csharp
ModHelper.AddModelToPool<SharedPotionPool, DamagePotion>();
```

## 药水池列表

`SharedPotionPool` / `IroncladPotionPool` / `SilentPotionPool` / `DefectPotionPool` / `NecrobinderPotionPool` / `RegentPotionPool`