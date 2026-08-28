# 自定义附魔

> 参考：[杀戮尖塔2模组开发教程05 - 自定义附魔 - 哔哩哔哩](https://www.bilibili.com/opus/1180713881530531843)（from 烟汐忆梦_YM）

---

## 概述

所有附魔效果继承 `EnchantmentModel` 抽象类。附魔附着在卡牌上（`Card` 属性），修改卡牌的行为。核心是：`CanonicalVars` 定义数值、`Enchant*Additive/Multiplicative` 改伤害/格挡、`OnPlay` 响应打出、`RecalculateValues` 让数值跟随附魔层数。

---

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

---

## 应用附魔

```csharp
// 为指定卡牌附魔 N 层（泛型指定附魔类型）
CardCmd.Enchant<ExampleEnchantment>(card, amount);

// 或传入附魔实例
CardCmd.Enchant(ModelDb.Enchantment<ExampleEnchantment>().ToMutable(), card, amount);
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `card` | `CardModel` | 要附魔的卡牌实例 |
| `amount` | `decimal` | 附魔层数 |

---

## 附魔回调速查

| 回调 | 触发时机 | 签名 |
|------|---------|------|
| `CanEnchant(CardModel)` | 附魔前 | `bool`（先查 `CanEnchantCardType`） |
| `CanEnchantCardType(CardType)` | 附魔前 | `bool` |
| `OnEnchant()` | 附魔时 | `void`（无参） |
| `EnchantDamageAdditive(decimal, ValueProp)` | 计算伤害 | 返回伤害增量 |
| `EnchantDamageMultiplicative(decimal, ValueProp)` | 计算伤害 | 返回伤害乘算 |
| `EnchantBlockAdditive(decimal)` | 计算格挡 | 返回格挡增量 |
| `EnchantBlockMultiplicative(decimal)` | 计算格挡 | 返回格挡乘算 |
| `OnPlay(PlayerChoiceContext, CardPlay?)` | 附魔卡打出 | 响应打出 |
| `RecalculateValues()` | 层数变化 | 值重算 |

---

## 本地化

路径：`res://<模组ID>/localization/<语言代码>/enchantments.json`（locTable = `enchantments`）

```json
{
  "EXAMPLE_ENCHANTMENT": {
    "title": "示例附魔",
    "description": "增加 {Amount} 点伤害。"
  }
}
```

- `title`：自动读取 `<类名大写>.title`
- `description`：支持 `{Amount}` 与动态变量占位符
- 类名 ID 规则与卡牌一致（大驼峰 → 大写加下划线）

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
| 附魔不生效 | 检查 `CanEnchant`/`CanEnchantCardType` 是否放行 |
| 伤害/格挡加成无效 | 重写 `EnchantDamageAdditive(decimal, ValueProp)` / `EnchantBlockAdditive(decimal)` |
| 数值不随层数变化 | 重写 `RecalculateValues()` 同步 `DynamicVars` |
| 图标不显示 | PNG 命名与附魔 ID 小写一致（`enchantments/<id小写>.png`） |
| 本地化不生效 | 必须 Publish 而非 Build；键用 `title`/`description` |
| 打出卡无反应 | 用 `OnPlay(PlayerChoiceContext, CardPlay?)`（没有 OnCardPlayed） |

---

## 进阶：纯原生自动注册

> 从 BaseLib 提炼，零第三方依赖。用 `[EnchantmentModel]` 标记 + ContentRegistry 统一注册。
> 框架完整代码见 [serialization.md](../serialization/serialization.md)「进阶：纯原生自动注册框架」。

```csharp
[EnchantmentModel]
public class MyEnchantment : EnchantmentModel { ... }
```

> 附魔通过 `EnchantmentModel` 继承生效，注册方式与能力类似（`ModelDb.Inject`），用 Attribute 标记后由 ContentRegistry 统一处理。

## 演进路线

- 当前：手动注册（保留为基准写法）
- 纯原生自动注册（Attribute 标记 + ContentRegistry）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`EnchantDamageAdditive(CardModel)`、`OnEnchant(CardModel)`、`OnCardPlayed(CardModel, PlayerChoiceContext)`、`CardCmd.Enchant(card, amount)` 两参、`Amount { get; set; }`）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖