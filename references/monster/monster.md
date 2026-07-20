# 自定义敌怪 / 遭遇

> 参考：[杀戮尖塔2模组开发教程09 - 自定义敌怪 - 哔哩哔哩](https://www.bilibili.com/opus/1183380755590414377)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomEncounterModel.cs` + `CustomMonsterModel.cs`

---

## 概述

自定义敌人需要两个类：

1. **MonsterModel** — 怪物本身（属性、AI 状态机、资源）
2. **EncounterModel** — 遭遇（怪物池、站位、奖励档位）

---

## 怪物类

```csharp
public class MyMonster : MonsterModel
{
    public override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        return new MonsterMoveStateMachine(
            new List<MoveState> { /* 状态节点列表 */ },
            "ATTACK_MOVE"
        );
    }
}
```

---

## 可覆盖属性（新增）

### MonsterModel

```csharp
public class MyMonster : MonsterModel
{
    // 是否显示血条（宠物设为 false）
    public override bool IsHealthBarVisible => true;
}
```

### EncounterModel

```csharp
public class MyEncounter : EncounterModel
{
    // 判断该遭遇是否在指定章节有效
    public override bool IsValidForAct(int act) => true;

    // 自定义遭遇场景路径
    public override string? CustomScenePath => null;

    // 自定义遭遇背景
    public override Texture2D? CustomEncounterBackground() => null;

    // 自定义运行历史图标
    public override string? RunHistoryIconPath => null;
}
```

### 属性说明

| 属性 | 所属 | 说明 |
|------|------|------|
| `IsHealthBarVisible` | MonsterModel | 是否显示血条 |
| `IsValidForAct(int)` | EncounterModel | 是否在指定章节有效 |
| `CustomScenePath` | EncounterModel | 自定义遭遇场景路径 |
| `CustomEncounterBackground()` | EncounterModel | 自定义遭遇背景 |
| `RunHistoryIconPath` | EncounterModel | 运行历史图标 |

---

## MonsterMoveStateMachine

```csharp
new MonsterMoveStateMachine(
    List<MoveState> states,   // 状态节点列表
    string initialStateId     // 初始状态 ID
)
```

## MoveState

```csharp
new MoveState(
    string stateId,                             // 状态唯一 ID
    Func<PlayerChoiceContext, Task> onPerform,  // 执行回调
    List<IntentType> intents                    // 意图类型列表
)
```

### 常用意图（IntentType）

Attack / AttackBuff / AttackDebuff / Buff / Debuff / Defend / DefendBuff / Sleep / Stun / Unknown / Trigger

---

## AI 行为树

### 顺序循环

```csharp
attackState.FollowUpState = buffState;
buffState.FollowUpState = attackState;
```

### 随机分支

```csharp
var randomBranch = new RandomBranchState("RANDOM");
randomBranch.AddBranch(attackState, cooldown: 2, RepeatType.NoRepeat, weight: 10);
randomBranch.AddBranch(buffState, cooldown: 3, RepeatType.MaxRepeats(1), weight: 5);
```

### 条件分支

```csharp
var conditionalBranch = new ConditionalBranchState("CONDITIONAL");
conditionalBranch.AddBranch(bigAttack, (ctx) => ctx.Player.Health <= 10);
conditionalBranch.AddBranch(normalAttack, (ctx) => true);
```

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
    public override RoomType RoomType => RoomType.Monster;
    public override List<MonsterModel> AllPossibleMonsters => [ new MyMonster() ];

    public override (MonsterModel, string?)[] GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Get<MyMonster>().ToMutable(), null),
        };
    }
}
```

### RoomType

Monster / Elite / Boss / Event / Shop / Rest / Treasure / BossChest / Ancient

### 自定义站位

```
res://scenes/encounters/<遭遇ID小写>.tscn
```
场景中的 `Marker2D` 节点名 = 站位 ID。

---

## 添加遭遇

```csharp
[HarmonyPatch(typeof(Overgrowth), "GenerateAllEncounters")]
public static class OvergrowthEncountersPatch
{
    public static void Postfix(ref List<EncounterModel> __result)
    {
        __result.Add(new MyEncounter());
    }
}
```

---

## 调试

```
encounter <遭遇ID>
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 怪物不攻击 | 检查 `GenerateMoveStateMachine` |
| 意图不显示 | 检查 `MoveState` 的 `intents` |
| 随机分支不生效 | 检查权重/冷却配置 |
| 条件分支不触发 | 按顺序判断，先精确后默认 |
| 遭遇不出现 | 检查 `GenerateAllEncounters` Patch |
| 遭遇只在特定章节 | 重写 `IsValidForAct(int)` |
| 怪物不显示血条 | 宠物重写 `IsHealthBarVisible` 为 false |

---

## 演进路线

- 当前：手动 Patch `GenerateAllEncounters`
- 更优：封装反射注册辅助方法
- BaseLib：`CustomEncounterModel` 带自动注册 + `CustomMonsterModel` 带场景转换