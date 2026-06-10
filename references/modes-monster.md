# 模式九：自定义怪物 & 遭遇

## 怪物类

### 最简示例（两个状态循环）

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves;

namespace MyMod.Monsters;

public class GoblinMonster : MonsterModel
{
    protected override IEnumerable<MoveState> GenerateMoveStateMachine()
    {
        var attack = new MoveState(
            "attack",
            new[] { MonsterIntent.Attack },
            async move => { /* 执行攻击逻辑 */ },
            followUpStateId: "defend"
        );

        var defend = new MoveState(
            "defend",
            new[] { MonsterIntent.Defend },
            async move => { /* 执行防御逻辑 */ },
            followUpStateId: "attack"
        );

        return new[] { attack, defend };
    }
}
```

### MoveState 构造函数

```csharp
new MoveState(
    stateId,           // 唯一标识（字符串）
    intents,           // 意图类型数组 MonsterIntent[]
    onPerform,         // 异步回调 Func<MoveState, Task>
    followUpStateId    // 下一状态 ID
)
```

### MonsterIntent 常用值

`Attack` / `Defend` / `Buff` / `Debuff` / `StrongAttack` / `Charge` / `Summon` / `Sleep`

## 随机分支 AI

```csharp
protected override IEnumerable<MoveState> GenerateMoveStateMachine()
{
    var randomBranch = new RandomBranchState("branch");
    var attack = new MoveState("attack", ..., "branch");  // 执行完回到分支
    var buff = new MoveState("buff", ..., "branch");
    var defend = new MoveState("defend", ..., "branch");

    // AddBranch(状态, 冷却回合, 重复类型, 权重)
    randomBranch.AddBranch(attack, cooldown: 2, repeatType: None, weight: 3);
    randomBranch.AddBranch(buff, cooldown: 1, repeatType: None, weight: 1);
    randomBranch.AddBranch(defend, cooldown: 2, repeatType: None, weight: 2);

    return new MoveState[] { randomBranch };
}
```

## 条件分支

```csharp
var condBranch = new ConditionalBranchState("conditional");
condBranch.AddBranch(
    state => state.GetPlayerHp() <= 10,
    bigAttack,    // 玩家血量 ≤ 10 时
    normalAttack  // 否则
);
```

## 怪物资源

| 资源 | 路径 |
|------|------|
| 待机动画场景 | `scenes/creature_visuals/<怪物ID小写>.tscn` |

### 场景搭建规则

- 根节点挂载 `NCreatureVisuals` 脚本（或继承类）
- 包含 `%Bounds` 节点（定义选择框、血条长度、意图图标位置）

### 怪物音效路径

```
攻击：event:/sfx/enemy/enemy_attacks/<怪物ID小写>/<怪物ID小写>_attack
施法：event:/sfx/enemy/enemy_attacks/<怪物ID小写>/<怪物ID小写>_cast
死亡：event:/sfx/enemy/enemy_attacks/<怪物ID小写>/<怪物ID小写>_die
```

## 怪物本地化

```json
// assets/localization/zhs/monsters.json
{
  "GOBLIN_MONSTER": {
    "name": "哥布林",
    "moves": {
      "attack": { "title": "攻击" },
      "defend": { "title": "防御" }
    }
  }
}
```

---

## 遭遇类

### 最简示例

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace MyMod.Encounters;

public class GoblinEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;
    public override IEnumerable<MonsterModel> AllPossibleMonsters => new[] { new GoblinMonster() };

    public override IEnumerable<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.GetById<MonsterModel>(ModelDb.GetId<GoblinMonster>()).ToMutable(), null),
        };
    }
}
```

### RoomType 枚举

| 值 | 说明 |
|----|------|
| `Monster` | 普通战斗 |
| `Elite` | 精英战斗 |
| `Boss` | Boss 战斗 |
| `Event` | 事件 |
| `Shop` | 商店 |
| `Rest` | 休息处 |
| `Treasure` | 宝箱 |

### GenerateMonsters 返回值

元组 `(MonsterModel, string?)`：
- 第一个元素：怪物实体（通过 `ModelDb.GetById().ToMutable()` 获取）
- 第二个元素：站位 ID（对应遭遇场景中的 `Marker2D` 节点名），`null` 为默认站位

### 自定义站位场景

需要创建 `scenes/encounters/<遭遇ID小写>.tscn`，在其中放置 `Marker2D` 节点标记怪物站位。

---

## 注册遭遇

```csharp
[HarmonyPatch(typeof(Overgrowth), nameof(Overgrowth.GenerateAllEncounters))]
[HarmonyPostfix]
static void Postfix(ref IEnumerable<EncounterModel> __result)
{
    __result = __result.Append(new GoblinEncounter());
}
```

## 遭遇本地化

```json
// assets/localization/zhs/encounters.json
{
  "GOBLIN_ENCOUNTER": {
    "title": "哥布林遭遇"
  }
}
```

## 控制台测试命令

```
# 触发指定遭遇
trigger_encounter <遭遇ID>
```