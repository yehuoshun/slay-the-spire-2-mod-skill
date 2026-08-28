# 自定义能力：模板、图标命名与核心属性

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

