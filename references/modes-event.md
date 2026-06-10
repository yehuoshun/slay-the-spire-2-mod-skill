# 模式六：自定义事件 & 先古之民

## 普通事件

### 最简示例（两个选项）

```csharp
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace MyMod.Events;

public class MyCustomEvent : EventModel
{
    public override string BackgroundImagePath => "res://MyMod/images/events/my_custom_event.png";

    protected override IEnumerable<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            new EventOption(
                "MY_CUSTOM_EVENT.pages.INITIAL.options.OPTION_1.title",
                async ctx =>
                {
                    var card = ctx.RunState.CreateCard<StrikeCard>(ctx.Player);
                    await CardPileCmd.Add(card, PileType.Deck);
                    await SetEventFinished("MY_CUSTOM_EVENT.pages.RESULT.description");
                }
            ),
            new EventOption(
                "MY_CUSTOM_EVENT.pages.INITIAL.options.OPTION_2.title",
                async ctx =>
                {
                    await PlayerCmd.GainGold(ctx.Player, 50);
                    await SetEventFinished("MY_CUSTOM_EVENT.pages.RESULT2.description");
                }
            ),
        };
    }
}
```

### 多页选项

```csharp
await SetEventState("MY_CUSTOM_EVENT.pages.PAGE2.description",
    new[]
    {
        new EventOption("选项A", async ctx => { SetEventFinished("..."); }),
        new EventOption("选项B", async ctx => { SetEventFinished("..."); }),
    });
```

### 注册到事件列表

```csharp
[HarmonyPatch(typeof(Overgrowth), "AllEvents", MethodType.Getter)]
static void Postfix(ref IEnumerable<EventModel> __result)
{
    __result = __result.Append(new MyCustomEvent());
}
```

### 事件本地化

```json
{
  "MY_CUSTOM_EVENT": {
    "title": "神秘事件",
    "pages": {
      "INITIAL": {
        "description": "你遇到了一个神秘商人...",
        "options": {
          "OPTION_1": { "title": "[交易] 获得一张卡牌" },
          "OPTION_2": { "title": "[离开] 获得 50 金币" }
        }
      },
      "RESULT": { "description": "你获得了一张强力卡牌！" },
      "RESULT2": { "description": "你带着金币离开了。" }
    }
  }
}
```

### 事件资源

| 资源 | 路径 |
|------|------|
| 背景图 | `images/events/<事件ID小写>.png`（推荐 3440×1613） |

---

## 先古之民事件

### 最简示例

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace MyMod.Events;

public class MyAncient : AncientEventModel
{
    public override string BackgroundImagePath => "res://MyMod/images/events/my_ancient.png";

    protected override IEnumerable<EventOption> AllPossibleOptions => new[]
    {
        // 所有可能的选项
    };

    protected override AncientDialogueSet DefineDialogues()
    {
        var dialogues = new AncientDialogueSet();
        // 定义对话...
        return dialogues;
    }
}
```

### 先古之民资源（5 种缺一不可）

| 资源 | 路径 |
|------|------|
| 背景图 | `images/events/<id小写>.png` |
| 地图图标 | `images/packed/map/ancients/ancient_node_<id小写>.png` |
| 地图描边 | `images/packed/map/ancients/ancient_node_<id小写>_outline.png` |
| 历史头像 | `images/ui/run_history/<id小写>.png` |
| 背景场景 | `scenes/events/background_scenes/<id小写>.tscn` |

### 先古之民本地化

```json
{
  "MY_ANCIENT": {
    "title": "神秘先古之民",
    "epithet": "远古守护者",
    "talk": {
      "firstVisitEver": {
        "0-0": "你好，旅行者...",
        "0-1": "欢迎来到我的领域。"
      }
    }
  }
}
```

本地化键规则：
- 名字：`<先古ID>.title`
- 称号：`<先古ID>.epithet`
- 首次对话：`<先古ID>.talk.firstVisitEver.<组索引>-<行索引>`
- 角色特殊对话：`<先古ID>.talk.<角色ID>.<组索引>-<行索引>`
- 通用对话：`<先古ID>.talk.ANY.<组索引>-<行索引>r`

对话行类型后缀：`ancient`（先古之民发言）/ `char`（角色发言）/ `next`（选项分支）

### 注册先古之民

```csharp
[HarmonyPatch(typeof(Hive), "AllAncients", MethodType.Getter)]
static void Postfix(ref IEnumerable<AncientEventModel> __result)
{
    __result = __result.Append(new MyAncient());
}
```

或将自定义事件加入 `ActModel.AllEvents` 随机池（与普通事件注册方式相同）。