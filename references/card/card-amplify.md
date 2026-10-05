# 自定义卡牌：增幅（Amplify/Kicker）系统

> 实战验证（STS2_MarisaMod 2026-10-05）。增幅 = 多付额外费用换更强效果（如 Master Spark：1 费打 8，增幅共 2 费打 15）。纯原生：基类 + 3 个 Harmony Patch，零 BaseLib 依赖。

## 1. 基类设计（费用结算 + 状态）
```csharp
public abstract class AmplifiedCard(int baseCost, int kickerCost, CardType type, CardRarity rarity, TargetType target)
    : CardModel(baseCost, type, rarity, target)
{
    public int KickerCost { get; } = kickerCost;          // 增幅额外费用
    public bool AmplifiedInPlay { get; protected set; }   // 本次打出是否增幅（OnPlay 时定稿）

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat([new EnergyVar(KickerCost)]);  // 费用标签显示

    // 费用结算：增幅则把基础费 + KickerCost（被 Entry 的 Patch 调用）
    public bool CalculateAmplifiedCost(ref int cost)
    {
        if (Owner.Creature.HasPower<OneTimeOffPower>()) return false;      // 门控 1：禁用增幅
        var costWithAmp = cost + KickerCost;
        if (Owner.PlayerCombatState?.Energy < costWithAmp) return false;   // 门控 2：能量不足
        cost = costWithAmp;
        return true;
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        AmplifiedInPlay = cardPlay.IsAutoPlay || /* 由费用 Patch 置位的标记 */;
        // 效果里用 AmplifiedInPlay 分支选伤害/数值
    }
}
```

> ⚠️ 关键：**不改 `EnergyCost` 字段**，只改 `GetAmountToSpend` 的返回值。`EnergyCost.AddThisCombat` 改的是模型状态，多人/重进战斗会残留；返回值的方案打完即清零（`AfterCardPlayed` 里重置 `PaidAmplifiedCost = false`）。

## 2. 三个必需 Patch（费用 → 标签 → 高亮）

```csharp
// ① 费用计算：卡牌打出时按增幅价扣费（____card = CardEnergyCost 私有字段注入）
[HarmonyPatch(typeof(CardEnergyCost), "GetAmountToSpend")]
static class CostPatch
{
    static void Postfix(CardEnergyCost __instance, CardModel ____card, ref int __result)
    {
        if (____card is not AmplifiedCard amp) return;
        amp.CalculateAmplifiedCost(ref __result);
    }
}

// ② 手牌费用标签：悬停预览时显示增幅后的总费用（含变色提示；____energyLabel = 私有字段注入）
[HarmonyPatch(typeof(NCard), "UpdateEnergyCostVisuals")]
static class CostVisualPatch
{
    static void Postfix(NCard __instance, PileType pileType, MegaLabel ____energyLabel)
    {
        if (pileType != PileType.Hand) return;
        if (__instance.Model is AmplifiedCard amp && 悬停中(amp))
        {
            var cost = amp.EnergyCost.GetWithModifiers(CostModifiers.All) + amp.KickerCost;
            ____energyLabel.SetTextAutoSize(cost.ToString());
        }
    }
}

// ③ 可打高亮：增幅态用专属辉光色（如 0x5244ff）
[HarmonyPatch(typeof(NHandCardHolder), "UpdateCard")]
static class HighlightPatch
{
    static void Postfix(NHandCardHolder __instance)
    {
        if (__instance.CardNode?.Model is not AmplifiedCard amp) return;
        if (amp.CanPlay() != true) return;
        __instance.CardNode.CardHighlight.Modulate =
            amp.AmplifiedInPreview ? AmplifiedCard.AmplifiedGlowColor : NCardHighlight.playableColor;
    }
}
```

`AmplifiedInPreview` 是**只读判定属性**（不进战斗状态）：在手牌 + 能量 ≥ 基础费+Kicker → true；抽牌堆/弃牌堆/消耗堆 → false；被免费增幅 Power 覆盖（MillisecondPulsarsPower/PulseMagicPower → 强制 true，OneTimeOffPower → 强制 false）。

## 3. 悬停刷新（切换悬停时重算费用显示）

```csharp
[HarmonyPatch(typeof(RunManager), "InitializeShared")]
static class HoverRefreshPatch
{
    static void Postfix(RunManager __instance)
    {
        __instance.HoveredModelTracker.HoverChanged += OnHoverChanged;
    }
}
// OnHoverChanged 里：旧悬停卡是 AmplifiedCard → NCard.FindOnTable(卡)?.UpdateVisuals(PileType.Hand, None)；
// 新悬停卡同样刷新。事件签名 Action<ulong>（玩家 NetId）。
```

> `HoveredModelTracker` 在 `MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput`（已用 sts2-res 验证）。

## 4. 超脱卡（Transcendence）

BaseLib 有 `ITranscendenceCard` 接口（`GetTranscendenceTransformedCard()`，配合原版遗物 ArchaicTooth 把基础卡进化）。**纯原生无此接口**，等价做法 = Patch `ArchaicTooth.GetTranscendenceStarterCard` / `GetTranscendenceTransformedCard`（Prefix，返回 null/自定义卡时 return false）。

## 5. 常见坑

| 坑 | 解法 |
|----|------|
| AutoPlay 不经过费用计算 | `OnPlay` 里 `cardPlay.IsAutoPlay` 强制 `AmplifiedInPlay = true` |
| 打出一张后再打下一张，增幅状态残留 | `AfterCardPlayed` 里 `cardPlay.Card == this && cardPlay.IsLastInSeries` 时重置 |
| 增幅费用显示没刷新 | 三个 Patch 缺一不可：费用计算 / 费用标签 / 高亮 |
| `EnergyVar` 只显示一次 | 增幅态总费用手写拼进标签（基础费 + Kicker） |
