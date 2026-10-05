# 自定义卡牌：悬停提示与原版容器注入

> 实战验证（STS2_MarisaMod 2026-10-05）。

## 悬停提示（ExtraHoverTips）

> 卡牌/遗物/药水可用 `ExtraHoverTips` 追加悬停提示，`HoverTipFactory`（sts2-res 已验证）提供工厂：

```csharp
protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    base.ExtraHoverTips.Concat([
        HoverTipFactory.FromEnchantment<StarlitEnchantment>(),  // 附魔说明
        HoverTipFactory.FromRelic<BigShroomBag>(),              // 遗物说明
        HoverTipFactory.FromCard<Spark>(),                      // 关联卡牌
        HoverTipFactory.FromKeyword(CardKeyword.Burn),          // 关键词
        HoverTipFactory.FromPower<StarlitPower>()               // 能力
    ]);
```

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
