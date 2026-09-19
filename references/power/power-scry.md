# 预见（Scry）修改

> 纯原生实现。灵感来源：BaseLib `IModifyScryAmount` + `IAfterScryed`。

## 概述

预见是从牌组顶看 X 张牌，选择弃掉一部分。两个接口分别修改预见数和响应预见完成。

## 接口定义

```csharp
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

/// <summary>修改即将触发的预见数</summary>
public interface IModifyScryAmount
{
    int ModifyScryAmount(Player player, int amount) => amount;
}

/// <summary>预见完成后的回调</summary>
public interface IAfterScryed
{
    Task AfterScryed(PlayerChoiceContext ctx, Player player,
        int scryAmount, int discardAmount,
        List<CardModel> seen, List<CardModel> discarded);
}
```

## Dispatch 辅助

遍历所有能力/遗物检查接口实现并调用：

```csharp
public static class ScryHooks
{
    /// <summary>在所有能力/遗物中分发 ModifyScryAmount</summary>
    public static int ModifyScryAmount(Player player, int amount)
    {
        int result = amount;
        foreach (var power in player.Creature.Powers)
        {
            if (power is IModifyScryAmount mod)
                result = mod.ModifyScryAmount(player, result);
        }
        return result;
    }

    /// <summary>在所有能力/遗物中分发 AfterScryed</summary>
    public static async Task AfterScryed(PlayerChoiceContext ctx, Player player,
        int scryAmount, int discardAmount,
        List<CardModel> seen, List<CardModel> discarded)
    {
        foreach (var power in player.Creature.Powers)
        {
            if (power is IAfterScryed hook)
                await hook.AfterScryed(ctx, player,
                    scryAmount, discardAmount, seen, discarded);
        }
    }
}
```

## Harmony Patch

拦截 `ScryCmd` 的入口和出口：

```csharp
using MegaCrit.Sts2.Core.Commands;

[HarmonyPatch(typeof(ScryCmd), nameof(ScryCmd.Execute))]
public static class ScryModifyPatch
{
    [HarmonyPrefix]
    private static void ModifyAmount(PlayerChoiceContext ctx,
        ref int amount, Player player)
    {
        amount = ScryHooks.ModifyScryAmount(player, amount);
    }
}

[HarmonyPatch(typeof(ScryCmd), nameof(ScryCmd.Execute))]
public static class ScryAfterPatch
{
    [HarmonyPostfix]
    private static async Task AfterScryed(PlayerChoiceContext ctx,
        int amount, Player player,
        List<CardModel> seen, List<CardModel> discarded)
    {
        await ScryHooks.AfterScryed(ctx, player,
            amount, discarded?.Count ?? 0, seen, discarded);
    }
}
```

> **注意**：`ScryCmd.Execute` 是 async 方法，验证 Postfix 中是否能取到 `seen` 和 `discarded` 参数。如果取不到，改用 Transpiler 拦截 async 状态机。

## 在能力中使用

```csharp
// 预见数 +2
public class ScryBuff : PowerModel, IModifyScryAmount
{
    public int ModifyScryAmount(Player player, int amount) => amount + 2;
}

// 每预见弃一张牌回 1 HP
public class ScryHeal : PowerModel, IAfterScryed
{
    public async Task AfterScryed(PlayerChoiceContext ctx, Player player,
        int scryAmount, int discardAmount,
        List<CardModel> seen, List<CardModel> discarded)
    {
        await CreatureCmd.Heal(Owner, discardAmount);
    }
}
```

## 注册

```csharp
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        var harmony = new Harmony("myskill");
        harmony.PatchAll();
    }
}
```

## 参见

- [power-callbacks.md](power-callbacks.md) — 完整回调列表