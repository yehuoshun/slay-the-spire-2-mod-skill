# 实战写法模式

> 常用代码片段汇总，快速复制修改。

---

## 卡牌

### 单目标攻击

```csharp
public override Task OnPlay(PlayerChoiceContext context, CardPlayState playState)
{
    if (context.Target == null) return Task.CompletedTask;
    return DamageCmd.Attack(Damage.Get(this))
        .FromCard(this).Targeting(context.Target.Creature)
        .Execute(playState.CombatState);
}
```

### 多段攻击（3 次）

```csharp
return DamageCmd.Attack(Damage.Get(this))
    .FromCard(this).Targeting(context.Target.Creature)
    .WithHitCount(3)
    .Execute(playState.CombatState);
```

### AOE 攻击（所有敌人）

```csharp
return DamageCmd.Attack(Damage.Get(this))
    .FromCard(this)
    .TargetingAllOpponents(playState.CombatState)
    .Execute(playState.CombatState);
```

### 攻击 + 施加能力

```csharp
var target = context.Target?.Creature;
if (target == null) return Task.CompletedTask;

// 先攻击
await DamageCmd.Attack(Damage.Get(this))
    .FromCard(this).Targeting(target)
    .Execute(playState.CombatState);

// 再施加能力
await PowerCmd.Apply<VulnerablePower>(target, 1);
```

### 格挡

```csharp
public override Task OnPlay(PlayerChoiceContext context, CardPlayState playState)
{
    // 对所有敌人施加格挡
    BlockCmd.GainBlock(context.Player, Block.Get(this));
    return Task.CompletedTask;
}
```

### 抽牌

```csharp
CardPileCmd.Draw(playState.CombatState, 2);
```

### 升级

```csharp
public override void OnUpgrade()
{
    base.Damage.UpgradeValueBy(3);
}
```

### 选择手牌消耗

```csharp
var selected = await CardSelectCmd.FromHand(context, context.Player,
    new CardSelectorPrefs("选择一张牌消耗", selectCount: 1),
    card => card != this, this);
foreach (var card in selected) CardCmd.Exhaust(card);
```

### 选择手牌升级

```csharp
var card = await CardSelectCmd.FromHandForUpgrade(context, context.Player,
    card => card.CanUpgrade(), this);
if (card != null) card.UpgradeInternal();
```

### 使用条件

```csharp
public override bool IsPlayable(PlayerChoiceContext context)
{
    return context.Player.Gold >= 100;
}

public override bool ShouldGlowGoldInternal(PlayerChoiceContext context)
{
    return context.Player.Gold >= 100;
}
```

---

## 遗物

### 回合开始给能量

```csharp
public override async Task AfterSideTurnStart(Side side, ICombatState combatState)
{
    if (side != Owner.Creature.Side) return;
    Flash();
    await PlayerCmd.GainEnergy(combatState, 1);
}
```

### 卡牌打出时触发

```csharp
public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlayState cardPlay)
{
    if (cardPlay.CardPlayed.Type == CardType.Attack)
    {
        Flash();
        // 攻击牌触发效果
    }
}
```

### 战斗结束时触发

```csharp
public override async Task AfterCombatEnd(PlayerChoiceContext context)
{
    // 战斗结束效果
}
```

### 玩家受伤时触发

```csharp
public override async Task OnPlayerDamaged(PlayerChoiceContext context, int damageTaken)
{
    if (damageTaken > 0)
    {
        Flash();
        // 受伤效果
    }
}
```

---

## 能力

### 回合结束递减层数

```csharp
public override Task OnTurnEnd(PlayerChoiceContext context)
{
    Amount--;
    return Task.CompletedTask;
}
```

### 卡牌打出时获得格挡

```csharp
public override Task OnCardPlayed(PlayerChoiceContext context, CardPlayState playState)
{
    BlockCmd.GainBlock(context.Player, Amount);
    return Task.CompletedTask;
}
```

### 临时能力（回合结束移除）

```csharp
public class MyTempPower : PowerModel
{
    public MyTempPower()
    {
        Type = PowerType.Buff;
        StackType = PowerStackType.Stacks;
        AllowNegative = false;
    }

    public override Task OnTurnEnd(PlayerChoiceContext context)
    {
        Amount--;
        return Task.CompletedTask;
    }
}
```

---

## 事件

### 单页事件

```csharp
public override Task<List<EventOption>> GenerateInitialOptions()
{
    return Task.FromResult(new List<EventOption>
    {
        new EventOption(InitialOptionKey("OptionA"), OnOptionA),
        new EventOption(InitialOptionKey("OptionB"), OnOptionB),
    });
}
```

### 多页事件

```csharp
public override Task<List<EventOption>> GenerateInitialOptions()
{
    return Task.FromResult(new List<EventOption>
    {
        new EventOption(InitialOptionKey("Enter"), OnEnter),
    });
}

private async Task OnEnter(PlayerChoiceContext context)
{
    SetEventState("PAGE2");
}

public override Task<List<EventOption>> GetStateOptions(string state)
{
    if (state == "PAGE2")
        return Task.FromResult(new List<EventOption> { /* 第二页选项 */ });
    return base.GetStateOptions(state);
}
```

---

## 注册

### 手动注册模型

```csharp
ModHelper.AddModelToPool<ColorlessCardPool>(typeof(MyCard));
ModHelper.AddModelToPool<SharedRelicPool>(typeof(MyRelic));
ModelDb.Inject(typeof(MyPower));
```

### HarmonyPatch 注入角色

```csharp
[HarmonyPatch(typeof(ModelDb), "AllCharacters", MethodType.Getter)]
public static void Postfix(ref List<CharacterModel> __result)
{
    __result.Add(new MyCharacter());
}
```

### HarmonyPatch 注入事件

```csharp
[HarmonyPatch(typeof(Overgrowth), "AllEvents", MethodType.Getter)]
public static void Postfix(ref List<EventModel> __result)
{
    __result.Add(new MyEvent());
}
```

### 序列化注册

```csharp
SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MyRelic));
```

---

## 修改角色初始遗物

```csharp
[HarmonyPatch(typeof(Ironclad), "StartingRelics", MethodType.Getter)]
public static void Postfix(Ironclad __instance, ref IReadOnlyList<RelicModel> __result)
{
    var list = new List<RelicModel>(__result) { ModelDb.Relic<MyRelic>() };
    __result = list;
}
```

---

## 常用组合

### 卡牌：攻击 + 抽牌

```csharp
public override async Task OnPlay(PlayerChoiceContext context, CardPlayState playState)
{
    if (context.Target == null) return;
    await DamageCmd.Attack(Damage.Get(this)).FromCard(this)
        .Targeting(context.Target.Creature).Execute(playState.CombatState);
    CardPileCmd.Draw(playState.CombatState, 1);
}
```

### 遗物：每回合首次受伤减半

```csharp
private bool _alreadyUsedThisTurn;

public override async Task AfterSideTurnStart(Side side, ICombatState combatState)
{
    if (side != Owner.Creature.Side) return;
    _alreadyUsedThisTurn = false;
}

public override async Task OnPlayerDamaged(PlayerChoiceContext context, int damageTaken)
{
    if (!_alreadyUsedThisTurn && damageTaken > 0)
    {
        Flash();
        // 减半伤害
        _alreadyUsedThisTurn = true;
    }
}
```

### 能力：At the start of turn, gain block

```csharp
public override Task OnTurnStart(PlayerChoiceContext context)
{
    BlockCmd.GainBlock(context.Player, Amount);
    return Task.CompletedTask;
}
```