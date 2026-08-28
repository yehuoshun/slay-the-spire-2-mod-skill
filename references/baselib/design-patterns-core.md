# 纯原生设计模式：自动注册与链式辅助

## 为什么学 BaseLib

BaseLib 的 `Custom*Model` 本质上是**原生 API 的薄封装**。研究它的源码后发现，它没有引入任何魔法——只是用：
- 自定义 Attribute（C# 标准库）
- 反射扫描（C# 标准库）
- 原生 `ModHelper.AddModelToPool` / `ModelDb.Inject`（sts2.dll）

把这些模式**用纯原生 API 复刻**，就能获得同样的开发便利，且不背第三方依赖。

**铁律：本 skill 所有写法零第三方依赖。BaseLib 只在文中作为"灵感来源"一句话标注，绝不教人使用。**

---

## 模式 1：自动注册（ContentRegistry）

### BaseLib 的做法

- 构造函数里调 `CustomContentDictionary.AddModel(GetType())` 收集类型
- `[Pool(Type)]` Attribute 标记模型属于哪个池
- 统一注册时调原生 `ModHelper.AddModelToPool(poolType, modelType)`

### 纯原生转译

```csharp
// 1. 自定义 Attribute（C# 标准库）
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class CardPoolAttribute(Type poolType) : Attribute
{
    public Type PoolType { get; } = poolType;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RelicPoolAttribute(Type poolType) : Attribute
{
    public Type PoolType { get; } = poolType;
}

// 2. ContentRegistry：反射扫描程序集，统一注册
public static class ContentRegistry
{
    public static void RegisterAll(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute<CardPoolAttribute>() is { } cardPool)
                ModHelper.AddModelToPool(cardPool.PoolType, type);
            else if (type.GetCustomAttribute<RelicPoolAttribute>() is { } relicPool)
                ModHelper.AddModelToPool(relicPool.PoolType, type);
        }
    }
}

// 3. ModEntry 里调用一次
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly());
    }
}
```

### 用法

```csharp
[CardPool(typeof(MyCardPool))]   // 自动进 MyCardPool，无需手动注册
public class MyCard : CardModel { ... }

[RelicPool(typeof(SharedRelicPool))]
public class MyRelic : RelicModel { ... }
```

### 关键点

- 注册时机必须在 `ModelDb` 初始化前（`ModEntry.Initialize` 阶段）
- 错过时机用原生 `ModelDb.Inject(type)` 补救（只注册 ID，不关联池）
- 池类型可在 Attribute 参数里动态指定，支持任意自定义池

---

## 模式 2：链式辅助方法（转译 Builder）

### BaseLib 的做法

`ConstructedCardModel` 用链式 Builder 一行定义卡牌：`.WithCost(1).WithDamage(6).WithUpgrade(...)`。

### 纯原生转译

原生 API 本身已经支持链式（`DamageCmd.Attack().FromCard().Targeting().Execute()`），可以再包一层静态辅助，把高频重复操作收敛成一行：

```csharp
public static class CardFx
{
    // 攻击：来源是当前卡牌，打指定目标
    public static Task DealDamage(this CardModel card, PlayerChoiceContext ctx,
        CardPlay play, int damage, int hits = 1, string? hitFx = null)
    {
        ArgumentNullException.ThrowIfNull(play.Target, "play.Target");
        var cmd = DamageCmd.Attack(damage).FromCard(card).Targeting(play.Target)
            .WithHitCount(hits);
        if (hitFx != null) cmd = cmd.WithHitFx(hitFx);
        return cmd.Execute(ctx);
    }

    // 攻击所有敌人
    public static Task DealDamageAll(this CardModel card, PlayerChoiceContext ctx,
        CardPlay play, int damage)
        => DamageCmd.Attack(damage).FromCard(card)
            .TargetingAllOpponents(card.CombatState).Execute(ctx);

    // 抽牌（真实签名：Draw(PlayerChoiceContext, decimal, Player, bool)）
    public static Task Draw(this CardModel card, PlayerChoiceContext ctx, int count)
        => CardPileCmd.Draw(ctx, count, card.Owner, false);
}

// 用法：一行完成攻击+抽牌
protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
{
    ArgumentNullException.ThrowIfNull(play.Target, "play.Target");
    await Task.WhenAll(
        this.DealDamage(ctx, play, 6),
        this.DealDamage(ctx, play, 3));
}
```

### 关键点

- 辅助方法只是收敛重复，**不改变原生行为**
- 扩展方法命名要带模块前缀（`CardFx` / `RelicFx` / `PowerFx`），避免与其他 Mod 冲突
- 保留原生链式能力，辅助方法内部仍走 `DamageCmd` / `CardPileCmd` 等原生命令

