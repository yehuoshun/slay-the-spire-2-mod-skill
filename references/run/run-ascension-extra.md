# 进阶难度扩展（11-15 进阶）

> 实战验证（sts2mod 更多进阶挑战 MoreAscensionChallenge v0.1.1，2026-10-07）。把进阶上限 10 → 15，新增 5 档难度。全部用 MonoMod detour 实现（见 [harmony-detour-monomod.md](../harmony/harmony-detour-monomod.md)）。无本地化/无 PCK（`has_pck:false` 纯 DLL mod）。

## ① 解锁全开（确保 UI 可选 15）

```csharp
private static void EnsureAllAscensionsUnlocked()
{
    try
    {
        var progress = SaveManager.Instance.Progress;
        if (progress.MaxMultiplayerAscension < 15) progress.MaxMultiplayerAscension = 15;
        foreach (var character in ModelDb.AllCharacters)
        {
            var stats = progress.GetOrCreateCharacterStats(character.Id);
            if (stats.MaxAscension < 15) stats.MaxAscension = 15;
        }
    }
    catch { }   // 解锁失败静默（不影响游戏可玩）
}
```

- `ProgressState.MaxMultiplayerAscension` + `CharacterStats.MaxAscension`（真实 API）；`GetOrCreateCharacterStats(ModelId)`。
- 每次 detour 入口先调（幂等，防其他 mod 重置）。

## ② 难度机制 detour 清单（进阶 11-15）

| 进阶 | 机制 | detour 目标 |
|------|------|------------|
| 11 补给短缺 | 药水掉落变稀有 | `PotionRewardOdds.Roll(Player, AscensionManager, RoomType)` 全重写（CurrentValue ±0.1 游走 + 按幕 baseOdds） |
| 12 路途匆匆 | 地图短一截 | `ActModel.GetNumberOfRooms(bool)` 减 1 |
| 13 浅眠难安 | 休息只回缺失生命 30% | `HealRestSiteOption.GetBaseHealAmount(Creature)` 改公式 + `NRestSiteRoom.SetText` 中英文案替换 |
| 14 进阶之灾+ | Bane 升级（去虚化+标题+） | `CardModel.Keywords` / `CardModel.Title` getter detour（`card is AscendersBane` + 进阶≥14） |
| 15 降级 | 不掉已升级卡 | `Hook.ModifyCardRewardUpgradeOdds(IRunState, Player, CardModel, decimal)` 返回 `decimal.MinValue` |

- 判断：`HasExtraAscension(runState, n)` = `runState.AscensionLevel >= n`（IRunState 接口）。
- **进阶 12 地图长度的坑**：`GetNumberOfRooms` 拿不到 runState → 用 `ActiveMapGenerationAscensionLevel`（`ActModel.CreateMap` detour 里 try/finally 记录当前进阶，供 GetNumberOfRooms 读）——跨方法状态传递模式。
- 进阶 13 文案：中英文案字符串 Replace（无本地化文件的纯 DLL mod 的做法）。

## ③ UI 覆写（面板/顶栏/历史记录）

- `NAscensionPanel.SetMaxAscension(int)` detour：单人/主机模式把上限提到 15（`_mode` 字段判 `MultiplayerUiMode`）。
- `NAscensionPanel.RefreshAscensionText` detour：>10 时自绘标题/描述（本地数组 ExtraTitles/Descriptions 双语）。
- `NTopBarPortraitTip.Initialize(IRunState)` detour：>10 时替换 hover 提示（`BuildAscensionHoverTip` 逐级列标题）。
- `NRunHistoryPlayerIcon.LoadRun(RunHistoryPlayer, RunHistory)` detour：>10 时**反射重建节点**（角色图标/进阶标签/hover 列表全手动）——原版假定 ≤10。

## ④ 偏好持久化（profile-scoped JSON）

```csharp
private static string GetPreferencePath()
{
    var profileId = SaveManager.Instance.CurrentProfileId;
    return ProjectSettings.GlobalizePath(UserDataPathProvider.GetProfileScopedPath(profileId, "more_ascension_prefs.json"));
}
// 结构：Dictionary<角色Entry, int>（只存 >10 的值，加载时 clamp 11-15）
// Set/Clear 在 SyncAscensionChange/SetLocalCharacter 等 detour 里按角色写
```

- `UserDataPathProvider.GetProfileScopedPath(int profileId, string dataType, ...)`（真实 API，带平台/用户参数默认值）。
- 读写全 try-catch（只 Warn 不炸）；`EnsurePrefsLoaded` 路径变化时重载并合并旧内存数据。

## ⑤ 多人同步（消息格式扩展）

- `ClientLobbyJoinResponseMessage.Serialize/Deserialize` **成对全重写**（见 harmony-detour-monomod.md）——不调 orig，完整读写。
- `StartRunLobby.TryAddPlayerInFirstAvailableSlot(SerializableUnlockState, int maxAscensionUnlocked, ulong)` detour：把解锁上限提到 15。
- 单人偏好恢复：`SetSingleplayerAscensionAfterCharacterChanged` detour 里 `PendingRestore*` 临时字段桥接原版流程（原版会重置进阶，detour 在 finally 后按存档值 SyncAscensionChange 恢复）。

## 相关 API 速查

| API | 说明 |
|-----|------|
| `PotionRewardOdds.Roll(...)` / `AbstractOdds.CurrentValue` / `OverrideCurrentValue` | 药水掉率 |
| `ActModel.GetNumberOfRooms(bool)` / `CreateMap(RunState, bool)` | 地图长度/生成 |
| `Hook.ModifyCardRewardUpgradeOdds(...)` | 升级卡概率修正点 |
| `HealRestSiteOption.GetBaseHealAmount(Creature)` | 休息基础回复 |
| `AscendersBane` | 进阶之灾卡 |
| `SaveManager.Instance.Progress` / `GetOrCreateCharacterStats(ModelId)` | 解锁状态 |
| `UserDataPathProvider.GetProfileScopedPath(int, string, ...)` | profile 路径 |