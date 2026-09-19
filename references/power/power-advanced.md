# 自定义能力：临时能力、调试与自动注册

> 图标命名约定见 [power-core.md](power-core.md)。

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

大图缺失时原生 `ResolvedBigIconPath` 自动回退 `BigIconPath → BigBetaIconPath → MissingIconPath`，无需写代码（见 [power-core.md](power-core.md)）。

---

## 自定义音效

能力施加时支持播放自定义音效（替代默认 Buff/Debuff 音效）。实现方案见 [harmony-custom-power-sfx.md](../harmony/harmony-custom-power-sfx.md)：

1. 定义接口 `ICustomPowerSfx` + Harmony Transpiler（零第三方依赖）
2. 能力类实现 `ICustomPowerSfx`，在 `PlayCustomSfx` 中调 `SfxCmd.Play`
3. `PatchAll()` 自动注册

## 治疗量修正

游戏原生没有治疗量修改回调（`ModifyDamageAdditive` 只改伤害，不治治疗）。如果需要「治疗效果 +50%」或「禁疗」这类效果，通过接口 + Harmony Prefix 实现。

### 定义接口 & Patch

```csharp
public interface IHealModifier
{
    decimal ModifyHealAdditive(Creature creature, decimal amount) => 0m;
    decimal ModifyHealMultiplicative(Creature creature, decimal amount) => 1m;
}

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal),
    [typeof(Creature), typeof(decimal)])]
public static class HealModifierPatch
{
    [HarmonyPrefix]
    private static void Prefix(Creature creature, ref decimal amount)
    {
        foreach (var model in creature.Powers) // 遍历能力
        {
            if (model is IHealModifier mod)
            {
                amount += mod.ModifyHealAdditive(creature, amount);
                amount *= mod.ModifyHealMultiplicative(creature, amount);
            }
        }
        amount = Math.Max(0m, amount); // 治疗量不能为负
    }
}
```

### 在能力中使用

```csharp
public class HopeRelicBuff : PowerModel, IHealModifier
{
    // 治疗效果 +50%
    public decimal ModifyHealMultiplicative(Creature creature, decimal amount) => 1.5m;
}
```

> 注意：Prefix 参数签名需匹配 `CreatureCmd.Heal` 的原生签名。如果签名变了（如多了 `AbstractModel source` 参数），同步修改即可。

预见修改见 [power-scry.md](power-scry.md)。

## 本地化

路径：`res://<模组ID>/localization/<语言代码>/powers.json`

```json
{
  "MyPower": {
    "name": "示例能力",
    "description": "简介文本",
    "smartDescription": "打出牌时获得 {Amount} 层格挡。"
  }
}
```

| 字段 | 说明 |
|------|------|
| `name` | 能力名称 |
| `description` | 普通简介文本 |
| `smartDescription` | 带动态变量信息的介绍（支持 `{Amount}` 等变量） |

