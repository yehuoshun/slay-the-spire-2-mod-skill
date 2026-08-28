# 自定义敌怪：状态机与 AI 行为树

## MonsterMoveStateMachine

```csharp
new MonsterMoveStateMachine(
    IEnumerable<MonsterState> states,  // 状态节点列表
    MonsterState initialState          // 初始状态对象（不是字符串！）
)
```

## MoveState

```csharp
new MoveState(
    string stateId,                                  // 状态唯一 ID
    Func<IReadOnlyList<Creature>, Task> onPerform,   // 执行回调（目标生物列表）
    params AbstractIntent[] intents                  // 意图（Intent 实例，不是枚举！）
)
```

## 意图（Intent 类，非枚举）

> `MoveState` 第三参是 `params AbstractIntent[]`，传**意图实例**；`IntentType` 枚举仅用于内部判断。

| Intent 类 | 说明 |
|-----------|------|
| `new SingleAttackIntent(decimal dmg)` | 单体攻击 |
| `new MultiAttackIntent(decimal dmg, int hits)` | 多段攻击 |
| `new AttackIntent()` | 攻击（通用） |
| `new DefendIntent()` | 防御 |
| `new BuffIntent()` | 增益 |
| `new DebuffIntent()` | 减益 |
| `new SleepIntent()` / `new StunIntent()` | 睡眠 / 眩晕 |
| `new HealIntent()` / `new SummonIntent()` / `new EscapeIntent()` / `new DeathBlowIntent()` | 治疗 / 召唤 / 逃跑 / 终结 |
| `new StatusIntent()` / `new CardDebuffIntent()` | 状态牌 / 诅咒牌 |
| `new UnknownIntent()` / `new HiddenIntent()` | 未知 / 隐藏 |

---

## AI 行为树

### 顺序循环（FollowUpState 链）

```csharp
attackState.FollowUpState = buffState;
buffState.FollowUpState = attackState;
```

### 随机分支（RandomBranchState）

```csharp
var randomBranch = new RandomBranchState("RANDOM");
// 不可重复：cooldown 2，权重 10
randomBranch.AddBranch(attackState, cooldown: 2, MoveRepeatType.CannotRepeat, () => 10f);
// 最多重复 1 次：cooldown 3，权重 5（maxRepeats 重载）
randomBranch.AddBranch(buffState, cooldown: 3, maxRepeats: 1, () => 5f);
```

### 条件分支（ConditionalBranchState）

```csharp
var conditionalBranch = new ConditionalBranchState("CONDITIONAL");
conditionalBranch.AddState(bigAttack, () => PlayerHpLow());   // Func<bool> 无参条件
conditionalBranch.AddState(normalAttack, () => true);
```

> `MoveRepeatType`：`CanRepeatForever` / `CanRepeatXTimes`（用 `maxRepeats` 重载）/ `CannotRepeat` / `UseOnlyOnce`

