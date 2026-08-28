# 自定义敌怪：纯原生注册辅助

## 进阶：纯原生注册辅助

> 从 BaseLib 提炼，零第三方依赖。遭遇走 Patch 注入，用 Attribute 标记 + 反射统一收集。

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RegisteredEncounterAttribute : Attribute { }

[HarmonyPatch(typeof(Overgrowth), nameof(Overgrowth.GenerateAllEncounters))]
public static class EncountersPatch
{
    private static void Postfix(ref IEnumerable<EncounterModel> __result)
    {
        var registered = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<RegisteredEncounterAttribute>() != null)
            .Select(t => (EncounterModel)Activator.CreateInstance(t)!);
        __result = __result.Concat(registered);
    }
}
```

```csharp
[RegisteredEncounter]
public class MyEncounter : EncounterModel { ... }   // 自动注册
```

