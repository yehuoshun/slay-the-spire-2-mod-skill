# 序列化 & 注册

> 三种注册策略，从简单到复杂。学海克斯符文 ModelBootstrap + YuWanCard 反射扫描 + ShunMod 属性注册。

---

## SavedProperty 属性

标记需要序列化保存的自定义属性：

```csharp
public sealed class MyRelic : RelicModel
{
    [SavedProperty] public int KillCount { get; set; }
    [SavedProperty] public bool HasTriggered { get; set; }
}
```

**关键**：必须在初始化时注入缓存：

```csharp
SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MyRelic));
```

---

## 注册策略一：属性扫描（推荐）

学 YuWanCard / ShunMod，用自定义 Attribute 标记模型，初始化时反射扫描自动注册。

```csharp
// 定义属性
[AttributeUsage(AttributeTargets.Class)]
public class CardPoolAttribute : Attribute
{
    public Type PoolType { get; }
    public CardPoolAttribute(Type poolType) => PoolType = poolType;
}

// 标记模型
[CardPool(typeof(RedPool))]
public sealed class MyCard : CardModel { ... }

// 扫描注册
public static void RegisterAll(Assembly assembly)
{
    foreach (var type in assembly.GetTypes())
    {
        if (type.IsAbstract) continue;
        var attr = type.GetCustomAttribute<CardPoolAttribute>();
        if (attr != null)
            ModHelper.AddModelToPool(attr.PoolType, type);
    }
}
```

**优点**：零手动注册，加新卡只需加 `[CardPool]` 属性。

---

## 注册策略二：集中注册表（海克斯符文）

适合大型 Mod，所有内容在一个地方声明：

```csharp
internal static class HextechContentRegistry
{
    // 注册表：类型 + 稀有度 + 角色限定 + 标签
    private static RuneRegistration Rune<TRune>(
        HextechRarityTier rarity,
        RuneFlags flags = RuneFlags.None,
        HextechCharacterPool? characterPool = null)
    {
        return new RuneRegistration(typeof(TRune), rarity, flags, characterPool);
    }

    // 集中声明所有内容
    private static readonly RuneRegistration[] RuneRegistrations =
    [
        Rune<AdamantRune>(HextechRarityTier.Silver),
        Rune<AdaptiveCapacitorRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Defect),
        // ... 几十个符文
    ];

    // 初始化时按稀有度/角色/标签分组
    private static RegistryLookups BuildRegistryLookups() { ... }
}
```

**优点**：一眼看清所有内容、稀有度、角色限定。适合 20+ 遗物/符文的大型 Mod。

---

## 注册策略三：ModelDb.Inject（运行时注入）

ModelDb 已初始化后，用 `Inject` 代替 `AddModelToPool`：

```csharp
// ModelDb 初始化后
ModelDb.Inject(typeof(MyLateCard));
ModelDb.Inject(typeof(MyLateRelic));
```

**场景**：Patch `ModelDb.Init` 的 Postfix 中注入、动态生成的内容。

---

## SavedProperty 批量注入

```csharp
// 方案一：逐个注入
SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MyRelic));
SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MyPower));

// 方案二：反射扫描（学 YuWanCard）
public static void RegisterAssembly(Assembly assembly)
{
    foreach (var type in assembly.GetTypes())
    {
        if (type.IsAbstract) continue;
        if (!typeof(AbstractModel).IsAssignableFrom(type)) continue;

        var hasSavedProp = type.GetProperties(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(p => p.GetCustomAttribute<SavedPropertyAttribute>() != null);

        if (hasSavedProp)
            SavedPropertiesTypeCache.InjectTypeIntoCache(type);
    }
}
```

---

## 完整初始化流程（生产级）

```csharp
public static void Initialize()
{
    // 1. 预加载依赖程序集
    PreloadDependencyAssemblies();

    // 2. 注入 SavedProperty 缓存
    InjectSavedPropertyCaches();

    // 3. 注册模型到池
    RegisterModels();

    // 4. 冻结注册（可选，防止后续意外注册）
    ContentRegistry.Freeze();
}

private static void InjectSavedPropertyCaches()
{
    foreach (var type in GetAllCustomTypes())
        SavedPropertiesTypeCache.InjectTypeIntoCache(type);
}

private static void RegisterModels()
{
    var assembly = Assembly.GetExecutingAssembly();
    foreach (var type in assembly.GetTypes())
    {
        if (type.IsAbstract) continue;

        if (typeof(CardModel).IsAssignableFrom(type))
            ModHelper.AddModelToPool(typeof(RedPool), type);
        else if (typeof(RelicModel).IsAssignableFrom(type))
            ModHelper.AddModelToPool(typeof(SharedPool), type);
    }
}
```

---

## ModelId 序列化缓存去重（学 YuWanCard）

多人模式下 `ModelId.ToTypeNameMap` 可能重复注入导致序列化异常：

```csharp
[HarmonyPatch(typeof(ModelId), nameof(ModelId.ToTypeNameMap), MethodType.Getter)]
public static class ModelIdSerializationCachePatch
{
    private static bool _patched;

    public static void Postfix(ref Dictionary<string, Type> __result)
    {
        if (_patched) return;
        _patched = true;

        var assembly = Assembly.GetExecutingAssembly();
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(AbstractModel).IsAssignableFrom(type))
                continue;

            var id = ModelDb.GetId(type);
            if (!string.IsNullOrEmpty(id) && !__result.ContainsKey(id))
                __result[id] = type;
        }
    }
}
```

---

## 确定性随机（学 YuWanCard）

多人模式必须用确定性随机替代 `System.Random`，否则状态不同步：

```csharp
public static class DeterministicRandomUtils
{
    // 基于种子 + 回合数 + 实体 ID 生成确定性随机数
    public static int Next(int seed, int turn, string entityId, int max)
    {
        var hash = HashCode.Combine(seed, turn, entityId);
        return Math.Abs(hash) % max;
    }

    // 洗牌
    public static void Shuffle<T>(int seed, int turn, string sourceId, IList<T> list)
    {
        var rng = new Random(HashCode.Combine(seed, turn, sourceId));
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
```

**关键**：所有随机操作（抽牌、伤害浮动、遗物触发）必须走确定性随机，否则多人模式会炸。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| SavedProperty 不保存 | 必须调 `InjectTypeIntoCache` |
| AddModelToPool 泛型报错 | 用反射重载 `AddModelToPool(poolType, modelType)` |
| 重复注册 | 加 `_initialized` 锁 + `Contains` 检查 |
| ModelDb 已初始化后注册 | 用 `ModelDb.Inject(type)` |
| 角色卡池缓存不刷新 | 反射重置 `CardPoolModel._allCards` |
| ModelId 序列化重复报错 | Patch `ModelId.ToTypeNameMap` getter 做去重（学 YuWanCard） |
| 多人模式随机结果不同步 | 用 `DeterministicRandomUtils` 替代 `System.Random`（学 YuWanCard） |