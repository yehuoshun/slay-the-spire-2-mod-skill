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
| 先古遗物不在图鉴显示 | 实现 `CustomAncientRegistry` + `AncientRelicCompendiumPatch`（学 YuWanCard） |

---

## 集成战略事件架构（学 STS2Mod）

大规模事件模组（50+ 事件 + 自定义遭遇）的生产级架构：

### 分层设计

```
IntegratedStrategyEventModel（总入口）
├── Navigation（地图节点生成、路线规则）
├── Options（选项定义、条件分支）
├── Rewards（卡牌池、遗物池、药水）
├── Vitals（HP/金币/能量修改）
└── Deck（牌组操作、升级、移除）
```

### 事件定义模式

每个事件一个 `Definition` 类 + 一个 `EventModel` 类：

```csharp
// Definition：声明选项、条件、奖励
public static class MysteriousAltarDefinition
{
    public static EventDefinition Create() => new()
    {
        Id = "MysteriousAltar",
        Rarity = EventRarity.Common,
        Acts = [ActType.Act1],
        Options = new[]
        {
            new EventOptionDefinition("Pray")
                .WithCost(EventCost.None)
                .WithReward(EventReward.Gold(30)),
            new EventOptionDefinition("Sacrifice")
                .WithCost(EventCost.Hp(10))
                .WithReward(EventReward.RandomRareCard()),
            new EventOptionDefinition("Leave")
                .WithCost(EventCost.None)
        }
    };
}

// EventModel：执行逻辑
public sealed class MysteriousAltarEvent : IntegratedStrategyEventModel
{
    public MysteriousAltarEvent() : base(MysteriousAltarDefinition.Create()) { }
}
```

### 遭遇音乐系统（EncounterMusicController）

自定义遭遇绑定 BGM：

```csharp
public class EncounterMusicController
{
    private const string MusicPath = "res://MyMod/audio/music/{0}.ogg";

    public static void PlayBattleMusic(string musicId)
    {
        var path = string.Format(MusicPath, musicId);
        // 注入到 CombatManager 的音乐播放逻辑
    }
}
```

### 树洞系统（TreeHole）

特殊地图节点 + 会话管理：

```csharp
// 树洞会话管理器
public class TreeHoleSessionManager
{
    private readonly TreeHoleSessionStore _store;

    public TreeHoleSession CreateSession(TreeHoleType type, int seed)
    {
        var session = new TreeHoleSession
        {
            Id = Guid.NewGuid(),
            Type = type,
            Seed = seed,
            CreatedAt = DateTime.UtcNow
        };
        _store.Save(session);
        return session;
    }

    public TreeHoleSession LoadSession(Guid id) => _store.Load(id);
}
```

### 事件回放（Event Replay）

支持重复触发事件（非一次性）：

```csharp
public class IntegratedStrategyEventReplay
{
    public static bool CanReplay(string eventId, RunState run)
    {
        // 检查冷却、次数限制
        return run.GetReplayCount(eventId) < MaxReplays;
    }
}
```

### 地图生成补丁

自定义特殊节点插入地图：

```csharp
[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.Generate))]
public static class IntegratedStrategyMapGenerationPatch
{
    public static void Postfix(MapGenerator __instance)
    {
        // 在生成的地图中插入自定义节点（如秘密关卡入口）
        InsertSecretNodes(__instance);
    }
}
```