# 自定义能力（Buff/Debuff）

---

## 最简能力

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyMod.Powers;

public sealed class ThornsPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override StackType StackType => StackType.Intensity;
    public override bool IsDebuff => false;
    public override string BigIconPath => "res://MyMod/images/powers/thorns.png";
    public override string SmallIconPath => BigIconPath;

    public ThornsPower() : base()
    {
    }

    public override async Task<int> OnAttacked(PlayerChoiceContext context, Creature attacker, int damage)
    {
        if (attacker.IsDead || Owner.IsDead) return damage;
        await CreatureCmd.Damage(context, attacker, Amount, Owner, this);
        return damage;
    }

    public override string GetEffectDescription()
    {
        return $"受到攻击时，对攻击者造成 {Amount} 点伤害。";
    }
}
```

---

## PowerType 决定行为

| PowerType | 说明 |
|-----------|------|
| `Buff` | 正面效果 |
| `Debuff` | 负面效果 |

---

## StackType 决定叠加方式

| StackType | 说明 |
|-----------|------|
| `Intensity` | 数值叠加（如力量 +1 +1） |
| `Duration` | 持续时间叠加（如易伤 2 回合） |
| `None` | 不叠加 |

---

## 常用生命周期钩子

| 钩子 | 触发时机 | 返回 |
|------|---------|------|
| `OnAttacked(context, attacker, damage)` | 被攻击时 | 修改后伤害 |
| `OnTurnStart(context)` | 回合开始 | - |
| `OnTurnEnd(context)` | 回合结束 | - |
| `OnCardPlayed(context, card)` | 打出卡牌时 | - |
| `AtDamageGiven(context, target, damage)` | 造成伤害时 | 修改后伤害 |
| `AtDamageReceived(context, attacker, damage)` | 受到伤害时 | 修改后伤害 |
| `OnDeath(context)` | 死亡时 | - |
| `OnHeal(context, amount)` | 治疗时 | 修改后治疗量 |
| `OnBlockGained(context, amount)` | 获得格挡时 | 修改后格挡量 |

---

## 带 SavedProperty 的能力

```csharp
public sealed class StackingPower : PowerModel
{
    [SavedProperty] public int HitCount { get; set; }

    public override async Task<int> OnAttacked(PlayerChoiceContext context, Creature attacker, int damage)
    {
        HitCount++;
        if (HitCount >= 3)
        {
            HitCount = 0;
            await CreatureCmd.Damage(context, attacker, Amount * 2, Owner, this);
        }
        return damage;
    }
}
```

---

## 临时属性力（回合结束消失）

```csharp
public sealed class TemporaryStrengthPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override StackType StackType => StackType.Intensity;

    public override async Task OnTurnEnd(PlayerChoiceContext context)
    {
        await PowerCmd.Remove<StrengthPower>(Owner, Amount);
    }
}
```

---

## 本地化

`assets/localization/zhs/powers.json`：

```json
{
  "ThornsPower": {
    "NAME": "荆棘",
    "DESCRIPTIONS": ["受到攻击时，对攻击者造成 #b{0} 点伤害。"]
  }
}
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 能力层数不累计 | 检查 `StackType` 是否为 `Intensity` |
| 回合结束效果不触发 | 确认 `OnTurnEnd` 返回值类型 |
| SavedProperty 不持久化 | 调 `SavedPropertiesTypeCache.InjectTypeIntoCache(GetType())` |