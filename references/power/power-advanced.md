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

## 调试

战斗中按反单引号 `` ` `` 打开控制台：

```
power <目标> <能力ID> <层数>
```

`目标` 为整数，单人游戏时 `0` 表示玩家角色。

## 生命条预测（HealthBarForecast）

> BaseLib v3.4.7 新方向（`OutwardFromCurrentHp` / `InwardFromMaxHp`）；实战项目 YuWanCard 已用。能力/遗物可实现预测段，在生命条上叠加显示（如中毒伤害、毁灭条）。

```csharp
// 能力实现 IHealthBarForecastSource，覆写 GetHealthBarForecastSegments
public override IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(
    HealthBarForecastContext context)
{
    if (context.Creature == Owner && Amount > 0)
    {
        yield return new HealthBarForecastSegment(
            Amount,                                // 预测值
            new Color(0.5f, 0.2f, 0.8f),          // 颜色
            HealthBarForecastDirection.FromRight,  // 方向
            Order: 0);                             // 排序
    }
}
```

毁灭条样式：`ShaderUtils.CreateDoomBarShaderMaterial(ShaderUtils.CreateVanillaDoomBarGradientTexture())` 作为第 5 参 `Material` 传入。

## 进阶：纯原生自动注册

> 从 BaseLib 提炼，零第三方依赖。能力不进池，用 `[PowerModel]` attribute 标记 + ContentRegistry 统一 `ModelDb.Inject`。框架完整代码见 [serialization.md](../serialization/serialization.md)「进阶：纯原生自动注册框架」。

```csharp
[PowerModel]
public class MyPower : PowerModel { ... }
```

### 图标回退（原生自动）

大图缺失时原生 `ResolvedBigIconPath` 自动回退 `BigIconPath → BigBetaIconPath → MissingIconPath`，无需写代码（见 [power-core.md](power-core.md)）。