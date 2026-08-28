# 自定义 Modifier（运行规则）

> 参考：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomModifierModel.cs` + `STS2Plus` 源码

---

> v3：API 全量校正（对照真实 ModifierModel/DeadlyEvents/AllStar：删 BaseLib 冒充原生的 Alignment/SortOrder，效果直接 override 钩子而非 Harmony Patch）。v2 为存档，勿直接使用。
## 概述

Modifier 是 Custom Run 界面中可勾选的运行规则（如"敌人 HP 翻倍"、"开局 1 血"）。继承 `ModifierModel` 抽象类。

Modifier 定义**元数据**（标题/描述/图标，全部自动从 `modifiers` 本地化表生成），效果通过 **override 钩子**（`AfterRunCreated` + AbstractModel 的 `Modify*` 系列）直接实现——**不需要 Harmony Patch**。

---

## 基础模板

```csharp
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

public class MyModifier : ModifierModel
{
    // 可选：开局清空牌组
    public override bool ClearsPlayerDeck => false;

    // 运行创建时：改全局配置（真实写法，对照 DeadlyEvents）
    protected override void AfterRunCreated(RunState runState)
    {
        // 例如：runState.Odds.UnknownMapPoint.EliteOdds = 0.1f;
    }

    // 效果示例：所有攻击伤害 +2（Modify 钩子直接 override）
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer?.Player != null && props.IsPoweredAttack())
            return amount + 2m;
        return amount;
    }
}
```

> ⚠️ 旧版写的 `Alignment`/`MutuallyExclusiveGroup`/`SortOrder`/`ModifierAlignment` 是 **BaseLib `CustomModifierModel`** 的，原生 `ModifierModel` 不存在。Custom Run UI 的"正面/负面"分类由引擎根据规则性质自动处理。

---

## 可覆盖成员（真实存在）

| 成员 | 类型 | 说明 |
|------|------|------|
| `Title` / `Description` | `public virtual LocString` | 自动从 `modifiers` 本地化表生成，无需手写 |
| `NeowOptionTitle` / `NeowOptionDescription` | `public virtual LocString` | Neow 选项标题/描述 |
| `IconPath` | `protected virtual string` | 图标路径（命名约定 `packed/modifiers/<id小写>.png`） |
| `ClearsPlayerDeck` | `public virtual bool` | 开局清空牌组 |
| `GenerateNeowOption(EventModel)` | `public virtual Func<Task>?` | 生成 Neow 选项（AllStar 用它发卡） |
| `AfterRunCreated(RunState)` | `protected virtual void` | 运行创建时改配置 |
| `AfterRunLoaded(RunState)` | `protected virtual void` | 读档后改配置 |
| `IsEquivalent(ModifierModel)` | `public virtual bool` | 判定是否等价（避免重复） |

> 效果类 `Modify*` 钩子（伤害/格挡/地图/奖励）来自 `AbstractModel`，见下文。

---

## 本地化

路径：`res://<模组ID>/localization/<语言代码>/modifiers.json`

```json
{
  "MY_MODIFIER": {
    "title": "示例规则",
    "description": "所有攻击牌获得 +2 点伤害。"
  }
}
```

---

## 注册到 Custom Run 界面

默认情况下，自定义 Modifier 不会出现在 Custom Run 的规则列表中。需要 Patch `NCustomRunModifiersList.GetModifiersTickedOn`：

```csharp
[HarmonyPatch(typeof(NCustomRunModifiersList), "GetModifiersTickedOn")]
public static class CustomModifierListPatch
{
    private static void Postfix(ref List<ModifierModel> __result)
    {
        __result.Add(ModelDb.Get<MyModifier>().ToMutable());
    }
}
```

---

## 序列化

Modifier 序列化由原生 `FromSerializable` 自动处理（`SaveUtil.ModifierOrDeprecated` + `SavedProperties`）。自定义属性用 `[SavedProperty]` + `InjectTypeIntoCache` 即可（见 [serialization.md](../serialization/serialization.md)），**无需 Patch `FromSerializable`**。

---

## 效果实现（override 钩子，非 Harmony Patch）

Modifier 的 `ShouldReceiveCombatHooks = true`（继承自 AbstractModel），所以直接在子类 override 钩子即可，**不需要 Harmony Patch**。

### 模式 1：运行级配置（AfterRunCreated）

```csharp
protected override void AfterRunCreated(RunState runState)
{
    // 改全局几率/抓包/配置：
    runState.Odds.UnknownMapPoint.EliteOdds = 0.1f;
    runState.SharedRelicGrabBag.Remove<JuzuBracelet>();
}
```

### 模式 2：伤害/格挡修正（Modify 钩子）

```csharp
// 所有攻击伤害 +2
public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
{
    if (dealer?.Player != null && props.IsPoweredAttack())
        return amount + 2m;
    return amount;
}

// 敌人 HP 翻倍（乘算钩子）
public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
{
    return amount * 2m;
}
```

### 模式 3：地图/奖励修改

```csharp
// 修改地图生成（对照 BigGameHunter）
public override ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex) => map;

// 修改卡牌奖励生成
public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options) => options;

// 修改未开放房间类型的几率加成
public override float ModifyOddsIncreaseForUnrolledRoomType(RoomType roomType, float oddsIncrease) => oddsIncrease;
```

> 更多 `Modify*`/`After*` 钩子见 [code-patterns.md](../patterns/code-patterns.md) 或直接 `grep Modify Core/Models/AbstractModel.cs`。

---

## 完整示例：攻击增强规则

```csharp
// Modifier 模型（元数据自动从 modifiers.json 读取）
public class AttackBuffModifier : ModifierModel
{
    // 效果直接写在钩子里：所有攻击伤害 +2
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer?.Player != null && props.IsPoweredAttack())
            return amount + 2m;
        return amount;
    }
}
```

配合 `modifiers.json`：

```json
{
  "ATTACK_BUFF_MODIFIER": {
    "title": "攻击增强",
    "description": "所有攻击牌伤害 +2。"
  }
}
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| Modifier 不显示在 Custom Run | 检查 `GetModifiersTickedOn` Patch |
| Modifier 效果不生效 | 检查 override 的 `Modify*`/`AfterRunCreated` 钩子签名 |
| 标题/描述不显示 | 检查 `modifiers.json` 键与类名一致，必须 Publish |
| 想限制同组互斥 | 原生无互斥分组（`MutuallyExclusiveGroup` 是 BaseLib 的），需自行实现 |
| 多人不同步 | 实现自定义网络消息同步规则选择 |

---

## 进阶：纯原生注册工厂（v2）

> 从 BaseLib 提炼，零第三方依赖。用工厂类统一创建 + Attribute 标记，替代多个手写 Patch。

```csharp
// 工厂：统一创建自定义 Modifier
public static class CustomModifierCatalog
{
    public static IEnumerable<ModifierModel> CreateAll()
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (type.GetCustomAttribute<RegisteredModifierAttribute>() != null)
                yield return (ModifierModel)Activator.CreateInstance(type)!;
        }
    }
}

[HarmonyPatch(typeof(NCustomRunModifiersList), nameof(NCustomRunModifiersList.GetModifiersTickedOn))]
public static class ModifierListPatch
{
    private static void Postfix(ref List<ModifierModel> __result)
    {
        __result.AddRange(CustomModifierCatalog.CreateAll());
    }
}
```

```csharp
[RegisteredModifier]
public class MyModifier : ModifierModel { ... }   // 自动进 Custom Run 规则列表
```

> 效果仍通过 `[HarmonyPatchCategory]` 分组实现（见上「效果实现模式」），工厂只负责注册元数据。

## 演进路线

- 当前：手动 Patch 注入（保留为基准写法）
- v2：纯原生自动注册工厂（Attribute 标记 + 反射收集）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`ModifierAlignment`/`Alignment`/`MutuallyExclusiveGroup`/`SortOrder`、效果用 Harmony Patch + IsModifierActive、Patch `FromSerializable`）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖