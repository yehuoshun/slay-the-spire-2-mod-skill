# 自定义能力（Buff / Debuff）

> 参考：[杀戮尖塔2模组开发教程07 - 自定义能力（Buff） - 哔哩哔哩](https://www.bilibili.com/opus/1181126133981118470)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomPowerModel.cs`

---

## 概述

所有能力/Buff/Debuff 继承 `PowerModel` 抽象类。通过属性定义行为类型，通过回调实现每回合效果。

---

## 基础模板

```csharp
using System.Threading.Tasks;
using STS2;
using STS2.Combat;
using STS2.Core.Cards;
using Godot;

public class MyPower : PowerModel
{
    public MyPower()
    {
        Type = PowerType.Buff;           // Buff 还是 Debuff
        StackType = PowerStackType.Stacks; // 层数叠加还是单状态
        IsInstanced = false;             // 是否独立实例
        AllowNegative = false;           // 是否允许负数层
    }

    // 玩家打出任意牌时触发
    public override Task OnCardPlayed(PlayerChoiceContext context, CardPlayState playState)
    {
        DamageCmd.GainBlock(context.Player, Amount);
        return Task.CompletedTask;
    }

    // 回合结束时：层数减1
    public override Task OnTurnEnd(PlayerChoiceContext context)
    {
        Amount--;
        return Task.CompletedTask;
    }
}
```

---

## 可覆盖属性（新增）

```csharp
public class MyPower : PowerModel
{
    // 64x64 战斗裁切图标
    public override string? PackedIconPath =>
        "res://MyMod/images/atlases/power_atlas.sprites/my_power.tres";

    // 256x256 大图
    public override string? BigIconPath =>
        "res://MyMod/images/powers/my_power.png";

    // 256x256 Beta 大图（备用艺术图）
    public override string? BigBetaIconPath =>
        "res://MyMod/images/powers/my_power_beta.png";
}
```

### 属性说明

| 属性 | 类型 | 分辨率 | 说明 |
|------|------|--------|------|
| `PackedIconPath` | `string?` | 64x64 | 战斗裁切图标 |
| `BigIconPath` | `string?` | 256x256 | 大图纹理 |
| `BigBetaIconPath` | `string?` | 256x256 | Beta 版大图 |

---

## 核心属性

### PowerType — 类型

| 值 | 说明 |
|-----|------|
| `PowerType.Buff` | Buff（增益） |
| `PowerType.Debuff` | Debuff（减益） |

### PowerStackType — 叠加方式

| 值 | 说明 |
|-----|------|
| `PowerStackType.Stacks` | 有层数的数值型 Buff（如中毒/易伤/虚弱） |
| `PowerStackType.Boolean` | 单纯存在/不存在状态（如夹击/壁垒/钢笔尖） |

### IsInstanced — 是否独立实例

| 值 | 说明 |
|-----|------|
| `false`（默认） | 重复施加时叠加层数，只存在一个实例 |
| `true` | 重复施加时创建独立实例，互不影响 |

### AllowNegative — 是否允许负数层

| 值 | 说明 |
|-----|------|
| `false`（默认） | 层数为 0 时自动从实体移除 |
| `true` | 层数可为负数（如力量/敏捷） |

---

## 常用回调

| 回调 | 触发时机 | 签名 |
|------|---------|------|
| `OnCardPlayed` | 持有者打出任意牌 | `Task OnCardPlayed(PlayerChoiceContext, CardPlayState)` |
| `OnTurnStart` | 回合开始时 | `Task OnTurnStart(PlayerChoiceContext)` |
| `OnTurnEnd` | 回合结束时 | `Task OnTurnEnd(PlayerChoiceContext)` |
| `OnTakeDamage` | 持有者受到伤害 | `Task OnTakeDamage(PlayerChoiceContext, DamageInfo)` |
| `OnDealDamage` | 持有者造成伤害 | `Task OnDealDamage(PlayerChoiceContext, DamageInfo)` |
| `OnApplyPower` | 持有者被施加能力 | `Task OnApplyPower(PlayerChoiceContext, PowerModel)` |
| `OnDeath` | 持有者死亡 | `Task OnDeath(PlayerChoiceContext)` |

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

临时能力是回合结束时自动减少层数、层数归零时自动移除的能力。典型例子：临时力量、临时敏捷。

### 模式

```csharp
public class MyTempPower : PowerModel
{
    public MyTempPower()
    {
        Type = PowerType.Buff;
        StackType = PowerStackType.Stacks;
        AllowNegative = false;
    }

    // 回合结束时层数减1
    public override Task OnTurnEnd(PlayerChoiceContext context)
    {
        Amount--;
        return Task.CompletedTask;
    }

    // 也可以在这里处理效果
    public override Task OnCardPlayed(PlayerChoiceContext context, CardPlayState playState)
    {
        // 临时效果逻辑
        return Task.CompletedTask;
    }
}
```

### 要点

- `AllowNegative = false` 确保层数归零时自动移除
- 在 `OnTurnEnd` 中递减 `Amount`
- 不主动移除，靠 `AllowNegative` 机制自动清理

### BaseLib 方式

`CustomTemporaryPowerModel` 封装了临时能力模式，支持 `ApplyPowerFunc` 和 `InternallyAppliedPower`：

```csharp
public class MyTempPower : CustomTemporaryPowerModel
{
    // 指定每回合自动施加的能力
    public override PowerModel? InternallyAppliedPower =>
        ModelDb.Get<VulnerablePower>().ToMutable();
}
```

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
| 能力不生效 | 检查是否注册到 `PowerModel` 池 |
| 层数不减 | 在 `OnTurnEnd` 中手动递减 `Amount` |
| 图标不显示 | 检查 `PackedIconPath` / `BigIconPath` 路径 |
| Beta 图标不显示 | 检查 `BigBetaIconPath` 路径 |
| 本地化不生效 | 使用 `smartDescription` 而非 `description` 显示动态变量 |
| Buff 不消失 | 将 `AllowNegative` 设为 false，层数归零自动移除 |
| 独立实例不生效 | 将 `IsInstanced` 设为 true |

---

## 演进路线

- 当前写法：手动 `Amount--` 递减层数
- 更优方案：可封装 `ReduceAmount()` 等辅助方法，统一处理移除逻辑