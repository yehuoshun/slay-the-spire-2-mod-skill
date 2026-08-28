# 自定义药水：模板、图标与属性

## 基础药水模板

```csharp
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.ValueProps;

public class MyAoePotion : PotionModel
{
    // 抽象属性，必须 override（不能构造函数赋值）
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    // 动态变量（数值与描述同步）
    protected override IEnumerable<DynamicVar> CanonicalVars => [ new DamageVar(30m, ValueProp.Move) ];

    // 使用药水：对所有可攻击敌人造成 30 点伤害
    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var enemies = Owner.Creature.CombatState.HittableEnemies.ToList();
        await CreatureCmd.Damage(choiceContext, enemies, DynamicVars.Damage.BaseValue, ValueProp.Move, Owner.Creature);
    }
}
```

---

## 图标（命名约定，不可 override）

药水图标路径由原生 `PotionModel` **私有自动生成**（不可 override），规则如下：

```
药水裁切纹理：     atlases/potion_atlas.sprites/<药水ID小写>.tres
药水描边裁切纹理： atlases/potion_outline_atlas.sprites/<药水ID小写>.tres
```

只需把对应 `.tres`/`.png` 资源放到上述路径即可，无需写代码。

---

## 属性

### Rarity — 稀有度

| 值 | 说明 |
|-----|------|
| `PotionRarity.None` | 无 |
| `PotionRarity.Common` | 普通，随机药水池可获取 |
| `PotionRarity.Uncommon` | 罕见，随机药水池可获取 |
| `PotionRarity.Rare` | 稀有，随机药水池可获取 |
| `PotionRarity.Event` | 事件专属 |
| `PotionRarity.Token` | 衍生物 |

### Usage — 使用时机

| 值 | 说明 |
|-----|------|
| `PotionUsage.None` | 无 |
| `PotionUsage.CombatOnly` | 仅战斗场景可用，非战斗时禁用 |
| `PotionUsage.AnyTime` | 任意场景可用（包括战斗外） |
| `PotionUsage.Automatic` | 不能主动触发，只能通过代码自动触发使用 |

