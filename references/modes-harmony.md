# Harmony 补丁

> 三种组织方式：最简 → 分组 → 生产级。从 STS2Plus / YuWanCard / 海克斯符文提炼。

---

## 基本语法

```csharp
using HarmonyLib;

namespace MyMod.Patches;

// 前置补丁：在原方法执行前运行
[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
[HarmonyPrefix]
public static bool Prefix(TargetClass __instance)
{
    // 返回 false 跳过原方法
    return true;
}

// 后置补丁：在原方法执行后运行
[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
[HarmonyPostfix]
public static void Postfix(TargetClass __instance, ref int __result)
{
    // 可以修改 __result
}

// 完全替换
[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
[HarmonyPrefix]
public static bool Prefix(TargetClass __instance, ref int __result)
{
    __result = 42;
    return false; // 跳过原方法
}
```

---

## 组织方式一：最简（1-2 个 Patch）

```csharp
// ModEntry.cs 中直接 PatchAll
_harmony.PatchAll(Assembly.GetExecutingAssembly());
```

---

## 组织方式二：分组 Patch（学 STS2Plus）

用 `HarmonyPatchCategory` 分组，按需加载：

```csharp
// Patch 类上标记分组
[HarmonyPatchCategory("MoreRules")]
[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.EndTurn))]
public static class EndTurnPatch { ... }

// ModEntry 中按分组加载
private static void PatchCategory(string category)
{
    try
    {
        _harmony.PatchCategory(typeof(ModEntry).Assembly, category);
        Log.Info($"Module loaded: {category}");
    }
    catch (Exception e)
    {
        Log.Error($"Module failed: {category} -> {e}");
    }
}
```

---

## 组织方式三：生产级（学海克斯符文 + YuWanCard）

### 方案 A：Try-Install 模式（海克斯符文）

每个 Hook 组独立 try-catch，一个挂了不影响其他：

```csharp
public static void Initialize()
{
    var harmony = new Harmony(HarmonyId);

    // 核心补丁——必须成功
    HextechRunLifecycleHooks.Install(harmony);
    HextechCombatHooks.Install(harmony);

    // 可选补丁——挂了也不影响
    TryInstallOptionalHookGroup("shop forge", () => HextechShopForgeHooks.Install(harmony));
    TryInstallOptionalHookGroup("inspect relic", () => HextechInspectHooks.Install(harmony));
}

private static void TryInstallOptionalHookGroup(string label, Action install)
{
    try { install(); }
    catch (Exception ex) { Log.Warn($"Optional hook skipped: {label}: {ex.Message}"); }
}
```

### 方案 B：ModPatcher 模式（YuWanCard）

注册-应用-回滚，关键 Patch 失败时全量回滚：

```csharp
public class ModPatcher
{
    private readonly Harmony _harmony;
    private readonly List<ModPatchInfo> _registered = [];

    public void RegisterPatch<T>() where T : IPatchMethod, new() { ... }
    public void ApplySingle(Action<Harmony> apply, string id)
    {
        try { apply(_harmony); }
        catch (Exception ex) { Log.Warn($"[Patcher] {id} failed: {ex.Message}"); }
    }

    public bool PatchAll()
    {
        // 逐个应用，关键失败则全量回滚
    }
}
```

---

## 常用 Patch 目标

| 目标 | 说明 |
|------|------|
| `CardModel.OnPlay` | 卡牌打出 |
| `CombatManager.StartCombat` | 战斗开始 |
| `CombatManager.EndCombat` | 战斗结束 |
| `CombatManager.EndTurn` | 回合结束 |
| `Player.Damage` | 玩家受伤 |
| `Player.Heal` | 玩家治疗 |
| `CardPileCmd.Add` | 卡牌加入牌堆 |
| `RewardScreen.GenerateRewards` | 奖励生成 |
| `MapNode.OnEnter` | 进入地图节点 |
| `RunState.StartRun` | 开始一局 |
| `ModelDb.Init` | 模型数据库初始化 |
| `AncientDialogueSet.GetValidDialogues` | 先古之民对话获取 |

---

## 补丁安全原则

1. **每个 Patch 类独立 try-catch**——一个挂了不影响其他
2. **可选补丁用 TryInstall**——非核心功能允许失败
3. **关键补丁失败 → 全量回滚**——避免半初始化状态
4. **Prefix 返回 false 要小心**——可能破坏其他 Mod 的 Postfix
5. **避免 Patch 构造函数**——用 `ModelDb.Init` 后注入代替

---

## 常见问题

| 问题 | 解决 |
|------|------|
| Harmony 报 .NET 版本 | `GodotPlugins.runtimeconfig.json` → `"version": "9.0.0"` |
| PatchAll 一个类炸了全挂 | 单类 try-catch 包裹 |
| 多个 Mod Patch 同一方法冲突 | 用 `HarmonyPriority` 控制顺序 |
| 找不到目标方法 | 检查方法名和参数签名，用 `MethodType` 区分重载 |