# 自定义附魔

> 参考：[杀戮尖塔2模组开发教程05 - 自定义附魔 - 哔哩哔哩](https://www.bilibili.com/opus/1180713881530531843)（from 烟汐忆梦_YM）

---

## 概述

所有附魔效果继承 `EnchantmentModel` 抽象类。附魔附着在卡牌上，修改卡牌的行为。比自定义卡牌简单很多，核心是计算动态变量数值与监听附魔目标卡牌。

---

## 基础附魔模板

```csharp
using STS2;
using STS2.Cards;
using STS2.Core.Cards;
using Godot;

public class MyEnchantment : EnchantmentModel
{
    public MyEnchantment() : base()
    {
    }
}
```

---

## 核心回调

### EnchantDamageAdditive — 计算伤害加成

```csharp
// 计算附魔对卡牌造成的伤害增量
public override int EnchantDamageAdditive(CardModel card)
{
    return Amount;  // 附魔层数
}
```

### EnchantBlockAdditive — 计算格挡加成

```csharp
// 计算附魔对卡牌造成的格挡增量
public override int EnchantBlockAdditive(CardModel card)
{
    return Amount;
}
```

### Amount — 附魔层数

```csharp
// 附魔量/层数/等级
public int Amount { get; set; }
```

---

## 附魔案例：攻击卡增伤+去消耗+打出给格挡

```csharp
public class ExampleEnchantment : EnchantmentModel
{
    // 附魔只能附到攻击卡上
    public override bool CanEnchant(CardModel card)
    {
        return card.Type == CardType.Attack;
    }

    // 附魔时：去掉消耗关键词
    public override void OnEnchant(CardModel card)
    {
        var keywords = card.CanonicalKeywords.ToList();
        keywords.Remove(CardKeyword.Exhaust);
        // 应用修改后的关键词
    }

    // 伤害加成 = 附魔层数
    public override int EnchantDamageAdditive(CardModel card)
    {
        return Amount;
    }

    // 打出卡牌时：给予玩家 Amount 点格挡
    public override void OnCardPlayed(CardModel card, PlayerChoiceContext context)
    {
        // 给予玩家 Amount 点格挡
    }
}
```

---

## 应用附魔

```csharp
// 为指定卡牌附魔 N 层
CardCmd.Enchant(card, amount);
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `card` | `CardModel` | 要附魔的卡牌实例 |
| `amount` | `int` | 附魔层数 |

---

## 附魔回调速查

| 回调 | 触发时机 | 说明 |
|------|---------|------|
| `CanEnchant(CardModel)` | 附魔前 | 判断是否可附魔到该卡牌 |
| `OnEnchant(CardModel)` | 附魔时 | 附魔效果应用 |
| `EnchantDamageAdditive(CardModel)` | 计算伤害 | 返回伤害增量 |
| `EnchantBlockAdditive(CardModel)` | 计算格挡 | 返回格挡增量 |
| `OnCardPlayed(CardModel, PlayerChoiceContext)` | 卡牌打出 | 响应卡牌打出事件 |

---

## 本地化

路径：`res://<模组ID>/localization/<语言代码>/enchantments.json`

```json
{
  "ExampleEnchantment": {
    "name": "示例附魔",
    "description": "增加 {Amount} 点伤害。"
  }
}
```

---

## 图标

```
附魔图标：res://images/enchantments/<附魔ID小写>.png
```

---

## 调试

按反单引号 `` ` `` 打开控制台，输入命令为手牌指定位置的卡牌附魔：

```
enchant <手牌位置> <附魔ID> <层数>
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 附魔不生效 | 检查 `CanEnchant` 是否返回 true |
| 伤害/格挡加成无效 | 重写 `EnchantDamageAdditive` / `EnchantBlockAdditive` |
| 图标不显示 | PNG 命名与附魔 ID 小写一致 |
| 本地化不生效 | 必须 Publish 而非 Build |

---

## 演进路线

- 当前写法：手动重写回调
- 更优方案：可参考 YuWanCard 封装更简洁的附魔 Builder 模式