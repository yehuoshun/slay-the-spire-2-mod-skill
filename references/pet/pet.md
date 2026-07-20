# 自定义宠物（Pet）

> 参考：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomPetModel.cs`

---

## 概述

宠物（召唤物）是跟随玩家的生物（如亡灵契约师的奥斯提）。继承 `MonsterModel` 但**不生成 AI 状态机**——固定无行动，所有行为由召唤者的卡牌/能力控制。

宠物与怪物的区别：

| | 怪物 | 宠物 |
|------|------|------|
| AI 状态机 | 自定义行为树 | 固定不行动 |
| 血条 | 显示 | 可选隐藏 |
| 阵营 | 敌方 | 己方 |
| 攻击 | 攻击玩家 | 不攻击 |

---

## 基础模板

```csharp
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Combat;
using Godot;

public class MyPet : MonsterModel
{
    // 宠物不显示血条（可选）
    public override bool IsHealthBarVisible => false;

    // 宠物固定不行动
    public override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var nothingState = new MoveState("NOTHING",
            _ => Task.CompletedTask,
            new List<IntentType>());
        nothingState.FollowUpState = nothingState;
        return new MonsterMoveStateMachine(
            new List<MoveState> { nothingState },
            nothingState
        );
    }
}
```

---

## 可覆盖属性

```csharp
public class MyPet : MonsterModel
{
    // 是否显示血条（宠物通常隐藏）
    public override bool IsHealthBarVisible => false;

    // 动画场景（与怪物相同规则）
    // res://scenes/creature_visuals/<ID小写>.tscn
}
```

### 属性说明

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `IsHealthBarVisible` | `true` | 是否显示血条（宠物设为 false） |
| `GenerateMoveStateMachine()` | - | 返回固定不行动的 AI 状态机 |

---

## 宠物场景

```
待机动画场景：res://scenes/creature_visuals/<宠物ID小写>.tscn
```

场景结构（与怪物/角色一致）：
```
NCreatureVisuals (根节点)
  └─ Visuals (Node2D，唯一名称 %)
```

---

## 注册

```csharp
// 手动注册
ModelDb.Inject(typeof(MyPet));
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 宠物乱动 | 检查 `GenerateMoveStateMachine` 是否返回固定状态机 |
| 血条显示 | 将 `IsHealthBarVisible` 设为 false |
| 场景不显示 | 检查 `res://scenes/creature_visuals/` 路径 |
| 宠物不跟随 | 需要通过卡牌/能力/事件代码召唤 |

---

## 演进路线

- 当前：手动继承 `MonsterModel` 并重写状态机
- BaseLib：`CustomPetModel` 即写即用，构造函数自带 `IsHealthBarVisible` 参数