# 自定义卡牌：悬停提示与原版容器注入

> 实战验证（STS2_MarisaMod 2026-10-05）。

## 悬停提示（ExtraHoverTips）

> 卡牌/遗物/药水可用 `ExtraHoverTips` 追加悬停提示，`HoverTipFactory`（sts2-res 已验证）提供工厂：

```csharp
protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    base.ExtraHoverTips
        .Concat(HoverTipFactory.FromEnchantment<StarlitEnchantment>())  // 附魔说明（返回 IEnumerable）
        .Concat(HoverTipFactory.FromRelic<BigShroomBag>())              // 遗物说明（返回 IEnumerable）
        .Append(HoverTipFactory.FromCard<Spark>())                      // 关联卡牌（返回 IHoverTip）
        .Append(HoverTipFactory.FromKeyword(CardKeyword.Innate))        // 关键词（枚举：Exhaust/Ethereal/Innate/Unplayable/Retain/Sly/Eternal）
        .Append(HoverTipFactory.FromPower<StarlitPower>());             // 能力
```

> 集合表达式写法（C# 12，实测 STS2_MarisaMod v0.4.2）：直接 `=> [HoverTipFactory.FromPower<HoukaiPower>()]`，多个逗号分隔。

> ⚠️ `FromCard<T>()` 引用**自定义卡**时，被引用卡需可见（同命名空间或 `using`）：MarisaMod 的 FourOfASpark 引用自己的 Token 卡 TabooSpark（`marisamod.Scripts.Cards.Colorless` 命名空间），靠 `using marisamod.Scripts.Cards.Colorless;` 解析。配套做法：被引用卡声明 `CanonicalKeywords`（如 TabooSpark 声明 `[CardKeyword.Exhaust]`），提示里才会带上关键词条。

## 往原版容器注入（TrashHeap）

> 垃圾堆容器 `TrashHeap` 的 `Relics`/`Cards` Getter Postfix 拼接自定义内容：

```csharp
[HarmonyPatch(typeof(TrashHeap), "Relics", MethodType.Getter)]
static class TrashHeapRelicPatch
{
    static void Postfix(ref RelicModel[] __result)
        => __result = __result.Concat([ModelDb.Relic<MyRelic>()]).ToArray();
}
```

---
