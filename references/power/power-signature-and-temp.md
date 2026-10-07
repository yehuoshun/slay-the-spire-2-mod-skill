# 能力进阶：签名适配基类 + 临时力量装饰层

> 实战验证（sts2mod 俄洛伊 Illaoi v0.2.0，2026-10-07）。两个可复用模式：① 回调签名适配基类（简化多参回调）；② 临时力量/敏捷「装饰层」实现（挂在原版力量系统上、回合自动消退）。

## ① 回调签名适配基类（PowerModel/RelicModel 通用）

原版回调带 `IReadOnlyList<Creature> participants` 等参与列表参数，每个能力都要写一遍；用 `sealed override` 钉死原版签名，转成简化 `virtual`：

```csharp
public abstract class MyPowerBase : PowerModel
{
    // 子类写这个简化版（无 participants）
    public virtual Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side) => Task.CompletedTask;

    // 原版签名 sealed 钉死，直接转发
    public sealed override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants) => AfterTurnEnd(choiceContext, side);
}
```

- 覆盖点：`BeforeSideTurnStart` / `AfterSideTurnStart` / `BeforeSideTurnEnd` / `AfterTurnEnd` / `AfterPowerAmountChanged` 五个。
- **必须 sealed**，否则子类不小心覆写原版签名会绕过适配层（俄洛伊用 sealed 防呆）。
- 同样做一份 `RelicBase`（遗物也有同签名回调）。

## ② 临时力量装饰层（ITemporaryPower + 负值抵消）

目标：独立的「临时力量/敏捷」——显示在原版力量系统上、X 回合后自动消退（原版 `TemporaryStrengthPower` 是回合末减层，见 power-advanced.md）：

```csharp
public abstract class MyTemporaryStatPower<TStatPower> : MyPowerBase, ITemporaryPower
    where TStatPower : PowerModel
{
    private bool _shouldIgnoreNextInstance;

    public abstract AbstractModel OriginModel { get; }                       // ITemporaryPower①
    public PowerModel InternallyAppliedPower => ModelDb.Power<TStatPower>(); // ITemporaryPower②

    public void IgnoreNextInstance() => _shouldIgnoreNextInstance = true;    // ITemporaryPower③

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (_shouldIgnoreNextInstance) { _shouldIgnoreNextInstance = false; return; }
        await PowerCmd.Apply<TStatPower>(target, amount, applier, cardSource, silent: true);  // 同步到真力量
    }
    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this || amount == Amount) return;   // 层数变化 → 同步差额
        if (_shouldIgnoreNextInstance) { _shouldIgnoreNextInstance = false; return; }
        await PowerCmd.Apply<TStatPower>(Owner, amount, applier, cardSource, silent: true);
    }
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side || Amount <= 0) return;
        Creature owner = Owner; decimal amount = Amount;
        Flash();
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<TStatPower>(owner, -amount, owner, null);   // 负值抵消
    }
}
public sealed class MyTemporaryStrengthPower : MyTemporaryStatPower<StrengthPower>
{ public override AbstractModel OriginModel => ModelDb.Card<MyCard>(); }
```

- `ITemporaryPower` 在 `MegaCrit.Sts2.Core.Models`（3 成员）。`IgnoreNextInstance` 给 Misery 类复制 debuff 效果用（内部 debuff 已被复制，不应再 Apply）。
- 真力量变更用 `silent: true`，避免双 UI 闪烁；装饰层自己 `Flash()`。
- **BeforeApplied + AfterPowerAmountChanged 双钩子**：新增层数走前者，后续增减走后者（`amount == Amount` 判断防重复应用）。
- 回合结束：先 `Remove` 装饰层再 Apply 负值——顺序保证真力量也先减后清。

## ③ 隐藏提示能力（IsVisibleInternal）

只用于 hover 提示、不在状态栏显示的能力：

```csharp
public override PowerType Type => PowerType.Buff;
public override PowerStackType StackType => PowerStackType.Counter;
protected override bool IsVisibleInternal => false;      // 隐藏（PowerModel.IsVisible 内部检查）
```

配套 hover 提示（无图标版）：

```csharp
public static IHoverTip FromPowerWithoutIcon<TPower>() where TPower : PowerModel
{
    PowerModel p = ModelDb.Power<TPower>();
    return new HoverTip(p.Title, p.Description.GetFormattedText())
    { Id = p.Id.ToString(), IsDebuff = p.Type == PowerType.Debuff, IsSmart = false };
}
```

## ④ 真实 API 行为笔记

- **致命一击不触发 `AfterDamageReceived`**（STS2 跳过）；要转移致死伤害用 `AfterDamageGiven`（dealer 侧）锚定 `target == Owner` 再处理。
- Died 事件订阅做**幂等重订阅**（对比 `_subscribedOwner != Owner` 才换订阅），多回调都调用，防漏绑/重绑。
- `ShouldPowerBeRemovedAfterOwnerDeath` / `ShouldOwnerDeathTriggerFatal` 覆写控制死亡联动（灵魂类传 false 防连锁）。