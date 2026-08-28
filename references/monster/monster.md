# 自定义敌怪 / 遭遇

> 参考：[杀戮尖塔2模组开发教程09 - 自定义敌怪 - 哔哩哔哩](https://www.bilibili.com/opus/1183380755590414377)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomEncounterModel.cs` + `CustomMonsterModel.cs`

---

## 概述

自定义敌人需要两个类：

1. **MonsterModel** — 怪物本身（血量、AI 状态机、资源）
2. **EncounterModel** — 遭遇（怪物池、站位、奖励档位）

---

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

---

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

---

## 怪物资源

```
待机动画场景：res://scenes/creature_visuals/<怪物ID小写>.tscn
```

场景结构：
```
NCreatureVisuals (根节点)
  ├─ Visuals (Node2D, 唯一名称 %)
  └─ Bounds (Node2D, 唯一名称 %) — 选择框/血条/意图位置
```

### 音效

```
攻击音效：event:/sfx/enemy/enemy_attacks/<ID小写>/<ID小写>_attack
施法音效：event:/sfx/enemy/enemy_attacks/<ID小写>/<ID小写>_cast
死亡音效：event:/sfx/enemy/enemy_attacks/<ID小写>/<ID小写>_die
```

---

## 本地化

路径：`res://<模组ID>/localization/<语言代码>/monsters.json`

```json
{
  "MY_MONSTER": {
    "name": "自定义怪物",
    "moves": {
      "ATTACK": { "title": "准备攻击" },
      "BUFF": { "title": "正在蓄力" }
    }
  }
}
```

---

## 遭遇类

```csharp
public class MyEncounter : EncounterModel
{
    // 站位 ID（GenerateMonsters 里引用）
    public override IReadOnlyList<string> Slots => new List<string> { "front" };
    public override bool HasScene => false;
    public override RoomType RoomType => RoomType.Monster;
    public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel> { ModelDb.Monster<MyMonster>() };

    // protected，返回 (怪物, 站位ID) 列表
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<MyMonster>().ToMutable(), "front"),
        };
    }
}
```

### RoomType（真实枚举）

`Monster` / `Elite` / `Boss` / `Treasure` / `Shop` / `Event` / `RestSite` / `Map`（外加 `Unassigned`）

> ⚠️ 旧版写的 `BossChest`/`Ancient` 不存在。

### 自定义站位

- `Slots` 属性声明站位 ID 列表
- `GenerateMonsters()` 返回 `(MonsterModel, string?)`，第二元素为站位 ID
- 自定义场景时 `HasScene = true`，场景资源按命名约定 `res://scenes/encounters/<遭遇ID小写>.tscn`，节点名 = 站位 ID

---

## 添加遭遇

```csharp
[HarmonyPatch(typeof(Overgrowth), nameof(Overgrowth.GenerateAllEncounters))]
public static class OvergrowthEncountersPatch
{
    public static void Postfix(ref IEnumerable<EncounterModel> __result)
    {
        __result = __result.Append(new MyEncounter());
    }
}
```

> 注意：`GenerateAllEncounters()` 真实返回 `IEnumerable<EncounterModel>`（不是 `List`），且是**实例方法**不是 getter，Patch 不要写 `MethodType.Getter`。

---

## 调试

```
encounter <遭遇ID>
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 怪物不攻击 | 检查 `GenerateMoveStateMachine`（protected + 初始状态对象） |
| 意图不显示 | `MoveState` 第三参传 Intent 实例（`new SingleAttackIntent(dmg)`） |
| 随机分支不生效 | `AddBranch(state, cooldown, MoveRepeatType, () => weight)` |
| 条件分支不触发 | `AddState(move, () => condition)`，按顺序判断，先精确后默认 |
| 遭遇不出现 | 检查 `GenerateAllEncounters` Patch（ref IEnumerable，非 getter） |
| 遭遇只在特定章节 | 章节有效性由章节配置控制（`IsValidForAct` 是 BaseLib 的） |
| 怪物不显示血条 | `IsHealthBarVisible = false`（宠物） |

---

## 进阶：纯原生注册辅助

> 从 BaseLib 提炼，零第三方依赖。遭遇走 Patch 注入，用 Attribute 标记 + 反射统一收集。

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RegisteredEncounterAttribute : Attribute { }

[HarmonyPatch(typeof(Overgrowth), nameof(Overgrowth.GenerateAllEncounters))]
public static class EncountersPatch
{
    private static void Postfix(ref IEnumerable<EncounterModel> __result)
    {
        var registered = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<RegisteredEncounterAttribute>() != null)
            .Select(t => (EncounterModel)Activator.CreateInstance(t)!);
        __result = __result.Concat(registered);
    }
}
```

```csharp
[RegisteredEncounter]
public class MyEncounter : EncounterModel { ... }   // 自动注册
```

## 演进路线

- 当前：手动 Patch 注入（保留为基准写法）
- 纯原生自动注册（Attribute 标记 + 反射统一注入）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`MoveState(Func<PlayerChoiceContext,Task>, List<IntentType>)`、`MonsterMoveStateMachine(字符串)`、`public GenerateMonsters`、`IsValidForAct/CustomScenePath`、`RepeatType`、`ref List` Patch）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖