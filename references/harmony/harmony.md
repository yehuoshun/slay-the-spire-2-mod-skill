# Harmony 补丁模式

> 参考：BaseLib 源码 `Utils/Patching/` + STS2Plus 源码 `Patches/`

---

## 概述

Harmony 是 STS2 模组开发的必需品。几乎所有自定义效果都要通过 Patch 修改游戏行为。

---

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

---

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

---

## 组织方式

```
Patches/
├── Core/
│   ├── PlusLifecyclePatch.cs
│   ├── SkipIntroLogoPatch.cs
│   └── SpeedControlBootstrapPatch.cs
├── MoreRules/
│   ├── AttackDefenseCardCreationPatch.cs
│   ├── EndlessModeHpScalingPatch.cs
│   └── GlassCannonEnergyPatch.cs
└── ModEntry.cs
```

或按功能平铺（STS2Plus 风格）：

```
Patches/
├── PlusLifecyclePatch.cs           [Core]
├── SkipIntroLogoPatch.cs           [Core]
├── CustomModifierListPatch.cs      [MoreRules]
├── CustomModifierSerializationPatch.cs [MoreRules]
├── AttackDefenseCardCreationPatch.cs  [MoreRules]
├── EndlessModeHpScalingPatch.cs    [MoreRules]
└── GlassCannonEnergyPatch.cs       [MoreRules]
```

---

## 常用 Patch 目标

### 注册 / 模型

| 目标 | 用途 |
|------|------|
| `ModelDb.AllCharacters` getter | 注册自定义角色 |
| `ModelDb.AllCardPools` getter | 注册自定义卡池 |
| `ModelDb.AllRelicPools` getter | 注册自定义遗物池 |
| `ModelDb.AllPotionPools` getter | 注册自定义药水池 |
| `ModifierModel.FromSerializable` | 自定义 Modifier 反序列化 |
| `NCustomRunModifiersList.GetModifiersTickedOn` | 注入运行规则 |

### 战斗

| 目标 | 用途 |
|------|------|
| `CardModel.GetDamage` | 修改卡牌伤害 |
| `CardModel.GetBlock` | 修改卡牌格挡 |
| `CardModel.ToMutable` | 卡牌创建时修改 |
| `Creature.SetCurrentHpInternal` | 限制 HP |
| `Creature.SetMaxHpInternal` | 限制最大 HP |
| `Creature.AfterAddedToRoom` | 怪物入场时修改 |
| `Creature.BeforeCombatStart` | 战斗开始前修改 |
| `CombatRoom` constructor | 替换遭遇 |

### UI

| 目标 | 用途 |
|------|------|
| `NMainMenu._Ready` | 主菜单初始化 |
| `NCombatRoom.OnCombatSetUp` | 战斗 UI 设置 |
| `NMapScreen` | 地图 UI 修改 |
| `NEnergyCounter.Create` | 自定义能量计数器 |

### 事件

| 目标 | 用途 |
|------|------|
| `ActModel.AllEvents` getter | 注入事件 |
| `ActModel.AllAncients` getter | 注入先古之民 |
| `GenerateAllEncounters` | 注入遭遇 |
| `EventModel.BeginEvent` | 事件开始时修改 |
| `EventModel.SetEventFinished` | 事件结束时修改 |

---

## 常见问题

| 问题 | 解决 |
|------|------|
| Patch 不生效 | 检查 `harmony.PatchAll()` 或 `PatchCategory()` 是否调用 |
| 一个类炸了全挂 | 每个 Patch 类别独立 try-catch |
| 找不到目标方法 | 使用 `RuntimeTypeResolver.FindType()` 反射查找 |
| 类型不匹配 | 用 `AccessTools` 而非直接写类型 |
| Postfix 修改返回值无效 | 参数声明为 `ref int __result` |
| 多人模式下崩溃 | 用 `MultiplayerSafety` 检查后再 Patch |

---

## 演进路线

- 当前：手动 `harmony.PatchAll()` 全量加载
- 更优：`[HarmonyPatchCategory]` + `PatchCategory()` 分类批量加载
- 再优：用 `ModAnalyzers` 编译器分析检测 Patch 正确性