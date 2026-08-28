# 自定义 Modifier：效果实现与完整示例

## 效果实现（override 钩子，非 Harmony Patch）

Modifier 的 `ShouldReceiveCombatHooks = true`（继承自 AbstractModel），所以直接在子类 override 钩子即可，**不需要 Harmony Patch**。

### 模式 1：运行级配置（AfterRunCreated）

```csharp
protected override void AfterRunCreated(RunState runState)
{
    // 改全局几率/抓包/配置：
    runState.Odds.UnknownMapPoint.EliteOdds = 0.1f;
    runState.SharedRelicGrabBag.Remove<JuzuBracelet>();
}
```

### 模式 2：伤害/格挡修正（Modify 钩子）

```csharp
// 所有攻击伤害 +2
public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
{
    if (dealer?.Player != null && props.IsPoweredAttack())
        return amount + 2m;
    return amount;
}

// 敌人 HP 翻倍（乘算钩子）
public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
{
    return amount * 2m;
}
```

### 模式 3：地图/奖励修改

```csharp
// 修改地图生成（对照 BigGameHunter）
public override ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex) => map;

// 修改卡牌奖励生成
public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options) => options;

// 修改未开放房间类型的几率加成
public override float ModifyOddsIncreaseForUnrolledRoomType(RoomType roomType, float oddsIncrease) => oddsIncrease;
```

> 更多 `Modify*`/`After*` 钩子见 [code-patterns.md](../patterns/code-patterns.md) 或直接 `grep Modify Core/Models/AbstractModel.cs`。

---

## 完整示例：攻击增强规则

```csharp
// Modifier 模型（元数据自动从 modifiers.json 读取）
public class AttackBuffModifier : ModifierModel
{
    // 效果直接写在钩子里：所有攻击伤害 +2
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer?.Player != null && props.IsPoweredAttack())
            return amount + 2m;
        return amount;
    }
}
```

配合 `modifiers.json`：

```json
{
  "ATTACK_BUFF_MODIFIER": {
    "title": "攻击增强",
    "description": "所有攻击牌伤害 +2。"
  }
}
```

