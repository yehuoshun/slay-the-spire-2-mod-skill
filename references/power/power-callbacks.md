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

---

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

