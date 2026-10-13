# 自定义能力：临时能力、调试与自动注册

> 图标命名约定见 [power-core.md](power-core.md)。自定义音效、治疗量修正与本地化见 [power-effects.md](power-effects.md)。

## 临时能力（Temporary Power）

临时能力是回合结束时自动减少层数、层数归零时自动移除的能力（临时力量/敏捷）。

### 原生模式（对照 TemporaryStrengthPower）

```csharp
public class MyTempPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;   // 层数归零自动移除

    // 回合结束：持有者在参与者列表里就减 1 层
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Decrement(this);
        }
    }
}
```

### 要点

- 用 `PowerCmd.Decrement(this)` 递减层数（不要直接改 `Amount` 字段）
- `AllowNegative = false` + 归零移除机制自动清理
- 判断持有者是否在场用 `participants.Contains(Owner)`（真实 TemporaryStrengthPower 风格）

### 变体：回合结束直接移除（不留递减过程）

> 实战验证（STS2_MarisaMod 2026-10-14，PrismaticNebularPower）。不需要「每回合减 1 层」的渐退过程、只要「本回合结束时整个消失」的临时 Buff，直接 `PowerCmd.Remove(this)`：

```csharp
public class PrismaticNebularPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)      // 只在自己的回合结束时移除
        {
            Flash();
            await PowerCmd.Remove(this);
        }
    }
}
```

- 判断己方回合用 `side == Owner.Side`（不依赖 participants 列表）
- 与 Decrement 模式的区别：Decrement 保留递减动画（每回合 -1 直到归零），Remove 直接整个消失；效果语义不同，按需选择

## 增益扩散器（ModifyPowerAmountGivenAdditive 拦截）

> 实战验证（STS2_MarisaMod 2026-10-14，PrismaticNebularPower）。持有本 Power 时，自己给**其他人**上某 Buff → 自动**扩散给全队队友**，且扩散量带修正（原始量 + 本层数 - 1）。核心：覆写 `ModifyPowerAmountGivenAdditive` 拦截「给他人施加」的瞬间。

```csharp
public override decimal ModifyPowerAmountGivenAdditive(
    PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
{
    // 拦截点：自己（giver == Owner）给他人（target != Owner）上目标 Buff 且量为正
    if (power is StarlitPower && giver == Owner && amount > 0)
    {
        var teammates = CombatState.GetTeammatesOf(Owner)
            .Where(c => c is { IsAlive: true, IsPlayer: true } && c != Owner);
        foreach (var creature in teammates)
        {
            // 扩散量 = 原始量 + 本层数 - 1；同步回调里用 Throwing 上下文发起异步命令
            _ = PowerCmd.Apply<StarlitPower>(
                new ThrowingPlayerChoiceContext(), creature, amount + Amount - 1, Owner, null);
        }
    }

    return base.ModifyPowerAmountGivenAdditive(power, giver, amount, target, cardSource);
}
```

- **拦截点语义**：`giver == Owner`（自己给的）、`power is 目标Buff`、`amount > 0`（排除移除/负向），三者齐了才扩散
- **ThrowingPlayerChoiceContext**：同步签名回调里发起异步 `PowerCmd.Apply` 必须用 `new ThrowingPlayerChoiceContext()`（丢弃上下文，立即执行；参考 power-signature-and-temp.md）
- **扩散量修正**：`amount + Amount - 1` 把本 Power 自身层数算进扩散值——层数越高，队友吃到的越多
- 与「临时力量装饰层」的区别：装饰层同步**同目标**的真力量；扩散器是**跨目标**复制增益（自己上 Buff 时捎带全队）

## 调试

战斗中按反单引号 `` ` `` 打开控制台：

```
power <目标> <能力ID> <层数>
```

`目标` 为整数，单人游戏时 `0` 表示玩家角色。

## 进阶：纯原生自动注册

> 从 BaseLib 提炼，零第三方依赖。能力不进池，用 `[PowerModel]` attribute 标记 + ContentRegistry 统一 `ModelDb.Inject`。框架完整代码见 [serialization.md](../serialization/serialization.md)「进阶：纯原生自动注册框架」。

```csharp
[PowerModel]
public class MyPower : PowerModel { ... }
```

### 图标回退（原生自动）

大图缺失时原生 `ResolvedBigIconPath` 自动回退 `BigIconPath → BigBetaIconPath → MissingIconPath`，无需写代码（见 [power-core.md](power-core.md)）。