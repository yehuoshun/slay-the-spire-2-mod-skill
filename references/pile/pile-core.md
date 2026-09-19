# 自定义牌堆：PileType 注入、基类与注册

## PileType 枚举注入

流程同 `RewardType`（见 [reward-core.md](../reward/reward-core.md)），用 `[CustomPileType]` Attribute + `ModelDb.Init` Prefix。

```csharp
using MegaCrit.Sts2.Core.Models;

[AttributeUsage(AttributeTargets.Field)]
public sealed class CustomPileTypeAttribute : Attribute { }

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public static class PileTypeInjector
{
    [HarmonyPrefix]
    public static void InjectPileTypes()
    {
        int baseValue = Enum.GetValues<PileType>().Max(v => Convert.ToInt32(v)) + 1;
        int offset = 0;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in asm.GetTypes())
            {
                foreach (var field in type.GetFields(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!field.IsDefined(typeof(CustomPileTypeAttribute))) continue;
                    if (field.FieldType != typeof(PileType)) continue;
                    field.SetValue(null, Enum.ToObject(typeof(PileType), baseValue + offset));
                    offset++;
                }
            }
        }
    }
}
```

### 定义自定义 PileType

```csharp
// 放在你的奖励类里
public static class MyPileTypes
{
    [CustomPileType]
    public static PileType VoidPile;  // 虚空堆

    [CustomPileType]
    public static PileType ReservePile; // 储备堆
}
```

> 每个自定义 PileType 配一个自定义牌堆类。**一个 PileType 对应一个类**。

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

### 核心方法说明

| 方法/属性 | 返回 | 说明 |
|----------|------|------|
| `CardShouldBeVisible` | `bool` | 卡牌是否在场上显示。true=可见（类似手牌），false=隐藏（类似弃牌堆） |
| `NeedsCustomTransitionVisual` | `bool` | 是否需要自定义入场动画。false=用默认移动动画 |
| `GetTargetPosition` | `Vector2` | 卡牌在堆中的渲染位置坐标 |
| `GetNCard` | `NCard?` | 获取该牌的 Ncard 节点（可见堆需要实现） |
| `CustomTween` | `bool` | 自定义动画。返回 true=已处理，false=走默认 |
| `IconPath` | `string?` | 多牌堆选择界面显示的图标 |
| `Name` | `LocString?` | 牌堆名称提示文本 |

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

- [pile-patches.md](pile-patches.md) — 5 个必要 Harmony Patch
- [reward-core.md](../reward/reward-core.md) — enum 注入详细说明