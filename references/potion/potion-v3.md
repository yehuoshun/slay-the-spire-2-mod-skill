# 自定义药水

> 参考：[杀戮尖塔2模组开发教程04 - 自定义药水 - 哔哩哔哩](https://www.bilibili.com/opus/1180032536494997541)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomPotionModel.cs`

---

> v3：API 全量校正（对照真实 PotionModel：Rarity/Usage/TargetType 属性化、OnUse 签名、CreatureCmd.Damage、图标命名约定）。v2 为存档，勿直接使用。
## 概述

所有药水继承 `PotionModel` 抽象类。`Rarity`/`Usage`/`TargetType` 是**抽象属性**（必须 override），使用效果在 `OnUse` 回调实现。

---

## 基础药水模板

```csharp
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.ValueProps;

public class MyAoePotion : PotionModel
{
    // 抽象属性，必须 override（不能构造函数赋值）
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    // 动态变量（数值与描述同步）
    protected override IEnumerable<DynamicVar> CanonicalVars => [ new DamageVar(30m, ValueProp.Move) ];

    // 使用药水：对所有可攻击敌人造成 30 点伤害
    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var enemies = Owner.Creature.CombatState.HittableEnemies.ToList();
        await CreatureCmd.Damage(choiceContext, enemies, DynamicVars.Damage.BaseValue, ValueProp.Move, Owner.Creature);
    }
}
```

---

## 图标（命名约定，不可 override）

药水图标路径由原生 `PotionModel` **私有自动生成**（不可 override），规则如下：

```
药水裁切纹理：     atlases/potion_atlas.sprites/<药水ID小写>.tres
药水描边裁切纹理： atlases/potion_outline_atlas.sprites/<药水ID小写>.tres
```

只需把对应 `.tres`/`.png` 资源放到上述路径即可，无需写代码。

---

## 属性

### Rarity — 稀有度

| 值 | 说明 |
|-----|------|
| `PotionRarity.None` | 无 |
| `PotionRarity.Common` | 普通，随机药水池可获取 |
| `PotionRarity.Uncommon` | 罕见，随机药水池可获取 |
| `PotionRarity.Rare` | 稀有，随机药水池可获取 |
| `PotionRarity.Event` | 事件专属 |
| `PotionRarity.Token` | 衍生物 |

### Usage — 使用时机

| 值 | 说明 |
|-----|------|
| `PotionUsage.None` | 无 |
| `PotionUsage.CombatOnly` | 仅战斗场景可用，非战斗时禁用 |
| `PotionUsage.AnyTime` | 任意场景可用（包括战斗外） |
| `PotionUsage.Automatic` | 不能主动触发，只能通过代码自动触发使用 |

---

## 回调

### OnUse — 使用药水时触发

```csharp
protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `choiceContext` | `PlayerChoiceContext` | 玩家选择上下文 |
| `target` | `Creature?` | 使用目标（TargetType 决定是否有目标；无目标时为 null） |

单目标药水先做空值检查（真实药水风格）：

```csharp
protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
{
    PotionModel.AssertValidForTargetedPotion(target);
    await CreatureCmd.Heal(target, 10);
}
```

### 其他可覆盖成员（原生真实存在）

| 成员 | 类型 | 说明 |
|------|------|------|
| `CanBeGeneratedInCombat` | `public virtual bool => true` | 是否可被随机生成（炼制药水等） |
| `PassesCustomUsabilityCheck` | `public virtual bool => true` | 自定义可用性检查 |
| `ExtraHoverTips` | `public virtual IEnumerable<IHoverTip>` | 额外悬停提示 |

```csharp
public override bool CanBeGeneratedInCombat => false;  // 阻止随机生成
```

---

## 添加药水到药水池

```csharp
// 方式②：手动注册（硬规则4-②；方式① attribute 自动注册见 design-patterns 模式1）
ModHelper.AddModelToPool(typeof(SharedPotionPool), typeof(MyAoePotion));
```

### 药水池类型

| 池 | 说明 |
|-----|------|
| `SharedPotionPool` | 共享药水池，所有角色都可获取 |
| `IroncladPotionPool` | 铁甲战士专属 |
| `SilentPotionPool` | 静默猎手专属 |
| `DefectPotionPool` | 故障机器人专属 |
| `NecrobinderPotionPool` | 亡灵契约师专属 |
| `RegentPotionPool` | 摄政者专属 |

> 完整池列表见源码 `Core/Models/PotionPools/`（另有 Event/Token/Deprecated 等边界池）。

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
| 图标不显示 | 检查 atlas 命名约定：`atlases/potion_atlas.sprites/<药水ID小写>.tres` |
| 本地化不生效 | 必须 Publish 而非 Build |
| 药水无法使用 | 检查 `Usage` 属性是否匹配当前场景 |
| 自动药水不触发 | `Usage = PotionUsage.Automatic`，只能代码触发 |
| 随机生成不想要 | 重写 `CanBeGeneratedInCombat` 返回 `false` |

---

## 进阶：纯原生自动注册（v2 延续）

> 从 BaseLib 提炼，零第三方依赖。仿照 design-patterns 模式1 的 `CardPoolAttribute`/`RelicPoolAttribute`，自定义 `PotionPoolAttribute` + ContentRegistry 反射扫描，免手动注册。框架完整代码见 [serialization.md](../serialization/serialization.md)「进阶：纯原生自动注册框架」。

```csharp
[PotionPool(typeof(SharedPotionPool))]   // 标记进哪个药水池（attribute 需仿照 design-patterns 模式1 自定义）
public class MyAoePotion : PotionModel { ... }
```

## 演进路线

- 当前：手动注册（保留为基准写法）
- v2 更优：**纯原生自动注册**（见上方「进阶」章节）——Attribute 标记 + ContentRegistry / 反射注册辅助，免手动注册
- v3（本版）：API 全量校正，弃用 v2 的错误签名（PotionTarget/ShouldDie/DamageCmd.FromCard）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖