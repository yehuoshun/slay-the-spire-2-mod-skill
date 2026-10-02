# 自定义牌堆：继承 CardPile 与注册

> 继承 `CardPile` + 注册 Registry。PileType 注入见 [pile-inject.md](pile-inject.md)。

## 继承 CardPile（真实 API）

> ⚠️ 2026-10-02 全面测试修正：`CardShouldBeVisible`/`GetTargetPosition`/`GetNCard`/`CustomTween`/`NeedsCustomTransitionVisual`/`IconPath`/`Name` 是 **BaseLib `CustomPile` 的成员，原生 `CardPile` 不存在**。纯原生自定义牌堆 = 自定义 PileType + `new CardPile(type)` + 内容管理走 `CardPileCmd`；可见堆/自定义动画需自己 Patch 牌堆 UI 节点。

```csharp
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

public class VoidPile : CardPile
{
    public VoidPile() : base(MyPileTypes.VoidPile) { }

    // 可选：牌堆显示名（PileType 扩展，非 CardPile 成员）
    public LocString DisplayName =>
        new LocString("gameplay_ui", "MYMOD_VOID_PILE");
}
```

### 原生成员（真实存在，对照 CardPile.cs）

| 成员 | 说明 |
|------|------|
| `Type` | PileType（构造传入） |
| `Cards` | 堆内卡牌列表（IReadOnlyList<CardModel>） |
| `IsEmpty` / `UpgradableCardCount` | 便捷属性 |
| `ContentsChanged` / `CardAdded` / `CardRemoved` / `CardAddFinished` / `CardRemoveFinished` | 内容变更事件 |
| `AddInternal` / `RemoveInternal` / `MoveToTopInternal` / `MoveToBottomInternal` / `Clear` | 内部操作（战斗系统用） |

> 内容管理走 `CardPileCmd`（如 `CardPileCmd.MoveToPile` 等），不要直接调 Internal 方法。

## 注册（自定义 PileType → 牌堆实例工厂）

```csharp
using System;
using System.Collections.Generic;

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

> ⚠️ Registry 的实例创建 + 战斗注入需配合 [pile-patches.md](pile-patches.md) 的 Patch 使用；不注入则自定义牌堆只是"存在"，不会出现在战斗中。

## 参见

- [pile-inject.md](pile-inject.md) — PileType 枚举注入
- [pile-patches.md](pile-patches.md) — 5 个必要 Harmony Patch
- 可编译示例：`sts2-mod-examples/Sts2ModExamplesCode/Piles/`（ExampleVoidPile / TestReservePile）
