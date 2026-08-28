# 自定义能力（Buff / Debuff）

> 参考：[杀戮尖塔2模组开发教程07 - 自定义能力（Buff） - 哔哩哔哩](https://www.bilibili.com/opus/1181126133981118470)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomPowerModel.cs`

---

## 概述

所有能力/Buff/Debuff 继承 `PowerModel` 抽象类。`Type`/`StackType` 是**抽象属性**（必须 override），行为通过真实回调（`ModifyDamageAdditive` 等 + AbstractModel 的 `After*` 钩子）实现。

---

## 基础模板

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

public class MyPower : PowerModel
{
    // 抽象属性（必须 override，不能构造函数赋值）
    public override PowerType Type => PowerType.Buff;           // Buff / Debuff / None
    public override PowerStackType StackType => PowerStackType.Counter; // Counter=有层数数值型 / Single=单状态

    // 可选覆盖
    public override PowerInstanceType InstanceType => PowerInstanceType.None; // Instanced=重复施加创建独立实例（如炸弹）
    public override bool AllowNegative => false;               // true=层数可为负（力量/敏捷）

    // 示例①：打出任意牌时 +Amount 格挡
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, cardPlay);
    }

    // 示例②：力量式伤害修正（持有者造成攻击伤害 +Amount）
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner != dealer) return 0m;
        return props.IsPoweredAttack() ? Amount : 0m;
    }
}
```

---

## 图标（命名约定，不可 override）

图标路径由原生 `PowerModel` 自动生成（`PackedIconPath` 非 virtual、`BigIconPath`/`BigBetaIconPath` private，均不可 override），规则：

```
战斗裁切纹理：res://images/atlases/power_atlas.sprites/<能力ID小写>.tres
大图纹理：    res://images/powers/<能力ID小写>.png
Beta 大图：   res://images/powers/beta/<能力ID小写>.png
```

推荐分辨率：256x256（大图），64x64（裁切）

**原生自动回退**：大图缺失时 `ResolvedBigIconPath` 自动回退 `BigIconPath → BigBetaIconPath → MissingIconPath`，无需写代码。

---

## 核心属性

### PowerType — 类型

| 值 | 说明 |
|-----|------|
| `PowerType.None` | 无 |
| `PowerType.Buff` | Buff（增益） |
| `PowerType.Debuff` | Debuff（减益） |

### PowerStackType — 叠加方式（真实枚举）

| 值 | 说明 |
|-----|------|
| `PowerStackType.None` | 无 |
| `PowerStackType.Counter` | 有层数的数值型（如中毒/易伤/力量），需手动增减 |
| `PowerStackType.Single` | 单状态（Amount 隐藏恒为 1，如夹击/壁垒） |

### InstanceType — 是否独立实例（替代旧版 IsInstanced）

| 值 | 说明 |
|-----|------|
| `PowerInstanceType.None`（默认） | 重复施加时叠加层数，只存在一个实例 |
| `PowerInstanceType.Instanced` | 重复施加时创建独立实例，互不影响（如炸弹） |

### AllowNegative — 是否允许负数层

| 值 | 说明 |
|-----|------|
| `false`（默认） | 层数为 0 时自动移除 |
| `true` | 层数可为负数（如力量/敏捷） |

---

## 真实回调

> 所有 `After*` 钩子定义在 `AbstractModel`（能力/遗物/卡牌通用），`Modify*`/`BeforeApplied` 等由 PowerModel 提供。

### 伤害 / 格挡修正（核心）

| 回调 | 签名 | 说明 |
|------|------|------|
| `ModifyDamageAdditive` | `decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)` | 伤害加减（力量） |
| `ModifyDamageMultiplicative` | 同左 5 参 | 伤害乘除（易伤） |
| `ModifyBlockAdditive` | `decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)` | 格挡加减 |
| `ModifyBlockMultiplicative` | 同左 5 参 | 格挡乘除 |

### 生命周期 / 事件钩子（AbstractModel）

| 回调 | 签名 | 说明 |
|------|------|------|
| `AfterCardPlayed` | `Task AfterCardPlayed(PlayerChoiceContext, CardPlay)` | 持有者打出任意牌后 |
| `AfterSideTurnEnd` | `Task AfterSideTurnEnd(PlayerChoiceContext, CombatSide, IEnumerable<Creature>)` | 某方回合结束后 |
| `AfterSideTurnStart` | `Task AfterSideTurnStart(CombatSide, IReadOnlyList<Creature>, ICombatState)` | 某方回合开始后 |
| `BeforeSideTurnStart` | `Task BeforeSideTurnStart(PlayerChoiceContext, CombatSide, IReadOnlyList<Creature>, ICombatState)` | 某方回合开始前 |

### PowerModel 自带

| 回调 | 签名 | 说明 |
|------|------|------|
| `BeforeApplied` | `Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)` | 施加前 |
| `AfterApplied` | `Task AfterApplied(Creature? applier, CardModel? cardSource)` | 施加后 |
| `AfterRemoved` | `Task AfterRemoved(Creature oldOwner)` | 移除后 |
| `ShouldPowerBeRemovedAfterOwnerDeath` | `bool` | 持有者死亡时是否移除 |

---

## 本地化

路径：`res://<模组ID>/localization/<语言代码>/powers.json`

```json
{
  "MyPower": {
    "name": "示例能力",
    "description": "简介文本",
    "smartDescription": "打出牌时获得 {Amount} 层格挡。"
  }
}
```

| 字段 | 说明 |
|------|------|
| `name` | 能力名称 |
| `description` | 普通简介文本 |
| `smartDescription` | 带动态变量信息的介绍（支持 `{Amount}` 等变量） |

---

## 图标

```
战斗裁切纹理：res://images/atlases/power_atlas.sprites/<能力ID小写>.tres
大图纹理：    res://images/powers/<能力ID小写>.png
Beta 大图：   res://images/powers/<能力ID小写>_beta.png
```

推荐分辨率：256x256（大图），64x64（裁切）

---

---

## 临时能力（Temporary Power）

临时能力是回合结束时自动减少层数、层数归零时自动移除的能力（临时力量/敏捷）。

### 原生模式（对照 TemporaryStrengthPower）

```csharp
public class MyTempPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;   // 层数归零自动移除

    // 回合结束：持有者在参与者列表里就减 1 层
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Decrement(this);
        }
    }
}
```

### 要点

- 用 `PowerCmd.Decrement(this)` 递减层数（不要直接改 `Amount` 字段）
- `AllowNegative = false` + 归零移除机制自动清理
- 判断持有者是否在场用 `participants.Contains(Owner)`（真实 TemporaryStrengthPower 风格）

## 调试

战斗中按反单引号 `` ` `` 打开控制台：

```
power <目标> <能力ID> <层数>
```

`目标` 为整数，单人游戏时 `0` 表示玩家角色。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 能力不生效 | 检查是否注册（attribute 自动注册 或 `ModelDb.Inject`） |
| 层数不减 | 用 `PowerCmd.Decrement` 或 `SetAmount(int, bool)`，不要直接改字段 |
| 图标不显示 | 检查 atlas 命名约定：`power_atlas.sprites/<能力ID小写>.tres` |
| 本地化不生效 | 使用 `smartDescription` 而非 `description` 显示动态变量 |
| Buff 不消失 | `AllowNegative = false`，层数归零自动移除 |
| 独立实例不生效 | `InstanceType = PowerInstanceType.Instanced` |
| 需要每次施加固定层数 | `PowerCmd.Apply<T>(choiceContext, target, amount, applier, cardSource)` |

---

## 进阶：纯原生自动注册

> 从 BaseLib 提炼，零第三方依赖。能力不进池，用 `[PowerModel]` attribute 标记 + ContentRegistry 统一 `ModelDb.Inject`。框架完整代码见 [serialization.md](../serialization/serialization.md)「进阶：纯原生自动注册框架」。

```csharp
[PowerModel]
public class MyPower : PowerModel { ... }
```

### 图标回退（原生自动）

大图缺失时原生 `ResolvedBigIconPath` 自动回退 `BigIconPath → BigBetaIconPath → MissingIconPath`，无需写代码（见上方「图标」章节）。

## 演进路线

- 当前：手动注册（保留为基准写法）
- 纯原生自动注册（Attribute 标记 + ContentRegistry）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（PowerStackType.Stacks/Boolean、IsInstanced、OnCardPlayed/OnTurnEnd 等编造回调、DamageCmd.GainBlock、CustomPackedIconPath）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖