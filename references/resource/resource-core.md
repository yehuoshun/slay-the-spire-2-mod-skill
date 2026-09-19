# 自定义资源：基类、注册与生命周期

## 资源基类

一个自定义资源就是一个继承 `AbstractModel` 的类（不是 `PowerModel`——资源不是能力，是独立的战斗状态）。

```csharp
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

public abstract class CustomResource : AbstractModel
{
    /// <summary>当前值</summary>
    protected int _amount;
    public int Amount => _amount;

    /// <summary>最大值（可选）</summary>
    public virtual int MaxAmount => 999;

    /// <summary>每回合是否重置到初始值</summary>
    public virtual bool ResetEachTurn => true;

    /// <summary>初始值</summary>
    public abstract int StartAmount { get; }

    /// <summary>准备战斗时调用</summary>
    public virtual void PrepForCombat(PlayerCombatState pcs)
    {
        _amount = StartAmount;
    }

    /// <summary>每回合开始重置</summary>
    public virtual void StartOfTurnReset(PlayerCombatState pcs)
    {
        if (ResetEachTurn) _amount = StartAmount;
    }

    /// <summary>消耗资源，返回是否成功</summary>
    public virtual bool Spend(int amount)
    {
        if (_amount < amount) return false;
        _amount -= amount;
        return true;
    }

    /// <summary>增加资源，不超过上限</summary>
    public virtual void Gain(int amount)
    {
        _amount = Math.Min(_amount + amount, MaxAmount);
    }
}
```

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

## 参见

- [resource-cost.md](resource-cost.md) — 资源费用 + UI