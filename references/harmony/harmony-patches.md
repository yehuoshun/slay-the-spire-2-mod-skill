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

## PatchAllSafe — 逐类 try-catch 的批量 PatchAll

> 实战项目验证（YuWanCard）。`PatchAll` 遇到一个加载不了的 patch 类型会中断整个初始化（Android/Mono AOT 下游戏类型静态构造函数 NRE 常见）。替代：逐个类 Patch + try-catch，坏类型只跳过不中断。

```csharp
public static void PatchAllSafe(Harmony harmony, Assembly assembly, HashSet<string>? exclude = null)
{
    foreach (var type in assembly.GetTypes())
    {
        if (type.GetCustomAttribute<HarmonyPatch>() == null) continue;
        if (exclude?.Contains(type.Name) == true) continue;   // 平台条件 patch 单独应用
        try { harmony.CreateClassProcessor(type).Patch(); }
        catch (Exception e) { Logger.Error($"Patch failed {type.Name}: {e}"); }
    }
}

// 平台条件 patch：单独包 try-catch 应用
if (!IsMobilePlatform())
{
    patcher.ApplySingle(h => h.CreateClassProcessor(typeof(DesktopOnlyPatch)).Patch(), "DesktopOnly");
}
```

移动端检测：`Godot.OS.GetName() == "Android" || "iOS"`。

---

## ModInterop — Transpiler 模组互操作

> 实战项目验证（YuWanCard 自研框架）。编译期**零依赖**调用其他 mod 的 API：目标 mod 未加载时空实现 fallback（不崩、无副作用），已加载则替换为直接 IL 调用。比反射调用性能好，比硬引用不炸。⚠️ **`[ModInterop]`/`[InteropTarget]`/`ModInteropProcessor` 均为 YuWan 自研，原生没有**——需自研实现（思路：扫描存根类 → 目标类型已加载则 `[HarmonyPatch]` + `TargetMethod()` 动态指定 + Transpiler 替换方法体，见 [harmony-basics.md](harmony-basics.md)）。

```csharp
// 自研存根类设计（attribute 需自己定义）
[ModInterop("目标modId")]                                   // 标记存根类归属哪个 mod
public static class MyStub
{
    [InteropTarget("命名空间.目标类型", "目标方法名")]
    public static object? GetData(string modId) { return null; }  // fallback 空实现
}

// 初始化时（Harmony 已就绪）
ModInteropProcessor.Process(harmony, Assembly.GetExecutingAssembly());
```

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

