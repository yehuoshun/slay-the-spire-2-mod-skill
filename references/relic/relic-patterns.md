# 自定义遗物：实战回调代码片段

> 钩子签名表见 [relic-callbacks.md](relic-callbacks.md)。

### 回合开始给能量

```csharp
protected override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
{
    if (!participants.Contains(Owner.Creature)) return;
    Flash();
    await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
}
```

### 卡牌打出时触发

```csharp
public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    if (cardPlay.Card.Type == CardType.Attack)
    {
        Flash();
        // 攻击牌触发效果
    }
}
```

### 战斗胜利时触发

```csharp
public override async Task AfterCombatVictory(CombatRoom room)
{
    Flash();
    await CreatureCmd.Heal(Owner.Creature, 6);
}
```

### 持有者受伤时触发

```csharp
public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
{
    if (target != Owner.Creature) return;
    Flash();
    // 受伤效果
}
```

### 每回合首次受伤减半

```csharp
private bool _alreadyUsedThisTurn;

public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
{
    if (participants.Contains(Owner.Creature))
        _alreadyUsedThisTurn = false;
}

public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
{
    if (target != Owner.Creature || _alreadyUsedThisTurn) return;
    _alreadyUsedThisTurn = true;
    Flash();
    // 减半伤害逻辑
}
```

### 自定义遗物稀有度（图鉴独立分类）

> 实战项目验证（YuWanCard 自研 `CustomRelicRarity`）。原生 `RelicRarity` 枚举不够用时，可建独立图鉴分类（自定义边框色/检视标签/排序）。⚠️ 原生 `RelicModel` 无此机制，需要自研：稀有度对象 + 覆写 `Rarity` 返回 `None` + 6 个 UI Harmony Patch（图鉴分组/过滤/检视标签/边框染色/禁多人交易）。

```csharp
// 核心：Rarity 必须返回 None，CustomRarity 指向自研稀有度对象
public sealed override RelicRarity Rarity => RelicRarity.None;
public override int MerchantCost => 999999999;  // 防误买
public override bool IsAllowedInShops => false; // 不进商店
```

代价（6 个 Patch：`NRelicCollection.LoadRelics` postfix 建分类、`NRelicCollectionCategory.LoadRelicNodes` prefix 过滤、`NInspectRelicScreen.UpdateRelicDisplay` postfix 标签+边框染色、`NRelic.Reload`/`NRelicCollectionEntry._Ready` postfix 描边、`RelicModel.get_IsTradable` postfix 禁交易）。收益：独立图鉴分类 + 品牌色边框。

### 金币修改防递归

覆写 `ModifyGoldGained` 又想在 `AfterModifyingGoldGained` 里扣金币时，会触发递归（扣金币→再走 ModifyGoldGained）。加 `bool _modifyingGold` 守卫：`ModifyGoldGained` 里 `if (_modifyingGold) return amount;`，副作用执行时置位，finally 复位。
