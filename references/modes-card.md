# 自定义卡牌

> 三种写法：最简 → Builder → 生产级。选适合你的。

---

## 写法一：最简（1-2 张卡的小 Mod）

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyMod.Cards;

[CardPool(typeof(RedPool))]  // ← 自动注册
public sealed class HeavyStrikeCard : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<RedPool>();
    public override CardPoolModel VisualCardPool => Pool;
    public override string PortraitPath => "res://MyMod/images/cards/heavy_strike.png";
    public override IEnumerable<string> AllPortraitPaths => [PortraitPath];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(15m, ValueProp.Move)
    ];

    public HeavyStrikeCard()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;
        await CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars.Damage, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}
```

---

## 写法二：Builder 模式（学 YuWanCard）

适合卡牌多的 Mod。链式声明变量、关键词、HoverTip，代码干净。

```csharp
public sealed class HeavyStrikeCard : YuWanCardModel
{
    public HeavyStrikeCard()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        // 链式声明
        WithDamage(15, upgrade: 5);
        WithKeywords(CardKeyword.Heavy);
        WithTip(CardKeyword.Heavy);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;
        await CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars.Damage, Owner.Creature, this);
    }
}
```

**Builder 方法速查**：

| 方法 | 说明 |
|------|------|
| `WithDamage(base, upgrade)` | 伤害变量 |
| `WithBlock(base, upgrade)` | 格挡变量 |
| `WithCards(base, upgrade)` | 抽牌数 |
| `WithEnergy(base, upgrade)` | 能量变量 |
| `WithHeal(base, upgrade)` | 治疗变量 |
| `WithPower<T>(base, upgrade)` | 能力变量 + 自动 HoverTip |
| `WithVar(name, base, upgrade)` | 自定义变量 |
| `WithKeywords(...)` | 关键词 |
| `WithKeyword(kw, upgradeType)` | 带升级行为的关键词 |
| `WithTags(...)` | 标签 |
| `WithTip(Type)` | 自动 HoverTip（Power/Card/Potion/Enchantment） |
| `WithTip(CardKeyword)` | 关键词 HoverTip |
| `WithHandGlowGold(fn)` | 手牌金色高亮条件 |
| `WithHandGlowRed(fn)` | 手牌红色警告条件 |
| `WithCostUpgradeBy(n)` | 升级减费 |

---

## 写法三：生产级（学海克斯符文）

完全控制所有细节。适合复杂卡牌。

```csharp
public sealed class ComplexCard : CardModel
{
    public override CardPoolModel Pool => IsMutable && Owner != null
        ? Owner.Character.CardPool
        : ModelDb.CardPool<TokenCardPool>();

    public override CardPoolModel VisualCardPool => Pool;

    public override string PortraitPath => "res://MyMod/images/cards/complex.png";
    public override IEnumerable<string> AllPortraitPaths => [PortraitPath];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust,
        CardKeyword.Ethereal
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new BlockVar(5m, ValueProp.Move),
        new PowerVar<VulnerablePower>(2m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    public ComplexCard()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        await CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars.Damage, Owner.Creature, this);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
        await PowerCmd.Apply<VulnerablePower>(
            cardPlay.Target, DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["VulnerablePower"].UpgradeValueBy(1m);
    }
}
```

---

## DynamicVars 类型速查

| 类型 | 构造 | 说明 |
|------|------|------|
| `DamageVar(base, props)` | `new DamageVar(10m, ValueProp.Move)` | 伤害 |
| `BlockVar(base, props)` | `new BlockVar(5m, ValueProp.Unpowered)` | 格挡 |
| `CardsVar(base)` | `new CardsVar(2)` | 抽牌数 |
| `EnergyVar(base)` | `new EnergyVar(1)` | 能量 |
| `HealVar(base)` | `new HealVar(5)` | 治疗 |
| `PowerVar<T>(base)` | `new PowerVar<WeakPower>(2m)` | 能力层数 |
| `DynamicVar(name, base)` | `new DynamicVar("Hits", 3m)` | 自定义 |

---

## 本地化

`assets/localization/zhs/cards.json`：

```json
{
  "HeavyStrike": {
    "NAME": "重击",
    "DESCRIPTION": "造成 !D! 点伤害。"
  }
}
```

Key 是类名（去掉 Card 后缀）。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 卡牌不显示在卡池 | 检查 `Pool` 返回的池类型，确认 `[CardPool]` 属性正确 |
| 动态变量数值不对 | `CanonicalVars` 返回的变量名必须和 `DynamicVars["Name"]` 一致 |
| 升级不生效 | `OnUpgrade()` 中调 `UpgradeValueBy()` 而非直接改值 |
| Token 卡牌不显示 | `shouldShowInCardLibrary: true` |
| 卡牌奖励序列化丢失自定义卡池 | 旧版 STS2 无 `CardCreationOptions.CustomCardPool` 属性，需用 `CardRewardSerializationCompatibility` 兼容层（BaseLib 3.3.4+）存储具体卡牌 ID 列表 |
| 卡牌奖励序列化后重连不恢复 | 检查 `CardRewardToSerializablePatch` 和 `CardRewardFromSerializablePatch` 是否已启用（BaseLib 3.3.4+ 已修复此项） |
