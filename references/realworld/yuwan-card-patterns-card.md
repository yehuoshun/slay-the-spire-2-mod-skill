# 卡牌/能力实战模式（YuWanCard 提炼）

> 均为 YuWan 自研框架设计思想，落地时转纯原生写法。

## 1. 声明式关键字升级

不用在 `OnUpgrade()` 里手动 AddKeyword/RemoveKeyword，构造函数里声明升级行为：

```csharp
// 伪代码（原生 CardModel）
WithKeywords(CardKeyword.Ethereal);                       // 始终存在
WithKeyword(CardKeyword.Innate, UpgradeType.Add);          // 升级时添加
WithKeyword(CardKeyword.Ethereal, UpgradeType.Remove);     // 升级时移除
```

升级时框架统一处理：遍历 DynamicVar 应用升级值 → 处理 CostUpgrade → 处理关键字。**纯原生写法**：`OnUpgrade()` 里按 `UpgradeType` 逻辑调用 `AddKeyword/RemoveKeyword`，或直接复制这套 Harmony 后置 Patch 思路（见 [harmony](../harmony/harmony.md)）。

## 2. 计算伤害（CalculatedDamageVar）

伤害 = (基础 + 额外 × 倍率) × 全局系数，自动建三个变量：

```csharp
// 原生 API 可用
new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
    (card, target) => card.CombatState?.Enemies?.Count ?? 0);
// 配套：CalculationBaseVar / ExtraDamageVar
// 本地化: {CalculatedDamage:diff()}
```

`DamageCmd.Attack(CalculatedDamageVar)` 重载直接接受计算伤害变量（sts2-res 已确认）。

## 3. 能力三字段本地化规则（重点）

| 字段 | 场景 | 动态变量 |
|------|------|---------|
| `description` | 图鉴/卡牌预览（规范模型） | 能力：❌ 不支持；卡牌：✅ 自动注入 |
| `smartDescription` | 战斗中生物悬浮提示（实例化后） | ✅ |
| `remoteDescription` | 多人对方玩家视角 | ✅ |

能力 `CanonicalVars` 里的变量只进 `smartDescription/remoteDescription`；卡牌 `CanonicalVars` 直接进 `description`。**写能力本地化时：description 写静态文本，动态值一律放 smartDescription。**

## 4. smartDescription 隐式变量

无需在 `CanonicalVars` 定义即可用：`{Amount}` `{Duration}` `{OnPlayer}` `{IsMultiplayer}` `{PlayerCount}` `{OwnerName}` `{ApplierName}` `{TargetName}` `{singleStarIcon}` `{energyPrefix}`。

## 5. 跨战斗持久化（BaseReplayCount + DeckVersion）

「永久升级/多次打出」类卡牌的标准模式：

```csharp
[SavedProperty] public int MyPrefix_ReplayCount { get; set; }
static MyCard() { SavedPropertyRegistration.RegisterType(typeof(MyCard)); }

protected override void AfterDeserialized()
{
    base.AfterDeserialized();
    BaseReplayCount = MyPrefix_ReplayCount;   // 读档恢复
}

// OnPlay 里同步回牌组规范实例（DeckVersion）
if (DeckVersion is MyCard deckCard)
{
    deckCard.MyPrefix_ReplayCount += 1;
    deckCard.BaseReplayCount = deckCard.MyPrefix_ReplayCount;
}
```

要点：`BaseReplayCount` 允许一回合多次打出；改 `DeckVersion`（牌组规范实例）才能跨战斗持久化；`[SavedProperty]` 属性名加模组前缀避免冲突。

## 6. 临时能力

`PowerModel` 包装器模式：应用时转换为内部能力（如临时力量）。简单版一行声明包装类，复杂版继承 `TemporaryPowerModel` 覆写 `InternallyAppliedPower` + `BeforeApplied`。纯原生需手写包装类（继承 `PowerModel`，`AfterApplied` 时调用 `PowerCmd.Apply<StrengthPower>` 再自移除）。

## 7. 药水/遗物也支持 CanonicalVars + HoverTips

真实代码中遗物可覆写 `CanonicalVars`（含 `PowerVar<T>`）与 `ExtraHoverTips`（`HoverTipFactory.FromPower<T>()`、`HoverTipFactory.Static(StaticHoverTip.Block)`）——遗物描述也可动态化，skill 原遗物文档未覆盖。

## 8. 常见坑补充

- `PowerCmd.Apply` 一定带 `PlayerChoiceContext`，无上下文用 `new ThrowingPlayerChoiceContext()`（YuWan 真实用法）
- AOE 在 `OnPlay` 里 `foreach (var enemy in choiceContext.CombatState.Enemies)` 逐发攻击
- 卡牌费用升级用 `WithCostUpgradeBy(-1)` 声明，框架自动 `EnergyCost.UpgradeBy`
