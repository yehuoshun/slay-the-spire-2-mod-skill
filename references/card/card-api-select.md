# 自定义卡牌：选择器（CardSelectorPrefs）

> 从 card-api.md 拆出。手牌选择/升级/丢弃/消耗的统一入口。

## CardSelectorPrefs 构造函数

```csharp
// 方式1：选择固定数量
new CardSelectorPrefs("选择一张牌", selectCount: 1);

// 方式2：选择数量范围
new CardSelectorPrefs("选择卡牌", minCount: 1, maxCount: 3);
```

## 选择过滤器

```csharp
// 过滤器：接受 CardModel 参数，返回 bool
card => card is ExampleStrike  // 只选择某类卡牌
card => card.CanDiscard()      // 只选可丢弃的
```

## 选择后操作（完整片段）

```csharp
// 选择手牌消耗
var selected = await CardSelectCmd.FromHand(choiceContext, Owner,
    new CardSelectorPrefs("选择一张牌消耗", selectCount: 1),
    card => card != this, this);
foreach (var card in selected) await CardCmd.Exhaust(choiceContext, card);

// 选择手牌升级（真实签名：FromHandForUpgrade(context, player, source)，无 filter 参数）
var card = await CardSelectCmd.FromHandForUpgrade(choiceContext, Owner, this);
if (card != null) await CardCmd.Upgrade(card);
```

## 相关命令

```csharp
// 从手牌选择
CardSelectCmd.FromHand(context, player, prefs, filter, source);

// 从手牌选牌升级（返回 CardModel，需手动调用 UpgradeInternal()）
CardSelectCmd.FromHandForUpgrade(context, player, filter, source);

// 从手牌选牌丢弃（需手动调用 CardCmd.Discard(card)）
CardSelectCmd.FromHandForDiscard(context, player, filter, source);
```

## 任意数量选择（0-N）+ 多玩家遍历

> 实战验证（STS2_MarisaMod 2026-10-14，HeatDeathOfUniverse）。`minCount: 0, maxCount: 99` = 任意数量（可一张不选）；`FromHand` 的 `player` 参数可传**其他玩家**——多玩家模式下每个队友独立选牌。

```csharp
// 每个存活玩家队友从手牌选任意张烧掉，烧几张叠几层（示例为蓄力）
var playersAvail = CombatState!.GetTeammatesOf(Owner.Creature)
    .Where(c => c is { IsAlive: true, IsPlayer: true })
    .Select(c => c.Player!).Distinct().ToList();

foreach (var player in playersAvail)
{
    foreach (var item in await CardSelectCmd.FromHand(choiceContext, player,
        new CardSelectorPrefs(SelectionScreenPrompt, 0, 99), null, this))
    {
        await CardCmd.Exhaust(choiceContext, item);
        count += 1;
    }
}

await PowerCmd.Apply<ChargeUpPower>(choiceContext, Owner.Creature, DynamicVars["Power"].BaseValue, Owner.Creature, this);
```

- **0-99 任意数量**：`new CardSelectorPrefs(提示, 0, 99)` —— 最小 0 = 允许一张不选直接确认；最大 99 约等于不限
- **filter 传 null**：不过滤，任何手牌可选
- **多玩家遍历**：`GetTeammatesOf` + `IsPlayer` 过滤 + `Distinct()` 去重（同一玩家多角色时防重复弹窗）
- 注意：每玩家选择结果独立处理（各自 Exhaust），跨玩家不串扰


### 按牌堆原顺序选牌（别用 `FromCombatPile` 的默认排序）

原生 `CardSelectCmd.FromCombatPile` 会把抽牌堆**按稀有度排序**再显示，并且只取一次快照。于是「看牌堆顶部 N 张」「别人塞牌后界面要跟着刷新」这两类需求都会错。

BaseLib v3.4.8 的做法值得抄结构：`BaseLibCardSelectCmd.FromCombatPileInOrder(context, pile, player, prefs, filter)` 保留牌堆原顺序，内部流程是

1. `CombatManager.Instance.IsEnding` 为真直接返回空（别在该结束的战斗里弹选择）；
2. 校验是真·战斗牌堆，否则 `InvalidOperationException`；
3. 玩家已 ready 且轮到我方时先撤销结束回合（`UndoEndTurnReadyForCardSelect`）；
4. 卡数 0 → 空；非强制确认且已满足最小选数 → 直接全给，不弹界面；
5. 没有现成 selector 时申请 `choiceId` 并广播「选择开始」；
6. 本地玩家才真正 `NCombatPileCardSelectScreen.Create(...)` + `NOverlayStack.Push`；
7. 最后 `SyncLocalChoice(player, choiceId, PlayerChoiceResult.FromMutableCombatCards(result))`。

要点：`prefs.Comparison` 必须留空才保序；多人同步复用原生 `PlayerChoiceSynchronizer`，**不要自己发网络消息**；只有当前没有 selector 时才申请 `choiceId`，否则会和别的选择流程抢。
