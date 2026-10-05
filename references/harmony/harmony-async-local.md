# Harmony：AsyncLocal 异步 Patch（Prefix 里跑异步逻辑）

> 实战验证（STS2_MarisaMod 2026-10-05 学习）。Harmony 的 `Prefix` 不能 `await`（同步方法），但原方法返回 `Task` 时，可以在 Prefix 里**构造异步委托**赋给 `ref Task __result`，用 `AsyncLocal` 把委托跨方法传出去。适合「替换原异步方法」的场景。

## 1. 模式（Entomancer.SpitMove 实例）

```csharp
[HarmonyPatch(typeof(Entomancer), "SpitMove")]
static class SpitMovePatch
{
    private static AsyncLocal<Func<Task>> _asyncWork = new();   // 跨 Prefix/Postfix 传委托

    private static bool Prefix(Entomancer __instance, ref Task __result)
    {
        if (__instance.Creature.HasPower<PersonalHivePower>())
            return true;                                          // 有特定能力 → 走原逻辑

        _asyncWork.Value = async () =>                            // 自定义异步逻辑
        {
            await CreatureCmd.TriggerAnim(__instance.Creature, "Cast", 0.5f);
            await PowerCmd.Apply<PersonalHivePower>(new ThrowingPlayerChoiceContext(),
                __instance.Creature, 1, __instance.Creature, null);
        };

        __result = _asyncWork.Value();                            // 把 Task 塞回原返回值
        return false;                                             // 跳过原方法体
    }

    private static void Postfix()
    {
        _asyncWork.Value = null!;                                 // 清理，防跨帧残留
    }
}
```

## 2. 变体：Prefix 里同步读私有字段 + 异步执行

```csharp
static bool Prefix(TheArchitect __instance, ref Task __result)
{
    var field = AccessTools.Field(typeof(TheArchitect), "_dialogue");
    if (field?.GetValue(__instance) != null) return true;        // 有对话 → 原逻辑

    if (LocalContext.IsMe(__instance.Owner))
        RunManager.Instance.ActChangeSynchronizer.SetLocalPlayerReady();

    __result = Task.CompletedTask;                                // 不需要异步时直接给完成 Task
    return false;
}
```

`AccessTools.Field`（HarmonyLib）可读**私有字段**，配合 Prefix 可以在跳过原方法前检查内部状态。

## 3. 适用场景

| 场景 | 做法 |
|------|------|
| 替换原异步方法（返回 Task） | Prefix + AsyncLocal + `ref Task __result` + `return false` |
| 完全跳过且无异步需求 | `__result = Task.CompletedTask; return false;` |
| 只改返回值 | Postfix + `ref Task/其他类型 __result` |
| 需要异步但原方法不是 Task | 不行——改成同步逻辑或换触发点（事件/帧回调） |

## 4. 常见坑

| 坑 | 解法 |
|----|------|
| `async void` Prefix 直接炸 | 永远别用；Prefix 保持同步，异步逻辑包进委托 |
| AsyncLocal 不清理 | Postfix 里 `= null!`，否则下次调用拿到旧委托 |
| 委托里异常吞掉 | 委托内 try-catch + `Log.Error`（硬规则 6） |
| `ref Task` 不赋值 | 必须赋值 `__result` 再 `return false`，否则原方法照跑 + 返回 null Task 崩 |
| Prefix 参数签名 | 只列需要的参数即可，`__instance` / `__result` 是约定名 |
