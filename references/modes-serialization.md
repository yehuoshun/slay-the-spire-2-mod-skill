# 模式十：自定义属性序列化 & 注册

## SavedProperty 属性

```csharp
using MegaCrit.Sts2.Core.Saves.Runs;

public class CustomCard : CardModel
{
    [SavedProperty]
    public int KillCount { get; set; }

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public bool HasTriggered { get; set; }
}
```

## 初始化时注入类型缓存

```csharp
// 在 ModEntry.Initialize() 中，AddModelToPool 之前
SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(CustomCard));
SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(CustomRelic));
```

**重要**：任何有自定义 `[SavedProperty]` 属性的类，都必须在初始化时调用这行，否则存档读取时会丢失自定义属性。

## 批量注册（反射扫描）

```csharp
foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
{
    if (!type.IsAbstract && typeof(RelicModel).IsAssignableFrom(type))
    {
        SavedPropertiesTypeCache.InjectTypeIntoCache(type);
    }
}
```

---

## 反射自动注册（属性标注 + 扫描）

不用手动写 `ModHelper.AddModelToPool`，用属性标注 + 反射扫描：

```csharp
// 1. 定义自定义属性
[AttributeUsage(AttributeTargets.Class)]
public class RegisterEventAttribute : Attribute { }

// 2. 标注类
[RegisterEvent]
public class MyEvent : EventModel { }

// 3. 扫描注册
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

---

## ModHelper.AddModelToPool 正确用法

```csharp
// ✅ 泛型版本（类型在编译时已知）
ModHelper.AddModelToPool<SharedRelicPool, MyRelic>();

// ✅ 反射版本（类型在运行时确定）
ModHelper.AddModelToPool(typeof(SharedRelicPool), typeof(MyRelic));

// ❌ 错误
// ModHelper.AddModelToPool<Type>(...) // 编译错误
```

---

## 初始化生命周期顺序

```csharp
public static void Initialize()
{
    // Phase 1: Harmony 补丁
    harmony.PatchAllSafe(assembly);

    // Phase 2: 序列化缓存注入
    SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(CustomCard));

    // Phase 3: 内容注册（AddModelToPool）
    ModHelper.AddModelToPool<SharedRelicPool, MyRelic>();

    // Phase 4: 场景脚本映射（有自定义场景时）
    ScriptManagerBridge.LookupScriptsInAssembly(assembly);

    // Phase 5: 配置、资源预加载
    AssetPreloader.Preload();
}
```

**关键顺序**：补丁 → 序列化 → 注册 → 场景映射 → 资源。不能颠倒。

---

## ModelDb 已初始化后的延迟注册

当 ModelDb 已经 Init 后（比如模组加载顺序导致），不能用 `AddModelToPool`，需要直接注入：

```csharp
// 检查 ModelDb 是否已初始化
if (ModelDb.Contains(typeof(Ironclad)))
{
    // 已初始化，用 Inject 绕过 Init
    foreach (Type type in ModelTypes)
    {
        if (ModelDb.Contains(type)) continue;
        ModelDb.Inject(type);
        ModelId id = ModelDb.GetId(type);
        ModelDb.GetById<AbstractModel>(id).InitId(id);
    }
}
```

---

## 注册后刷新 ModelDb 缓存

注册新模型后，ModelDb 的内部缓存可能还是旧的，需要反射重置：

```csharp
string[] cacheFields = { "_allCards", "_allCardPools", "_allRelics", "_allPotions" };
foreach (string fieldName in cacheFields)
{
    FieldInfo? field = typeof(ModelDb).GetField(fieldName,
        BindingFlags.Static | BindingFlags.NonPublic);
    field?.SetValue(null, null);
}

// 卡池自身的缓存也要重置
FieldInfo? poolField = typeof(CardPoolModel).GetField("_allCards",
    BindingFlags.Instance | BindingFlags.NonPublic);
poolField?.SetValue(cardPool, null);
```