# 自定义牌堆：Patch 5 与注册

## Patch 5：FindOnTable

`NCard.FindOnTable` 在战场上查找某张牌对应的 `NCard` 节点。对不可见牌堆返回 null，对可见堆返回牌堆自己管理的节点。

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

[HarmonyPatch(typeof(NCard), nameof(NCard.FindOnTable), [typeof(CardModel)])]
public static class FindOnTablePatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel card, ref NCard? __result)
    {
        var pile = card.Pile;
        if (pile != null && CustomPileRegistry.IsCustom(pile.Type))
        {
            if (pile is VoidPile vp)
                __result = vp.GetNCard(card);
        }
    }
}
```

## 完整注册

```csharp
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        var harmony = new Harmony("myskill");
        harmony.PatchAll(); // 注册所有 Patch + PileTypeInjector

        CustomPileRegistry.Register(MyPileTypes.VoidPile, () => new VoidPile());
    }
}
```

## 完整示例：虚空堆

战斗中将卡牌移入虚空堆：

```csharp
// 在一张牌的效果中
await CardPileCmd.Add(new[] { card },
    CardPile.Get(MyPileTypes.VoidPile, Owner),
    CardPilePosition.Bottom, this, false, false);
```

把虚空堆的牌拉回手牌：

```csharp
var voidPile = CardPile.Get(MyPileTypes.VoidPile, Owner);
if (voidPile != null && voidPile.Cards.Count > 0)
{
    var card = voidPile.Cards[0];
    await CardPileCmd.Add(new[] { card },
        CardPile.Get(PileType.Hand, Owner),
        CardPilePosition.Top, this, false, false);
}
```

## 参见

- [pile-inject.md](pile-inject.md) — PileType 枚举注入
- [pile-base.md](pile-base.md) — 继承 CardPile + 注册
- [pile-patches.md](pile-patches.md) — Patch 1-4 详解