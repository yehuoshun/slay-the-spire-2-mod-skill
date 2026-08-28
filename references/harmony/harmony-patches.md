# Harmony 补丁：PatchCategory 与安全模式

## PatchCategory — 分类批量加载

```csharp
// 定义 Patch 类别
public static class PatchCategories
{
    public const string Core = "Core";
    public const string MoreRules = "MoreRules";
    public const string DpsMeter = "DpsMeter";
}

// 标记 Patch 类所属类别
[HarmonyPatchCategory("Core")]
[HarmonyPatch(typeof(NMainMenu), "_Ready")]
internal static class PlusLifecyclePatch { ... }

[HarmonyPatchCategory("MoreRules")]
[HarmonyPatch(typeof(ModifierModel), "FromSerializable")]
internal static class CustomModifierSerializationPatch { ... }

// 分类加载
public static void Initialize()
{
    var harmony = new Harmony("mymod");
    PatchCategory(harmony, "Core");
    PatchCategory(harmony, "MoreRules");
}

private static void PatchCategory(Harmony harmony, string category)
{
    try
    {
        harmony.PatchCategory(typeof(ModEntry).Assembly, category);
    }
    catch (Exception e)
    {
        Logger.Error($"Module failed: {category} -> {e}");
    }
}
```

### 优点

- 按功能分类，方便开关
- 一个类炸了不影响其他类别
- 多人兼容：可根据角色选择是否加载

---

## 安全模式

每个 Patch 类独立 try-catch，防止一个 Patch 炸了整个 Mod：

```csharp
// 方式1：类别级 try-catch（推荐）
private static void PatchCategory(Harmony harmony, string category)
{
    try
    {
        harmony.PatchCategory(typeof(ModEntry).Assembly, category);
        Logger.Info($"Loaded: {category}");
    }
    catch (Exception e)
    {
        Logger.Error($"Failed: {category} -> {e}");
    }
}

// 方式2：单个 Patch 类 try-catch（不推荐，但可用）
[HarmonyPatch(typeof(X), nameof(X.Y))]
public static class SafePatch
{
    private static void Postfix()
    {
        try { /* 效果逻辑 */ }
        catch (Exception e) { Logger.Error(e.ToString()); }
    }
}
```

