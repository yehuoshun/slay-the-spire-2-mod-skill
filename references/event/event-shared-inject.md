# 共享事件注入（AllSharedEvents）+ 事件选项链式构建

> 实战验证（sts2mod 心之钢 Heartsteel v0.2.0，2026-10-07）。把自定义事件挂进**全角色共享事件池**（所有角色开局都可能遇到），以及选项链式写法。

## 注入共享事件（getter postfix）

```csharp
public static class OrnnsForgeRegistration
{
    public static void Install()
    {
        MethodInfo getter = typeof(ModelDb)
            .GetProperty(nameof(ModelDb.AllSharedEvents), BindingFlags.Static | BindingFlags.Public)?.GetMethod
            ?? throw new InvalidOperationException("Could not find ModelDb.AllSharedEvents getter.");

        new Harmony("MyMod.SharedEvent")
            .Patch(getter, postfix: new HarmonyMethod(typeof(OrnnsForgeRegistration), nameof(AppendSharedEvent)));
    }

    private static void AppendSharedEvent(ref IEnumerable<EventModel> __result)
    {
        __result = __result.Concat([ModelDb.Event<MyEvent>()]).Distinct();
    }
}
```

- `ModelDb.AllSharedEvents` 是 static getter（内部 `_allSharedEvents` 缓存字段，真实 API）；patch getter 后追加。
- `Distinct()` 防重复（多 patch 或缓存重建场景）。
- 共享事件池 = 所有角色开局随机遇到的非章节事件（区别于章节专属事件池）。

## 事件选项链式构建

```csharp
protected override IReadOnlyList<EventOption> GenerateInitialOptions()
{
    Player owner = GetOwnerOrThrow();
    List<EventOption> options =
    [
        new EventOption(this, Greet, InitialOptionKey("GREET")),                                    // 纯文本选项
        (owner.Gold >= TradeGoldCost
            ? new EventOption(this, FairTrade, InitialOptionKey("FAIR_TRADE"), relic.HoverTips)
                .WithRelic(relic)                                                                   // 选项带遗物（拿了进奖励展示）
            : new EventOption(this, null, InitialOptionKey("FAIR_TRADE_LOCKED"))),                  // 锁定选项（条件不满足，置灰）
        (owner.Creature.CurrentHp >= StealHpLoss + 1
            ? new EventOption(this, GrabAndRun, InitialOptionKey("GRAB_AND_RUN"))
                .ThatDoesDamage(StealHpLoss)                                                        // 链式：选它=扣血
            : new EventOption(this, null, InitialOptionKey("GRAB_AND_RUN_LOCKED")))
    ];
    return options;
}

public override bool IsAllowed(IRunState runState)   // 出现条件（不满足 = 事件不刷出来）
{
    return runState.Players.All(p => p.Gold >= TradeGoldCost || p.Creature.CurrentHp >= StealHpLoss + 1);
}
```

- 构造重载：`EventOption(EventModel, Func<Task>? onChosen, string textKey, params IHoverTip[])`（还有带 LocString 标题/描述的重载）。
- 链式：`WithRelic(RelicModel)`（选项获得遗物）、`ThatDoesDamage(decimal)`（扣血）、`WithOverridenHistoryName(LocString)`（历史记录名）。
- **锁定选项**：`onChosen = null` + 独立文案键（置灰显示条件不满足原因）；满足/不满足用三元表达式生成不同选项。
- `IsAllowed(IRunState)` virtual：事件可否出现的条件门。
- `SetEventFinished(LocString)` / `InitialOptionKey(string)` 是 protected（EventModel），完成选项后调用结束事件。

## 选项动作实现

```csharp
private async Task FairTrade()
{
    Player owner = GetOwnerOrThrow();
    await PlayerCmd.LoseGold(TradeGoldCost, owner, GoldLossType.Spent);   // 扣钱（带花费类型）
    await RelicCmd.Obtain<HeartsteelRelic>(owner);                       // 给遗物（泛型便捷版）
    await CreatureCmd.GainMaxHp(owner.Creature, TradeMaxHpGain);
    SetEventFinished(PageDescription("FAIR_TRADE"));                      // 结束事件（PageDescription 是 RitsuLib 扩展，纯原生用 new LocString(...)）
}
```

- `RelicCmd.Obtain<T>(Player)` 泛型重载 + `Obtain(RelicModel, Player, int index)`。
- `PlayerCmd.LoseGold(decimal, Player, GoldLossType)`（Spent/Lost 等来源类型）。
- `CreatureCmd.GainMaxHp(Creature, decimal)`：加最大生命（同时回当前生命）。

## 相关 API 速查

| API | 说明 |
|-----|------|
| `ModelDb.AllSharedEvents` / `AllEvents` | 共享事件池 / 全事件（含章节） |
| `ModelDb.Event<T>()` | 拿事件原型 |
| `EventOption(EventModel, Func<Task>?, string, params IHoverTip[])` | 选项构造 |
| `EventOption.WithRelic/ThatDoesDamage/WithOverridenHistoryName` | 链式修饰 |
| `EventModel.IsAllowed(IRunState)` | 事件出现条件 |
| `RelicCmd.Obtain<T>(Player)` | 泛型获得遗物 |
| `NCombatRoom.Instance.GetCreatureNode(Creature?)` | 战斗场景取生物节点 |

> ℹ️ RitsuLib 生态对照：心之钢本体用 `ModEventTemplate` + `EventAssetProfile`（集中声明资源）——skill 纯原生做法见 event/ 模块（继承 EventModel + 资源路径属性），AssetProfile 思想可参考 character-asset-hooks.md 的资源注入总闸。