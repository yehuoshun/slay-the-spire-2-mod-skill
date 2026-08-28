# 序列化与注册：纯原生自动注册框架

## 进阶：纯原生自动注册框架

> 从 BaseLib 提炼的设计模式，用纯原生 API 实现自动注册，免去每个模型手动 `AddModelToPool`。
> 框架代码见 [design-patterns.md](../baselib/design-patterns.md)「模式 1」；本文给本模块可直接套用的完整版。

### 自定义 Attribute

```csharp
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

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PowerModelAttribute : Attribute { } // 不进池的模型
```

### ContentRegistry — 反射扫描统一注册

```csharp
using System.Reflection;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

public static class ContentRegistry
{
    public static void RegisterAll(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute<CardPoolAttribute>() is { } cp)
            {
                ModHelper.AddModelToPool(cp.PoolType, type);      // 进卡池
            }
            else if (type.GetCustomAttribute<RelicPoolAttribute>() is { } rp)
            {
                ModHelper.AddModelToPool(rp.PoolType, type);      // 进遗物池
            }
            else if (type.GetCustomAttribute<PowerModelAttribute>() != null)
            {
                ModelDb.Inject(type);                              // 能力：只注册 ID，不进池
            }
        }
    }
}
```

### ModEntry 调用

```csharp
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
[CardPool(typeof(ColorlessCardPool))]
public class MyCard : CardModel { ... }

[RelicPool(typeof(SharedRelicPool))]
public class MyRelic : RelicModel { ... }

[PowerModel]
public class MyPower : PowerModel { ... }
```

### 注意

- 必须在 `ModelDb` 初始化前注册（`Initialize` 阶段）
- 需要额外逻辑的注册（角色/事件/遭遇/Modifier 走 Patch 注入）不要用此框架，见下方「注册方式 3」
- `[Pool]` 类 Attribute 可自定义扩展，覆盖任意原生池类型

