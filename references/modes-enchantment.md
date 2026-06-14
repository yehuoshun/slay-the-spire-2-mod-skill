# 自定义附魔

> 附魔是 STS2 的特色系统——给卡牌附加额外属性。

---

## 最简附魔（加伤害）

```csharp
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace MyMod.Enchantments;

public sealed class SharpEnchantment : EnchantmentModel
{
    public override EnchantmentRarity Rarity => EnchantmentRarity.Common;
    public override string PortraitPath => "res://MyMod/images/enchantments/sharp.png";
    public override IEnumerable<string> AllPortraitPaths => [PortraitPath];

    public SharpEnchantment() : base()
    {
    }

    public override void ModifyCard(CardModel card)
    {
        // 给卡牌加 3 点伤害
        var damageVar = card.DynamicVars.FirstOrDefault(v => v is DamageVar) as DamageVar;
        if (damageVar != null)
        {
            damageVar.BaseValue += 3;
        }
    }
}
```

---

## 附魔基类关键成员

| 成员 | 说明 |
|------|------|
| `Rarity` | Common / Uncommon / Rare |
| `ModifyCard(card)` | 核心方法——修改卡牌属性 |
| `PortraitPath` / `AllPortraitPaths` | 图标 |
| `CanEnchant(card)` | 覆盖以限制可附魔的卡牌类型 |

---

## 限定卡牌类型的附魔

```csharp
public sealed class AttackOnlyEnchantment : EnchantmentModel
{
    public override bool CanEnchant(CardModel card)
    {
        return card.Type == CardType.Attack;
    }

    public override void ModifyCard(CardModel card)
    {
        // 只对攻击牌生效
    }
}
```

---

## 附带能力的附魔

```csharp
public sealed class PoisonousEnchantment : EnchantmentModel
{
    public override void ModifyCard(CardModel card)
    {
        // 给卡牌附加中毒能力
        // 需要 Patch 卡牌打出逻辑来应用 PowerCmd.Apply<PoisonPower>
    }
}
```

---

## 本地化

`assets/localization/zhs/enchantments.json`：

```json
{
  "SharpEnchantment": {
    "NAME": "锋利",
    "DESCRIPTION": "附魔后卡牌伤害 +3。"
  }
}
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 附魔不出现在选择界面 | 检查 `Rarity` 和 `CanEnchant` 逻辑 |
| ModifyCard 不生效 | 确保在卡牌实例化后调用，检查变量名匹配 |