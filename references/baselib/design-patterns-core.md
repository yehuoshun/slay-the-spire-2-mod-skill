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

### 工程细节（实战项目验证）

大型 mod 会把注册拆成「扫描 → 延迟 → 冻结」三阶段，防 Android 崩溃 + 防晚期注册：

1. **安全扫描**：`GetTypes()` 包 try-catch（`ReflectionTypeLoadException`，Android/Mono 常见），坏类型跳过
2. **延迟注册**：`[Pool]` 立即 `AddModelToPool`；事件/先古/球/怪/附魔/单例/角色等先收集到静态集合，`ModelDb.Init` 阶段（Harmony Patch 内）再建规范实例注册（`CustomEventRegistry` 等）
3. **冻结**：`Freeze()` 后所有晚期 `AddModel` 变 no-op + 警告——防止 `ModelDb.Init` 之后的意外注册污染

### 基类注解继承（学自 ModTemplate-StS2，推荐写法）

> [Pool] 标在抽象基类上，子类继承自动进池 → [design-patterns-pooling.md](design-patterns-pooling.md)

---

> 链式辅助方法（转译 Builder）→ [design-patterns-builder.md](design-patterns-builder.md)
