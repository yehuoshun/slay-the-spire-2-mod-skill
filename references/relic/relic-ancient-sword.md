# 遗物获得流程 + 涅奥固定选项 + 实时材质图标

> 实战验证（sts2mod 寰宇支配之剑 v0.3.0，2026-10-07）。三件套：遗物获得时发卡、给涅奥加固定第 4 选项、遗物图标用运行时 Shader 材质。补丁框架见 [harmony-attribute-patcher.md](../harmony/harmony-attribute-patcher.md)。

## ① 遗物获得时发卡（AfterObtained）

```csharp
public sealed class UniversalDominionSwordRelic : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<UniversalDominionSwordCard>();  // hover 显示关联卡

    public override async Task AfterObtained()
    {
        if (Owner == null) return;
        CardModel card = Owner.RunState.CreateCard(ModelDb.Card<UniversalDominionSwordCard>(), Owner);
        CardPileAddResult result = await CardPileCmd.Add(card, PileType.Deck, CardPilePosition.Bottom);
        if (result.success)
        {
            SaveManager.Instance.MarkCardAsSeen(result.cardAdded);   // 图鉴已见
            Flash();
            CardCmd.PreviewCardPileAdd([result], 2f);                // 加牌展示动画
        }
    }
}
```

- 关联卡注册进 `TokenCardPool`（`ModHelper.AddModelToPool<TokenCardPool, ...>`），遗物进 `EventRelicPool`。
- `CardPileAddResult.success/cardAdded`：加牌结果字段。

## ② 涅奥固定第 4 选项（两处 postfix）

```csharp
[SwordPatch("neow.initial-options", "涅奥第四个先古遗物选项")]
[HarmonyPatch(typeof(Neow), "GenerateInitialOptions")]
internal static class NeowInitialOptionsPatch
{
    [HarmonyPostfix]
    private static void Postfix(Neow __instance, ref IReadOnlyList<EventOption> __result)
    {
        // 修正器局（挑战/自定义）保持原版三选项；已包含则跳过；追加选项
        List<EventOption> options = [.. __result, option];
        __result = options;
    }
}

[SwordPatch("neow.all-possible-options", "涅奥第 4 选项(图鉴/预览列表)")]
[HarmonyPatch(typeof(Neow), nameof(Neow.AllPossibleOptions), MethodType.Getter)]
internal static class NeowAllPossibleOptionsPatch { /* 同样追加 */ }
```

- `GenerateInitialOptions()` 是 **protected override**（Harmony 字符串名可 patch）；`AllPossibleOptions` 是 getter。
- **复用原版选项工厂**：`AncientEventModel.RelicOption(RelicModel, pageName="INITIAL", customDonePage)` protected 方法——反射调用，选项与原版三选完全同构。
- 前提：`__instance.Owner == null || Owner.RunState.Modifiers.Count > 0` 时跳过（修正器局选项由原版决定）。

## ③ 实时材质图标（Shader 代码 + 参数纹理）

```csharp
// shader 代码是 C# raw string 常量（AvaritiaCosmicShader.Code）
_shader ??= new Shader { Code = AvaritiaCosmicShader.Code };
ShaderMaterial material = new() { Shader = _shader };
material.SetShaderParameter("layer_0", Load(Layer0Path));   // 帧层纹理
material.SetShaderParameter("blade_mask", Load(MaskPath));
for (int i = 0; i < 28; i++) material.SetShaderParameter($"cosmic_{i}", Load(...));  // 星场帧
rect.Material = material;
```

- **只挂纯表现节点**（不动任何取图 getter）：`NRelic.Reload`（遗物节点）、`NInspectRelicScreen.UpdateRelicDisplay`（检视大图）、`NEventOptionButton._Ready`（涅奥选项按钮）——每处 `[HarmonyPrepare]` 检查反射句柄，`Optional = true` 降级。
- 检视翻页复用同一 TextureRect：`Dictionary<ulong, Material?>` 记原材质，翻走还原（`TextureFilter` 一并还原）。
- 静态贴图仍走 `PackedIconPath`/`BigIconPath`（材质是叠加层，不冲突）。

## ④ 永久耗能卡（SavedProperty + SetCustomBaseCost）

```csharp
[SavedProperty]
public int PermanentCostIncrease
{
    get => _permanentCostIncrease;
    set { _permanentCostIncrease = Math.Clamp(value, 0, Max); EnergyCost.SetCustomBaseCost(Math.Min(EnergyCost.Canonical + _permanentCostIncrease, Max)); }
}
// 打出时：IncreasePermanentCost() + 同步 DeckVersion（牌组原型）
// ⚠️ SavedProperty 属性名决定联机 net-id 布局：改名/增删须随版本号发布
```

- `CardEnergyCost.SetCustomBaseCost(int)` public（真实 API）；`Canonical` 是基础耗能。

## 相关 API 速查

| API | 说明 |
|-----|------|
| `RunState.CreateCard(CardModel, Player)` | 生成卡实例 |
| `CardPileCmd.Add(CardModel, PileType, CardPilePosition.Bottom)` | 加牌（返回 CardPileAddResult） |
| `CardCmd.PreviewCardPileAdd(IReadOnlyList<CardPileAddResult>, float)` | 加牌展示动画 |
| `HoverTipFactory.FromCardWithCardHoverTips<TCard>()` | 遗物 hover 显示关联卡 |
| `CreatureCmd.Damage(choiceContext, IEnumerable<Creature>, decimal, ValueProp, Creature)` | 批量伤害 |
| `PowerCmd.Remove(PowerModel)` | 剥能力 |