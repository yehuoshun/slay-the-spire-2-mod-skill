# 自定义资源：注册、生命周期与示例

> 资源基类见 [resource-core.md](resource-core.md)。

## 注册与生命周期

资源需要跟随 `PlayerCombatState` 创建/销毁：

```csharp
using MegaCrit.Sts2.Core.Entities.Players;

public static class CustomResourceManager
{
    private static readonly Dictionary<string, Func<CustomResource>> _factories = new();
    private static readonly ConditionalWeakTable<PlayerCombatState, List<CustomResource>> _instances = new();

    public static void Register<T>(string id) where T : CustomResource, new()
    {
        _factories[id] = () => new T();
    }

    public static T Get<T>(PlayerCombatState pcs) where T : CustomResource
    {
        var list = _instances.GetOrCreateValue(pcs);
        return list.OfType<T>().FirstOrDefault()!;
    }

    public static void Setup(PlayerCombatState pcs)
    {
        var list = new List<CustomResource>();
        foreach (var factory in _factories.Values)
        {
            var resource = factory();
            resource.PrepForCombat(pcs);
            list.Add(resource);
        }
        _instances.Add(pcs, list);
    }

    public static void Cleanup(PlayerCombatState pcs) => _instances.Remove(pcs);

    public static IEnumerable<CustomResource> GetAll(PlayerCombatState pcs) =>
        _instances.TryGetValue(pcs, out var list) ? list : Enumerable.Empty<CustomResource>();
}
```

### 生命周期 Patch

```csharp
[HarmonyPatch(typeof(PlayerCombatState), MethodType.Constructor, typeof(Player))]
public static class ResourceSetupPatch
{
    [HarmonyPostfix]
    private static void Postfix(PlayerCombatState __instance)
        => CustomResourceManager.Setup(__instance);
}

[HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.AfterCombatEnd))]
public static class ResourceCleanupPatch
{
    [HarmonyPostfix]
    private static void Postfix(PlayerCombatState __instance)
        => CustomResourceManager.Cleanup(__instance);
}

[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.SetupPlayerTurn))]
public static class ResourceTurnResetPatch
{
    [HarmonyPostfix]
    private static void Postfix(Player player)
    {
        foreach (var r in CustomResourceManager.GetAll(player.PlayerCombatState))
            r.StartOfTurnReset(player.PlayerCombatState);
    }
}
```

## 完整示例：法力系统

```csharp
public class ManaResource : CustomResource
{
    public override int StartAmount => 3;
    public override int MaxAmount => 10;
    public override bool ResetEachTurn => true;
}

// 注册
CustomResourceManager.Register<ManaResource>("Mana");

// 能力里使用
var mana = CustomResourceManager.Get<ManaResource>(Owner.PlayerCombatState);
if (mana.Amount >= 2)
{
    mana.Spend(2);
    // 执行高耗能效果
}
```

## 自定义图标预加载（防战斗中丢图）

游戏的运行期预加载列表由 `PreloadManager.GetRunAssetPaths`（**private static**，在 `MegaCrit.Sts2.Core.Assets`）生成：卡牌走 `CardModel.RunAssetPaths`（虚属性 `ExtraRunAssetPaths` 可覆写）、卡池走 `CardPoolModel.EnergyIconPath`、角色走 `CharacterModel.AssetPaths`。**能力的图标不在此列**（原生按 Id 约定路径推导），所以用非常规路径的自定义图标，战斗中可能加载失败/显示占位图。

补法：Postfix 把自定义图标路径并进返回值。

```csharp
[HarmonyPatch(typeof(PreloadManager), "GetRunAssetPaths")]   // private → 用字符串名
static class CustomIconPreloadPatch
{
    [HarmonyPostfix]
    static void AddCustomIcons(ref IEnumerable<string> __result)
        => __result = __result.Concat(GetCustomIconPaths()).Distinct(StringComparer.Ordinal);

    private static IEnumerable<string> GetCustomIconPaths()
    {
        foreach (PowerModel p in ModelDb.AllPowers)          // 遍历自定义能力，取自研图标属性
            if (p is IHasCustomIcon ic) yield return ic.PackedIconPath;
        foreach (CardPoolModel pool in ModelDb.AllCardPools)
            yield return pool.EnergyIconPath;
    }
}
```

> `ModelDb.AllPowers` / `ModelDb.AllCardPools` 可直接枚举；`Distinct` 去重避免与原生路径重复。`IHasCustomIcon` 为自研接口（原生 `PowerModel` 无自定义图标属性）。

## 参见

- [resource-core.md](resource-core.md) — 资源基类
- [resource-cost.md](resource-cost.md) — 资源费用