# 模式七：Harmony 补丁

## 基本语法

```csharp
using HarmonyLib;

namespace MyMod.Patches;

// 前置补丁：在原方法执行前运行
[HarmonyPatch(typeof(GameClass), "MethodName")]
[HarmonyPrefix]
static void Prefix() { /* 在原方法前 */ }

// 后置补丁：在原方法执行后运行
[HarmonyPatch(typeof(GameClass), "MethodName")]
[HarmonyPostfix]
static void Postfix(ref int __result) { /* 可修改返回值 */ }

// 修改 Getter
[HarmonyPatch(typeof(GameClass), "PropertyName", MethodType.Getter)]
[HarmonyPostfix]
static void Postfix(ref string __result) { __result = "new value"; }

// 修改 Setter
[HarmonyPatch(typeof(GameClass), "PropertyName", MethodType.Setter)]
[HarmonyPrefix]
static void Prefix(ref string value) { value = "new input"; }

// Transpiler：修改方法体 IL（高级）
[HarmonyPatch(typeof(GameClass), "MethodName")]
[HarmonyTranspiler]
static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    return new CodeMatcher(instructions).InstructionEnumeration();
}
```

## 安全安装：逐类 try-catch

```csharp
public static void Initialize()
{
    var harmony = new Harmony("MyMod");
    var assembly = Assembly.GetExecutingAssembly();

    // PatchAll 自动发现所有 [HarmonyPatch] 类
    harmony.PatchAll(assembly);
}

// 更安全：逐类 try-catch，一个类失败不影响其他
public static void PatchAllSafe(Harmony harmony, Assembly assembly)
{
    foreach (var type in assembly.GetTypes())
    {
        try
        {
            if (type.GetCustomAttribute<HarmonyPatch>() != null)
                harmony.CreateClassProcessor(type).Patch();
        }
        catch (Exception ex)
        {
            Log.Warn($"Patch failed: {type.Name}: {ex.Message}");
        }
    }
}
```

## 分组安装（可选组失败不影响核心）

```csharp
TryInstall("cards", () => harmony.PatchAll(typeof(CardPatches).Assembly));
TryInstall("relics", () => harmony.PatchAll(typeof(RelicPatches).Assembly));

private static void TryInstall(string label, Action install)
{
    try { install(); }
    catch (Exception ex) { Log.Warn($"Optional group skipped: {label}: {ex.Message}"); }
}
```

## 常用 Patch 场景

### 修改角色初始遗物

```csharp
[HarmonyPatch(typeof(Ironclad), "StartingRelics", MethodType.Getter)]
[HarmonyPostfix]
static void Postfix(ref IEnumerable<RelicModel> __result)
{
    __result = __result.Append(new MyRelic());
}
```

### 添加事件到地图

```csharp
[HarmonyPatch(typeof(Overgrowth), "AllEvents", MethodType.Getter)]
static void Postfix(ref IEnumerable<EventModel> __result)
{
    __result = __result.Append(new MyCustomEvent());
}
```

### 添加遭遇

```csharp
[HarmonyPatch(typeof(Overgrowth), nameof(Overgrowth.GenerateAllEncounters))]
[HarmonyPostfix]
static void Postfix(ref IEnumerable<EncounterModel> __result)
{
    __result = __result.Append(new GoblinEncounter());
}
```

### 注册角色

```csharp
[HarmonyPatch(typeof(ModelDb), "AllCharacters", MethodType.Getter)]
[HarmonyPostfix]
static void Postfix(ref IEnumerable<CharacterModel> __result)
{
    __result = __result.Append(new WatcherCharacter());
}
```

### 修改卡牌升级上限

```csharp
[HarmonyPatch(typeof(CardModel), "MaxUpgradeLevel", MethodType.Getter)]
static void Postfix(ref int __result) => __result = 99;
```

## 防重复初始化

```csharp
private static bool _initialized;
private static readonly object _lock = new();

public static void Initialize()
{
    if (_initialized) return;
    lock (_lock)
    {
        if (_initialized) return;
        // ... 初始化逻辑 ...
        _initialized = true;
    }
}
```

游戏可能在多个时机调用 `Initialize`（主菜单和进入游戏时各一次），必须防重复。