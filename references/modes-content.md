# 模式索引：内容类型

---

## 模式一：自定义卡牌

### 最简示例

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

### CardType 枚举

| 值 | 含义 |
|----|------|
| `Attack` | 攻击牌 |
| `Skill` | 技能牌 |
| `Power` | 能力牌 |
| `Status` | 状态牌 |
| `Curse` | 诅咒牌 |
| `Token` | 衍生牌（如小刀） |
| `Quest` | 任务牌 |

### CardRarity 枚举

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

### TargetType 枚举

| 值 | 说明 |
|----|------|
| `None` | 无目标（自施法） |
| `Opponent` | 单个敌人 |
| `AllOpponents` | 全部敌人 |
| `Ally` | 单个友军 |
| `AllAllies` | 全部友军 |
| `AnyEnemy` | 任意敌人（通常 >= Opponent） |
| `Self` | 自身 |
| `Osty` | 奥斯提（亡灵契约师宠物） |

### 卡牌标记

```csharp
protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike, CardTag.Defend];
```

### 卡牌关键词

```csharp
protected override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];
```

常用：`Exhaust`（消耗）/ `Retain`（保留）/ `Innate`（固有）/ `Ethereal`（虚无）

### 注册到卡池

```csharp
// 在 ModEntry.Initialize() 中：
ModHelper.AddModelToPool<ColorlessCardPool, StrikeCard>();
```

### DrawCard / 选牌 / 丢弃

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

---

## 模式二：自定义遗物

### 最简示例

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;

namespace MyMod.Relics;

public class EnergyRelic : RelicModel
{
    // 稀有度决定获取途径
    public override RelicRarity Rarity => RelicRarity.Boss;

    // 图标
    protected override string BigIconPath => "res://MyMod/images/relics/energy_relic.png";
    public override string PackedIconPath => BigIconPath;
    protected override string PackedIconOutlinePath => BigIconPath;

    // 回合开始钩子
    public override async Task AfterSideTurnStart(CombatSide side,
        IReadOnlyList<Creature> participants, HextechCombatState combatState)
    {
        if (side == Owner.Creature.Side)
        {
            Flash();
            await PlayerCmd.GainEnergy(Owner, 1);
        }
    }
}
```

### RelicRarity 枚举

| 值 | 获取途径 |
|----|---------|
| `Starter` | 初始遗物（不进入随机池） |
| `Common` | 宝箱/精英/商店 |
| `Uncommon` | 宝箱/精英/商店 |
| `Rare` | 宝箱/精英/商店 |
| `Boss` | Boss 掉落 |
| `Shop` | 仅商店 |
| `Event` | 仅事件 |
| `Ancient` | 仅先古之民 |

### 注册到遗物池

```csharp
ModHelper.AddModelToPool<SharedRelicPool, EnergyRelic>();
// 或角色专属
ModHelper.AddModelToPool<IroncladRelicPool, EnergyRelic>();
```

遗物池：`SharedRelicPool`（宝箱+精英+商店）/ `IroncladRelicPool` / `SilentRelicPool` 等 / `EventRelicPool`

### 常用钩子

```csharp
// 回合开始前
BeforeSideTurnStart(PlayerChoiceContext, CombatSide, IReadOnlyList<Creature>, CombatState)
// 回合开始后
AfterSideTurnStart(CombatSide, IReadOnlyList<Creature>, CombatState)
// 回合结束前
BeforeSideTurnEnd(PlayerChoiceContext, CombatSide, IEnumerable<Creature>)
// 回合结束后
AfterSideTurnEnd(PlayerChoiceContext, CombatSide, IEnumerable<Creature>)
// 战斗开始前
BeforeCombatStart() / BeforeCombatStart(PlayerChoiceContext)
// 战斗结束后
AfterCombatEnd(CombatRoom)
// 获取遗物时
OnObtained()
// 失去遗物时
OnLost()
// 卡牌打出后
AfterCardPlayed(CardModel, CardPlay)
// 受到伤害后
AfterReceiveDamage(PlayerChoiceContext, Creature, decimal)
```

### 显示计数器

```csharp
public override bool ShowCounter => true;
public override int DisplayAmount => _counter;

protected override IEnumerable<DynamicVar> CanonicalVars => [
    new DynamicVar("Counter", () => _counter)
];
```

---

## 模式三：自定义药水

### 最简示例

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;

namespace MyMod.Potions;

public class DamagePotion : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;

    // 图标
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

### PotionUsage 枚举

| 值 | 说明 |
|----|------|
| `CombatOnly` | 仅战斗中使用 |
| `AnyTime` | 随时可用（如鲜血药水） |
| `Triggered` | 代码触发（如瓶中精灵） |

### PotionRarity 枚举

`Common` / `Uncommon` / `Rare`

### 注册到药水池

```csharp
ModHelper.AddModelToPool<SharedPotionPool, DamagePotion>();
```

---

## 模式四：自定义能力（Buff/Debuff）

### 最简示例

```csharp
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MyMod.Powers;

public class ThornsPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Intensity;
    public override bool IsInstanced => false;   // 不重复创建实例
    public override bool AllowNegative => false; // 不允许负层数

    protected override string BigIconPath => "res://MyMod/images/powers/thorns_power.png";

    // 受到攻击时反弹伤害
    protected override async Task AfterReceiveDamage(
        PlayerChoiceContext ctx, Creature source, decimal damage, CardModel? cardSource)
    {
        if (source != null && source.Side == CombatSide.Enemy)
        {
            await DamageCmd.Attack(Amount)
                .FromCard(null)
                .Targeting(source)
                .Execute();
        }
    }
}
```

### PowerType 枚举

| 值 | 说明 |
|----|------|
| `Buff` | 增益 |
| `Debuff` | 减益 |

### PowerStackType 枚举

| 值 | 说明 |
|----|------|
| `Intensity` | 有层数（如中毒/力量） |
| `Duration` | 仅存在（如壁垒/钢笔尖） |

### 施加能力

```csharp
await CreatureCmd.ApplyPower(target, typeof(ThornsPower), 3, Owner.Creature, this);
```

---

## 模式五：自定义附魔

### 最简示例

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

### 为卡牌附魔

```csharp
await CardCmd.Enchant(card, typeof(SharpEnchantment), 3);
```

---

## 模式六：自定义事件

### 最简示例

```csharp
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace MyMod.Events;

public class MyCustomEvent : EventModel
{
    // 事件背景图
    public override string BackgroundImagePath => "res://MyMod/images/events/my_custom_event.png";

    protected override IEnumerable<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            new EventOption(
                "MY_CUSTOM_EVENT.pages.INITIAL.options.OPTION_1.title",
                async ctx =>
                {
                    // 给玩家一张自定义卡牌
                    var card = ctx.RunState.CreateCard<StrikeCard>(ctx.Player);
                    await CardPileCmd.Add(card, PileType.Deck);
                    await SetEventFinished("MY_CUSTOM_EVENT.pages.RESULT.description");
                }
            ),
            new EventOption(
                "MY_CUSTOM_EVENT.pages.INITIAL.options.OPTION_2.title",
                async ctx =>
                {
                    await PlayerCmd.GainGold(ctx.Player, 50);
                    await SetEventFinished("MY_CUSTOM_EVENT.pages.RESULT2.description");
                }
            ),
        };
    }
}
```

### 多页选项

```csharp
protected override IEnumerable<EventOption> GenerateInitialOptions()
{
    return new[]
    {
        new EventOption("继续", async ctx =>
        {
            await SetEventState("MY_CUSTOM_EVENT.pages.PAGE2.description",
                new[]
                {
                    new EventOption("选项A", async ctx2 => { /* ... */ SetEventFinished("结束"); }),
                    new EventOption("选项B", async ctx2 => { /* ... */ SetEventFinished("结束"); }),
                });
        }),
    };
}
```

### 注册到事件列表（Patch ActModel.AllEvents）

```csharp
[HarmonyPatch(typeof(Overgrowth), "AllEvents", MethodType.Getter)]
static void Postfix(ref IEnumerable<EventModel> __result)
{
    __result = __result.Append(new MyCustomEvent());
}
```

---

## 模式七：先古之民事件

### 最简示例

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace MyMod.Events;

public class MyAncient : AncientEventModel
{
    public override string BackgroundImagePath => "res://MyMod/images/events/my_ancient.png";

    protected override IEnumerable<EventOption> AllPossibleOptions => new[]
    {
        // 所有可能的选项
    };

    protected override AncientDialogueSet DefineDialogues()
    {
        return new AncientDialogueSet();
    }
}
```

### 先古之民资源

| 资源 | 路径 |
|------|------|
| 背景图 | `images/events/<id小写>.png` |
| 地图图标 | `images/packed/map/ancients/ancient_node_<id小写>.png` |
| 地图描边 | `images/packed/map/ancients/ancient_node_<id小写>_outline.png` |
| 历史头像 | `images/ui/run_history/<id小写>.png` |
| 背景场景 | `scenes/events/background_scenes/<id小写>.tscn` |

### 注册先古之民

```csharp
[HarmonyPatch(typeof(Hive), "AllAncients", MethodType.Getter)]
static void Postfix(ref IEnumerable<AncientEventModel> __result)
{
    __result = __result.Append(new MyAncient());
}
```