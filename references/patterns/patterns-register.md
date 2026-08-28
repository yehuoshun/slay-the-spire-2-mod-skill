# 实战写法模式：注册与常用组合

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

