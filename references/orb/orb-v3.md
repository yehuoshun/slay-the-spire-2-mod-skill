# 自定义球体（Orb）

> 参考：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomOrbModel.cs`

---

> v3：API 全量校正（对照真实 OrbModel/LightningOrb：Passive(PlayerChoiceContext, Creature?)、Evoke(PlayerChoiceContext)→目标列表、PassiveVal/EvokeVal/DarkenedColor 必填、图标命名约定、CreatureCmd.Damage）。v2 为存档，勿直接使用。
## 概述

球体是故障机器人（以及部分 Mod 角色）的充能球系统。继承 `OrbModel` 抽象类。每个球体有**被动效果**（回合结束触发）和**激发效果**（释放时触发）。

---

## 基础模板

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

public class MyOrb : OrbModel
{
    // 抽象必填：被动值 / 激发值 / 变暗颜色
    public override decimal PassiveVal => 3m;
    public override decimal EvokeVal => 8m;
    public override Color DarkenedColor => new("796606");

    // 音效（protected virtual）
    protected override string PassiveSfx => "event:/sfx/characters/defect/defect_lightning_passive";
    protected override string EvokeSfx => "event:/sfx/characters/defect/defect_lightning_evoke";
    protected override string ChannelSfx => "event:/sfx/characters/defect/defect_lightning_channel";

    // 回合结束触发被动（真实签名：PlayerChoiceContext + 可选目标）
    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext)
    {
        await Passive(choiceContext, null);
    }

    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        Trigger();
        await DealDamage(PassiveVal, target, choiceContext);
    }

    // 激发：返回被击中的目标列表
    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext choiceContext)
    {
        return await DealDamage(EvokeVal, null, choiceContext);
    }

    private async Task<IEnumerable<Creature>> DealDamage(decimal value, Creature? target, PlayerChoiceContext choiceContext)
    {
        var opponents = CombatState.GetOpponentsOf(Owner.Creature).Where(e => e.IsHittable).ToList();
        if (opponents.Count == 0) return [];

        var targets = target == null
            ? new List<Creature> { Owner.RunState.Rng.CombatTargets.NextItem(opponents) }
            : new List<Creature> { target };

        return await CreatureCmd.Damage(choiceContext, targets, value, ValueProp.Unpowered, Owner.Creature);
    }
}
```

---

## 可覆盖成员（真实存在）

| 成员 | 类型 | 说明 |
|------|------|------|
| `PassiveVal` / `EvokeVal` | `public abstract decimal` | 被动/激发数值（**必填**） |
| `DarkenedColor` | `public abstract Color` | 球体变暗颜色（**必填**） |
| `PassiveSfx` / `EvokeSfx` / `ChannelSfx` | `protected virtual string` | 音效路径 |
| `BeforeTurnEndOrbTrigger(PlayerChoiceContext)` | `public virtual Task` | 回合结束触发被动 |
| `Passive(PlayerChoiceContext, Creature?)` | `public virtual Task` | 被动效果 |
| `Evoke(PlayerChoiceContext)` | `public virtual Task<IEnumerable<Creature>>` | 激发效果（返回命中目标） |
| `OnChannel(...)` | - | 充能时 |

> ⚠️ 图标/精灵路径 `IconPath`/`SpritePath` 是原生 **private**（命名约定 `orbs/<id小写>.png` + `orb_visuals/<id小写>` 场景），不可 override。`IncludeInRandomPool` 是 BaseLib `CustomOrbModel` 的，原生用 `OrbModel.GetRandomOrb(Rng)` 随机池机制。

---

## 完整示例：雷电球（对照真实 LightningOrb）

```csharp
public class LightningOrb : OrbModel
{
    public override Color DarkenedColor => new("796606");
    public override decimal PassiveVal => ModifyOrbValue(3m);
    public override decimal EvokeVal => ModifyOrbValue(8m);

    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext)
    {
        await Passive(choiceContext, null);
    }

    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        Trigger();
        await ApplyLightningDamage(PassiveVal, target, choiceContext);
    }

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext choiceContext)
    {
        return await ApplyLightningDamage(EvokeVal, null, choiceContext);
    }

    private async Task<IEnumerable<Creature>> ApplyLightningDamage(decimal value, Creature? target, PlayerChoiceContext choiceContext)
    {
        var enemies = CombatState.GetOpponentsOf(Owner.Creature).Where(e => e.IsHittable).ToList();
        if (enemies.Count == 0) return [];
        var targets = target == null
            ? new List<Creature> { Owner.RunState.Rng.CombatTargets.NextItem(enemies) }
            : new List<Creature> { target };
        VfxCmd.PlayOnCreature(targets[0], "vfx/vfx_attack_lightning");
        PlayEvokeSfx();
        return await CreatureCmd.Damage(choiceContext, targets, value, ValueProp.Unpowered, Owner.Creature);
    }
}
```

---

## 注册

球体需要在 ModEntry 中注册：

```csharp
// 手动注册到 ModelDb
ModelDb.Inject(typeof(MyOrb));
```

BaseLib 方式：继承 `CustomOrbModel` 自动注册。

---

## 本地化

路径：`res://<模组ID>/localization/<语言代码>/orbs.json`（locTable = `orbs`）

```json
{
  "MY_ORB": {
    "title": "自定义球体",
    "description": "被动：每回合造成 {D} 点伤害。激发：造成 {D} 点伤害。"
  }
}
```

> 键用 `title`/`description`（旧版写 `name` 是错的）。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 球体不显示 | 检查命名约定 `orbs/<id小写>.png` + `orb_visuals/<id小写>` 场景 |
| 被动不触发 | 重写 `BeforeTurnEndOrbTrigger` → `Passive(PlayerChoiceContext, Creature?)` |
| 激发无效果 | 重写 `Evoke(PlayerChoiceContext)` → 返回 `IEnumerable<Creature>` |
| 音效不播放 | 检查 `PassiveSfx`/`EvokeSfx`/`ChannelSfx` 路径 |
| 随机球池 | 原生用 `OrbModel.GetRandomOrb(Rng)`（无 IncludeInRandomPool 属性） |
| 伤害没打出来 | 用 `CombatState.GetOpponentsOf(Owner.Creature)` + `CreatureCmd.Damage` |

---

## 进阶：纯原生自动注册（v2）

> 从 BaseLib 提炼，零第三方依赖。用 `[OrbModel]` 标记 + ContentRegistry 统一 `ModelDb.Inject`。
> 框架完整代码见 [serialization.md](../serialization/serialization.md)「进阶：纯原生自动注册框架」。

```csharp
[OrbModel]
public class MyOrb : OrbModel { ... }
```

> 需要进随机池时，ContentRegistry 里额外处理（或参考上方「随机池注入」Patch）。

## 演进路线

- 当前：手动注册（保留为基准写法）
- v2：纯原生自动注册（Attribute 标记 + ContentRegistry）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`Passive/Evoke(CombatState)`、`IconPath/SpritePath` override、`IncludeInRandomPool`、`.Random()` 扩展、`DamageCmd.Attack().FromCard(this)`）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖