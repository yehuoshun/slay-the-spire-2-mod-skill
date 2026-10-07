# 遗物开局选择流程（开局必选一个遗物）

> 实战验证（sts2mod 基石符文 KeystoneRunes v0.4.1，2026-10-07）。模式：每局开始时给玩家一个自定义选择界面，从一组遗物里选一个获得。含多人同步、选择状态持久化、联机属性位宽修复。

## 流程接线（Task 替换 patch）

原版开局流程是异步的，postfix 里不能直接 `await`——用「替换 Task」模式：

```csharp
harmony.Patch(
    RequireMethod(typeof(RunManager), nameof(RunManager.FinalizeStartingRelics), BindingFlags.Instance | BindingFlags.Public),
    postfix: new HarmonyMethod(typeof(ModEntry), nameof(FinalizeStartingRelicsPostfix)));

private static void FinalizeStartingRelicsPostfix(RunManager __instance, ref Task __result)
{
    __result = AfterOriginal(__result, __instance);   // 替换成包装任务
}
private static async Task AfterOriginal(Task original, RunManager self)
{
    await original;                                   // 先等原版完成
    foreach (Player player in runState.Players) RemoveRunesFromGrabBags(player);  // 再做事
}
```

- 挂点：`RunManager.FinalizeStartingRelics`（清 grab bag）+ `NGame.StartRun(RunState)` / `NGame.LoadRun(RunState, SerializableRoom)` postfix（触发选择）。
- 载入旧局要谨慎：只在 Act0/Floor1、历史记录 ≤1、不在战斗中才弹选择（防读档闪界面）。
- 全部玩家的选择完成前用 `_selectionInProgress` 布尔防重入。

## 多人同步（PlayerChoiceSynchronizer）

```csharp
PlayerChoiceSynchronizer? synchronizer = await WaitForPlayerChoiceSynchronizerAsync(runManager);  // 轮询 200×50ms
uint choiceId = synchronizer.ReserveChoiceId(player);         // 为远程玩家保留选择槽
// 本地玩家：弹自己的选择界面 → synchronizer.SyncLocalChoice(player, choiceId, PlayerChoiceResult.FromIndex(index))
// 远程玩家：await synchronizer.WaitForRemoteChoice(player, choiceId) → result.AsIndex()
// AI 玩家（AITeammate mod）：随机选 / 主机代选
```

- `PlayerChoiceResult.FromIndex(int)` / `.AsIndex()`：选择结果编解码。
- 多人按 `NetId` 排序逐个发起（保证顺序一致）；每个玩家 `RelicCmd.Obtain(selected, player)`。

## 选择状态持久化（序列化 marker 技巧）

不想改存档格式又要记住「这局选过了」——把标记塞进现有 SavedProperty 容器：

```csharp
// ① 遗物基类自带标记字段（[SavedProperty]）
public abstract class Keystone_RelicBase : RelicModel
{
    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public bool KeystoneRunes_SelectionHandled { get; set; }   // 选过=某个遗物上为 true
}

// ② Player.ToSerializable postfix：把标记写进 save.Deck/Relics 第一个条的 SavedProperties bools
// ③ Player.FromSerializable postfix：读回标记 → 运行时表
private static readonly ConditionalWeakTable<Player, KeystoneSelectionRuntimeState> _selectionStates = new();
```

- 运行时状态用 `ConditionalWeakTable<Player, T>`（不持有 Player 引用，防泄漏）。
- marker 写入：`SavedProperties.bools` 里按 `name` 找/加 `SavedProperty<bool>`；读：`props.bools.Any(p => p.name == X && p.value)`。

## 联机属性位宽修复（重要坑）

`SavedPropertiesTypeCache.NetIdBitSize` 是联机属性 ID 的位宽，自定义 SavedProperty 多到溢出会 **desync**：

```csharp
// 读 _netIdToPropertyNameMap 数量 → 算所需位数 → 不足就把 backing field 调大
FieldInfo? backing = typeof(SavedPropertiesTypeCache).GetField("<NetIdBitSize>k__BackingField",
    BindingFlags.NonPublic | BindingFlags.Static);
backing.SetValue(null, targetBitSize);   // 反射改自动属性 backing field
```

- 自定义 SavedProperty 多的 mod（本例 16+）必须在注册后检查并扩位宽，否则多人联机丢数据。

## 可选 hook 组（容错安装）

```csharp
TryInstallOptionalHookGroup("asset hooks", () => AssetHooks.Install(harmony));
TryInstallOptionalHookGroup("collection hooks", () => CollectionHooks.Install(harmony));
// 每组独立 try-catch：游戏版本缺成员只跳过该组，不炸整个 mod
```

- 组内反射也用 `TryGetField/TryGetMethod`（找不到返回 null），比 fail-fast 更适合 UI 类 hook（可降级）。

## 相关 API 速查

| API | 说明 |
|-----|------|
| `RunManager.DebugOnlyGetState()` | 拿 RunState（调试名，真实 API） |
| `RelicCmd.Obtain(RelicModel, Player)` | 获得遗物（含图鉴标记） |
| `SaveManager.Instance.MarkRelicAsSeen(relic)` | 图鉴「已见」标记（选择界面出现过的遗物） |
| `player.RelicGrabBag.Remove(relic)` / `player.RunState.SharedRelicGrabBag.Remove(relic)` | 从随机池剔除（开局已选的不该再随机掉） |
| `NOverlayStack.Instance.Push/Remove` | 顶层 overlay 栈（选择界面挂这） |