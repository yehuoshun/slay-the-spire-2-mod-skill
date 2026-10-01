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
