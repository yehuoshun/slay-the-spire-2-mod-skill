# 自定义药水

> 参考：[杀戮尖塔2模组开发教程04 - 自定义药水 - 哔哩哔哩](https://www.bilibili.com/opus/1180032536494997541)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomPotionModel.cs`

---

## 概述

所有药水继承 `PotionModel` 抽象类。通过构造函数定义基础属性，通过回调实现使用效果。

---

## 基础药水模板

```csharp
using System.Threading.Tasks;
using STS2;
using STS2.Potions;
using STS2.Core.Cards;
using STS2.Combat;
using Godot;

public class MyAoePotion : PotionModel
{
    public MyAoePotion()
    {
        Rarity = PotionRarity.Uncommon;
        Usage = PotionUsage.CombatOnly;
    }

    public override Task OnUse(PlayerChoiceContext context, PotionTarget target)
    {
        return DamageCmd.Attack(30)
            .FromCard(this)
            .TargetingAllOpponents(context.CombatState)
            .Execute(context.CombatState);
    }
}
```

---

## 可覆盖属性（新增）

```csharp
public class MyPotion : PotionModel
{
    // 药水裁切图标路径
    public override string? PackedImagePath =>
        "res://MyMod/images/atlases/potion_atlas.sprites/my_potion.tres";

    // 药水描边裁切路径
    public override string? PackedOutlinePath =>
        "res://MyMod/images/atlases/potion_outline_atlas.sprites/my_potion.tres";
}
```

### 属性说明

| 属性 | 类型 | 说明 |
|------|------|------|
| `PackedImagePath` | `string?` | 药水裁切图标路径（atlas） |
| `PackedOutlinePath` | `string?` | 药水描边裁切路径（未发现时显示） |

---

## 属性

### Rarity — 稀有度

| 值 | 说明 |
|-----|------|
| `PotionRarity.Common` | 普通，随机药水池可获取 |
| `PotionRarity.Uncommon` | 罕见，随机药水池可获取 |
| `PotionRarity.Rare` | 稀有，随机药水池可获取 |

### Usage — 使用时机

| 值 | 说明 |
|-----|------|
| `PotionUsage.AnyTime` | 任意场景可用（包括战斗外） |
| `PotionUsage.CombatOnly` | 仅战斗场景可用，非战斗时禁用 |
| `PotionUsage.Passive` | 不能主动触发，只能通过代码触发使用 |

---

## 回调

### OnUse — 使用药水时触发

```csharp
public override Task OnUse(PlayerChoiceContext context, PotionTarget target)
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `context` | `PlayerChoiceContext` | 玩家选择上下文 |
| `target` | `PotionTarget` | 药水使用目标 |

### ShouldDie — 阻止死亡

```csharp
public override async Task<bool> ShouldDie(PlayerChoiceContext context, Creature creature)
{
    return false; // 返回 false 阻止死亡
}
```

### AfterPreventingDeath — 阻止死亡后触发

```csharp
public override async Task AfterPreventingDeath(PlayerChoiceContext context, Creature creature)
{
    // 消耗药水 + 回复生命
}
```

### CanBeGeneratedInCombat — 是否可被随机生成

```csharp
public override bool CanBeGeneratedInCombat => false;
```

返回 `false` 时，炼制药水等随机获取不会生成此药水。

---

## 添加药水到药水池

```csharp
// 手动注册
ModHelper.AddModelToPool(typeof(MyAoePotion), PotionPools.Shared);
```

### 药水池类型

| 池 | 说明 |
|-----|------|
| `PotionPools.Shared` | 共享药水池，所有角色都可获取 |
| `PotionPools.Ironclad` | 铁甲战士专属 |
| `PotionPools.Silent` | 静默猎手专属 |
| `PotionPools.Defect` | 故障机器人专属 |
| `PotionPools.Watcher` | 观者专属 |
| `PotionPools.Necrobinder` | 亡灵契约师专属 |
| `PotionPools.Regent` | 摄政者专属 |
| `PotionPools.Amnesiac` | 迷失者专属 |

---

## 自定义药水图标

```
药水裁切纹理：     res://images/atlases/potion_atlas.sprites/<药水ID小写>.tres
药水描边裁切纹理： res://images/atlases/potion_outline_atlas.sprites/<药水ID小写>.tres
药水大图纹理：     res://images/potions/<药水ID小写>.png
```

添加描边纹理后，未发现的药水会显示描边效果。

---

## 药水本地化

路径：`res://<模组ID>/localization/<语言代码>/potions.json`

```json
{
  "MyAoePotion": {
    "name": "范围伤害药水",
    "description": "对所有敌人造成 30 点伤害。"
  }
}
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 药水不显示 | 检查手动注册或 Attribute 是否生效 |
| 图标不显示 | 检查 `PackedImagePath` / `PackedOutlinePath` 路径 |
| 本地化不生效 | 必须 Publish 而非 Build |
| 药水无法使用 | 检查 `Usage` 属性是否匹配当前场景 |
| 被动药水自动触发 | 在 `ShouldDie`/`AfterPreventingDeath` 中处理 |
| 随机生成不想要 | 重写 `CanBeGeneratedInCombat` 返回 `false` |

---

## 演进路线

- 当前写法：构造函数直接设置属性
- 更优方案：可参考 YuWanCard 的 Builder 模式 + 反射一键注册药水
- BaseLib：`CustomPotionModel` 构造函数自带 `autoAdd` 参数