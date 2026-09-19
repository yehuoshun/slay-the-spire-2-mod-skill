# 自定义能力：真实回调与本地化

## 真实回调

> 所有 `After*` 钩子定义在 `AbstractModel`（能力/遗物/卡牌通用），`Modify*`/`BeforeApplied` 等由 PowerModel 提供。

### 伤害 / 格挡修正（核心）

| 回调 | 签名 | 说明 |
|------|------|------|
| `ModifyDamageAdditive` | `decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)` | 伤害加减（力量） |
| `ModifyDamageMultiplicative` | 同左 5 参 | 伤害乘除（易伤） |
| `ModifyBlockAdditive` | `decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)` | 格挡加减 |
| `ModifyBlockMultiplicative` | 同左 5 参 | 格挡乘除 |

### 生命周期 / 事件钩子（AbstractModel）

| 回调 | 签名 | 说明 |
|------|------|------|
| `AfterCardPlayed` | `Task AfterCardPlayed(PlayerChoiceContext, CardPlay)` | 持有者打出任意牌后 |
| `AfterSideTurnEnd` | `Task AfterSideTurnEnd(PlayerChoiceContext, CombatSide, IEnumerable<Creature>)` | 某方回合结束后 |
| `AfterSideTurnStart` | `Task AfterSideTurnStart(CombatSide, IReadOnlyList<Creature>, ICombatState)` | 某方回合开始后 |
| `BeforeSideTurnStart` | `Task BeforeSideTurnStart(PlayerChoiceContext, CombatSide, IReadOnlyList<Creature>, ICombatState)` | 某方回合开始前 |

### PowerModel 自带

| 回调 | 签名 | 说明 |
|------|------|------|
| `BeforeApplied` | `Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)` | 施加前 |
| `AfterApplied` | `Task AfterApplied(Creature? applier, CardModel? cardSource)` | 施加后 |
| `AfterRemoved` | `Task AfterRemoved(Creature oldOwner)` | 移除后 |
| `ShouldPowerBeRemovedAfterOwnerDeath` | `bool` | 持有者死亡时是否移除 |

## 实战回调代码片段

### 回合结束递减层数（临时能力）

```csharp
public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
{
    if (participants.Contains(Owner))
    {
        await PowerCmd.Decrement(this);
    }
}
```

### 回合开始获得格挡

```csharp
public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
{
    if (participants.Contains(Owner))
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
}
```

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

