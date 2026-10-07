# 全事件「离开」选项（SetEventState prefix 注入）

> 实战验证（sts2mod 我路过 WoLuGuo v1.0.2，2026-10-07）。130 行小 mod：给**所有事件**追加一个「离开」选项，不执行事件专属操作直接跳过。核心：patch 事件选项生成入口 + 安全结束事件 + 多人推进地图。

## 核心 hook（SetEventState prefix）

```csharp
private static void InstallHooks()
{
    harmony.Patch(
        RequireMethod(typeof(EventModel), "SetEventState",
            BindingFlags.Instance | BindingFlags.NonPublic, typeof(LocString), typeof(IEnumerable<EventOption>)),
        prefix: new HarmonyMethod(typeof(ModEntry), nameof(SetEventStatePrefix))
        {
            priority = Priority.Last      // 最后执行：别的 mod 加的选项不被覆盖
        });
}

private static void SetEventStatePrefix(EventModel __instance, ref IEnumerable<EventOption> eventOptions)
{
    eventOptions = BuildEventOptions(__instance, eventOptions);   // 直接改 ref 参数
}
```

- `SetEventState(LocString, IEnumerable<EventOption>)` 是 **protected virtual**（真实 API）——所有事件生成选项必经入口，prefix 一处全覆盖。
- `priority = Priority.Last`：确保其他 mod 在 SetEventState 上的注入先跑，自己最后追加。

## 追加条件（ShouldAddLeaveOption）

```csharp
private static bool ShouldAddLeaveOption(EventModel eventModel, List<EventOption> options)
{
    if (eventModel.Id.Entry == "THE_ARCHITECT") return false;   // 终局事件不许跳
    if (eventModel.Description != null) return false;           // 有自定义描述的事件 = 特殊流程（多页/剧情），不加
    if (options.Count == 0) return false;                       // 空选项不加
    return !options.Any(o => o.IsProceed || o.TextKey == LeaveOptionTextKey);  // 已有离开/继续选项则不加
}
```

- `EventModel.Description`（LocString?）：非 null 表示事件走了自定义描述流程（多页/剧情事件），跳过注入。
- `EventOption.IsProceed`（继续选项，如 BOSS 战前确认）+ `TextKey` 判重。

## 离开动作（结束事件 + 多人安全推进）

```csharp
private static Task LeaveEvent(EventModel eventModel)
{
    if (!eventModel.IsFinished)
    {
        // SetEventFinished 是 protected → 反射调用（句柄启动时静态缓存）
        SetEventFinishedMethod.Invoke(eventModel, new object[] { CreateLeaveDescription() });
    }

    if (LocalContext.IsMine(eventModel) && eventModel.Node != null)
    {
        return NEventRoom.Proceed();   // 推进地图（离开事件节点）
    }

    return Task.CompletedTask;          // 多人非本地：不推进，等主机同步
}
```

- `LocalContext.IsMine(EventModel?)`：多人身份检查——只有本地事件才推进地图，远程玩家的选项由同步驱动。
- `NEventRoom.Proceed()` static（真实 API）：地图推进入口。
- `EventModel.Node`（Control?）：事件 UI 节点已挂载才推进（防初始化时序问题）。

## 本地化（多语言）

```csharp
// 全局 14 语言 events.json，键：
//   WOLUGUO.leave.title / WOLUGUO.leave.description
// 选项构造用带 LocString 的重载（标题/描述齐全，否则原版用 TextKey 查表）
new EventOption(eventModel, () => LeaveEvent(eventModel),
    new LocString("events", LeaveTitleKey),
    new LocString("events", LeaveDescriptionKey),
    LeaveOptionTextKey,
    Array.Empty<IHoverTip>());
```

- 6 参构造：`EventOption(EventModel, Func<Task>?, LocString title, LocString description, string textKey, IEnumerable<IHoverTip>)`——显式标题/描述优先，TextKey 兜底查表。

## 相关 API 速查

| API | 说明 |
|-----|------|
| `EventModel.SetEventState(LocString, IEnumerable<EventOption>)` | protected virtual，选项生成入口 |
| `EventModel.SetEventFinished(LocString)` | protected，结束事件 |
| `EventModel.Description/IsFinished/Node` | 事件状态 |
| `EventOption.IsProceed/TextKey` | 选项属性（判重） |
| `LocalContext.IsMine(EventModel?)` | 多人本地身份检查 |
| `NEventRoom.Proceed()` | 地图推进（离开事件） |