# 自定义事件：多页选项与注册辅助

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

