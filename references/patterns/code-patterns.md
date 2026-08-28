# 实战写法模式

> 常用代码片段汇总（全部对照真实 `sts2-res/src/` 源码重写）。快速复制修改。
> 完整模板见各模块：card / relic / power / event / monster 等。

---

## 卡牌

### 单目标攻击

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
    await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
        .FromCard(this)
        .Targeting(cardPlay.Target)
        .Execute(choiceContext);
}
```

### 多段攻击（3 次）

```csharp
await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
    .FromCard(this).Targeting(cardPlay.Target)
    .WithHitCount(3)
    .Execute(choiceContext);
```

### AOE 攻击（所有敌人）

```csharp
await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
    .FromCard(this)
    .TargetingAllOpponents(CombatState)
    .Execute(choiceContext);
```

### 攻击 + 施加能力

```csharp
ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
    .FromCard(this).Targeting(cardPlay.Target)
    .Execute(choiceContext);

await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, 1, Owner.Creature, this);
```

### 格挡

```csharp
// 真实签名：CreatureCmd.GainBlock(Creature, decimal, ValueProp, CardPlay?, bool)
await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);
```

### 抽牌

```csharp
// 真实签名：CardPileCmd.Draw(PlayerChoiceContext, decimal, Player, bool)
await CardPileCmd.Draw(choiceContext, 2, Owner);
```

### 升级

```csharp
protected override void OnUpgrade()
{
    DynamicVars.Damage.UpgradeValueBy(3m);
}
```

### 选择手牌消耗

```csharp
var selected = await CardSelectCmd.FromHand(choiceContext, Owner,
    new CardSelectorPrefs("选择一张牌消耗", selectCount: 1),
    card => card != this, this);
foreach (var card in selected) await CardCmd.Exhaust(choiceContext, card);
```

### 选择手牌升级

```csharp
// 真实签名：FromHandForUpgrade(context, player, source)（无 filter 参数）
var card = await CardSelectCmd.FromHandForUpgrade(choiceContext, Owner, this);
if (card != null) CardCmd.Upgrade(card);
```

### 使用条件（属性，不是方法）

```csharp
// IsPlayable / ShouldGlowGoldInternal 都是 protected virtual 属性
protected override bool IsPlayable => Owner.Gold >= 100;

protected override bool ShouldGlowGoldInternal => Owner.Gold >= 100;
```

---

## 遗物

### 回合开始给能量

```csharp
protected override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
{
    if (!participants.Contains(Owner.Creature)) return;
    Flash();
    await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);  // (decimal, Player)
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

---

## 能力

### 伤害修正（力量式）

```csharp
public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
{
    if (Owner != dealer) return 0m;
    return props.IsPoweredAttack() ? Amount : 0m;
}
```

### 回合结束递减层数（临时能力）

```csharp
public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
{
    if (participants.Contains(Owner))
    {
        await PowerCmd.Decrement(this);
    }
}
```

### 卡牌打出时获得格挡

```csharp
public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, cardPlay);
}
```

### 临时能力（完整类）

```csharp
public class MyTempPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;   // 归零自动移除

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Decrement(this);
        }
    }
}
```

---

## 事件

### 单页事件

```csharp
protected override IReadOnlyList<EventOption> GenerateInitialOptions()
{
    return new List<EventOption>
    {
        new EventOption(this, OnOptionA, "MY_EVENT.pages.INITIAL.options.A"),
        new EventOption(this, OnOptionB, "MY_EVENT.pages.INITIAL.options.B"),
    };
}

private async Task OnOptionA()
{
    SetEventFinished(L10NLookup("MY_EVENT.pages.A.description"));
}
```

### 多页事件

```csharp
protected override IReadOnlyList<EventOption> GenerateInitialOptions()
{
    return new List<EventOption>
    {
        new EventOption(this, OnEnter, "MY_EVENT.pages.INITIAL.options.ENTER"),
    };
}

private async Task OnEnter()
{
    SetEventState(
        L10NLookup("MY_EVENT.pages.PAGE2.description"),
        new List<EventOption>
        {
            new EventOption(this, OnLeave, "MY_EVENT.pages.PAGE2.options.LEAVE"),
        });
}
```

### 事件给卡 / 金币

```csharp
private async Task OnGetCard()
{
    var card = Owner!.RunState.CreateCard<MyCustomCard>(Owner);
    CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
    SetEventFinished(L10NLookup("MY_EVENT.pages.GET_CARD.description"));
}

private async Task OnGetGold()
{
    await PlayerCmd.GainGold(50, Owner!);
    SetEventFinished(L10NLookup("MY_EVENT.pages.GET_GOLD.description"));
}
```

---

## 注册

### 手动注册模型

```csharp
// 双泛型（无参）或反射重载
ModHelper.AddModelToPool<ColorlessCardPool, MyCard>();
ModHelper.AddModelToPool<SharedRelicPool, MyRelic>();
ModelDb.Inject(typeof(MyPower));
```

### HarmonyPatch 注入角色

```csharp
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)]
public static class AllCharactersPatch
{
    public static void Postfix(ref IEnumerable<CharacterModel> __result)
    {
        __result = __result.Append(new MyCharacter());
    }
}
```

### HarmonyPatch 注入事件

```csharp
[HarmonyPatch(typeof(Overgrowth), nameof(Overgrowth.AllEvents), MethodType.Getter)]
public static class AllEventsPatch
{
    public static void Postfix(ref IEnumerable<EventModel> __result)
    {
        __result = __result.Append(new MyEvent());
    }
}
```

### 序列化注册

```csharp
SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MyRelic));
```

---

## 修改角色初始遗物

```csharp
[HarmonyPatch(typeof(Ironclad), nameof(Ironclad.StartingRelics), MethodType.Getter)]
public static class IroncladStartingRelicsPatch
{
    private static void Postfix(Ironclad __instance, ref IReadOnlyList<RelicModel> __result)
    {
        var list = new List<RelicModel>(__result) { ModelDb.Relic<MyCustomRelic>() };
        __result = list;
    }
}
```

---

## 常用组合

### 卡牌：攻击 + 抽牌

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
    await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
        .Targeting(cardPlay.Target).Execute(choiceContext);
    await CardPileCmd.Draw(choiceContext, 1, Owner);
}
```

### 遗物：每回合首次受伤减半

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

### 能力：回合开始获得格挡

```csharp
public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
{
    if (participants.Contains(Owner))
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
}
```
