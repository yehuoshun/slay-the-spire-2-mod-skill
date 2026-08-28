# 自定义事件：模板、资源与选项构造

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

