# 自定义球体（Orb）

> 参考：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomOrbModel.cs`

---

## 概述

球体是故障机器人（以及部分 Mod 角色）的充能球系统。继承 `OrbModel` 抽象类。每个球体有**被动效果**（每回合触发）和**激发效果**（释放时触发）。

---

## 基础模板

```csharp
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;
using Godot;

public class MyOrb : OrbModel
{
    // 球体图标路径
    public override string? IconPath => "res://MyMod/images/orbs/my_orb.png";

    // 球体精灵图路径
    public override string? SpritePath => "res://MyMod/images/orbs/my_orb_sprite.png";

    // 是否包含在随机球池（如混沌）
    public override bool IncludeInRandomPool => false;

    // 被动效果（每回合触发）
    public override async Task Passive(CombatState state)
    {
        // 每回合触发效果
    }

    // 激发效果（释放时触发）
    public override async Task Evoke(CombatState state)
    {
        // 释放时触发的效果
    }
}
```

---

## 可覆盖属性

```csharp
public class MyOrb : OrbModel
{
    // 球体图标路径（UI 显示）
    public override string? IconPath => "res://MyMod/images/orbs/my_orb.png";

    // 球体精灵图路径（场景中显示）
    public override string? SpritePath => "res://MyMod/images/orbs/my_orb_sprite.png";

    // 是否包含在随机球池（如混沌/随机生成）
    public override bool IncludeInRandomPool => false;

    // 自定义球体精灵（返回 Node2D，优先级高于 SpritePath）
    public override Node2D? CreateSprite() => null;

    // 音效
    protected override string PassiveSfx => "";   // 被动触发音效
    protected override string EvokeSfx => "";     // 激发音效
    protected override string ChannelSfx => "";   // 充能音效
}
```

### 属性说明

| 属性 | 类型 | 说明 |
|------|------|------|
| `IconPath` | `string?` | 球体图标路径（UI 列表显示） |
| `SpritePath` | `string?` | 球体精灵图路径（场景中渲染） |
| `IncludeInRandomPool` | `bool` | 是否出现在随机球池 |
| `CreateSprite()` | `Node2D?` | 自定义创建精灵节点 |
| `PassiveSfx` | `string` | 被动触发音效 |
| `EvokeSfx` | `string` | 激发音效 |
| `ChannelSfx` | `string` | 充能音效 |

---

## 回调

| 回调 | 触发时机 | 签名 |
|------|---------|------|
| `Passive(CombatState)` | 每回合结束时触发 | `Task Passive(CombatState)` |
| `Evoke(CombatState)` | 球体被释放时触发 | `Task Evoke(CombatState)` |
| `OnChannel(CombatState)` | 球体被充能时触发 | `Task OnChannel(CombatState)` |

---

## 完整示例：雷电球

```csharp
public class LightningOrb : OrbModel
{
    public override string? IconPath => "res://images/orbs/lightning.png";
    public override bool IncludeInRandomPool => true;

    // 被动：对随机敌人造成 3 点伤害
    public override async Task Passive(CombatState state)
    {
        var target = state.Enemies.Where(e => e.IsAlive).Random();
        if (target != null)
        {
            await DamageCmd.Attack(3)
                .FromCard(this)
                .Targeting(target)
                .Execute(state);
        }
    }

    // 激发：对随机敌人造成 8 点伤害
    public override async Task Evoke(CombatState state)
    {
        var target = state.Enemies.Where(e => e.IsAlive).Random();
        if (target != null)
        {
            await DamageCmd.Attack(8)
                .FromCard(this)
                .Targeting(target)
                .Execute(state);
        }
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

路径：`res://<模组ID>/localization/<语言代码>/orbs.json`

```json
{
  "MyOrb": {
    "name": "自定义球体",
    "description": "被动：每回合造成 {D} 点伤害。激发：造成 {D} 点伤害。"
  }
}
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 球体不显示 | 检查 `IconPath` / `SpritePath` 路径 |
| 随机池不出现 | 设置 `IncludeInRandomPool = true` |
| 被动不触发 | 重写 `Passive(CombatState)` 回调 |
| 激发无效果 | 重写 `Evoke(CombatState)` 回调 |
| 音效不播放 | 检查音效路径 |
| 自定义精灵不生效 | 重写 `CreateSprite()` 返回 Node2D |

---

## 演进路线

- 当前：手动注册 + 基础回调
- BaseLib：`CustomOrbModel` 带自动注册 + 随机池注入 Patch