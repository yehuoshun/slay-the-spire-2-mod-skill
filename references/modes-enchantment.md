# 模式五：自定义附魔

## 最简示例

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace MyMod.Enchantments;

public class SharpEnchantment : EnchantmentModel
{
    // 增加攻击力
    public override decimal EnchantDamageAdditive(CardModel card) => Amount * 2;

    // 增加格挡
    public override decimal EnchantBlockAdditive(CardModel card) => Amount;

    protected override string IconPath => "res://MyMod/images/enchantments/sharp_enchantment.png";
}
```

## 核心回调

| 回调 | 用途 |
|------|------|
| `EnchantDamageAdditive(card)` | 修改卡牌的伤害加成 |
| `EnchantBlockAdditive(card)` | 修改卡牌的格挡加成 |
| `ModifyCard(card)` | 修改卡牌本身属性（如关键词） |

## 移除卡牌关键词示例

```csharp
public class RemoveExhaustEnchantment : EnchantmentModel
{
    public override void ModifyCard(CardModel card)
    {
        if (card.Keywords.Contains(CardKeyword.Exhaust))
            card.RemoveKeyword(CardKeyword.Exhaust);
    }

    protected override string IconPath => "res://MyMod/images/enchantments/remove_exhaust.png";
}
```

## 为卡牌附魔

```csharp
await CardCmd.Enchant(card, typeof(SharpEnchantment), 3); // 3 层附魔
```

## 控制台测试命令

```
# 为手牌第 N 张牌附魔
enchant_hand <N> <附魔ID> <层数>
```

## 附魔资源

| 资源 | 路径 |
|------|------|
| 图标 | `images/enchantments/<附魔ID小写>.png` |

## 附魔本地化

```json
// assets/localization/zhs/enchantments.json
{
  "SHARP_ENCHANTMENT": {
    "title": "锋利",
    "description": "增加 {Amount} 点伤害和 {Amount} 点格挡。"
  }
}
```