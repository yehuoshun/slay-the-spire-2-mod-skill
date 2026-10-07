# 遗物机制模式：计数器追踪、临时力量、选择界面、图鉴子分类

> 实战验证（sts2mod 基石符文 KeystoneRunes v0.4.1，2026-10-07）。LOL 基石符文 ×15 的遗物实现模式。流程层见 [relic-keystone-flow.md](relic-keystone-flow.md)。

## ① 计数器追踪（连击/计数类遗物）

电刑（3 次连续命中同目标触发）的追踪骨架：

```csharp
private int _consecutiveHitsThisTurn;
private int _trackedTargetCombatId = -1;      // 追踪目标（换目标重置计数）
private CardModel? _currentTrackedCard;       // 追踪当前打出的牌
private bool _currentTrackedCardHadHit;       // 该牌是否造成过伤害

[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
public int SavedConsecutiveHitsThisTurn { get => _consecutiveHitsThisTurn;
    set { _consecutiveHitsThisTurn = Math.Max(0, value); RefreshVisualState(); } }

// BeforeCardPlayed：预登记（IsPotentialElectrocuteCard = 自己 + AnyEnemy 目标）
// AfterDamageGiven：dealer 是自己 + 敌人 + TotalDamage>0 + 非 Unpowered → 同目标++ / 异目标重置=1
// AfterCardPlayedLate：打出牌没造成伤害 → 清计数；IsLastInSeries → 清追踪
// AfterSideTurnStart(Player)：回合开始重置
```

- **战斗内瞬态字段也上 `[SavedProperty]`**（SaveIfNotTypeDefault）——中途读档不丢计数。
- `AfterCardPlayedLate` + `IsLastInSeries`：区分多段攻击内与牌间。
- `RefreshVisualState()`：`Status = RelicStatus.Active/Normal` + `InvokeDisplayAmountChanged()`——遗物点亮 + 计数显示。
- `ShowCounter`/`DisplayAmount`：`CombatManager.Instance?.IsInProgress == true && !IsCanonical` 才显示（战斗内 + 非原型）。

## ② 继承原版临时能力改来源（临时力量遗物）

征服者（每 2 张攻击 +1 临时力量，有上限）：

```csharp
public sealed class Keystone_ConquerorTemporaryStrengthPower : TemporaryStrengthPower   // 继承原版！
{
    public override AbstractModel OriginModel => ModelDb.Relic<Keystone_ConquerorRune>();  // 改来源显示
    protected override bool IsVisibleInternal => false;                                    // 隐藏（由遗物计数代替）
}
// 触发：Sts2Compat.ApplyPower<Keystone_ConquerorTemporaryStrengthPower>(...)
// 发放用 while 循环补差额（_strengthGrantedThisTurn < targetStrength），满上限后每次攻击回血
```

- **直接继承原版 `TemporaryStrengthPower` 覆写 `OriginModel`**——比自研装饰层省事（见 power-signature-and-temp.md 的对照）。
- `Flash(Array.Empty<Creature>())`：遗物闪光（空目标数组 = 自己）。

## ③ 跨版本兼容包装（Sts2Compat）

```csharp
#if STS2_104_OR_NEWER
// 新版 PowerCmd.Apply 带 BlockingPlayerChoiceContext 第一参
#else
// 旧版无 context；SetAmount 直接可用
#endif
// SetPowerAmount：新版用 Apply(差值) 代替 SetAmount；AddGeneratedCardToCombat 参数不同同样分支
```

- 条件编译符号在 csproj 定义，ModInfo 里 `TargetGameVersion` 也按符号取值。
- 签名适配基类同款思路：`#if STS2_106_OR_NEWER` 内 sealed override 原版签名转简化 virtual（版本差异大时用）。

## ④ 自定义选择界面（IOverlayScreen + TSCS）

```csharp
internal sealed class KeystoneRuneSelectionScreen : Control, IOverlayScreen, IScreenContext
{
    private readonly TaskCompletionSource<IEnumerable<RelicModel>> _completionSource = new();
    public NetScreenType ScreenType => NetScreenType.Rewards;
    // 构建：VBox/HBox + 分组列 + Button(Flat) + TextureRect 图标 + MegaLabel
    // 点击 → TrySetResult([relic])；跳过 → TrySetResult([]) + Close
    public async Task<IEnumerable<RelicModel>> RelicsSelected(bool closeOnSelection = true)
        { var r = await _completionSource.Task; if (closeOnSelection) CloseSelectionScreen(); return r; }
}
```

- 复用原版跳过按钮场景 + `NChoiceSelectionSkipButton` + `Released` 信号。
- hover：`NHoverTipSet.CreateAndShow(owner, relic.HoverTips, ...)` + `SetFollowOwner()`；移出 `Remove(owner)`。
- 显示前 `Task.Yield` 轮询等 `NOverlayStack.Instance` 就绪。

## ⑤ 图鉴子分类注入（RelicCollection）

```csharp
// NRelicCollectionCategory.LoadRelics(RelicRarity, NRelicCollection, LocString, HashSet, UnlockState, HashSet) postfix
// Starter 稀有度时：反射 CreateForSubcategory() + LoadSubcategory(collection, LocString, runes, seen, unlocked) 建子分类
// 表头文案：复制 Starter 表头模板，中英文关键词替换（"初始："→"基石："）
```

- 全部 `TryGetField/TryGetMethod`（缺成员 Warn + 跳过整组，可降级）；`MoveChild(subCategory, insertIndex)` 插到 Starter 表头后。

## 相关 API 速查

| API | 说明 |
|-----|------|
| `RelicModel.Status` / `RelicStatus.Active/Normal` | 遗物视觉状态（点亮/普通） |
| `Flash(IEnumerable<Creature>)` | 遗物闪光（可带目标） |
| `AfterCardPlayedLate` / `CardPlay.IsLastInSeries` | 多段攻击边界判断 |
| `PowerModel.GetTypeForAmount(amount)` | 按层数符号判 Buff/Debuff（敌人 debuff 判定用） |
| `CardModel.ToSerializable()` 对比 `SerializableCard` | 读档找回战斗内卡（IsSameSavedCard：Id+升级+附魔） |