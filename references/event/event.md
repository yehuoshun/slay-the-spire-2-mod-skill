# 自定义事件

> 参考：[杀戮尖塔2模组开发教程06 - 自定义事件 - 哔哩哔哩](https://www.bilibili.com/opus/1180714323922649110)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomEventModel.cs`

---

## 概述

所有事件继承 `EventModel` 抽象类。事件是玩家进入问号房间后随机发生的，核心由 `GenerateInitialOptions` 返回选项列表；选项回调是无参 `async Task` 方法，通过 `SetEventState`/`SetEventFinished` 控制流程。

---

## 基础事件模板

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

public class MyCustomEvent : EventModel
{
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new List<EventOption>
        {
            new EventOption(this, OnGetCard, "MY_CUSTOM_EVENT.pages.INITIAL.options.GET_CARD"),
            new EventOption(this, OnGetGold, "MY_CUSTOM_EVENT.pages.INITIAL.options.GET_GOLD"),
        };
    }

    // 回调：无参 async Task（不是 Func<PlayerChoiceContext,Task>）
    private async Task OnGetCard()
    {
        var card = Owner!.RunState.CreateCard<MyCustomCard>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
        SetEventFinished(L10NLookup("MY_CUSTOM_EVENT.pages.GET_CARD.description"));
    }

    private async Task OnGetGold()
    {
        await PlayerCmd.GainGold(50, Owner!);
        SetEventFinished(L10NLookup("MY_CUSTOM_EVENT.pages.GET_GOLD.description"));
    }
}
```

---

## 事件资源（命名约定，不可 override）

`InitialPortraitPath`/`BackgroundScenePath`/`VfxPath` 均为原生 **private** 属性，按命名约定自动生成：

```
事件初始肖像图：res://images/events/<事件ID小写>.png
事件背景场景：  res://scenes/events/background_scenes/<事件ID小写>.tscn
事件特效场景：  res://scenes/vfx/events/<事件ID小写>_vfx.tscn
```

推荐分辨率：背景图 3440x1613。

---

## EventOption 构造

```csharp
new EventOption(
    EventModel eventModel,      // 所属事件（通常传 this）
    Func<Task>? onChosen,       // 选择回调（无参 async Task 方法；null = 不可选）
    string textKey,             // 完整本地化键（如 "MY_EVENT.pages.INITIAL.options.X"）
    bool disableOnChosen = true, // 选择后是否禁用
    bool isProceed = false,      // 是否推进选项
    params IHoverTip[] hoverTips
)
```

### 链式扩展（EventOption 自带）

| 扩展 | 说明 |
|------|------|
| `.WithRelic(RelicModel)` | 选项旁显示遗物图标 |
| `.ThatDoesDamage(decimal)` | 造成伤害（带死亡警告） |
| `.ThatDecreasesMaxHp(decimal)` | 降低最大生命 |
| `.ThatWillKillPlayerIf(Func<Player,bool>)` | 条件必死 |
| `.ThatHasDynamicTitle()` | 动态标题 |
| `.ThatWontSaveToChoiceHistory()` | 不记入选择历史 |

### 辅助方法（EventModel 提供）

| 方法 | 说明 |
|------|------|
| `L10NLookup(string key)` | 本地化查询，返回 `LocString` |
| `SetEventFinished(LocString desc)` | 退出事件并显示结束文本 |
| `SetEventState(LocString desc, IEnumerable<EventOption> options)` | 切换到新一页选项（多页） |
| `InitialOptionKey(string id)` | 构建初始选项本地化键 |

---

## 本地化

路径：`res://<模组ID>/localization/<语言代码>/events.json`

```json
{
  "MY_CUSTOM_EVENT": {
    "title": "自定义事件",
    "pages": {
      "INITIAL": {
        "description": "你遇到了一扇神秘的门...",
        "options": {
          "GetCard": { "title": "打开宝箱", "description": "获得一张卡牌" },
          "GetGold": { "title": "拿走金币", "description": "获得 50 金币" }
        }
      }
    },
    "finish": {
      "get_card": "你获得了一张卡牌！",
      "get_gold": "你获得了 50 金币！"
    }
  }
}
```

键格式：`<事件ID>.pages.<STATE>.options.<KEY>`（描述为 `pages.<STATE>.description`）

---

## 背景图

```
事件背景纹理：res://images/events/<事件ID小写>.png
```

分辨率：3440x1613

---

## 添加事件

```csharp
[HarmonyPatch(typeof(Overgrowth), nameof(Overgrowth.AllEvents), MethodType.Getter)]
public static class OvergrowthAllEventsPatch
{
    public static void Postfix(ref IEnumerable<EventModel> __result)
    {
        __result = __result.Append(new MyCustomEvent());
    }
}
```

> 注意：`AllEvents` 真实返回类型是 `IEnumerable<EventModel>`（不是 `List`），Postfix 需 `ref IEnumerable<EventModel>`。
> 也可用下方「进阶」的 attribute + 反射统一注入，避免逐事件手写 Patch。

---

## 多页选项（真实写法）

多页 = 在回调里再次调用 `SetEventState` 传新描述 + 新选项列表，无独立的 `GetStateOptions`。

```csharp
protected override IReadOnlyList<EventOption> GenerateInitialOptions()
{
    return new List<EventOption>
    {
        new EventOption(this, OnEnter, "MY_CUSTOM_EVENT.pages.INITIAL.options.ENTER"),
    };
}

private async Task OnEnter()
{
    SetEventState(
        L10NLookup("MY_CUSTOM_EVENT.pages.RECHARGE.description"),
        new List<EventOption>
        {
            new EventOption(this, OnHeal, "MY_CUSTOM_EVENT.pages.RECHARGE.options.HEAL"),
            new EventOption(this, OnLeave, "MY_CUSTOM_EVENT.pages.RECHARGE.options.LEAVE"),
        });
}
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 事件不触发 | 检查 `Overgrowth.AllEvents` Patch 是否生效（ref IEnumerable） |
| 选项不可点击 | 检查回调是否为 null / `disableOnChosen` 设置 |
| 肖像图不显示 | 检查命名约定 `images/events/<事件ID小写>.png` |
| 本地化不显示 | 检查 events.json 键格式（`pages.<STATE>.options.<KEY>`），必须 Publish |
| 背景图不显示 | PNG 命名与事件 ID 小写一致 |
| 多页切换不生效 | 回调里调 `SetEventState(LocString, 新选项)` 双参 |
| 拿玩家/金币 | 事件用 `Owner`（Player?），加金币 `PlayerCmd.GainGold` |

---

## 进阶：纯原生注册辅助

> 从 BaseLib 提炼，零第三方依赖。事件走 Patch 注入，用 Attribute 标记 + 反射在 Patch 里统一收集，替代逐个手写 Postfix。

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RegisteredEventAttribute : Attribute { }

// 单个 Patch 统一注入所有标记事件（AllEvents 是 IEnumerable，用 Concat）
[HarmonyPatch(typeof(Overgrowth), nameof(Overgrowth.AllEvents), MethodType.Getter)]
public static class AllEventsPatch
{
    private static void Postfix(ref IEnumerable<EventModel> __result)
    {
        var registered = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<RegisteredEventAttribute>() != null)
            .Select(t => (EventModel)Activator.CreateInstance(t)!);
        __result = __result.Concat(registered);
    }
}
```

```csharp
[RegisteredEvent]
public class MyEvent : EventModel { ... }   // 自动进事件池
```

> 相比手写多个 Patch，此方式新增事件只加一个 Attribute，无需改注册代码。

## 演进路线

- 当前：手动 Patch 注入（保留为基准写法）
- 纯原生自动注册（Attribute 标记 + 反射统一注入）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`Task<List<EventOption>>`、`EventOption(LocString, Func<...>)`、`SetEventState/Finished(string)`、`GetStateOptions`、`ref List` Patch）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖