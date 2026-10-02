# 自定义牌堆：继承 CardPile 与注册

> 继承 `CardPile` + 注册 Registry。PileType 注入见 [pile-inject.md](pile-inject.md)。

## 继承 CardPile

纯原生直接继承 `CardPile`，重写关键行为：

```csharp
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

public class VoidPile : CardPile
{
    public VoidPile() : base(MyPileTypes.VoidPile) { }

    /// <summary>卡牌是否在场上可见（类似手牌）</summary>
    public bool CardShouldBeVisible(CardModel card) => false;

    /// <summary>自定义过渡动画</summary>
    public bool NeedsCustomTransitionVisual => false;

    /// <summary>卡牌在牌堆中的位置</summary>
    public Vector2 GetTargetPosition(CardModel model, Vector2 size)
    {
        return new Vector2(0, -500); // 屏幕外上方
    }

    /// <summary>获取卡牌的 Ncard 节点</summary>
    public NCard? GetNCard(CardModel card) => null;

    /// <summary>自定义移动动画</summary>
    public bool CustomTween(Tween tween, CardModel card,
        NCard cardNode, CardPile? oldPile) => false;

    /// <summary>牌堆图标路径</summary>
    public string? IconPath =>
        "res://myskill/images/piles/void_pile.png";

    /// <summary>牌堆名称</summary>
    public LocString? Name =>
        new LocString("gameplay_ui", "MYSKILL-VOID_PILE");
}
```

### 原生成员（真实存在）

| 成员 | 说明 |
|------|------|
| `Type` | PileType（构造传入） |
| `Cards` | 堆内卡牌列表 |
| `IsEmpty` / `UpgradableCardCount` | 便捷属性 |
| `ContentsChanged` / `CardAdded` / `CardRemoved` 事件 | 内容变更监听 |

> ⚠️ **`CardShouldBeVisible`/`GetTargetPosition`/`GetNCard`/`CustomTween`/`NeedsCustomTransitionVisual`/`IconPath`/`Name` 是 BaseLib `CustomPile` 的成员，原生 `CardPile` 不存在**（旧版编造，已删）。纯原生自定义牌堆 = 自定义 PileType + `new CardPile(type)` + 内容管理走 `CardPileCmd`；可见堆/自定义动画需自己 Patch 牌堆 UI 节点。

## 注册

```csharp
using System;

public static class CustomPileRegistry
{
    private static readonly Dictionary<PileType, Func<CardPile>> _providers = new();

    public static void Register(PileType type, Func<CardPile> constructor)
    {
        _providers[type] = constructor;
    }

    public static CardPile? Get(PileType type) =>
        _providers.TryGetValue(type, out var ctor) ? ctor() : null;

    public static bool IsCustom(PileType type) => _providers.ContainsKey(type);

    // 战斗开始时创建所有自定义牌堆实例
    public static CardPile[] CreateAll()
    {
        var list = new List<CardPile>(_providers.Count);
        foreach (var ctor in _providers.Values)
            list.Add(ctor());
        return list.ToArray();
    }
}

// 注册（ModEntry.Initialize 中）
CustomPileRegistry.Register(MyPileTypes.VoidPile, () => new VoidPile());
```

## 参见

- [pile-inject.md](pile-inject.md) — PileType 枚举注入
- [pile-patches.md](pile-patches.md) — 5 个必要 Harmony Patch