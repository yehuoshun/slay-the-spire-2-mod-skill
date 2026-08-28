# 自定义遗物：模板、属性与池

## 代码模板

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace MyCustomMod.Relics;

public sealed class MyCustomRelic : RelicModel
{
    // 稀有度（抽象属性，枚举是 RelicRarity 不是 Rarity）
    public override RelicRarity Rarity => RelicRarity.Starter;

    // 动态变量（描述同步）：注意 EnergyVar 参数是 int
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new EnergyVar(1) };

    // 某方回合开始：持有者在参与者里就给 1 点能量
    protected override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature)) return;
        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);  // (decimal, Player)
    }
}
```

> 图标路径由 `IconBaseName`（= 遗物 ID 小写）自动生成，无需 override（见下方「图标资源」）。`Owner` 是 `Player`（不是 Creature）。

---

## 可覆盖属性（真实存在）

| 属性 | 类型 | 说明 |
|------|------|------|
| `Rarity` | `public abstract RelicRarity` | 稀有度，决定获取途径 |
| `IconBaseName` | `protected virtual string` | 图标基准名（默认 = 遗物 ID 小写），override 可自定义 |
| `PackedIconPath` | `public virtual string` | 裁切图标路径（由 IconBaseName 自动生成） |
| `PackedIconOutlinePath` | `protected virtual string` | 描边裁切路径（由 IconBaseName 自动生成） |
| `BigIconPath` | `protected virtual string` | 大图标路径（由 IconBaseName 自动生成） |
| `IsAllowedInShops` | `public virtual bool` | 是否可进商店 |
| `IsUsedUp` | `public virtual bool` | 使用后是否消失 |
| `HasUponPickupEffect` | `public virtual bool` | 拾取时是否有一次性效果 |
| `IsStackable` | `public virtual bool` | 是否可叠加 |
| `SpawnsPets` / `AddsPet` | `public virtual bool` | 宠物相关 |
| `ShowCounter` / `DisplayAmount` | `public virtual bool / int` | 计数器显示 |

> ⚠️ 旧版写的 `GetUpgradeReplacement()`（遗物升级替代）在原生 `RelicModel` **不存在**（当前版本），已移除。

---

## 稀有度（RelicRarity）

| 值 | 获取方式 |
|------|---------|
| `None` | 无 |
| `Common` | 常规遗物池（宝箱/精英） |
| `Uncommon` | 常规遗物池（宝箱/精英） |
| `Rare` | 常规遗物池（宝箱/精英） |
| `Starter` | 初始遗物，**不走随机池**，需 Patch 或事件给 |
| `Shop` | 商店专用 |
| `Event` | 事件专属 |
| `Ancient` | 先古之民遗物 |

**规则**：Starter/Event/Ancient 不走常规池，不可交易（物交换事件不会选）。

---

## 遗物池

通过 `ModHelper.AddModelToPool` 注册：

| 池 | 说明 |
|----|------|
| `IroncladRelicPool` | 铁血战士专属 |
| `SilentRelicPool` | 静默猎手专属 |
| `DefectRelicPool` | 故障机器人专属 |
| `NecrobinderRelicPool` | 亡灵契约师专属 |
| `RegentRelicPool` | 储君专属 |
| `SharedRelicPool` | 公共池（精英、商店、宝箱） |
| `EventRelicPool` | 事件池（仅问号事件获取） |
| `FallbackRelicPool` | 兜底池（头环） |
| `DeprecatedRelicPool` | 废弃内容/存档兼容 |

