# 属性式 Patch 框架（元数据 + Prepare 门控 + 共享目标检测）

> 实战验证（sts2mod 寰宇支配之剑 v0.3.0，2026-10-07）。小 mod 的「补丁管理框架」：每个 patch 类带元数据、可降级、启动时全量汇报，还能列出与其他 mod 共享的补丁点。

## 元数据 Attribute + 统一应用入口

```csharp
[AttributeUsage(AttributeTargets.Class)]
internal sealed class SwordPatchAttribute : Attribute
{
    public SwordPatchAttribute(string id, string feature) { Id = id; Feature = feature; }
    public string Id { get; } public string Feature { get; }
    public bool Optional { get; init; }   // true = 失败只 Info 不 Warn
}

internal static class SwordPatcher
{
    internal static void ApplyAll(Harmony harmony, Assembly assembly)
    {
        foreach (Type type in AccessTools.GetTypesFromAssembly(assembly))
        {
            SwordPatchAttribute? meta = type.GetCustomAttribute<SwordPatchAttribute>();
            bool hasHarmonyAttributes = HarmonyMethodExtensions.GetFromType(type).Count > 0;
            if (!hasHarmonyAttributes)
            {
                // 声明了元数据却没有 [HarmonyPatch] 目标 → 显形（静默跳过 = 补丁凭空消失）
                if (meta != null) Log.Warn($"Patch declared but has no target: {meta.Id}");
                continue;
            }
            try
            {
                List<MethodInfo>? patched = harmony.CreateClassProcessor(type).Patch();
                bool gated = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Any(m => m.GetCustomAttribute<HarmonyPrepare>() != null);
                if ((patched == null || patched.Count == 0) && !gated && meta?.Optional != true)
                    throw new InvalidOperationException("class processor patched no methods");
            }
            catch (Exception ex)
            {
                // 解包根因（HarmonyException/TargetInvocationException 的 InnerException）
                // Optional → Info；否则 Warn；结果进汇总表
            }
        }
    }
    internal static void LogSummary() { /* 统计 Applied x/y + 未应用清单 + 缺失成员 */ }
}
```

- 用法：`[SwordPatch("neow.initial-options", "涅奥第四个先古遗物选项", Optional = true)]` + `[HarmonyPatch(...)]` 双特性。
- 好处：启动日志里每个补丁「应用/跳过/失败 + 原因」一目了然；`Optional` 让版本兼容降级不再静默。
- 无元数据但有 [HarmonyPatch] 的类照常应用（元数据只是增强）。

## [HarmonyPrepare] 门控降级（配合集中反射）

```csharp
// VanillaMembers：本 mod 触碰的全部原版私有成员集中定义，找不到进 MissingMembers
internal static class VanillaMembers
{
    private static readonly List<string> Missing = [];
    internal static IReadOnlyList<string> MissingMembers => Missing;
    internal static readonly MethodInfo? AncientRelicOption = Method(typeof(AncientEventModel),
        "RelicOption", [typeof(RelicModel), typeof(string), typeof(string)]);
}

[HarmonyPatch(typeof(Neow), "GenerateInitialOptions")]
internal static class NeowInitialOptionsPatch
{
    [HarmonyPrepare]
    private static bool Prepare() => VanillaMembers.AncientRelicOption != null;  // 缺成员 → 补丁整体跳过
    [HarmonyPostfix]
    private static void Postfix(...) { ... }
}
```

- 反射句柄集中一处 + 缺失进启动汇总 Warn——**避免各补丁文件自持句柄悄悄失效**。
- `[HarmonyPrepare]` 返回 false = 该类不应用（Harmony 原生机制）。

## 共享补丁目标检测（冲突排查）

```csharp
foreach (MethodBase method in Harmony.GetAllPatchedMethods())
{
    HarmonyLib.Patches? info = Harmony.GetPatchInfo(method);
    string[] others = [.. info.Prefixes, .. info.Postfixes, .. info.Transpilers, .. info.Finalizers]
        .Where(p => p.owner != harmony.Id).Select(p => p.owner)
        .Distinct(StringComparer.Ordinal).OrderBy(o => o, StringComparer.Ordinal).ToArray();
    if (others.Length > 0)
        lines.Add($"{method.DeclaringType?.FullName}.{method.Name} <- {string.Join(", ", others)}");
}
```

- 启动列出全部「本 mod 与其他 mod 共享的补丁点」：玩家报兼容问题时，这行日志就是答案。

## 版本差异双实现（partial class + 条件编译）

```csharp
#if STS2_107_1
// Hook.AfterTurnEnd(combatState, enemy, enemies)   0.107 分发钩子名
#elif STS2_108_OR_NEWER
// Hook.AfterSideTurnEnd(...)                       0.108+ 改名
#endif
```

- 同一 partial 类不同文件按符号各编译一份；csproj 按目标版本定义符号。