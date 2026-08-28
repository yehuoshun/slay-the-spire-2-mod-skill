# 自定义球体：模板与可覆盖成员

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

