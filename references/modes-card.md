# 模式一：自定义卡牌

## 最简示例

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyMod.Cards;

public class StrikeCard : CardModel
{
    // 构造：(基础耗能, 卡牌类型, 稀有度, 目标类型, 是否在百科中显示)
    public StrikeCard() : base(1, CardType.Attack, CardRarity.Basic, TargetType.Opponent) { }

    // 卡面图路径
    public override string PortraitPath => "res://MyMod/images/cards/strike_card.png";

    // 动态变量
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(6, ValueProp.Move).WithUpgrade(3)
    ];

    // 打出回调
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage)
            .FromCard(this)
            .Targeting(ctx.Targets.First())
            .Execute();
    }

    // 升级回调
    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars.Damage.UpgradeValueBy(3); // 6→9
    }
}
```

## CardType 枚举

| 值 | 含义 |
|----|------|
| `Attack` | 攻击牌 |
| `Skill` | 技能牌 |
| `Power` | 能力牌 |
| `Status` | 状态牌 |
| `Curse` | 诅咒牌 |
| `Token` | 衍生牌（如小刀） |
| `Quest` | 任务牌 |

## CardRarity 枚举

| 值 | 获取方式 |
|----|---------|
| `Basic` | 初始卡组 |
| `Common` | 普通卡池 |
| `Uncommon` | 罕见卡池 |
| `Rare` | 稀有卡池 |
| `Event` | 事件获取 |
| `Token` | 衍生（不进入随机池） |
| `Status` | 状态牌 |
| `Curse` | 诅咒牌 |
| `Ancient` | 先古之民推荐 |
| `Quest` | 任务牌 |

## TargetType 枚举

| 值 | 说明 |
|----|------|
| `None` | 无目标（自施法） |
| `Opponent` | 单个敌人 |
| `AllOpponents` | 全部敌人 |
| `Ally` | 单个友军 |
| `AllAllies` | 全部友军 |
| `AnyEnemy` | 任意敌人 |
| `Self` | 自身 |
| `Osty` | 奥斯提（亡灵契约师宠物） |

## 卡牌标记

```csharp
protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike, CardTag.Defend];
```

## 卡牌关键词

```csharp
protected override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];
```

常用：`Exhaust`（消耗）/ `Retain`（保留）/ `Innate`（固有）/ `Ethereal`（虚无）/ `Unplayable`（无法打出）

## 注册到卡池

```csharp
// 在 ModEntry.Initialize() 中：
ModHelper.AddModelToPool<ColorlessCardPool, StrikeCard>();
```

## 抽牌 / 选牌 / 丢弃

```csharp
// 抽牌
await CardPileCmd.Draw(Owner, 2);

// 从手牌选择一张消耗
var selected = await CardSelectCmd.FromHandGeneric(ctx, Owner,
    new CardSelectorPrefs("选择要消耗的牌", 1), card => true, this);
await CardCmd.Discard(selected);

// 从手牌选择一张升级
var selected = await CardSelectCmd.FromHandForUpgrade(ctx, Owner,
    new CardSelectorPrefs("选择要升级的牌", 1), card => true, this);
selected.Upgrade();
```

## 出牌条件控制

```csharp
// 自定义打出条件
protected override bool IsPlayable => base.IsPlayable && Owner.Gold >= 100;

// 金色发光提示（满足条件时卡牌闪烁金光）
protected override bool ShouldGlowGoldInternal => Owner.Gold >= 100;
```

## DynamicVar 链式构造

```csharp
new DamageVar(6, ValueProp.Move).WithUpgrade(3)    // 伤害 6，升级 +3
new BlockVar(5, ValueProp.Move).WithUpgrade(2)     // 格挡 5，升级 +2
new EnergyVar(1)                                     // 能量 1
new HealVar(3).WithUpgrade(2)                       // 治疗 3，升级 +2
new CardsVar(2)                                      // 抽牌 2 张
new PowerVar<VulnerablePower>(2).WithUpgrade(1)     // 施加 2 层易伤，升级 3
```