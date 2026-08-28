# 自定义 Modifier：纯原生注册工厂

## 进阶：纯原生注册工厂

> 从 BaseLib 提炼，零第三方依赖。用工厂类统一创建 + Attribute 标记，替代多个手写 Patch。

```csharp
// 工厂：统一创建自定义 Modifier
public static class CustomModifierCatalog
{
    public static IEnumerable<ModifierModel> CreateAll()
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (type.GetCustomAttribute<RegisteredModifierAttribute>() != null)
                yield return (ModifierModel)Activator.CreateInstance(type)!;
        }
    }
}

[HarmonyPatch(typeof(NCustomRunModifiersList), nameof(NCustomRunModifiersList.GetModifiersTickedOn))]
public static class ModifierListPatch
{
    private static void Postfix(ref List<ModifierModel> __result)
    {
        __result.AddRange(CustomModifierCatalog.CreateAll());
    }
}
```

```csharp
[RegisteredModifier]
public class MyModifier : ModifierModel { ... }   // 自动进 Custom Run 规则列表
```

> 效果仍通过 `[HarmonyPatchCategory]` 分组实现（见上「效果实现模式」），工厂只负责注册元数据。

