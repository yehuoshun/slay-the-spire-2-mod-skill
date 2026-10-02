# 创建自定义休息点选项

> ⚠️ 2026-10-02 全面测试重写：旧模板（`OnOptionSelected`/override `Title`/`IconPath`/`room.AddOption`）全部是**编造 API**，真编译验证失败。真实 API 如下。

## 基本模板

继承 `RestSiteOption`，重写关键成员：

```csharp
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

public class TradeOption : RestSiteOption
{
    public TradeOption(Player owner) : base(owner) { }

    // 唯一必须 override 的标识（本地化/图标全部由它自动生成）
    public override string OptionId => "MYSKILL_TRADE";

    // 玩家选中时的行为（唯一必须 override 的回调）
    public override async Task<bool> OnSelect()
    {
        // 扣血换金币
        // 真实签名：CreatureCmd.Damage(PlayerChoiceContext, Creature, decimal, ValueProp, Creature?)
        // （无 2 参重载；非战斗场景用 ThrowingPlayerChoiceContext）
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(), Owner.Creature, 6m, ValueProp.Move, Owner.Creature);
        await PlayerCmd.GainGold(75, Owner);
        return true;   // 是否"成功"（如锻造无牌可升时返回 false）
    }
}
```

## 必重写成员（真实 API，对照 RestSiteOption.cs）

| 成员 | 类型 | 说明 |
|------|------|------|
| `OptionId` | `abstract string` | 选项唯一 ID |
| `OnSelect()` | `abstract Task<bool>` | 选中后行为，返回是否成功 |

## 自动生成成员（不可 override）

| 成员 | 说明 |
|------|------|
| `Title` | **非 virtual**：自动读 `rest_site_ui` 表 `OPTION_<OptionId>.name` |
| `Description` | `virtual`（可 override）：自动读 `OPTION_<OptionId>.description` |
| `IconPath` | **private**：自动生成 `ui/rest_site/option_<id小写>.png` |
| `Icon` | 由 IconPath 加载的纹理 |

> ⚠️ 旧文档写的 override `Title`/`IconPath`/`OnOptionSelected(RestSiteRoom, RestSiteContext)` 均**不存在**，照写编译失败。

## 添加到休息点（真实注入途径）

原生生成流程：`RestSiteOption.Generate(player)` 内建 休息/锻造 两个选项（多人加 治疗），然后调 `Hook.ModifyRestSiteOptions(runState, player, options)` 遍历所有 Hook 监听者（AbstractModel）。

### 途径 1：继承 AbstractModel 重写 TryModifyRestSiteOptions（推荐）

```csharp
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;

public class MyRestSiteModel : AbstractModel
{
    public override bool ShouldReceiveCombatHooks => false;

    // 每次进入休息点时被调用，往 options 里加自定义选项
    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        options.Add(new TradeOption(player));
        return true;
    }
}
```

注册：`ModelDb.Inject(typeof(MyRestSiteModel))`（ModEntry.Initialize 中）。

### 途径 2：Patch RestSiteOption.Generate

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;

[HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Generate))]
public static class AddTradeOptionPatch
{
    private static void Postfix(List<RestSiteOption> __result, Player player)
    {
        __result.Add(new TradeOption(player));
    }
}
```

## 本地化（rest_site_ui 表）

```json
{
  "OPTION_MYSKILL_TRADE.name": "交易",
  "OPTION_MYSKILL_TRADE.description": "失去 6 点生命，获得 75 金币。"
}
```

## 参见

- [rest-site.md](rest-site.md) — 休息点概述
- 可编译示例：`sts2-mod-examples/Sts2ModExamplesCode/RestSite/`（ExampleRestOption / TestTradeOption）
