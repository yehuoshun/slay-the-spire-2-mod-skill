# 模式二：自定义遗物

## 最简示例

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;

namespace MyMod.Relics;

public class EnergyRelic : RelicModel
{
    // 稀有度决定获取途径
    public override RelicRarity Rarity => RelicRarity.Boss;

    // 图标
    protected override string BigIconPath => "res://MyMod/images/relics/energy_relic.png";
    public override string PackedIconPath => BigIconPath;
    protected override string PackedIconOutlinePath => BigIconPath;

    // 回合开始钩子
    public override async Task AfterSideTurnStart(CombatSide side,
        IReadOnlyList<Creature> participants, HextechCombatState combatState)
    {
        if (side == Owner.Creature.Side)
        {
            Flash();
            await PlayerCmd.GainEnergy(Owner, 1);
        }
    }
}
```

## RelicRarity 枚举

| 值 | 获取途径 |
|----|---------|
| `Starter` | 初始遗物（不进入随机池） |
| `Common` | 宝箱/精英/商店 |
| `Uncommon` | 宝箱/精英/商店 |
| `Rare` | 宝箱/精英/商店 |
| `Boss` | Boss 掉落 |
| `Shop` | 仅商店 |
| `Event` | 仅事件 |
| `Ancient` | 仅先古之民 |

## 注册到遗物池

```csharp
ModHelper.AddModelToPool<SharedRelicPool, EnergyRelic>();
// 或角色专属
ModHelper.AddModelToPool<IroncladRelicPool, EnergyRelic>();
```

## 常用钩子

```csharp
// 回合开始前
BeforeSideTurnStart(PlayerChoiceContext, CombatSide, IReadOnlyList<Creature>, CombatState)
// 回合开始后
AfterSideTurnStart(CombatSide, IReadOnlyList<Creature>, CombatState)
// 回合结束前
BeforeSideTurnEnd(PlayerChoiceContext, CombatSide, IEnumerable<Creature>)
// 回合结束后
AfterSideTurnEnd(PlayerChoiceContext, CombatSide, IEnumerable<Creature>)
// 战斗开始前
BeforeCombatStart() / BeforeCombatStart(PlayerChoiceContext)
// 战斗结束后
AfterCombatEnd(CombatRoom)
// 获取遗物时
OnObtained()
// 失去遗物时
OnLost()
// 卡牌打出后
AfterCardPlayed(CardModel, CardPlay)
// 受到伤害后
AfterReceiveDamage(PlayerChoiceContext, Creature, decimal)
```

## 显示计数器

```csharp
public override bool ShowCounter => true;
public override int DisplayAmount => _counter;

protected override IEnumerable<DynamicVar> CanonicalVars => [
    new DynamicVar("Counter", () => _counter)
];
```

## 遗物池列表

| 池 | 说明 |
|----|------|
| `SharedRelicPool` | 公共池（宝箱+精英+商店） |
| `IroncladRelicPool` | 铁血战士专属 |
| `SilentRelicPool` | 静默猎人专属 |
| `DefectRelicPool` | 机器人专属 |
| `NecrobinderRelicPool` | 亡灵契约师专属 |
| `RegentRelicPool` | 储君专属 |
| `EventRelicPool` | 事件专属 |
| `ShopRelicPool` | 商店专用 |
| `BossRelicPool` | Boss 掉落 |
| `FallbackRelicPool` | 兜底池（Circlet） |

## 修改角色初始遗物（Harmony Patch）

```csharp
[HarmonyPatch(typeof(Ironclad), "StartingRelics", MethodType.Getter)]
[HarmonyPostfix]
static void Postfix(ref IEnumerable<RelicModel> __result)
{
    __result = __result.Append(new MyRelic());
}
```