# 自定义遗物

> 参考：[烟汐忆梦_YM 的 B站教程](https://www.bilibili.com/opus/1179604439936270359)
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomRelicModel.cs`

---

> v3：API 全量校正（对照真实 RelicModel/BurningBlood：RelicRarity 枚举、图标命名约定、AfterSideTurnStart 3 参、GainEnergy(decimal, Player)、删编造的 GetUpgradeReplacement）。v2 为存档，勿直接使用。
## 概述

所有遗物继承于 `RelicModel`（→ `AbstractModel`）。AbstractModel 实现了大量事件钩子，重写即可监听对应事件。

所有模型必须注册到 `ModelDb`，注册时生成 ModelId（用于冲突校验、本地化等）。

---

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

---

## 修改角色初始遗物（Harmony 补丁）

```csharp
[HarmonyPatch(typeof(Ironclad), nameof(Ironclad.StartingRelics), MethodType.Getter)]
public static class IroncladStartingRelicsPatch
{
    private static void Postfix(Ironclad __instance, ref IReadOnlyList<RelicModel> __result)
    {
        var list = new List<RelicModel>(__result)
        {
            ModelDb.Relic<MyCustomRelic>()
        };
        __result = list;
    }
}
```

---

## 动态变量

`CanonicalVars` 返回 `IEnumerable<DynamicVar>`，用于代码和描述同步。**注意构造参数类型**：能量/金币/抽牌是 `int`，治疗/伤害/格挡是 `decimal`。

| 类型 | 构造 | 说明 |
|------|------|------|
| `EnergyVar` | `new EnergyVar(1)` | 能量（int） |
| `GoldVar` | `new GoldVar(10)` | 金币（int） |
| `HealVar` | `new HealVar(5m)` | 治疗（decimal） |
| `DamageVar` | `new DamageVar(10m, ValueProp.Move)` | 伤害（decimal + props） |
| `BlockVar` | `new BlockVar(5m, ValueProp.Unpowered)` | 格挡（decimal + props） |
| `CardsVar` | `new CardsVar(2)` | 抽牌数（int） |
| `DynamicVar` | `new DynamicVar("Count", 3m)` | 自定义（name + decimal） |

取值：`DynamicVars.<字段名>.BaseValue` 或 `.IntValue`

---

## 生命周期钩子（真实签名，来自 AbstractModel）

> 玩家受伤/击杀等用 `AfterDamageReceived`/`AfterDamageGiven` 实现（旧版 `OnPlayerDamaged`/`OnPlayerKill` 不存在）。

| 钩子 | 签名 | 触发时机 |
|------|------|---------|
| `AfterSideTurnStart` | `Task AfterSideTurnStart(CombatSide, IReadOnlyList<Creature>, ICombatState)` | 某方回合开始 |
| `BeforeSideTurnStart` | `Task BeforeSideTurnStart(PlayerChoiceContext, CombatSide, IReadOnlyList<Creature>, ICombatState)` | 某方回合开始前 |
| `AfterSideTurnEnd` | `Task AfterSideTurnEnd(PlayerChoiceContext, CombatSide, IEnumerable<Creature>)` | 某方回合结束 |
| `AfterPlayerTurnStart` | `Task AfterPlayerTurnStart(PlayerChoiceContext, Player)` | 玩家回合开始 |
| `AfterPlayerTurnEnd` | `Task AfterPlayerTurnEnd(PlayerChoiceContext, Player)` | 玩家回合结束 |
| `AfterCardPlayed` | `Task AfterCardPlayed(PlayerChoiceContext, CardPlay)` | 打出卡牌后 |
| `AfterCombatEnd` | `Task AfterCombatEnd(CombatRoom)` | 战斗结束 |
| `AfterCombatVictory` | `Task AfterCombatVictory(CombatRoom)` | 战斗胜利（推荐） |
| `AfterDamageReceived` | `Task AfterDamageReceived(PlayerChoiceContext, Creature target, DamageResult, ValueProp, Creature? dealer, CardModel?)` | 持有者受伤（target == Owner.Creature） |
| `AfterDamageGiven` | `Task AfterDamageGiven(PlayerChoiceContext, Creature? dealer, DamageResult, ValueProp, Creature target, CardModel?)` | 造成伤害（含击杀判断） |
| `BeforeBlockGained` / `AfterBlockGained` | `Task ...(Creature, decimal, ValueProp, CardModel?)` | 获得格挡前后 |

> 判断持有者是否参与回合：`participants.Contains(Owner.Creature)`（真实遗物写法）。

---

## 本地化

路径：`res://<ModId>/localization/<语言代码>/relics.json`

```json
{
  "MY_CUSTOM_RELIC": {
    "title": "自定义遗物",
    "description": "每场战斗开始时，获得 {0} 点能量。",
    "flavor": "一段引文。"
  }
}
```

**遗物 ID 规则**：大驼峰类名 → 大写字母加下划线分割。如 `MyCustomRelic` → `MY_CUSTOM_RELIC`。

---

## 图标资源

### 文件结构

```
res://MyCustomMod/images/
├── relics/
│   ├── my_custom_relic.png          ← 大图标（256x256）
│   └── my_custom_relic_outline.png  ← 描边图标
├── atlases/
│   ├── relic_atlas.sprites/
│   │   └── my_custom_relic.tres     ← AtlasTexture 资源
│   └── relic_outline_atlas.sprites/
│       └── my_custom_relic.tres     ← 描边 AtlasTexture 资源
```

### 创建步骤

1. 在对应路径右键 → 新建 → 资源 → 搜索 `AtlasTexture` → 创建
2. 命名：`<小写遗物ID>.tres`
3. 双击资源 → 属性检查器 → 拖入 PNG 到 `Atlas`
4. 设置 `Region` 的 `w`/`h` 为图标大小（256x256）
5. 描边同样操作，路径在 `relic_outline_atlas.sprites/`

**回退**：若找不到 atlas 资源，游戏会尝试用大图标替换。

---

## 注册（ModEntry）

```csharp
[ModInitializer(nameof(Initialize))]
public static class MyCustomModInitializer
{
    private const string HarmonyId = "Author.MyCustomMod";
    private static Harmony? _harmony;

    public static void Initialize()
    {
        try
        {
            _harmony = new Harmony(HarmonyId);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            ModHelper.AddModelToPool<IroncladRelicPool, MyCustomRelic>();
        }
        catch (Exception e)
        {
            Log.Error("[MyCustomMod] 加载失败");
            Log.Error(e.ToString());
        }
    }
}
```

---

## 进阶：纯原生自动注册（v2）

> 从 BaseLib 提炼，零第三方依赖。用 `[RelicPool]` 标记 + ContentRegistry 反射扫描，免去每个遗物手动 `AddModelToPool`。
> 框架完整代码见 [serialization.md](../serialization/serialization.md)「进阶：纯原生自动注册框架」。

```csharp
[RelicPool(typeof(SharedRelicPool))]   // 标记进哪个遗物池
public class MyCustomRelic : RelicModel { ... }

[RelicPool(typeof(IroncladRelicPool))]
public class MyIroncladRelic : RelicModel { ... }
```

- Starter / Event / Ancient 稀有度不走随机池，仍需 Patch 或事件发放（见上方「修改角色初始遗物」）
- 自定义 Attribute 定义 + ContentRegistry 扫描代码见 serialization.md，本模块只需加 `[RelicPool]` 标记

## 演进路线

- 当前：手动注册（保留为基准写法）
- v2：纯原生自动注册（Attribute 标记 + ContentRegistry）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`Rarity`→`RelicRarity`、`BigIconPath` public override、`AfterSideTurnStart` 2 参、`GainEnergy(state, amount)`、`EnergyVar(1m)`、编造的 `GetUpgradeReplacement`）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖
## 常见问题

| 问题 | 解决 |
|------|------|
| 图标不显示 | 检查 atlas `.tres` 命名与遗物 ID 小写一致（`IconBaseName`） |
| 描边不对 | 描边纹理大小必须与遗物纹理一致 |
| Starter 遗物在池中不出现 | Starter 不走随机池，需 Patch 初始遗物列表 |
| 本地化不生效 | 必须 Publish 而非 Build |
| `.NET 版本冲突` | 修改 `GodotPlugins.runtimeconfig.json` 为 `"version": "9.0.0"` |
| 遗物 ID 冲突 | 类名必须唯一 |
| 动态变量值不对 | `CanonicalVars` 中的变量名必须与 `DynamicVars["Name"]` 一致；注意构造参数 int/decimal |