# 自定义遗物

> 参考：[烟汐忆梦_YM 的 B站教程](https://www.bilibili.com/opus/1179604439936270359)

---

## 概述

所有遗物继承于 `RelicModel`（→ `AbstractModel`）。AbstractModel 实现了大量事件钩子，重写即可监听对应事件。

所有模型必须注册到 `ModelDb`，注册时生成 ModelId（用于冲突校验、本地化等）。

---

## 代码模板

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace MyCustomMod.Relics;

public sealed class MyCustomRelic : RelicModel
{
    // 稀有度决定获取途径
    public override Rarity Rarity => Rarity.Starter;

    // 图标路径
    public override string BigIconPath => "res://MyCustomMod/images/relics/my_custom_relic.png";
    public override string PackedIconPath => BigIconPath;
    protected override string PackedIconOutlinePath => "res://MyCustomMod/images/relics/my_custom_relic_outline.png";

    // 动态变量（描述同步）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1m)
    ];

    // 第一回合开始，给予持有者 1 点能量
    public override async Task AfterSideTurnStart(Side side, ICombatState combatState)
    {
        if (side != Owner.Creature.Side) return;
        Flash();
        await PlayerCmd.GainEnergy(combatState, DynamicVars.Energy.IntValue);
    }
}
```

---

## 稀有度（Rarity）

| 值 | 获取方式 |
|------|---------|
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

`CanonicalVars` 返回 `IEnumerable<DynamicVar>`，用于代码和描述同步。

| 类型 | 构造 | 说明 |
|------|------|------|
| `EnergyVar(base)` | `new EnergyVar(1m)` | 能量 |
| `GoldVar(base)` | `new GoldVar(10m)` | 金币 |
| `HealVar(base)` | `new HealVar(5m)` | 治疗 |
| `DamageVar(base, props)` | `new DamageVar(10m, ValueProp.Move)` | 伤害 |
| `BlockVar(base, props)` | `new BlockVar(5m, ValueProp.Unpowered)` | 格挡 |
| `CardsVar(base)` | `new CardsVar(2)` | 抽牌数 |
| `DynamicVar(name, base)` | `new DynamicVar("Count", 3m)` | 自定义 |

取值：`DynamicVars.<字段名>.BaseValue` 或 `.IntValue`

---

## 生命周期钩子

| 钩子 | 触发时机 |
|------|---------|
| `AfterSideTurnStart(side, state)` | 某方回合开始 |
| `BeforeSideTurnStart(side, state)` | 某方回合结束 |
| `AfterCombatEnd(context)` | 战斗结束后 |
| `AfterPlayerTurnStart(context, player)` | 玩家回合开始 |
| `AfterPlayerTurnEnd(context, player)` | 玩家回合结束 |
| `AfterCardPlayed(context, cardPlay)` | 打出卡牌后 |
| `OnPlayerDamaged(context, damageTaken)` | 玩家受伤时 |
| `OnPlayerKill(context, target)` | 玩家击杀敌人时 |
| `BeforePlayerBlockLost(context, amountLost)` | 格挡消失前 |
| `AfterPlayerHeal(context, amountHealed)` | 玩家治疗后 |

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

            ModHelper.AddModelToPool<IroncladRelicPool>(typeof(MyCustomRelic));
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

##": "## 演进路线

当前方案是**手动注册**（`ModHelper.AddModelToPool`），每个遗物都要手动加一行。


已知更优方案：

- **属性扫描注册**：用自定义 Attribute 标记模型类（如 `[RelicPool]`），初始化时反射扫描自动注册，无需手动调 `AddModelToPool`
- **Builder 模式**：YuWanCard 的 `YuWanRelicModel` 支持 `autoAdd: true` 自动注册

---

## 常见问题"}]

| 问题 | 解决 |
|------|------|
| 图标不显示 | 检查 atlas `.tres` 路径和文件名是否与遗物 ID 一致 |
| 描边不对 | 描边纹理大小必须与遗物纹理一致 |
| Starter 遗物在池中不出现 | Starter 不走随机池，需 Patch 初始遗物列表 |
| 本地化不生效 | 必须 Publish 而非 Build |
| `.NET 版本冲突` | 修改 `GodotPlugins.runtimeconfig.json` 为 `"version": "9.0.0"` |
| 遗物 ID 冲突 | 类名必须唯一 |
| 动态变量值不对 | `CanonicalVars` 中的变量名必须与 `DynamicVars["Name"]` 一致 |