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

> ⚠️ **原生 `sts2.dll` 无此 API**（反编译验证：无 `IHealthBarForecastSource`/`HealthBarForecastSegment`）。这是 BaseLib v3.4.7 的方向、YuWanCard 自研接口。纯原生想实现要自研：定义接口 + Harmony Patch 生命条 UI 节点，成本高；**需要时建议直接用 BaseLib**（本 skill 唯一允许的第三方场景是设置界面，生命条预测不属于，此处仅作知识记录）。

```csharp
// 自研接口设计（灵感 BaseLib v3.4.7：OutwardFromCurrentHp / InwardFromMaxHp）
// 能力实现 IHealthBarForecastSource，返回预测段：
//   new HealthBarForecastSegment(Amount, color, HealthBarForecastDirection.FromRight, Order: 0)
//   （Amount=预测值, color=颜色, direction=方向, order=排序, material=可选毁灭条着色器）
// 毁灭条样式：ShaderUtils.CreateDoomBarShaderMaterial(
//     ShaderUtils.CreateVanillaDoomBarGradientTexture())
```

## 进阶：纯原生自动注册

> 从 BaseLib 提炼，零第三方依赖。能力不进池，用 `[PowerModel]` attribute 标记 + ContentRegistry 统一 `ModelDb.Inject`。框架完整代码见 [serialization.md](../serialization/serialization.md)「进阶：纯原生自动注册框架」。

```csharp
[PowerModel]
public class MyPower : PowerModel { ... }
```

### 图标回退（原生自动）

大图缺失时原生 `ResolvedBigIconPath` 自动回退 `BigIconPath → BigBetaIconPath → MissingIconPath`，无需写代码（见 [power-core.md](power-core.md)）。