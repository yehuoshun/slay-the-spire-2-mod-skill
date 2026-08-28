# 自定义角色：纯原生一键注册

## 进阶：纯原生一键注册

> 从 BaseLib 提炼，零第三方依赖。用 `[RegisteredCharacter]` 标记 + 反射在 Patch 里统一注册角色及其关联池，替代逐个手写。

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RegisteredCharacterAttribute : Attribute { }

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)]
public static class AllCharactersPatch
{
    private static void Postfix(ref IEnumerable<CharacterModel> __result)
    {
        var registered = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<RegisteredCharacterAttribute>() != null)
            .Select(t => (CharacterModel)Activator.CreateInstance(t)!);
        __result = __result.Concat(registered);
    }
}
```

```csharp
[RegisteredCharacter]
public class MyCharacter : CharacterModel { ... }   // 自动注册
```

> 角色的卡池/遗物池/药水池由角色类自身的 `CardPool` / `RelicPool` / `PotionPool` 属性关联，无需额外注册。

