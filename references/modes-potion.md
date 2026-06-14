# 自定义药水

---

## 最简药水

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MyMod.Potions;

public sealed class FirePotion : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override TargetType TargetType => TargetType.AnyEnemy;
    public override string PortraitPath => "res://MyMod/images/potions/fire.png";
    public override IEnumerable<string> AllPortraitPaths => [PortraitPath];

    public FirePotion() : base()
    {
    }

    public override async Task Use(PlayerChoiceContext context, Creature target)
    {
        await CreatureCmd.Damage(context, target, 20, Owner.Creature, this);
    }
}
```

---

## Usage 决定使用场景

| Usage | 说明 |
|-------|------|
| `PotionUsage.Combat` | 仅战斗中使用 |
| `PotionUsage.OutOfCombat` | 战斗外使用 |
| `PotionUsage.Any` | 任意场景 |

---

## 治疗药水

```csharp
public sealed class HealingSalve : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override TargetType TargetType => TargetType.Self;
    public override PotionUsage Usage => PotionUsage.Any;

    public override async Task Use(PlayerChoiceContext context, Creature target)
    {
        await PlayerCmd.Heal(context, 10);
    }
}
```

---

## 目标型药水

```csharp
public sealed class WeakeningDraft : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override TargetType TargetType => TargetType.AnyEnemy;

    public override async Task Use(PlayerChoiceContext context, Creature target)
    {
        await PowerCmd.Apply<WeakPower>(target, 3, Owner.Creature, this);
    }
}
```

---

## 本地化

`assets/localization/zhs/potions.json`：

```json
{
  "FirePotion": {
    "NAME": "火焰药水",
    "DESCRIPTION": "对一名敌人造成 20 点伤害。"
  }
}
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 药水不出现 | 确认 `PotionRarity` 不是 `Special` |
| 战斗外无法使用 | 改 `Usage = PotionUsage.Any` |