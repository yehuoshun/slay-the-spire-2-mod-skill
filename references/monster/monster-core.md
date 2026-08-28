# 自定义敌怪：怪物类与模型

## 怪物类

```csharp
public class MyMonster : MonsterModel
{
    // 抽象属性：生命值范围（必须 override）
    public override int MinInitialHp => 40;
    public override int MaxInitialHp => 46;

    // AI 状态机（protected，不是 public）
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var attack = new MoveState("ATTACK_MOVE", AttackMove, new SingleAttackIntent(6));
        var defend = new MoveState("DEFEND_MOVE", DefendMove, new DefendIntent());
        attack.FollowUpState = defend;
        defend.FollowUpState = attack;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { attack, defend },
            attack);   // 第二参：初始状态对象（不是字符串 ID）
    }

    // 移动回调：参数是目标生物列表
    private async Task AttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(6).FromMonster(this)
            .WithAttackerAnim("Attack", 0.35f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task DefendMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, 5, ValueProp.Move, null);
    }
}
```

---

## 可覆盖成员（真实存在）

### MonsterModel

| 成员 | 类型 | 说明 |
|------|------|------|
| `MinInitialHp` / `MaxInitialHp` | `public abstract int` | 初始血量范围 |
| `IsHealthBarVisible` | `public virtual bool` | 是否显示血条（宠物 false） |
| `TakeDamageSfxType` | `public virtual DamageSfxType` | 受击音效类型 |
| `AfterAddedToRoom()` | `public virtual Task` | 入场后 |
| `AfterDeath(...)` | `public virtual Task` | 死亡后 |
| `GenerateMoveStateMachine()` | `protected abstract` | AI 状态机 |

### EncounterModel

| 成员 | 类型 | 说明 |
|------|------|------|
| `RoomType` | `public abstract RoomType` | 房间类型 |
| `AllPossibleMonsters` | `public abstract IEnumerable<MonsterModel>` | 可出现的怪物 |
| `Slots` | `public virtual IReadOnlyList<string>` | 站位 ID 列表（默认空） |
| `HasScene` | `public virtual bool` | 是否有自定义场景 |
| `IsWeak` | `public virtual bool` | 是否为弱点遭遇 |
| `ShouldGiveRewards` | `public virtual bool` | 是否给奖励 |
| `MinGoldReward` / `MaxGoldReward` | `public virtual int` | 金币奖励区间 |
| `Tags` | `public virtual IEnumerable<EncounterTag>` | 遭遇标签 |
| `CustomBgm` / `AmbientSfx` | `public virtual string` | 自定义 BGM / 环境音效 |
| `HasCustomBackground` | `protected virtual bool` | 自定义背景 |

> ⚠️ 旧版写的 `IsValidForAct`/`CustomScenePath`/`CustomEncounterBackground`/`RunHistoryIconPath` 是 **BaseLib `CustomEncounterModel`** 的，原生不存在。章节有效性由章节配置控制。

