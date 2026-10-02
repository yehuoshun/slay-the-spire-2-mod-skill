# 遗物/高级模式（YuWanCard 提炼）

## 1. 自定义遗物稀有度（CustomRarity）

引擎稀有度不够用时，`RelicRarity.None` + 自定义稀有度对象可建独立图鉴分类（自定义边框色/检视标签/排序）：

```csharp
// 自定义稀有度对象（YuWan 自研，纯原生需自己实现等价类）
static readonly YuWanCustomRelicRarity WhatIfRarity = new(
    "MOD-WHAT_IF",                          // 唯一 ID
    "relics",                               // 图鉴标题本地化表
    "MOD-WHAT_IF_CATEGORY.header",          // 图鉴标题键
    displayLocalizationKey: "MOD-WHAT_IF_RARITY.label",
    visualRarity: RelicRarity.Event,        // 借原版边框样式
    borderColor: new Color("741ADB"),
    sortOrder: 100);

public sealed override RelicRarity Rarity => RelicRarity.None;  // 必须 None
public override CustomRarity? CustomRarity => WhatIfRarity;
public override int MerchantCost => 999999999;                  // 防误买
public override bool IsAllowedInShops => false;                 // 不进商店
```

**关键规则**：Rarity 必须 `None`；禁商店；高 MerchantCost。UI 集成（图鉴分组、检视标签、边框染色、禁多人交易）需要 6 个 Harmony Patch（`NRelicCollection.LoadRelics` postfix、`NInspectRelicScreen.UpdateRelicDisplay` postfix、`NRelic.Reload` postfix 等）——纯原生实现工作量中等，收益是独立图鉴分类。

## 2. 生命条预测（IHealthBarForecastSource）

在敌人/玩家生命条上叠加预测段（如中毒伤害预测、毁灭条着色器）：

```csharp
// 原生接口（YuWan 基类默认实现，纯原生直接实现接口）
public override IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(
    HealthBarForecastContext context)
{
    if (context.Creature == Owner && Amount > 0)
    {
        yield return new HealthBarForecastSegment(
            Amount,
            new Color(0.5f, 0.2f, 0.8f),
            HealthBarForecastDirection.FromRight,
            Order: 0);
    }
}
```

`HealthBarForecastSegment` 构造：`(decimal amount, Color color, HealthBarForecastDirection direction, int order, Material? material = null)`。毁灭条样式用 `ShaderUtils.CreateDoomBarShaderMaterial(CreateVanillaDoomBarGradientTexture())`。

## 3. 金币修改保护（GoldModificationGuard）

遗物覆写 `ModifyGoldGained` 时防递归（修改金币→触发 AfterModifyingGoldGained→再修改…）：

```csharp
// 思想：守卫对象包装「获取玩家、计算系数、执行扣减」三个委托
// 覆写 ModifyGoldGained 时查守卫（是否正在修改中，是则返回原值）
// AfterModifyingGoldGained 里执行副作用（如扣一半金币）
```

纯原生写法：遗物内加 `bool _modifyingGold` 标志，`ModifyGoldGained` 里 `if (_modifyingGold) return amount;`，`AfterModifyingGoldGained` 里设标志再执行扣减，finally 复位。

## 4. 遗物升级链

```csharp
// 基础遗物 autoAdd=true 自动注册
public class PigCarrot : YuWanRelicModel
{
    public PigCarrot() : base(true) { }
    public override RelicModel? GetUpgradeReplacement() => ModelDb.Relic<GoldenCarrot>();
}
// 升级版 autoAdd=false 不自动注册（由 GetUpgradeReplacement 引用）
```

## 5. 遗物 Hook 补充（skill 原表缺）

数值修改类：`ModifyDamageMultiplicative(decimal, Player)`、`ModifyBlockMultiplicative`、`ModifyMaxEnergy(Player, decimal)`、`ModifyHandDraw(Player, int)`、`ModifyRestSiteHealAmount`、`ModifyGoldGained(Player, decimal)`（返回修改后值）、`ModifyPowerAmountGivenAdditive/Multiplicative(PowerModel, Creature, decimal, Creature?, CardModel?)`。

奖励类：`TryModifyRewards(Rewards)`、`TryModifyCardRewardOptions(Player, List<CardCreationResult>, CardCreationOptions)`（替换奖励卡牌）、`AfterModifyingGoldGained(Player, decimal)`。

## 6. 事件/先古之民真实形态

`AncientOption(string, Func<Task>, LocString title, LocString desc)`；事件 `EventState("page_1", new EventPage(locString, EventOption[]))`；选项回调 `async () => await PlayerCmd.GainGold(50, player)`。先古之民 `GenerateOptions(Player)` 返回选项列表——skill 事件文档已有基础，此处确认签名一致。

## 7. 静态工具确认

- `ModelDb.Card<T>()` / `ModelDb.Relic<T>()` / `ModelDb.Power<T>()` / `ModelDb.CardPool<T>()` 均确认
- `Owner.RunState.CreateCard(targetModel, Owner)` 生成新卡实例（奖励替换用）
- `ResourceLoader.Exists(path)` 检查资源存在后再加载（肖像/边框回退逻辑）
