# 自定义怪物 & 遭遇

---

## 怪物类

### 最简示例（两个状态循环）

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace MyMod.Monsters;

public sealed class GoblinScout : MonsterModel
{
    public override string MonsterId => "GoblinScout";
    public override int MaxHp => 30;
    public override string PortraitPath => "res://MyMod/images/monsters/goblin_scout.png";

    public GoblinScout() : base()
    {
    }

    public override IEnumerable<MoveState> GenerateMoveStateMachine()
    {
        return new[]
        {
            // 状态 1：攻击
            new MoveState("Attack")
                .SetMoveText("攻击")
                .SetIntent(IntentType.Attack)
                .SetDamage(8)
                .OnExecute(async context =>
                {
                    await CreatureCmd.Damage(context, context.Player, 8, this, this);
                })
                .NextState("Defend"),

            // 状态 2：防御
            new MoveState("Defend")
                .SetMoveText("防御")
                .SetIntent(IntentType.Defend)
                .SetBlock(5)
                .OnExecute(async context =>
                {
                    await CreatureCmd.GainBlock(this, 5, this);
                })
                .NextState("Attack")
        };
    }
}
```

---

## 带条件转移的状态机

```csharp
public override IEnumerable<MoveState> GenerateMoveStateMachine()
{
    return new[]
    {
        new MoveState("Idle")
            .SetMoveText("观察")
            .SetIntent(IntentType.Unknown)
            .OnExecute(async context =>
            {
                // 什么都不做
            })
            .NextState("ChooseAction"),

        new MoveState("ChooseAction")
            .SetMoveText("选择")
            .SetIntent(IntentType.Unknown)
            .OnExecute(async context =>
            {
                // 根据 HP 比例选择下一状态
            })
            // 条件转移
            .AddTransition("Enrage", ctx => ctx.Monster.HpPercent < 0.3f)
            .AddTransition("Attack", ctx => ctx.Monster.HpPercent >= 0.3f),

        new MoveState("Attack")
            .SetMoveText("猛击")
            .SetIntent(IntentType.Attack)
            .SetDamage(12)
            .OnExecute(async context =>
            {
                await CreatureCmd.Damage(context, context.Player, 12, this, this);
            })
            .NextState("ChooseAction"),

        new MoveState("Enrage")
            .SetMoveText("狂暴")
            .SetIntent(IntentType.Buff)
            .OnExecute(async context =>
            {
                await PowerCmd.Apply<StrengthPower>(this, 3, this, this);
            })
            .NextState("Attack")
    };
}
```

---

## IntentType 速查

| IntentType | 显示 |
|------------|------|
| `Attack` | 剑图标 + 伤害数值 |
| `Defend` | 盾图标 + 格挡数值 |
| `Buff` | 增益图标 |
| `Debuff` | 减益图标 |
| `Unknown` | 问号 |
| `None` | 无显示 |

---

## 遭遇（Encounter）

遭遇是"一组怪物"的配置。

```csharp
public sealed class GoblinAmbush : EncounterModel
{
    public override string EncounterId => "GoblinAmbush";
    public override string DisplayName => "哥布林伏击";
    public override ActType ValidAct => ActType.Act1;

    public override IEnumerable<MonsterModel> GenerateMonsters()
    {
        return new MonsterModel[]
        {
            ModelDb.Monster<GoblinScout>(),
            ModelDb.Monster<GoblinScout>(),
            ModelDb.Monster<GoblinWarrior>()
        };
    }
}
```

---

## 怪物注册

```csharp
// 属性扫描
[RegisterMonster]
public sealed class GoblinScout : MonsterModel { ... }

// 或手动
ModHelper.AddModelToPool(typeof(MonsterPool), typeof(GoblinScout));
```

---

## 本地化

`assets/localization/zhs/monsters.json`：

```json
{
  "GoblinScout": {
    "NAME": "哥布林侦察兵",
    "DESCRIPTION": "一只瘦小的哥布林，拿着生锈的匕首。"
  }
}
```

`assets/localization/zhs/encounters.json`：

```json
{
  "GoblinAmbush": {
    "NAME": "哥布林伏击",
    "DESCRIPTION": "三只哥布林从灌木丛中跳出来！"
  }
}
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 怪物不攻击 | 检查状态机 `NextState` 是否正确 |
| 条件转移不触发 | 检查 `AddTransition` 的条件表达式 |
| 怪物不出现 | 检查遭遇注册和 `ValidAct` |