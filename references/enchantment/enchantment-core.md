# 自定义附魔：模板、回调与案例

## 基础附魔模板

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

public class MyEnchantment : EnchantmentModel
{
    // 显示层数徽标
    public override bool ShowAmount => true;
    public override bool HasExtraCardText => true;

    // 动态变量（伤害加成 = 层数）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(0m, ValueProp.Move) };

    // 伤害增量（真实签名：decimal + ValueProp 双参）
    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
    {
        return originalDamage + Amount;
    }

    // 打出附魔卡时触发（不是编造的 OnCardPlayed）
    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        await CreatureCmd.GainBlock(Card.Owner.Creature, DynamicVars.Damage.BaseValue, ValueProp.Move, cardPlay);
    }

    // 数值跟随层数（真实附魔写法，对照 Adroit）
    public override void RecalculateValues()
    {
        DynamicVars.Damage.BaseValue = Amount;
    }
}
```

---

## 核心回调（真实签名）

### 伤害/格挡修正

```csharp
// 伤害增量（双参：基础伤害 + 属性）
public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props) => originalDamage + Amount;

// 伤害乘算
public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props) => originalDamage;

// 格挡增量（单参）
public override decimal EnchantBlockAdditive(decimal originalBlock) => originalBlock + Amount;

// 格挡乘算
public override decimal EnchantBlockMultiplicative(decimal originalBlock) => originalBlock;
```

### 其他真实回调

| 回调 | 签名 | 说明 |
|------|------|------|
| `CanEnchant` | `bool CanEnchant(CardModel)` | 附魔前判断（自动先查 `CanEnchantCardType`） |
| `CanEnchantCardType` | `bool CanEnchantCardType(CardType)` | 按卡牌类型过滤 |
| `OnEnchant` | `protected void OnEnchant()` | **无参**，附魔应用时（旧版写 `OnEnchant(CardModel)` 是错的） |
| `OnPlay` | `Task OnPlay(PlayerChoiceContext, CardPlay?)` | 附魔卡被打出时 |
| `RecalculateValues` | `void RecalculateValues()` | 值重算（附魔层数变化后） |
| `ShowAmount` | `bool` | 是否显示层数徽标 |
| `HasExtraCardText` | `bool` | 是否有额外卡面文本 |
| `IsStackable` | `bool` | 是否可叠加 |
| `Status` | `EnchantmentStatus`（`Normal`/`Disabled`） | 附魔状态 |

> ⚠️ 旧版写的 `OnCardPlayed(CardModel, PlayerChoiceContext)` **不存在**，卡牌打出用 `OnPlay(PlayerChoiceContext, CardPlay?)`。

---

## 附魔案例：攻击卡增伤 + 打出给格挡

```csharp
public class ExampleEnchantment : EnchantmentModel
{
    public override bool ShowAmount => true;

    // 附魔只能附到攻击卡上
    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType == CardType.Attack;
    }

    // 伤害增量 = 附魔层数（真实签名）
    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
    {
        return originalDamage + Amount;
    }

    // 打出附魔卡时：给持有者 Amount 点格挡
    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        await CreatureCmd.GainBlock(Card.Owner.Creature, Amount, ValueProp.Move, cardPlay);
    }

    // 数值跟随层数
    public override void RecalculateValues()
    {
        // 如需动态变量同步：DynamicVars.X.BaseValue = Amount;
    }
}
```

