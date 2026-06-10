# 真实项目写法模式

> 从 STS2Plus / YuWanCard / Natsuki（海克斯符文）三个生产级模组中提炼的写法模式。

---

## 模式 A：Builder 链式构造（YuWanCard 风格）

YuWanCard 用 Builder 模式让卡牌定义极其简洁：

```csharp
public class PigStrike : YuWanCardModel
{
    public PigStrike() : base(1, CardType.Attack, CardRarity.Basic, TargetType.Opponent)
    {
        // 链式声明所有属性
        WithDamage(6, upgrade: 3)          // 伤害 6，升级 +3
            .WithTags(CardTag.Strike)       // 打击标签
            .WithKeyword(CardKeyword.None)  // 无特殊关键词
            .WithTip(typeof(VulnerablePower)); // 悬停提示：易伤
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage)
            .FromCard(this).Targeting(ctx.Targets.First()).Execute();
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        ConstructedUpgrade(); // YuWanCard 的统一升级处理
    }
}
```

**核心思想**：所有声明性信息（伤害/格挡/标签/关键词/悬停提示）都在构造函数中用链式调用声明，不分散在多个 override 中。

### Builder 方法清单

```csharp
WithDamage(baseVal, upgrade)       // 伤害动态变量
WithBlock(baseVal, upgrade)        // 格挡动态变量
WithCards(baseVal, upgrade)        // 抽牌数
WithEnergy(baseVal, upgrade)       // 能量
WithHeal(baseVal, upgrade)         // 治疗
WithPower<T>(baseVal, upgrade)     // 施加能力
WithVar(name, baseVal, upgrade)    // 自定义变量
WithTags(CardTag...)               // 卡牌标签
WithKeywords(CardKeyword...)       // 关键词
WithKeyword(keyword, upgradeType)  // 带升级行为的关键词
WithTip(Type)                      // 悬停提示（自动推断类型）
WithTip(CardKeyword)               // 关键词悬停
WithHandGlowGold(func)             // 金色发光条件
WithHandGlowRed(func)              // 红色发光条件
```

---

## 模式 B：反射自动注册（YuWanCard 风格）

不用手动写 `ModHelper.AddModelToPool`，用属性标注 + 反射扫描：

```csharp
// 自定义属性
[AttributeUsage(AttributeTargets.Class)]
public class RegisterEventAttribute : Attribute { }

// 标注
[RegisterEvent]
public class MyEvent : EventModel { }

// 扫描注册
public static void RegisterAll(Assembly assembly)
{
    foreach (var type in assembly.GetTypes())
    {
        if (type.IsAbstract) continue;

        // [Pool] 属性 → 自动 AddModelToPool
        var poolAttr = type.GetCustomAttribute<PoolAttribute>();
        if (poolAttr != null)
        {
            ModHelper.AddModelToPool(poolAttr.PoolType, type);
            continue;
        }

        // 自定义属性 → 收集到列表，稍后统一注册
        if (type.HasAttribute<RegisterEventAttribute>())
            EventTypes.Add(type);
    }
}
```

**优点**：新增内容只需加属性，不用改 ModEntry。

---

## 模式 C：基类封装（Natsuki 风格）

Natsuki 的海克斯符文模组用 `HextechRelicBase` 封装了常用逻辑：

```csharp
public abstract class HextechRelicBase : RelicModel
{
    // 统一处理回合范围状态（防止跨回合数据污染）
    protected void EnsureTurnScopedStateCurrent(Action resetState) { /* ... */ }

    // 判断是否为拥有者的卡牌
    protected bool IsOwnedCard(CardModel? card) => card?.Owner == Owner;

    // 判断是否为拥有者的攻击牌
    protected bool IsOwnedAttack(CardModel? card) => /* ... */;

    // 网络多人模式检测
    protected static bool IsNetworkMultiplayer() => /* ... */;

    // 安全闪烁
    protected void FlashDeferred(IEnumerable<Creature>? targets = null) { /* ... */ };
}
```

**优点**：子类只需关注业务逻辑，样板代码全在基类。

---

## 模式 D：ModEntry 安全初始化（Natsuki/STS2Plus 风格）

```csharp
public static class ModEntry
{
    private static readonly object _lock = new();
    private static bool _initialized;

    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized) return; // 防止重复初始化
            _initialized = true;
        }

        var harmony = new Harmony("MyMod");

        // 逐组安装，一组失败不影响其他
        TryInstall("cards", () => harmony.PatchAll(typeof(CardPatches).Assembly));
        TryInstall("relics", () => harmony.PatchAll(typeof(RelicPatches).Assembly));
        TryInstall("events", () => harmony.PatchAll(typeof(EventPatches).Assembly));
    }

    private static void TryInstall(string label, Action install)
    {
        try { install(); }
        catch (Exception ex) { Log.Warn($"Optional hook group skipped: {label}: {ex.Message}"); }
    }
}
```

---

## 模式 E：SavedAttachedState（YuWanCard 风格）

用于给任意模型附加自定义状态（不修改模型类本身）：

```csharp
// 定义附加状态
public static readonly SavedAttachedState<CardModel, int> CardPlayCount =
    new("CardPlayCount", defaultValueFactory: () => 0);

// 使用
int count = CardPlayCount[card];
CardPlayCount[card] = count + 1;
```

**注意**：这是 YuWanCard 自己实现的框架，不是游戏原生 API。纯原生 Mod 用 `[SavedProperty]` 属性即可。

---

## 模式 F：内容注册的生命周期（YuWanCard 风格）

```csharp
public static void Initialize()
{
    // Phase 1: Harmony 补丁
    harmony.PatchAllSafe(assembly);

    // Phase 2: 内容发现（扫描 [Pool] 属性等）
    ContentRegistry.RegisterAll(assembly);

    // Phase 3: 自定义属性序列化注册
    SavedPropertyRegistration.RegisterAssembly(assembly);

    // Phase 4: 配置、场景、资源
    Config = new ModConfig();
    AssetPreloader.Preload();
}
```

**关键顺序**：补丁 → 内容注册 → 序列化 → 配置/资源。不能颠倒。

---

## 模式 G：ModHelper.AddModelToPool 的正确用法

```csharp
// ✅ 泛型版本（类型在编译时已知）
ModHelper.AddModelToPool<SharedRelicPool, MyRelic>();

// ✅ 反射版本（类型在运行时确定）
ModHelper.AddModelToPool(typeof(SharedRelicPool), typeof(MyRelic));

// ❌ 错误：不要用 Type 作为泛型参数
// ModHelper.AddModelToPool<Type>(...) // 编译错误
```

---

## 模式 H：避免重复初始化

```csharp
private static bool _initialized;
private static readonly object _lock = new();

public static void Initialize()
{
    if (_initialized) return;
    lock (_lock)
    {
        if (_initialized) return;
        // ... 初始化逻辑 ...
        _initialized = true;
    }
}
```

游戏可能在多个时机调用 `Initialize`（如主菜单和进入游戏时各一次），必须防重复。

---

## 模式 I：资源路径的自动推断

```csharp
// 从类名自动推断资源路径
protected virtual string CardId => Regex.Replace(GetType().Name, "([a-z])([A-Z])", "$1_$2").ToLowerInvariant();
// MyCustomCard → my_custom_card

protected virtual string PortraitPath => $"res://MyMod/images/cards/{CardId}.png";
protected virtual string BigIconPath => $"res://MyMod/images/relics/{CardId}.png";
```

这样新增卡牌/遗物时不需要手动指定每个资源路径，只要按命名规范放好图片即可。

---

## 模式 J：DynamicVar 的链式构造

```csharp
// 游戏原生支持链式调用
new DamageVar(6, ValueProp.Move).WithUpgrade(3)
// 基础伤害 6，升级 +3

new BlockVar(5, ValueProp.Move).WithUpgrade(2)
// 基础格挡 5，升级 +2

new EnergyVar(1)
// 能量 1

new PowerVar<VulnerablePower>(2).WithUpgrade(1)
// 施加 2 层易伤，升级后 3 层
```

---

## 模式 K：日志

```csharp
// 游戏内置的 Log
MegaCrit.Sts2.Core.Logging.Log.Info($"[MyMod] 消息");
MegaCrit.Sts2.Core.Logging.Log.Warn($"[MyMod] 警告");
MegaCrit.Sts2.Core.Logging.Log.Error($"[MyMod] 错误");

// 或创建专属 Logger
public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
    new("MyMod", MegaCrit.Sts2.Core.Logging.LogType.Generic);
Logger.Info("初始化完成");
```