# 自定义角色：资源覆写点与解锁屏蔽

> 实战验证（STS2_MarisaMod 2026-10-05）。BaseLib `PlaceholderCharacterModel` 提供约 20 个 `Custom*` 覆写点（任意指定资源路径）；**纯原生 `CharacterModel` 只有 2 个路径覆写点**，其余靠默认路径约定（character-paths.md）或 Patch 兜底。

## 1. BaseLib 覆写点全清单（理解 BaseLib mod 用）

```csharp
public class MarisaCharacter : PlaceholderCharacterModel
{
    // 场景（路径任意，仅示例）
    public override string CustomVisualPath => "res://scenes/x.tscn";              // 战斗待机
    public override string CustomTrailPath => "res://scenes/x_trail.tscn";         // 卡牌拖尾
    public override string CustomIconPath => "res://scenes/x_icon.tscn";           // 头像图标场景
    public override string CustomEnergyCounterPath => "res://scenes/x_energy.tscn"; // 能量计数器
    public override string CustomRestSiteAnimPath => "res://scenes/x_rest.tscn";   // 火堆休息
    public override string CustomMerchantAnimPath => "res://scenes/x_merchant.tscn"; // 商店
    public override string CustomCharacterSelectBg => "res://scenes/x_bg.tscn";    // 选人背景

    // 纹理
    public override string CustomIconTexturePath => "res://images/x_icon.png";             // 头像缩略图
    public override string CustomCharacterSelectIconPath => "res://images/x_select.png";   // 选人底图
    public override string CustomCharacterSelectLockedIconPath => "res://images/x_locked.png"; // 未解锁底图
    public override string CustomArmPointingTexturePath => "res://images/x_point.png";     // 联机手势×4
    public override string CustomArmRockTexturePath => "res://images/x_rock.png";
    public override string CustomArmPaperTexturePath => "res://images/x_paper.png";
    public override string CustomArmScissorsTexturePath => "res://images/x_scissors.png";
    public override RelicIconData CustomYummyCookie => new("x.png", "x_s.png", "x_s_o.png"); // 曲奇图标
    public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_ironclad";  // 进战斗过场音
}
```

配套行为覆写：`StartingDeck` / `StartingRelics` / `GetArchitectAttackVfx()`（攻击建筑师特效名列表）/ `GenerateAnimator`（monster-animator.md）/ `NameColor` / `EnergyLabelOutlineColor` / 三池（`ModelDb.CardPool<T>` 引用）。

## 2. 纯原生对照（CharacterModel）

| 覆写点 | BaseLib Placeholder | 纯原生 CharacterModel |
|--------|--------------------|----------------------|
| 场景/纹理/手势路径 | `Custom*` 全可覆写 | ❌ 无，走默认路径约定（character-paths.md） |
| 进战斗过场音 | `CharacterTransitionSfx` | ✅ 原生虚属性（sts2-res 已验证） |
| 能量标签描边色 | `EnergyLabelOutlineColor` | ✅ 原生虚属性 |
| 动画状态机 | `GenerateAnimator` | ✅ 原生虚方法 |
| 曲奇图标 | `CustomYummyCookie` | ❌ 需 Patch `YummyCookie.IconBaseName` Getter |

> 纯原生要换资源 = 把资源放到默认约定路径（`res://scenes/creature_visuals/<ID>.tscn` 等）；改曲奇/地图标记这类无约定的，Patch 对应 Getter 返回自定义路径（Entry.cs 注释代码里有 `YummyCookie.IconBaseName` Patch 示例）。

## 3. 解锁进度屏蔽（自定义角色不计入全局解锁）

原版 `ProgressSaveManager` 有全局解锁进度（15 精英/15 Boss/角色解锁），自定义角色跑局会污染进度。用 Prefix 屏蔽：

```csharp
[HarmonyPatch(typeof(ProgressSaveManager), "ObtainCharUnlockEpoch")]
static class UnlockPatch1 { static bool Prefix(ProgressSaveManager __instance, Player localPlayer)
    => localPlayer.Character is not MarisaCharacter; }   // 自定义角色 → 跳过

[HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenElitesDefeatedEpoch")]
static class UnlockPatch2 { static bool Prefix(ProgressSaveManager __instance, Player localPlayer)
    => localPlayer.Character is not MarisaCharacter; }

[HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenBossesDefeatedEpoch")]
static class UnlockPatch3 { static bool Prefix(ProgressSaveManager __instance, Player localPlayer)
    => !localPlayer.Character.Id.ToString().Contains("MarisaMod", StringComparison.OrdinalIgnoreCase); }
```

> 三方法均在 `MegaCrit.Sts2.Core.Saves.Managers.ProgressSaveManager`（private，Prefix 可 Patch，已对照 sts2-res）。`ObtainCharUnlockEpoch` 带 `int act` 参数，Prefix 签名只列需要的参数即可。

## 4. 常见坑

| 坑 | 解法 |
|----|------|
| BaseLib 覆写点移植纯原生报错 | 先查 `CharacterModel.cs` 有无该虚属性（目前只有 2 个路径点），没有就走路径约定/Patch |
| 选人界面图缺失 | 锁定图与底图都要给，缺锁定图选人界面花屏 |
| 解锁进度被自定义角色刷掉 | 三个 ProgressSaveManager Patch 缺一不可 |
| 曲奇遗物图标不生效 | 纯原生 Patch `YummyCookie.IconBaseName` Getter（`IsCanonical` 为真时放行） |
