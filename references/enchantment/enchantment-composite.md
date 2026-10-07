# 复合附魔容器（EnchantmentModel 装多个附魔）

> 实战验证（sts2mod 可重复附魔 RepeatableEnchantments，2026-10-07）。
> 原版一张卡只能挂一个附魔（`CardModel.Enchantment` 单值）。本模式用一个 `EnchantmentModel` 子类当**容器**，内部持有多个附魔并转发全部行为。配合 CardCmd.Enchant 拦截（见 [enchantment-multi.md](enchantment-multi.md)）实现「多附魔 + 同类叠层」。

## 容器模型骨架

```csharp
public sealed class RepeatableCompositeEnchantment : EnchantmentModel
{
    private List<EnchantmentModel> _innerEnchantments = new();
    private List<EnchantmentModel> _subscribedInnerEnchantments = new(); // 已订阅 StatusChanged 的

    // 序列化：整个内部列表存成 JSON 字符串（[SavedProperty] 只能存基础类型）
    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    private string? SavedEnchantmentsJson
    {
        get => _innerEnchantments.Count == 0 ? null
            : JsonSerializer.Serialize(_innerEnchantments.Select(e => e.ToSerializable()).ToArray());
        set { /* 反序列化：Unsubscribe → FromSerializable 逐个还原 → 刷新状态 */ }
    }

    public override bool CanEnchant(CardModel card) => false;   // 容器本身不可直接附魔
}
```

- `ToSerializable()` / `EnchantmentModel.FromSerializable(SerializableEnchantment)`：附魔 ⇄ 可存档形态（原版提供）。
- 容器 `Amount` = 内部附魔数量；`DisplayAmount`/`ShowAmount` 单附魔时透传 lead 附魔、多附魔时显示数量。

## 回调转发（全部转发给内部附魔）

```csharp
public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
{
    foreach (var e in _innerEnchantments) { await e.OnPlay(choiceContext, cardPlay); e.InvokeExecutionFinished(); }
}
// AfterCardPlayed / AfterCardDrawn / AfterPlayerTurnStart / BeforeFlush / ModifyShuffleOrder 同理逐个转发
```

- `InvokeExecutionFinished()` 在 **AbstractModel**（真实 API），OnPlay 转发后调用（多人流程结算钩子）。
- 签名对齐 AbstractModel：`AfterCardPlayed(choiceContext, CardPlay)`、`AfterCardDrawn(choiceContext, CardModel, bool)`、`AfterPlayerTurnStart(choiceContext, Player)`、`BeforeFlush(choiceContext, Player)`、`ModifyShuffleOrder(Player, List<CardModel>, bool)`。

## 数值链式计算（加→乘顺序）

```csharp
public override decimal EnchantBlockAdditive(decimal originalBlock)
    => CalculateFinalBlock(originalBlock) - originalBlock;   // 对外返回差值
public override decimal EnchantBlockMultiplicative(decimal originalBlock) => 1m;

private decimal CalculateFinalBlock(decimal originalBlock)
{
    decimal current = originalBlock;
    foreach (var e in _innerEnchantments)
    {
        current += e.EnchantBlockAdditive(current);          // 先加
        current *= e.EnchantBlockMultiplicative(current);    // 后乘
    }
    return current;
}
// 伤害同理：EnchantDamageAdditive(current, props) → Multiplicative(current, props)
// EnchantPlayCount：current = e.EnchantPlayCount(current) 逐层嵌套
```

## 内部附魔绑定管理

```csharp
private void EnsureInnerBindings()   // 每次读内部列表前调用
{
    if (!HasCard) return;
    foreach (var e in _innerEnchantments)
    {
        if (!e.HasCard || !ReferenceEquals(e.Card, Card)) { e.ClearInternal(); e.ApplyInternal(Card, e.Amount); }
        SubscribeToInnerEnchantment(e);   // StatusChanged 事件幂等订阅
    }
}
```

- `ApplyInternal(CardModel, decimal)` / `ClearInternal()`：附魔内部绑定 API（`AssertMutable` 前置）。
- `StatusChanged`（`event Action?`，原版提供）：内部状态变化 → `RefreshCompositeStatus()`（任一 Normal → Normal，否则 Disabled）。
- 克隆：`DeepCloneFields()` 逐个 `ClonePreservingMutability()` 重造列表 + 清空订阅（防共享可变状态）。

## 特殊附魔特判（Goopy / Imbued）

```csharp
// Goopy：每次打出 +1 层，且要同步牌组原型 DeckVersion 的层数
//   goopy.Amount++; 同步 card.DeckVersion?.Enchantment（复合/直挂两种情况）
// Imbued：第一回合回合开始自动打出
if (enchantment is Imbued && player == compositeCard.Owner && combatState?.RoundNumber == 1)
    await CardCmd.AutoPlay(choiceContext, compositeCard, null);
```

- `Card.DeckVersion?.Enchantment`：牌组原型版本，Goopy 叠层双向同步。
- `CardCmd.AutoPlay(choiceContext, card, Creature?)`：原版自动打出入口。

## 卡面文本

`GetVisibleExtraCardTextLines()`：内部附魔 `DynamicDescription.GetFormattedText()`，`HashSet` 去重，非空行包 `[purple]...[/purple]` 追加到卡面（配合 `GetDescriptionForPile` postfix，见 enchantment-multi.md）。