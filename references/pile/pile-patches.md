# 自定义牌堆：Harmony Patch

自定义牌堆需要 5 个 Patch 才能正常工作。忽略任何一个都可能导致牌堆不可见或行为异常。

## Patch 总览

| # | 目标 | 类型 | 作用 |
|:-:|------|:----:|------|
| 1 | `CardPile.Get(PileType, Player)` | Prefix | 按 PileType 查找牌堆时返回自定义实例 |
| 2 | `PileTypeExtensions.IsCombatPile` | Prefix | 标记为战斗牌堆 |
| 3 | `PlayerCombatState.AllPiles` getter | Transpiler | 加入 AllPiles 枚举 |
| 4 | `PileTypeExtensions.GetTargetPosition` | Transpiler | 自定义卡牌定位 |
| 5 | `NCard.FindOnTable(CardModel)` | Postfix | 查找卡牌节点 |

## Patch 1：CardPile.Get

```csharp
[HarmonyPatch(typeof(CardPile), nameof(CardPile.Get))]
public static class GetCustomPilePatch
{
    [HarmonyPrefix]
    private static bool Prefix(PileType type, Player player, ref CardPile __result)
    {
        if (CustomPileRegistry.IsCustom(type))
        {
            __result = CustomPileRegistry.Get(type);
            return false;
        }
        return true;
    }
}
```

## Patch 2：IsCombatPile

```csharp
[HarmonyPatch(typeof(PileTypeExtensions), nameof(PileTypeExtensions.IsCombatPile))]
public static class CustomPileIsCombatPatch
{
    [HarmonyPrefix]
    private static bool Prefix(PileType pileType, ref bool __result)
    {
        if (CustomPileRegistry.IsCustom(pileType))
        {
            __result = true;
            return false;
        }
        return true;
    }
}
```

## Patch 3：AllPiles

```csharp
[HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.AllPiles), MethodType.Getter)]
public static class AddCustomPilesPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Stfld &&
                codes[i].operand is FieldInfo fi && fi.Name == "_piles")
            {
                codes.Insert(i, new CodeInstruction(OpCodes.Call,
                    typeof(CustomPileRegistry).GetMethod(nameof(CustomPileRegistry.MergeInto))));
                codes.Insert(i, CodeInstruction.LoadArgument(0));
                break;
            }
        }
        return codes.AsEnumerable();
    }
}
```

需要加合并方法到 `CustomPileRegistry`：

```csharp
public static CardPile[] MergeInto(PlayerCombatState state, CardPile[] original)
{
    var custom = CreateAll();
    return custom.Length > 0 ? original.Concat(custom).ToArray() : original;
}
```

## Patch 4：GetTargetPosition

```csharp
[HarmonyPatch(typeof(PileTypeExtensions), nameof(PileTypeExtensions.GetTargetPosition))]
public static class CustomPilePositionPatch
{
    [HarmonyPrefix]
    private static bool Prefix(PileType pileType, NCard? card, Vector2 size, ref Vector2 __result)
    {
        if (!CustomPileRegistry.IsCustom(pileType)) return true;
        var pile = CustomPileRegistry.Get(pileType);
        if (pile is VoidPile vp && card?.Model != null)
        {
            __result = vp.GetTargetPosition(card.Model, size);
            return false;
        }
        return true;
    }
}
```

Patch 5 及完整注册见 [pile-patches-more.md](pile-patches-more.md)。

## 参见

- [pile-core.md](pile-core.md) — PileType 注入 + 基类 + 注册
- [pile-patches-more.md](pile-patches-more.md) — Patch 5 + ModEntry 注册
- [harmony-basics.md](../harmony/harmony-basics.md) — Prefix / Transpiler 语法
