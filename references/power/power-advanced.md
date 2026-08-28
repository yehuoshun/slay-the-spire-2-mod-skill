# 自定义能力：临时能力、调试与自动注册

## 图标

```
战斗裁切纹理：res://images/atlases/power_atlas.sprites/<能力ID小写>.tres
大图纹理：    res://images/powers/<能力ID小写>.png
Beta 大图：   res://images/powers/<能力ID小写>_beta.png
```

推荐分辨率：256x256（大图），64x64（裁切）

---

---

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

---

## 进阶：纯原生自动注册

> 从 BaseLib 提炼，零第三方依赖。能力不进池，用 `[PowerModel]` attribute 标记 + ContentRegistry 统一 `ModelDb.Inject`。框架完整代码见 [serialization.md](../serialization/serialization.md)「进阶：纯原生自动注册框架」。

```csharp
[PowerModel]
public class MyPower : PowerModel { ... }
```

### 图标回退（原生自动）

大图缺失时原生 `ResolvedBigIconPath` 自动回退 `BigIconPath → BigBetaIconPath → MissingIconPath`，无需写代码（见上方「图标」章节）。

