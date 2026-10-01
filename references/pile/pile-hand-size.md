# 手牌上限修改

> 纯原生实现。灵感来源：BaseLib `IMaxHandSizeModifier`。

## 概述

游戏手牌上限写死为 10。想让能力/遗物修改这个值（如「手牌上限 +2」），需要：
1. 定义 `IMaxHandSizeModifier` 接口
2. 能力/遗物实现它
3. Harmony Transpiler 把所有写死的 `10` 替换为动态值

## 实现

### 接口

```csharp
using MegaCrit.Sts2.Core.Entities.Players;

public interface IMaxHandSizeModifier
{
    /// <summary>前置修正（多个 modifier 依次叠加）</summary>
    int ModifyMaxHandSize(Player player, int current) => current;

    /// <summary>后置修正（可在前置之后统一调整）</summary>
    int ModifyMaxHandSizeLate(Player player, int current) => current;
}
```

### 计算辅助

```csharp
public static class HandSizeHelper
{
    public const int DefaultMax = 10;

    public static int GetMaxHandSize(Player player, int baseLimit = DefaultMax)
    {
        int amount = baseLimit;
        var list = new List<IMaxHandSizeModifier>();

        foreach (var model in player.PlayerCombatState.AllModels)
        {
            if (model is IMaxHandSizeModifier mod)
            {
                list.Add(mod);
                amount = mod.ModifyMaxHandSize(player, amount);
            }
        }
        foreach (var mod in list)
            amount = mod.ModifyMaxHandSizeLate(player, amount);

        return Math.Max(0, amount);
    }
}
```

### Transpiler

把所有 `ldc.i4 10`（手牌上限常量）替换为 `call GetMaxHandSize`：

```csharp
using System.Reflection.Emit;
using HarmonyLib;

[HarmonyPatch]
public static class HandSizeTranspiler
{
    // 目标：所有包含常量 10 的与手牌相关的方法
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(CardPileCmd),
            nameof(CardPileCmd.CheckIfDrawIsPossibleAndShowThoughtBubbleIfNot));
        yield return AccessTools.Method(typeof(CombatManager),
            nameof(CombatManager.SetupPlayerTurn));
    }

    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        foreach (var ins in instructions)
        {
            // 检测 ldc.i4 10（手牌上限硬编码）
            if ((ins.opcode == OpCodes.Ldc_I4_S &&
                 ins.operand is sbyte sb && sb == 10) ||
                (ins.opcode == OpCodes.Ldc_I4 &&
                 ins.operand is int i && i == 10))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0); // player
                yield return ins; // 10（作为 baseLimit 参数）
                yield return new CodeInstruction(OpCodes.Call,
                    typeof(HandSizeHelper).GetMethod(
                        nameof(HandSizeHelper.GetMaxHandSize),
                        [typeof(Player), typeof(int)]));
            }
            else
            {
                yield return ins;
            }
        }
    }
}
```

### 在能力中使用

```csharp
public class ExtraDraw : PowerModel, IMaxHandSizeModifier
{
    // 手牌上限 +3
    public int ModifyMaxHandSize(Player player, int current) => current + 3;
}
```

> 手牌超 10 张的布局修正（HandPosFix Prefix）→ [pile-hand-layout.md](pile-hand-layout.md)

## 注册

```csharp
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        var harmony = new Harmony("myskill");
        harmony.PatchAll(); // 注册 HandSizeTranspiler
    }
}
```

## 参见

- [pile-base.md](pile-base.md) — 牌堆基础概念