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

## UI 显示

资源在战斗界面显示。最简单的做法是 Patch `ExtraCombatUi`（战斗场景的扩展 UI 层）：

```csharp
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.OnCombatSetUp))]
public static class ResourceUiPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCombatRoom __instance)
    {
        var label = new Label
        {
            Text = "Mana: 0",
            Position = new Vector2(200, 600),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        __instance.AddChild(label);
        __instance.AddChild(new NRewardHighlight()); // 可选高亮效果
    }
}
```

每帧更新资源 UI：

```csharp
[HarmonyPatch]
public static class ResourceUiRefresh
{
    private static Label? _manaLabel;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Process))]
    private static void UpdateManaDisplay(NCombatRoom __instance, double delta)
    {
        var player = __instance.Player;
        if (player == null) return;

        var mana = CustomResourceManager.Get<ManaResource>(
            player.PlayerCombatState);
        if (mana != null && _manaLabel != null)
            _manaLabel.Text = $"Mana: {mana.Amount}/{mana.MaxAmount}";
    }
}
```

> 更好的方式：用 `SpireField<NCombatRoom, Control>` 缓存 UI 节点，避免每帧 Find。

## 注册

```csharp
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        CustomResourceManager.Register<ManaResource>("Mana");
        var harmony = new Harmony("myskill");
        harmony.PatchAll();
    }
}
```

## 参见

- [resource-core.md](resource-core.md) — 资源基类与生命周期