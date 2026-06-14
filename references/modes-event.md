# 自定义事件 & 先古之民

---

## 普通事件

### 最简示例（两个选项）

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace MyMod.Events;

[EventPool]  // ← 自动注册
public sealed class MysteriousAltarEvent : EventModel
{
    public override EventRarity Rarity => EventRarity.Common;
    public override string PortraitPath => "res://MyMod/images/events/altar.png";

    public override IEnumerable<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            new EventOption("祈祷", "获得 30 金币")
                .OnSelect(async context =>
                {
                    await PlayerCmd.GainGold(context, 30);
                }),

            new EventOption("献祭", "失去 10 HP，获得一张稀有卡牌")
                .OnSelect(async context =>
                {
                    await PlayerCmd.Damage(context, 10);
                    // 给一张随机稀有卡牌
                }),

            new EventOption("离开")
                .OnSelect(_ => Task.CompletedTask)
        };
    }
}
```

---

## 带条件选项

```csharp
public override IEnumerable<EventOption> GenerateInitialOptions()
{
    var options = new List<EventOption>();

    // 总是可用
    options.Add(new EventOption("离开").OnSelect(_ => Task.CompletedTask));

    // 需要金币
    if (Owner.Gold >= 50)
    {
        options.Add(new EventOption("购买遗物", "花费 50 金币")
            .OnSelect(async context =>
            {
                await PlayerCmd.GainGold(context, -50);
                // 给一个随机遗物
            }));
    }

    // 需要特定遗物
    if (Owner.Relics.Any(r => r is GoldenIdol))
    {
        options.Add(new EventOption("献上金像", "失去金像，获得 100 金币")
            .OnSelect(async context =>
            {
                // 移除金像，获得金币
                await PlayerCmd.GainGold(context, 100);
            }));
    }

    return options;
}
```

---

## 先古之民（Ancient Event）

先古之民是带对话树的事件。需要覆盖 `DefineDialogues()`。

```csharp
public sealed class ElderTreeAncient : AncientEventModel
{
    public override string PortraitPath => "res://MyMod/images/events/elder_tree.png";

    public override IEnumerable<AncientDialogue> DefineDialogues()
    {
        return new[]
        {
            new AncientDialogue("elder_tree_greeting")
                .SetText("古老的大树在你面前低语...")
                .AddOption("倾听", "获得 2 点力量")
                    .OnSelect(async context =>
                    {
                        await PowerCmd.Apply<StrengthPower>(
                            Owner.Creature, 2, Owner.Creature, this);
                    })
                    .NextDialogue("elder_tree_thanks"),

            new AncientDialogue("elder_tree_thanks")
                .SetText("大树满意地摇晃着枝叶。")
                .AddOption("离开")
                    .OnSelect(_ => Task.CompletedTask)
        };
    }
}
```

---

## 事件注册

事件不走 `AddModelToPool`，需要特殊处理：

```csharp
// 方案一：Patch ModelDb.Init 时注入
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public static class EventRegistrationPatch
{
    public static void Postfix()
    {
        // 在 ModelDb 初始化后注册自定义事件
        CustomEventRegistry.Register<MysteriousAltarEvent>();
    }
}

// 方案二：用 ShunMod 的 EventPoolAttribute + EventRegistry
// 在 ContentRegistry.RegisterAll 中自动收集
```

---

## 本地化

`assets/localization/zhs/events.json`：

```json
{
  "MysteriousAltar": {
    "NAME": "神秘祭坛",
    "DESCRIPTIONS": [
      "你发现了一座古老的祭坛，上面刻着模糊的文字。",
      "祈祷",
      "获得 30 金币",
      "献祭",
      "失去 10 HP，获得一张稀有卡牌",
      "离开"
    ]
  }
}
```

`assets/localization/zhs/ancients.json`：

```json
{
  "ElderTree": {
    "NAME": "古老大树",
    "DESCRIPTIONS": [
      "古老的大树在你面前低语...",
      "倾听",
      "获得 2 点力量",
      "大树满意地摇晃着枝叶。",
      "离开"
    ]
  }
}
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 事件不触发 | 检查 `EventRarity` 和注册方式 |
| 先古之民对话不显示 | Patch `AncientDialogueSet.GetValidDialogues` + `DefineDialogues` |
| 选项条件不更新 | 条件在 `GenerateInitialOptions` 中实时计算 |