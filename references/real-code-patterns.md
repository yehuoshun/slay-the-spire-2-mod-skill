# 实战写法模式

> 从 STS2Plus / YuWanCard / 海克斯符文 / ShunMod 四个生产级模组中提炼的写法模式。

---

## 模式一：三种卡牌写法对比

### 最简：直接继承 CardModel

适合 1-2 张卡的小 Mod。所有声明内联。

```csharp
public sealed class Strike2 : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<RedPool>();
    public override string PortraitPath => "res://...";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];

    public Strike2() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cp)
    {
        await CreatureCmd.Damage(ctx, cp.Target, DynamicVars.Damage, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
```

### Builder：链式声明

适合 10+ 张卡的 Mod。流式代码，变量/关键词/HoverTip 一行声明。

```csharp
public sealed class Strike2 : YuWanCardModel
{
    public Strike2() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithDamage(6, upgrade: 3);
        WithKeywords(CardKeyword.Strike);
        WithTags(CardTag.Strike);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cp)
    {
        await CreatureCmd.Damage(ctx, cp.Target, DynamicVars.Damage, Owner.Creature, this);
    }
}
```

### 生产级：完全控制

适合复杂卡牌，需要精确控制 Pool、Keywords、HoverTips、PortraitPath。

```csharp
public sealed class ComplexCard : CardModel
{
    public override CardPoolModel Pool => IsMutable && Owner != null
        ? Owner.Character.CardPool
        : ModelDb.CardPool<TokenCardPool>();
    public override CardPoolModel VisualCardPool => Pool;
    public override string PortraitPath => "...";
    public override IEnumerable<string> AllPortraitPaths => [PortraitPath];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [...];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [...];
    // ...
}
```

---

## 模式二：两种注册策略对比

### 属性扫描（YuWanCard / ShunMod）

**适用**：内容型 Mod，自动发现注册。

```csharp
[CardPool(typeof(RedPool))]
public sealed class MyCard : CardModel { }

// ModEntry 中
ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly());
```

**优点**：零手动注册，加新卡只需加属性。
**缺点**：所有卡进同一个池，角色限定需要额外逻辑。

### 集中注册表（海克斯符文）

**适用**：大型 Mod，需要稀有度/角色限定/标签管理。

```csharp
private static readonly RuneRegistration[] RuneRegistrations = [
    Rune<AdamantRune>(Silver),
    Rune<AdaptiveCapacitorRune>(Gold, characterPool: Defect),
    Rune<BerserkerRune>(Gold, flags: FirstActExcluded),
];
```

**优点**：一眼看清所有内容配置。
**缺点**：新增内容需要手动加一行。

---

## 模式三：三种补丁组织对比

### 全量 PatchAll（ModTemplate）

```csharp
harmony.PatchAll(); // 简单粗暴
```

### 分组 PatchCategory（STS2Plus）

```csharp
[HarmonyPatchCategory("MoreRules")]
public static class SomePatch { ... }

harmony.PatchCategory(assembly, "Core");
harmony.PatchCategory(assembly, "MoreRules");
```

### Try-Install 模式（海克斯符文）

```csharp
TryInstallOptionalHookGroup("shop forge", () => HextechShopForgeHooks.Install(harmony));
```

---

## 模式四：两种设置界面对比

### 依赖 ModConfig 框架

```csharp
ModConfigApi.Register("MyMod", "我的模组", entries);
```

**优点**：一行注册，社区标准。
**缺点**：依赖第三方 Mod。（本指南目标是纯原生，所以也提供了自研方案）

### 自研零 Harmony（ShunMod）

```csharp
SettingsUI.Initialize(entries);
```

**优点**：零依赖，完全可控。
**缺点**：需要自己写 UI 注入逻辑。

---

## 模式五：ModEntry 初始化三阶段

所有生产级 Mod 都遵循这个模式：

```
Phase 1: Harmony 补丁（try-catch 保护）
  ↓
Phase 2: 内容注册（属性扫描 / 集中注册表）
  ↓
Phase 3: 设置系统（加载配置 + 注入 UI）
```

```csharp
public static void Initialize()
{
    lock (Lock) { if (_initialized) return; _initialized = true; }

    // Phase 1
    _harmony = new Harmony(HarmonyId);
    try { _harmony.PatchAll(Assembly.GetExecutingAssembly()); }
    catch (Exception e) { Log.Error($"Harmony: {e}"); }

    // Phase 2
    ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly());

    // Phase 3
    SettingsManager.Initialize();
    SettingsUI.Initialize(entries);
}
```

---

## 选型建议

| 模组规模 | 推荐方案 |
|---------|---------|
| 1-2 张卡/遗物 | 最简写法 + PatchAll + 手动注册 |
| 5-10 张卡 | Builder 模式 + 属性扫描 |
| 10+ 内容 | 集中注册表 + Try-Install 补丁 |
| 自定义角色 | ModTemplate 角色模板 + 属性扫描 |
| 需要设置界面 | ModConfig 框架（或用 ShunMod 自研方案） |