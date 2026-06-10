# 模式四：自定义能力（Buff/Debuff）

## 最简示例

```csharp
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MyMod.Powers;

public class ThornsPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Intensity;
    public override bool IsInstanced => false;    // 不重复创建实例
    public override bool AllowNegative => false;  // 不允许负层数

    protected override string BigIconPath => "res://MyMod/images/powers/thorns_power.png";

    // 受到攻击时反弹伤害
    protected override async Task AfterReceiveDamage(
        PlayerChoiceContext ctx, Creature source, decimal damage, CardModel? cardSource)
    {
        if (source != null && source.Side == CombatSide.Enemy)
        {
            await DamageCmd.Attack(Amount)
                .FromCard(null)
                .Targeting(source)
                .Execute();
        }
    }
}
```

## 核心属性

### PowerType

| 值 | 说明 |
|----|------|
| `Buff` | 增益 |
| `Debuff` | 减益 |

### PowerStackType

| 值 | 说明 |
|----|------|
| `Intensity` | 有层数（如中毒/力量/敏捷） |
| `Duration` | 仅存在性（如壁垒/钢笔尖/夹击） |

### IsInstanced

- `false`（默认）：重复施加时合并层数，只有一个实例
- `true`：每次施加创建独立实例

### AllowNegative

- `false`（默认）：层数为 0 时自动移除
- `true`：允许负层数（如力量/敏捷可为负）

## 施加能力

```csharp
await CreatureCmd.ApplyPower(target, typeof(ThornsPower), 3, Owner.Creature, this);
```

## 控制台测试命令

```
# 给玩家施加能力
apply_power 0 <能力ID> <层数>

# 给敌人施加能力
apply_power 1 <能力ID> <层数>
```

## 能力资源

| 资源 | 路径 |
|------|------|
| 战斗图标 | `images/atlases/power_atlas.sprites/<能力ID小写>.tres` |
| 大图纹理 | `images/powers/<能力ID小写>.png`（推荐 256×256） |

## 能力本地化

```json
// assets/localization/zhs/powers.json
{
  "THORNS_POWER": {
    "title": "荆棘",
    "description": "受到攻击时，反弹 {Amount} 点伤害。",
    "smartDescription": "受到攻击时，反弹 {Amount:diff()} 点伤害。"
  }
}
```