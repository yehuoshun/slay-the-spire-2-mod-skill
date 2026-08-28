# Harmony 补丁：基础 Patch 类型与参数

## 基础 Patch 类型

### Prefix — 在目标方法前执行

```csharp
[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDamage))]
public static class MyPatch
{
    // 返回 false 跳过原方法
    private static bool Prefix(ref int __result)
    {
        __result = 999; // 覆盖返回值
        return false;   // 跳过原方法
    }
}
```

### Postfix — 在目标方法后执行

```csharp
[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDamage))]
public static class MyPatch
{
    private static void Postfix(CardModel __instance, ref int __result)
    {
        __result += 2; // 修改返回值
    }
}
```

### Transpiler — 修改 IL 代码

```csharp
[HarmonyPatch(typeof(SomeClass), nameof(SomeClass.SomeMethod))]
public static class MyTranspiler
{
    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        // 修改 IL 指令
        return instructions;
    }
}
```

### TargetMethod — 动态指定目标

```csharp
[HarmonyPatch] // 不指定目标类型/方法
public static class MyPatch
{
    // 运行时动态返回目标方法
    private static MethodBase? TargetMethod()
    {
        var type = RuntimeTypeResolver.FindType("MegaCrit.Sts2.Core.Models.CardModel");
        return AccessTools.Method(type, nameof(CardModel.ToMutable));
    }

    private static void Postfix(object? __result) { }
}
```

### TargetMethods — 多个目标

```csharp
[HarmonyPatch]
public static class MyMultiPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(CardModel), nameof(CardModel.GetDamage));
        yield return AccessTools.Method(typeof(CardModel), nameof(CardModel.GetBlock));
    }

    private static void Postfix(ref int __result) { }
}
```

---

## Patch 参数速查

| 参数 | 说明 |
|------|------|
| `__instance` | 被 Patch 的实例（实例方法） |
| `__result` | 返回值（ref，Postfix 可修改） |
| `__state` | 在 Prefix 中设置，Postfix 中使用 |
| `__args` | 原始参数数组 |
| 原方法参数名 | 按名称访问原方法参数 |

