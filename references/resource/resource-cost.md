# 自定义资源：卡牌费用与 UI

## 资源费用接口

让卡牌消耗自定义资源：在卡牌上标记费用，打牌时检查并扣取。

### 定义接口

```csharp
using MegaCrit.Sts2.Core.Entities.Cards;

/// <summary>卡牌上的资源费用标记</summary>
public interface ICustomResourceCost
{
    /// <summary>资源类型 ID</summary>
    string ResourceId { get; }

    /// <summary>费用数量</summary>
    int Amount { get; }

    /// <summary>是否可变费用（如 X 费）</summary>
    bool CostsX { get; }
}
```

### 卡牌上设置资源费用

```csharp
public class ManaCard : CardModel
{
    public ManaCard() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        // 设置 CanonicalCost 为自定义资源费用
        CustomResources<ManaResource>.SetCanonicalCost(this, 3);
    }
}
```

需要在卡牌上存储和读取自定义费用信息：

```csharp
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

/// <summary>通过 SpireField 在 CardModel 上附加资源费用数据</summary>
public static class CustomResources<T> where T : CustomResource, new()
{
    private static readonly SpireField<CardModel, int> _canonicalCosts = new(() => 0);
    private static readonly SpireField<CardModel, bool> _costsX = new(() => false);

    public static void SetCanonicalCost(CardModel card, int cost)
        => _canonicalCosts[card] = cost;

    public static void SetXCost(CardModel card)
        => _costsX[card] = true;

    public static int CanonicalCost(CardModel card)
        => _canonicalCosts[card];

    public static T Get(PlayerCombatState pcs)
        => CustomResourceManager.Get<T>(pcs);
}
```

### 打牌时检查并扣费

```csharp
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.PlayCard))]
public static class ResourceCostCheckPatch
{
    [HarmonyPrefix]
    private static bool Prefix(CardPlayContext context, PlayerChoiceContext ctx)
    {
        var card = context.Play.Card;
        var cost = CustomResources<ManaResource>.CanonicalCost(card);
        if (cost <= 0) return true; // 不走自定义费用

        var pcs = context.Player.PlayerCombatState;
        var mana = CustomResourceManager.Get<ManaResource>(pcs);
        if (mana.Amount < cost)
        {
            // 费用不足，不能打出
            return false;
        }
        mana.Spend(cost);
        return true; // 继续原生打出流程
    }
}
```

> 注意：实际实现需要处理 `PlayCard` 的 async 状态机。如果 Prefix 不够用，改用 Transpiler/Postfix。`SpireField` 是原生提供的 `ConditionalWeakTable` 包装，可直接使用。

