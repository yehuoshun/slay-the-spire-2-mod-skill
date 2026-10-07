# 手牌 UI 布局覆写

> 实战验证（sts2mod 手牌上限解除 RemoveHandLimit v0.3.1，2026-10-07）。手牌上限 10 → 20，超过 10 张双排显示。核心技术：原版布局表「偷换」+ 节点分层/hitbox 修正 + 键盘快捷路径接管。

## ① 手牌上限（getter prefix + transpiler）

```csharp
// MaxCardsInHand 是 CardPile 静态 getter（原版 => 10）
private static bool MaxCardsInHandPrefix(ref int __result) { __result = 20; return false; }
// 但 Add/Draw/CheckIfDrawIsPossible 内部硬编码调用该 getter → async transpiler 替换为常量 20
// （见 harmony-transpiler-async.md：GetAsyncStateMachineTarget + Ldc_I4 + expectedCount 断言）
```

## ② 原版布局表偷换（HandPosHelper prefix）

```csharp
// 启动时反射读原版静态布局表（扇形坐标/角度/基础缩放）并缓存
private static readonly Vector2[][] VanillaCardPositionData = GetStaticFieldValue<Vector2[][]>(
    RequireField(typeof(HandPosHelper), "_cardPositionData", BindingFlags.Static | BindingFlags.NonPublic));
// 同样缓存 _cardAngleData、_baseScale

private static bool GetPositionPrefix(int handSize, int cardIndex, ref Vector2 __result)
{
    if (handSize <= 10) return true;               // 10 张内走原版
    if (cardIndex < 10)                             // 下排：原版 10 张布局 + Y 偏移
    {
        Vector2 p = GetVanillaPosition(10, cardIndex);
        __result = new Vector2(p.X, p.Y + 28f);
    }
    else                                            // 上排：原版 (handSize-10) 张布局 + Y 偏移 -72
    {
        Vector2 p = GetVanillaPosition(handSize - 10, cardIndex - 10);
        __result = new Vector2(p.X, p.Y - 72f);
    }
    return false;                                  // 短路原版
}
// GetAnglePrefix：上排角度 = 原版角度 × 0.82（收窄）；GetScalePrefix：固定用 10 张缩放
```

- **复用原版扇形表**（坐标/角度/缩放按手牌数查表）只做行偏移——双排视觉与原版一致，不用手写曲线。
- `GetVanillaScale`：8~12 张有额外缩放系数（0.95~0.75）查表乘系数。

## ③ 分层与 hitbox 修正（RefreshLayout postfix）

```csharp
private static void RefreshLayoutPostfix(NPlayerHand __instance)
{
    UpdateHolderLayering(__instance);   // ZIndex：下排 0-9、上排 10-19（rowBase = i<10 ? 10 : 0）
    UpdateHolderHitboxes(__instance);   // 上排 hitbox 高度压缩 ×0.56（防遮挡点击）
}
// 注意先关 YSort：hand.CardHolderContainer.YSortEnabled = false（否则 ZIndex 被覆盖）
```

- 上排卡片 hover 时也要重排（`NHandCardHolder.DoCardHoverEffects` postfix → 调 UpdateHolderLayering）。
- hitbox 原值按 `GetInstanceId()` 字典缓存（节点复用，首次记录后恢复/压缩用）。

## ④ 键盘快捷路径接管（StartCardPlay prefix）

```csharp
// 原版 _selectCardShortcuts 只有 10 个快捷槽；第 11+ 张卡无快捷 → 手动接管完整拖拽流程
private static bool StartCardPlayPrefix(NPlayerHand __instance, NHandCardHolder holder, bool startedViaShortcut)
{
    if (使用手柄 || holderIndex < shortcuts.Length) return true;   // 原版路径够用
    // 手动重放原版逻辑：
    SetDraggedHolderIndex(holderIndex);
    holdersAwaitingQueue.Add(holder);
    holder.Reparent(hand); holder.BeginDrag();
    NCardPlay cardPlay = NMouseCardPlay.Create(holder, MegaInput.releaseCard, startedViaShortcut);
    cardPlay.Connect(NCardPlay.SignalName.Finished, Callable.From<bool>(success => { /* 清理+回手+刷新 */ }));
    cardPlay.Start();
    return false;   // 短路原版
}
```

- 场景：原版快捷选择逻辑以 `_selectCardShortcuts.Length` 为界，超出即数组越界——prefix 全部接管。
- 手柄玩家不接管（手柄导航另有路径）：`NControllerManager.Instance?.IsUsingController ?? false` 时放行。

## ⑤ 焦点管理（超限时接管 OnHolderFocused/Unfocused）

```csharp
// 超过 10 张时原版焦点逻辑假定单排 → prefix 短路，自己记录 lastFocusedIndex + hover 追踪
private static bool OnHolderFocusedPrefix(NPlayerHand __instance, NHandCardHolder holder)
{
    if (ActiveHolders.Count <= 10) return true;
    SetLastFocusedHolderIndex(__instance, holder.GetIndex());
    RunManager.Instance.HoveredModelTracker.OnLocalCardHovered(holder.CardModel);
    UpdateHolderHitboxes(__instance);
    return false;
}
```

- 配套：RefreshLayout prefix 在 >10 张时先把 `FocusedHolder` 置 null（避免原版恢复焦点到越界索引）。

## 相关 API 速查

| API | 说明 |
|-----|------|
| `CardPile.MaxCardsInHand` | 手牌上限静态 getter（原版 10） |
| `HandPosHelper.GetPosition/GetAngle/GetScale` | 扇形布局表查询（public static） |
| `NPlayerHand.RefreshLayout/OnHolderFocused/StartCardPlay` | 私有布局/焦点/出牌方法 |
| `NHandCardHolder.BeginDrag/SetIndexLabel/Hitbox` | 卡片节点操作 |
| `NMouseCardPlay.Create(holder, input, bool)` | 鼠标出牌流程 |