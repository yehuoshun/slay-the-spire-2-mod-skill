# 先古之民事件：纯原生注册辅助

## 进阶：纯原生注册辅助

> 从 BaseLib 提炼，零第三方依赖。先古之民走 Patch 注入，用 Attribute 标记 + 反射统一收集。

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RegisteredAncientAttribute : Attribute { }

[HarmonyPatch(typeof(Hive), nameof(Hive.AllAncients), MethodType.Getter)]
public static class AllAncientsPatch
{
    private static void Postfix(ref IEnumerable<AncientEventModel> __result)
    {
        var registered = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<RegisteredAncientAttribute>() != null)
            .Select(t => (AncientEventModel)Activator.CreateInstance(t)!);
        __result = __result.Concat(registered);
    }
}
```

```csharp
[RegisteredAncient]
public class MyAncient : AncientEventModel { ... }   // 自动注册
```

