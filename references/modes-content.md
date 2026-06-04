## 🃏 模式一：自定义卡牌（最常见）

### 最小可用模板

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;

namespace STS2_ShunMod.Cards;

public class MyCard : CardModel
{
    // ══════ 构造函数 ══════
    // CardModel(int cost, CardType type, CardRarity rarity, TargetType target)
    public MyCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.Opponent) {}

    // ══════ 必须实现 ══════
    public override string PortraitPath =>
        $"res://{MainFile.ModId}/images/cards/my_card.png";

    // ══════ 打出效果 ══════
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // ctx.Targets 是选中的目标（target 为 Opponent 时是敌人）
        await DamageCmd.Attack(8)           // 8 点伤害
            .FromCard(this)                  // 来源
            .Targeting(ctx.Targets.First())  // 目标
            .Execute();
    }

    // ══════ 升级回调 ══════
    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        // 修改伤害值：在 DynamicVars 上操作
        DynamicVars["Damage"].UpgradeValueBy(3); // 伤害 8→11
    }
}
```

### 卡牌构造参数速查

| 参数 | 值 | 说明 |
|------|-----|------|
| `cost` | int | 能耗 X |
| `type` | Attack / Skill / Power / Status / Curse | 卡牌类型 |
| `rarity` | Common / Uncommon / Rare / Event / Token | 稀有度 |
| `target` | Opponent / Ally / Self / AllOpponents / Everyone | 目标类型 |
| `showInLibrary` | true/false | 图鉴可见 |

### 常用卡牌功能速查

```csharp
// ═══ 伤害 ═══
await DamageCmd.Attack(6).FromCard(this).Targeting(target).Execute();                    // 单体
await DamageCmd.Attack(6).FromCard(this).TargetingAllOpponents(combatState).Execute();   // AOE
await DamageCmd.Attack(6).FromCard(this).TargetingRandomOpponents(combatState).Execute(); // 随机
await DamageCmd.Attack(6).FromCard(this).WithHitCount(3).Targeting(target).Execute();    // 三连击

// ═══ 格挡 ═══
await PlayerCmd.GainBlock(Owner, 5);

// ═══ 抽牌 ═══
await CardPileCmd.Draw(Owner, 2);

// ═══ 能力 ═══
await PlayerCmd.ApplyPower(Owner, PowerDb.Get<StrengthPower>(), 2);  // 2 层力量
await PlayerCmd.ApplyPower(target, PowerDb.Get<VulnerablePower>(), 1);

// ═══ 选牌（从手牌选一张消耗）═══
var picked = await CardSelectCmd.FromHandGeneric(Owner,
    new CardSelectorPrefs(ChooseCardPrompt.Exhaust, 1), filter: null);

// ═══ 关键词/标签 ═══
public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
public override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
```

### 升级差异显示

```csharp
// 伤害值升级 → 卡牌描述自动显示 8→11
DynamicVars["Damage"].SetBase(8);          // OnUpgrade: UpgradeValueBy(3)
DynamicVars["Block"].SetBase(5);           // OnUpgrade: UpgradeValueBy(3)
```

### 卡牌注册（`[Pool]` 自动注册）

```csharp
[Pool(typeof(ColorlessCardPool))]  // ← 标注就自动注册到对应卡池
public class MyCard : CardModel { ... }
```

卡池类型：`IroncladCardPool` / `SilentCardPool` / `ColorlessCardPool` / `SharedCardPool` 等。

---

## 💎 模式二：自定义遗物

### 最小可用模板

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Commands;

namespace STS2_ShunMod.Relics;

public class MyRelic : RelicModel
{
    // ══════ 稀有度（决定获取途径）══════
    protected override RelicRarity Rarity => RelicRarity.Common;
    // Starter=初始 Event=事件 Ancient=先古 Common/Uncommon/Rare=奖励/商店 Shop=仅商店

    // ══════ 回合开始效果 ══════
    public override async Task AfterSideTurnStart(Side side, CombatState combatState)
    {
        if (side == Owner!.Creature.Side)  // 玩家回合
        {
            await Flash();                 // 遗物闪烁动画
            await PlayerCmd.GainEnergy(Owner, 1);
        }
    }
}
```

### 常用遗物钩子

```csharp
// 战斗开始时
public override async Task OnCombatStart(CombatState combatState) { ... }
// 回合开始前
public override async Task BeforeSideTurnStart(Side side, CombatState state) { ... }
// 获得遗物
public override async Task OnObtained() { ... }
// 失去遗物
public override async Task OnRemoved() { ... }
// 受到伤害
public override async Task OnDamageReceived(DamageInfo info) { ... }
// 卡牌打出后
public override async Task AfterCardPlayed(CardModel card) { ... }
```

### 遗物图标路径

```
images/relics/<小写ID>.png              ← 256×256 大图
images/relics/<小写ID>_outline.png       ← 剪影
images/atlases/relic_atlas.sprites/<小写ID>.tres      ← 裁切纹理
images/atlases/relic_outline_atlas.sprites/<小写ID>.tres ← 剪影裁切
```

---

## 🧪 模式三：自定义药水

```csharp
public class MyPotion : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    // CombatOnly=仅战斗 AnyTime=任意场景 ProgrammaticOnly=代码触发

    public override TargetType TargetType => TargetType.Opponent;

    public override async Task OnUse(PlayerChoiceContext ctx, PotionTarget target)
    {
        await DamageCmd.Attack(30).FromCard(this).Targeting(target.TargetCreature!).Execute();
    }
}
```

---

## ⚡ 模式四：自定义能力（Buff/Debuff）

```csharp
public class MyPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;          // Buff/Debuff/Neutral
    public override PowerStackType StackType => PowerStackType.Intensity;  // Intensity=层数 Duration=存在/不存在
    public override bool AllowNegative => false;

    // 每回合开始
    public override async Task AfterSideTurnStart(Side side, CombatState state)
    {
        if (side == Owner!.Side) await PlayerCmd.GainBlock(Owner, Amount);  // Amount=层数
    }

    // 受伤时修正
    public override decimal ModifyDamage(DamageInfo info, decimal damage)
        => damage + Amount;  // 每层 +1 伤害
}
```

---

## ✨ 模式五：自定义附魔

```csharp
public class MyEnchant : EnchantmentModel
{
    public override decimal EnchantDamageAdditive => Amount * 3;   // 每层 +3 伤害
    public override decimal EnchantBlockAdditive => Amount * 2;     // 每层 +2 格挡
    public override void ModifyCard() { }
}

// 附魔卡牌
await CardCmd.Enchant(mutableEnch, card, 1);
```

---

## 📜 模式六：自定义事件

```csharp
internal sealed class MyEvent : EventModel
{
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var player = Owner;
        return new List<EventOption>
        {
            // 选项一：给卡牌
            Opt(async () =>
            {
                await GiveCard(typeof(MyCard));
                SetEventFinished(L10NLookup("pages.DONE.description"));
            }, "OPT_1"),

            // 选项二：给金币
            Opt(async () =>
            {
                await PlayerCmd.GainGold(player!, 100);
                SetEventFinished(L10NLookup("pages.GOLD.description"));
            }, "OPT_2"),

            // 离开
            new EventOption(this, async () => SetEventFinished(L10NLookup("pages.CLOSE.description")),
                $"{Id.Entry}.pages.INITIAL.options.OPT_3")
        };
    }

    private EventOption Opt(Func<Task> cb, string key, IEnumerable<IHoverTip>? tips = null) =>
        new(this, cb, $"{Id.Entry}.pages.INITIAL.options.{key}", tips ?? []);
}
```

**多页选项**：用 `SetEventState(description, newOptions)` 切换页面；用 `SetEventFinished(text)` 结束事件。

**添加到地图：**

```csharp
[HarmonyPatch(typeof(Overgrowth), "AllEvents", MethodType.Getter)]
static void Postfix(ref IReadOnlyList<EventModel> __result)
{
    var list = __result.ToList();
    list.Add(ModelDb.GetByIdOrNull<EventModel>(ModelDb.GetId(typeof(MyEvent))));
    __result = list;
}
```

---
