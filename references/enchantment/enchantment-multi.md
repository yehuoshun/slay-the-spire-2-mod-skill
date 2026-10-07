# 多附魔机制：CardCmd.Enchant 全接管 + UI

> 实战验证（sts2mod 可重复附魔 RepeatableEnchantments，2026-10-07）。容器模型见 [enchantment-composite.md](enchantment-composite.md)。本文件是「拦截层」：让原版附魔流程支持多附魔/叠层，并让 UI 显示多个附魔图标。

## 核心：接管 CardCmd.Enchant 入口

原版 `CardCmd.Enchant(EnchantmentModel, CardModel, decimal)`（static）是附魔唯一入口，prefix 完全接管（`AssertMutable` → 规则校验 → `ApplyEnchantmentToCard` → 返回 false 短路）。

`ApplyEnchantmentToCard` 四分支：

| 卡片现状 | 处理 |
|---------|------|
| 无附魔 | `card.EnchantInternal(e, amount)` + `ModifyCard()` + `FinalizeUpgradeInternal()` |
| 已是复合容器 | `composite.AddOrStackEnchantment(e, amount, refreshConsumed)` |
| 同类直挂 | `existing.Amount += (int)amount`；消耗型（Disabled）刷新为 Normal |
| 异类直挂 | 清原附魔 → 挂容器 → `ImportExistingEnchantment(existing)` → 再加新的 |

- `card.EnchantInternal(EnchantmentModel, decimal)` / `ClearEnchantmentInternal()` / `FinalizeUpgradeInternal()`：原版卡牌附魔内部 API（真实存在）。
- `CanEnchant(CardModel)` prefix 同步放行：复合容器直接 false；Unplayable 卡在牌组中禁止；`card.Enchantment == null || CanAttachEnchantment(card, type)`。

## 叠层规则（Layered 类型白名单）

```csharp
// 可重复获得的附魔类型（8 种）：Adroit / Goopy / Momentum / Nimble / Sharp / Sown / Swift / Vigorous
// 重复获得时同类叠层：existing.Amount += amount
// 消耗型（Sown / Swift / Vigorous）：重复获得时 Disabled → Normal 刷新
```

- 同类叠层后 `RecalculateValues()` + `Card.DynamicVars.RecalculateForUpgradeOrEnchant()`。
- 非白名单类型重复获得 = 跳过（防重复附魔破坏数值）。

## 存档历史

`RecordEnchantmentHistory(card, id)`：`card.Owner.RunState.CurrentMapPointHistoryEntry?.GetEntry(owner.NetId).CardsEnchanted.Add(new CardEnchantmentHistoryEntry(card, id))`——地图点历史记录（原版 API）。

## UI 层

### 卡面多附魔 tab（NCard.UpdateEnchantmentVisuals prefix）

```csharp
// 主 tab 显示 lead 附魔；后续附魔 Duplicate 主 tab 追加：
Control extraTab = sourceTab.Duplicate() as Control;              // 复用原版 tab 结构
extraTab.Name = "RepeatableEnchantmentsExtraTab" + index;         // 前缀命名便于清理
extraTab.Material = extraTab.Material?.Duplicate() as Material;   // 材质 Duplicate 防共享状态
parent.AddChildSafely(extraTab);
// Icon/Label 从 tab 子树取（GetNodeOrNull/FindChild），填图标/层数/状态着色
// 刷新时按前缀清掉全部额外 tab（ClearExtraEnchantmentTabs）
```

- 私有字段反射：`NCard._enchantmentIcon` / `_enchantmentLabel` / `_defaultEnchantmentPosition`（静态缓存 FieldInfo）。
- Disabled 状态用 ShaderMaterial 参数着色（`SetShaderParameter("h"/"s"/"v")` hue/saturation/value）+ `icon.UseParentMaterial` + `label.SelfModulate = StsColors.gray`。

### 附魔预览重写（NEnchantPreview.Init prefix 全接管）

```csharp
// 原版预览只支持单附魔；prefix 重写完整 before/after 双卡：
//   RemoveExistingCards → NCard.Create(card) 挂 before holder → UpdateVisuals(pileType, Normal)
//   cardScope.CloneCard(card) 拷一份 → IsEnchantmentPreview = true →
//   ApplyEnchantmentToCard(previewCard, e.ToMutable(), amount, recordHistory: false) → NCard.Create 挂 after
// 返回 false 短路原版
```

- `NCard.Create(CardModel)` / `NPreviewCardHolder.Create(...)` / `card.CardScope.CloneCard(card)`：预览节点原版 API。

### 附魔动画特效（NCardEnchantVfx._Ready postfix）

- 取 `_cardModel`/`_cardNode`/`_enchantmentIcon`/`_enchantmentLabel` 私有字段，复合时显示 **lead 附魔** 的图标 + 层数（动画只播一个，避免叠影）。

## 周边联动

| 场景 | 做法 |
|------|------|
| 休息点复制选项（Clone） | `CloneRestSiteOption.OnSelect` prefix：牌组含复合 Clone → 重写实现（RunState.CloneCard + CardPileCmd.Add + PreviewCardPileAdd），短路原版 |
| 偷牌怪优先级 | `ThievingHopper._stealPriorities`（private static readonly Func[]）反射**改数组元素**（readonly 只锁引用，元素可改）：Imbued 卡优先级最高 |
| 卡面描述 | `CardModel.GetDescriptionForPile/GetDescriptionForUpgradePreview` postfix：`AppendCompositeExtraText` 追加 `[purple]` 附魔行 |
| hover 提示 | `EnchantmentModel.get_HoverTips` postfix：复合 = 容器标题 + 各内部附魔 HoverTips 拼接 |

## 注意

- `RestSiteOption.Owner` 是 **protected** 属性，外部类用反射读（`RequireProperty`）。
- 全部 hook 走 `RequireMethod/RequireField/RequireProperty`（fail fast，游戏版本不匹配启动即报）。
- 依赖 `MegaCrit.Sts2.addons.mega_text`（MegaLabel.SetTextAutoSize，游戏自带 UI 库）。