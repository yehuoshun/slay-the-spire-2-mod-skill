# 无尽循环：确定性种子 / RunState 重建 / 多人同步

> 实战验证（sts2mod 无尽模式 EndlessMode v0.4.1，2026-10-07）。通关后跨轮重开的核心流程。

## 确定性轮种子（多人一致的关键）

```csharp
private static string CreateDeterministicLoopSeed(RunState state, int nextLoopIndex)
{
    string actIds = string.Join(",", state.Acts.Select(static a => a.Id.Entry));
    string basis = string.Join("|", state.Rng.StringSeed ?? string.Empty,
        nextLoopIndex, state.Players.Count, state.CurrentActIndex, actIds);
    uint high = (uint)StringHelper.GetDeterministicHashCode(basis);
    uint low = (uint)StringHelper.GetDeterministicHashCode(basis + "|EndlessMode");
    return ToSeedString(((ulong)high << 32) | low);   // 64 位 → 10 字符种子串（SeedAlphabet 进制）
}
```

- 派生输入**只用确定性状态**（Acts Id / 轮次 / 玩家数 / 当前幕 / 基础种子串）——禁读本地配置/随机源，保证联机两端同种子同结果。

## RunState 重建（跨轮清零 + 反射 init-only）

```csharp
private static void PrepareRunForLoop(RunState state, string loopSeed)
{
    AccumulateFloorsBeforeLoop(state);                    // ① 跨轮楼层累计（Interop 用）
    ((HashSet<ModelId>)VisitedEventIdsField.GetValue(state)!).Clear();   // ② 清已见事件
    ((List<List<MapPointHistoryEntry>>)MapPointHistoryField.GetValue(state)!).Clear(); // 清地图历史
    RunRngSet loopRng = new(loopSeed);
    RunOddsSet loopOdds = new(loopRng.UnknownMapPoint);
    RunStateRngSetter.Invoke(state, new object[] { loopRng });   // ③ 反射设置 init-only
    RunStateOddsSetter.Invoke(state, new object[] { loopOdds });
    RunStateMapSetter.Invoke(state, new object?[] { null });
    state.ClearVisitedMapCoordsDebug();                   // ④ 清地图坐标
    state.ExtraFields.StartedWithNeow = false;            // ⑤ 禁用第二幕 Neow
}
```

- `RunState.Rng/Odds` 是 `{ get; init; }`——外部只能反射 PropertyInfo 的 SetMethod 设置（`RequirePropertySetter` 拿 setter 方法 + Invoke）。`Map`/`Acts` 同理（Acts 也是 init 或私有 set）。
- 历史字段用反射：`_mapPointHistory`（List<List<MapPointHistoryEntry>>）、`_visitedEventIds`（HashSet<ModelId>）。

## 章节重建（条件编译种子差异）

```csharp
private static void RebuildActsForLoop(RunState state, string loopSeed)
{
#if STS2_109_OR_NEWER
    Rng actRng = new((ulong)(uint)StringHelper.GetDeterministicHashCode(loopSeed));  // 0.109+：ulong 单参
#else
    Rng actRng = new((uint)StringHelper.GetDeterministicHashCode(loopSeed), 0);      // 旧版：(uint, int)
#endif
    List<ActModel> rebuilt = ActModel.GetRandomList(actRng, state.UnlockState, state.Players.Count > 1)
        .Select(static a => a.ToMutable()).ToList();
    RunStateActsSetter.Invoke(state, new object[] { rebuilt });
}
```

- `ActModel.GetRandomList(Rng, UnlockState, bool isMultiplayer)`（真实 API）重新生成章节池；种子映射跨版本不同无碍（幕重建只需同版本两端一致，结果持久化进存档）。

## 完整流程（防重入 + 可重试）

```csharp
private static async Task EnterEndlessModeAsync(EventModel eventModel, ...)
{
    // 1. 校验：owner/state 存在 + 当前在建筑师事件（IsRunAtArchitectEvent 防过期请求）
    // 2. transitionKey = 轮次+玩家标识 → TryBeginEndlessTransition 防重复（重复请求直接忽略）
    // 3. loopSeed = CreateDeterministicLoopSeed(...)
    // 4. PrepareScreensForEndlessTransitionAsync（奖励屏/卡选屏清理，Detached 集合防泄漏）
    // 5. ResolveLoopConfigAsync（多人：PlayerChoiceSynchronizer 同步配置，不可用回退默认）
    // 6. AwardLoopRewardsAsync（按轮次 tier 发放遗物）
    // 7. PrepareRunForLoop + ResetHextechMayhemForEndlessLoop + RebuildActsForLoop
    // 8. runManager.GenerateRooms() → 等地图过渡帧 → EnterLoopFirstActWithoutRoomFade
    // 9. CompleteEndlessTransition；异常：Timeout → 30s 后重试（forget key）；其他 → rethrow
}
```

- 阶段 2 的 key 用 `Dictionary<string, EndlessTransitionRecord>` + lock（并发防重）。
- 远程选择等待上限 10 分钟（`RemoteEndlessChoiceMaxWait`），超时进入可重试分支。
- 轮次遗物 tier 封顶 4（第 5 轮起重复 HorribleTrophy 叠加层数）。

## 相关 API 速查

| API | 说明 |
|-----|------|
| `ActModel.GetRandomList(Rng, UnlockState, bool)` | 随机章节池（真实 API） |
| `RunState.Rng/Odds/Map/Acts` | `{ get; init; }` 或私有 set → 反射 PropertyInfo.SetMethod |
| `RunState.ClearVisitedMapCoordsDebug()` | 清地图坐标（真实 API） |
| `state.ExtraFields.StartedWithNeow` | 第二轮禁用 Neow（SerializableExtraRunFields） |
| `StringHelper.GetDeterministicHashCode(string)` | 确定性哈希（派生种子） |
| `RunRngSet(string)` / `RunOddsSet(Rng)` | 重建随机源 |