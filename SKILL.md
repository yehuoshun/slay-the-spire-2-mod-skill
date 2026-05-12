# 杀戮尖塔 2 (Slay the Spire 2) Mod 开发技能

> 基于 STS2Plus v0.2.0 (by 4step) 深度逆向，作为 Mod 开发的蓝本参考。
> STS2Plus GitHub: https://github.com/StephenSHorton/STS2Plus

**适用场景**：本技能用于帮助 AI 理解 STS2 的 Mod 运行时架构，辅助编写、调试、扩展 STS2 Mod。当你被问到任何关于 STS2 Mod 开发的问题时，使用本技能作为参考。

**核心技能**：你需要参考本仓库的源代码来回答 STS2 Mod 开发相关问题。本 SKILL.md 是对 STS2Plus 的深度分析文档，但实际实现细节应以仓库源码为准。

> **作者注**：本仓库当前为占位框架（SKILL.md + README.md 为空），但 .git commit history 中包含 STS2Plus v1.1 的完整实现。`git log` 可追溯历史版本，`git diff` 可查看最新变更。建议在需要时 checkout 历史版本查看完整源码。

---

## 🏗️ 架构概览

STS2 使用 **Godot 4 + C# (.NET 9)** 开发。Mod 通过 **Harmony** 运行时修补（Runtime Patching）实现，以 DLL 形式加载。

### 技术栈

| 组件 | 版本/类型 |
|------|-----------|
| 语言 | C# 12 (.NET 9) |
| 游戏引擎 | Godot 4.x (GodotSharp) |
| 修改框架 | Harmony (Lib.Harmony) |
| 构建 | dotnet CLI + PowerShell |
| 目标框架 | `net9.0` |
| 输出 | Library (DLL) |

### 项目结构

```
STS2Plus/
├── STS2Plus.csproj          # 项目文件
├── build.ps1                # 构建脚本
├── mod_manifest.json        # Mod 清单（复制为 STS2Plus.json 到 Mods/ 目录）
├── GodotPlugins.Game/       # 联机/多玩家 Hook 入口
├── Properties/              # AssemblyInfo
├── STS2Plus/               # 核心入口 + 状态管理
├── STS2Plus.Config/         # 配置系统
├── STS2Plus.Features/       # 游戏功能逻辑
├── STS2Plus.Localization/   # 本地化（英/土耳其语）
├── STS2Plus.Modifiers/      # 自定义游戏修正器（Custom Modifiers）
├── STS2Plus.Modules/        # 模块定义
├── STS2Plus.Multiplayer/    # 联机消息定义
├── STS2Plus.Patches/        # Harmony 补丁集合
├── STS2Plus.Reflection/     # 运行时反射工具
└── STS2Plus.Ui/             # Godot UI 控件
```

---

## 🔌 mod_manifest.json 格式

```json
{
  "id": "STS2Plus",           // Mod 唯一 ID
  "name": "STS2Plus",         // 显示名称
  "author": "4step",          // 作者
  "description": "...",       // 描述
  "version": "0.2.0",         // 版本号
  "has_dll": true,            // 是否包含 DLL
  "has_pck": false,           // 是否包含 Godot 包
  "dependencies": [],         // 依赖
  "affects_gameplay": true    // 是否影响游戏玩法
}
```

安装位置：`<GameDir>/Mods/STS2Plus/STS2Plus.dll` + `<GameDir>/Mods/STS2Plus/STS2Plus.json`

---

## 🚀 入口点 (ModEntry)

通过 `[ModInitializer("Initialize")]` 属性标记入口方法，游戏在加载时自动调用。

```csharp
[ModInitializer("Initialize")]
[ScriptPath("res://src/ModEntry.cs")]
public class ModEntry : Node
```

**初始化流程**：
1. 加载配置 (`ConfigManager.Load()`)
2. 创建 Harmony 实例
3. 按补丁分类打补丁（Core → MoreRules）
4. 合并本地化文本
5. 完成初始化

**补丁分类**：

| 分类 | 内容 |
|------|------|
| `Core` | 基础功能（速度控制/快速重启/伤害告警/路线建议/跳过Intro/稀有度标签/卡牌总伤害预览） |
| `MoreRules` | 自定义游戏规则修正器 |
| `DpsMeter` | DPS 统计相关（未实现） |

### 日志系统

使用游戏自带 `MegaCrit.Sts2.Core.Logging.Logger`，带 Verbose 开关控制：

```csharp
public static Logger Logger { get; } = new Logger("STS2Plus", LogType.Info);
public static void Verbose(string message)
{
    if (ConfigManager.Current.VerboseLoggingEnabled)
        Logger.Info("[VERBOSE] " + message);
}
```

---

## ⚙️ 配置系统 (Config)

### PlusConfig

JSON 配置文件，存储在 `%AppData%/SlayTheSpire2/ModConfig/STS2Plus.json`。

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `MoreRulesEnabled` | bool | true | 自定义规则总开关 |
| `RouteAdvisorEnabled` | bool | true | 路线建议 |
| `CompactRelicDrawerEnabled` | bool | false | 紧凑遗物界面 |
| `SpeedControlEnabled` | bool | true | 战斗加速 |
| `QuickRestartEnabled` | bool | true | F5 快速重新开始 |
| `SkipIntroEnabled` | bool | true | 跳过启动画面 |
| `CardTotalDamagePreviewEnabled` | bool | true | 卡牌总伤害预览 |
| `RarityTagsEnabled` | bool | true | 稀有度标签 |
| `PlayerCombatShieldEnabled` | bool | true | 玩家护盾数值显示 |
| `VerboseLoggingEnabled` | bool | true | 详细日志 |

### ConfigManager

```csharp
internal static class ConfigManager
{
    public static PlusConfig Current { get; private set; }
    public static void Load();     // 从文件加载，不存在则创建
    public static void Save();     // 保存到文件
}
```

---

## 🎭 自定义修正器 (Modifiers) — 核心系统

### 修正器架构

每个修正器继承 `SyncedModifierModel → ModifierModel`，仅需一个空类体（逻辑通过 Harmony Patches 注入）：

```csharp
namespace STS2Plus.Modifiers;
internal sealed class AttackDefense : SyncedModifierModel { }
```

### SyncedModifierModel

提供 Title/LocString 和 IconPath 的标准实现，支持联机同步：

```csharp
internal abstract class SyncedModifierModel : ModifierModel
{
    public override LocString Title => new LocString("modifiers", Id.Entry + ".title");
    public override LocString Description => new LocString("modifiers", Id.Entry + ".description");
}
```

**图标映射**（使用游戏内置修正器图标）：

| Entry | 内置图标 |
|-------|----------|
| ATTACK_DEFENSE | `all_star` |
| ATTACK_DEFENSE_PLUS | `murderous` |
| IRON_SKIN | `terminal` |
| GIANT_CREATURES | `big_game_hunter` |
| HARD_ELITES | `vintage` |
| ENDLESS_MODE | `flight` |
| GLASS_CANNON | `insanity` |
| UNLIMITED_GROWTH | `hoarder` |
| SANDBOX | `sealed_deck` |
| BUILD_CREATOR | `sealed_deck` |

### CustomModifierCatalog

修正器的注册、匹配和反序列化工厂。关键方法：

```csharp
// 创建修正器实例
public static ModifierModel Create(string entry);

// 从存档数据尝试恢复修正器（支持旧版兼容）
public static ModifierModel? TryCreate(SerializableModifier serializable);

// 检查修正器列表中是否包含某条目
public static bool ContainsEntry(IEnumerable<ModifierModel>? modifiers, string entry);

// 判断修正器是否已知
public static bool IsKnownModifier(ModifierModel? modifier);
```

**修正器编号常量**（用于存档兼容）：

| 位 (Flags) | 常量 | Entry |
|-----------|------|-------|
| 1 | AttackDefenseFlag | ATTACK_DEFENSE |
| 2 | AttackDefensePlusFlag | ATTACK_DEFENSE_PLUS |
| 4 | IronSkinFlag | IRON_SKIN |
| 8 | GiantCreaturesFlag | GIANT_CREATURES |
| 16 | HardElitesFlag | HARD_ELITES |
| 32 | EndlessModeFlag | ENDLESS_MODE |
| 64 | GlassCannonFlag | GLASS_CANNON |
| 128 | UnlimitedGrowthFlag | UNLIMITED_GROWTH |
| 256 | SandboxFlag | SANDBOX |
| 512 | BuildCreatorFlag | BUILD_CREATOR |

### 修正器选择/同步状态 (PlusState)

核心状态管理类，跟踪所有修正器的选择状态、提供活跃性检查、计算攻击/防御卡牌数值加成。

**选择状态同步**：
- `SyncRuleSelections(IEnumerable<ModifierModel>)`：从修正器列表同步
- `SyncRuleSelectionsFromRunState()`：从运行状态同步，支持挂起选择恢复
- 每项都有 `Selected` 属性和 `Set{Name}Rule(bool)` 方法

**挂起选择 (Pending Selections)**：当运行状态丢失修正器时自动恢复挂起选择

**数值计算**：
```csharp
// 根据活跃修正器计算攻击卡牌加成
public static decimal GetAttackCardBonus()
// 根据活跃修正器计算防御卡牌加成
public static decimal GetDefenseCardBonus()
```

**GlassCannon 特殊处理**：
- `EnsureGlassCannonExpectedMaxHp(object? player, int fallbackMaxHp)`
- `RememberGlassCannonExpectedMaxHp(object? player, int value)`
- `TryGetGlassCannonExpectedMaxHp(object? player, out int value)`
- `IncreaseGlassCannonExpectedMaxHp(object? player, int amount)`

**DPS 统计**：
```csharp
public static decimal CombatDamageTotal { get; private set; }
public static void AddCombatDamage(decimal amount);
public static void ResetCombatDamage();
```

---

## 🧩 修正器详解 (10种)

### 1. ATTACK_DEFENSE — 攻守平衡

所有攻击卡牌 **+2 伤害**，所有防御卡牌 **+2 格挡**。

### 2. ATTACK_DEFENSE_PLUS (Warrior) — 战士

所有攻击卡牌 **+5 伤害**，所有防御卡牌 **+5 格挡**。

### 3. IRON_SKIN — 铁甲

所有攻击卡牌 **-2 伤害**，所有防御卡牌 **+5 格挡**。

### 4. GIANT_CREATURES — 巨型生物

遭遇的所有生物 **HP 翻倍**，战斗金币奖励 **+100%**。

**补丁范围**：HP 相关 Patch、金币相关 Patch、奖励标记 (AppliedTracker)

### 5. HARD_ELITES — 精英强化

精英房间敌人 **+50% HP**，精英战斗金币奖励 **+200%**，精英 **掉落 2 件遗物**。

### 6. ENDLESS_MODE — 无尽模式

第 3 幕结束后不终止游戏，进入**新循环**。

**补丁范围**：Act 条幅、架构者、进入 Act、HP 缩放、杀死玩家、涅奥选项、涅奥房间、UI 刷新、获胜结算等等

### 7. GLASS_CANNON — 玻璃大炮

- 起始 **1 最大 HP**
- **+100% 金币**获得
- 每场战斗后 **+1 最大 HP**
- 攻击卡牌 **+3 伤害**
- 每回合 **4 能量**
- 每回合保留最多 **15 格挡**

### 8. UNLIMITED_GROWTH — 无限成长

卡牌可**重复升级**，每次升级**再次应用当前升级效果**。

**补丁范围**：反序列化、最大升级等级、序列化上下文

### 9. SANDBOX — 沙盒

所有敌人每阶段起始 **1 HP**。

### 10. BUILD_CREATOR — 构筑者

自定义开局：在涅奥事件中选择任意数量的**角色牌池中的卡牌** (+升级版)、**遗物**，然后与一个不朽的木桩战斗。

**特殊点**：`BuildCreator : SyncedModifierModel` 重写了 `GenerateNeowOption` 方法：

```csharp
public override Func<Task>? GenerateNeowOption(EventModel eventModel)
{
    return () => BuildCreatorOverlay.OpenAsync(eventModel);
}
```

---

## ✨ UI 组件 (6个)

### SpeedControlOverlay

**作用**：战斗速度控制（1x ~ 5x），右上角显示速度选择器。

- 通过 `Engine.TimeScale` 控制游戏速度
- 仅在战斗中激活（`IsCombatSpeedActive()`）
- 自动挂载到场景树的 Root Window 上
- 使用 CanvasLayer 作为独立图层
- 暂停时自动隐藏 (NOverlayStack 检测)
- 拖尾动画调整：`ApplyTweenSpeed(Tween tween)`

**速度选择**：
- 按钮：`1x`, `2x`, `3x`, `4x`, `5x`（游戏速 = %Speed 按钮值 / %Combat 当前选择速度）
- SpeedGlyph: 自定义绘制控件，用三角形数量指示速度等级
- 持久 Manager Node (`_Process` 处理状态同步)

### RouteAdvisorHighlighter

**作用**：地图路线建议高亮。

- `Safe` 路线（金色）：偏好商店(7) > ?事件(5) > 休息(4) > 普通战斗(2) > 精英(1) > 宝藏(-3) > 首领(3) 
- `Aggressive` 路线（红色）：偏好精英(8) > 宝藏(3) > 休息(4) > 普通战斗(2) > 商店(1) > ?事件(1) > 首领(5)

**高亮逻辑**：
- 路线地图段被着色为金色/红色
- 两条路线重叠部分显示橙色
- 使用 `VisualStateCache`（`ConditionalWeakTable`）记录原始颜色/缩放用于重置

### IncomingDamageOverlay

**作用**：在敌方单位头上显示**对本玩家的预计伤害值**。

- 实时追踪意图伤害
- 显示扣血值 + LETHAL 警告
- 颜色分级：<10 黄，10-20 紫，>20 红
- 通过 `IncomingDamageTracker` 处理伤害追踪
- 附着在敌方 CreatureNode 上
- 自动隐藏当无有效伤害时

### PlayerDamageShieldBadge

**作用**：玩家头顶显示**攻防差值** (Block + PetHP - 收到的伤害)。

- 正数=蓝（可承受）→ 零数=白 → 负数=黄/紫/红（致命）
- 显示 `+N` / `-N` / `0`
- 通过 `PlayerDamageTracker` 计算
- 考虑 Osty (宠物) HP 作为缓冲

### CompactRelicDrawer

**作用**：紧凑遗物抽屉，代替原生遗物界面。

- 按钮显示已拥有遗物数量
- 点击弹出覆盖层显示所有遗物（网格布局）
- 支持搜索（使用游戏内置 `MegaInput.cancel` 关闭）
- 支持联机模式下手动定位
- 自动将原生 NRelicInventory 隐藏

### MoreRulesUi

**作用**：在创建游戏时向自定义规则列表注入 STS2Plus 的规则行。

- 搜索 `NRunModifierTickbox` 模板行，克隆后注入
- 每个修正器对应一行，带勾选框
- 支持联机锁定 (MultiplayerSafety interaction lock)
- 行点击切换状态 → 触发 `EmitSignalModifiersChanged`

### BuildCreatorOverlay（在 Modifiers/BuildCreator.cs 中关联）

**作用**：构筑者 UI，在涅奥事件选择时打开。

---

## 🔧 核心补丁 (Patches)

### 核心功能 (PatchCategory: Core)

| 补丁文件 | 作用 |
|---------|------|
| `SpeedControlBootstrapPatch` | 附加到 NGame._Ready，启动速度控制 Overlay |
| `SpeedControlMainMenuPatch` | 返回主菜单时重置/隐藏速度控制 |
| `SpeedControlEnterActPatch` | 进入新 Act 时同步速度控制 |
| `SpeedControlCombatSetupPatch` | 战斗开始时显示速度控制 |
| `SpeedControlCombatExitPatch` | 战斗结束时调整速度控制 |
| `SpeedControlRunUiReadyPatch` | UI 就绪时应用速度设置 |
| `SpeedControlTweenPatch` | 为任意 Tween 调用 ApplyTweenSpeed |
| `QuickRestartPatch` | F5 快速重新开始当前角色运行 |
| `CardTotalDamageHoverPatch` | 卡牌悬浮提示显示总伤害 = 单次 × 连击次数 |
| `SkipIntroLogoPatch` | 跳过开场 Logo |
| `SkipEarlyAccessDisclaimerPatch` | 跳过 Early Access 声明 |
| `NMainMenuPatch` | 主菜单同步/控制 |
| `HoverTipRarityPatch` | 卡牌悬浮提示显示稀有度标签 |
| `RouteAdvisorMapPatch` | 地图 UI 更新时刷新路线建议高亮 |
| `CompactRelicDrawer*Patch` (6个) | 紧凑遗物抽屉的附着/动画/显示/隐藏/定位 |
| `IncomingDamageIntentPatch` | 追踪意图伤害 |
| `IncomingDamageCombatSetupPatch` | 战斗开始重置伤害追踪 |
| `IncomingDamageCreatureExitPatch` | 生物退场清理 |
| `IncomingDamageHideIntentPatch` | 隐藏意图时清理 |
| `IncomingDamagePerformIntentPatch` | 意图执行时清理 |
| `IncomingDamageCreatureRefreshPatch` | 生物刷新时更新 UI |
| `IncomingDamageCreatureLifecyclePatch` | 生物生命周期管理 |
| `PlayerDamageIntentVisualsPatch` | 意图可视化时重新计算玩家防御 |
| `PlayerDamageIntentUpdatePatch` | 意图更新时重新计算 |
| `PlayerDamagePerformIntentPatch` | 意图执行后重新计算 |
| `PlayerDamageCombatSetupPatch` | 战斗开始时初始化 |
| `PlayerDamageCreatureExitPatch` | 生物退场时清理 |
| `PlayerDamageCreatureUpdateIntentPatch` | 生物意图更新时重新计算 |
| `PlayerDamageGainBlockPatch` | 获得格挡时更新 |
| `PlayerDamageLoseBlockPatch` | 失去格挡时更新 |
| `PlayerDamageClearBlockPatch` | 清除格挡时更新 |
| `PlayerDamageHideIntentPatch` | 隐藏意图时清理 |
| `PlayerDamageRefreshIntentsPatch` | 意图刷新时重算 |
| `MapDrawingMessageSafetyPatch` | 地图绘图消息安全性（联机） |
| `MultiplayerQuickRestartCoordinator` | 联机模式 F5 协调 |
| `MultiplayerRuleSyncCoordinator` | 修正器规则同步协调 |
| `MultiplayerRuleSyncCleanupPatch` | 规则同步清理 |
| `MultiplayerRuleSyncLaunchPatch` | 规则同步启动 |
| `MultiplayerRuleSyncMainMenuPatch` | 主菜单规则同步 |
| `NCustomRunModifiersListPatch` | 自定义修改器列表 UI 注入 |
| `PlusLifecyclePatch` | Plus 生命周期管理 |
| `QuickRestartMainMenuResetPatch` | 快速重新开始时重置主菜单 |
| `QuickRestartRunCleanupPatch` | 快速重新开始时清理运行 |
| `QuickRestartRunLaunchPatch` | 快速重新开始时启动运行 |
| `SilverCrucibleTreasurePatch` | 银圣杯宝藏处理 |
| `DeprecatedModifierHoverTipPatch` | 旧版修正器悬浮提示 |
| `DeprecatedModifierTopBarPatch` | 旧版修正器顶部栏 |
| `ModifierLocalizationPatch` | 修正器本地化 |

### 规则修正器补丁 (HarmonyPatchCategory: MoreRules)

| 补丁文件 | 作用 |
|---------|------|
| `AttackDefenseCardCreationPatch` | 攻击/防御卡牌创建时应用加成 |
| `AttackDefensePlayerSyncPatch` | 联机同步攻击/防御加成 |
| `GiantCreaturesPatch` | 巨型生物 HP 翻倍 |
| `GiantCreaturesGoldRewardFixedPatch` | 固定金币奖励翻倍 |
| `GiantCreaturesGoldRewardRangePatch` | 范围金币奖励翻倍 |
| `HardElitesPatch` | 精英 HP +50% |
| `HardElitesExtraRelicPatch` | 精英掉率 2 件遗物 |
| `HardElitesGoldRewardFixedPatch` | 精英固定金币 +200% |
| `HardElitesGoldRewardRangePatch` | 精英范围金币 +200% |
| `EndlessMode*Patch` (7个) | 无尽模式全套实现 |
| `GlassCannon*Patch` (16个) | 玻璃大炮全套实现（HP/能量/格挡/金币/同步等） |
| `UnlimitedGrowthMaxUpgradePatch` | 移除升级上限 |
| `UnlimitedGrowthDeserializePatch` | 重复升级反序列化 |
| `SandboxEnemyHpPatch` | 敌人 HP=1 |
| `SandboxEnemyMaxHpClampPatch` | 敌人最大 HP 限制 |
| `SandboxEnemyCurrentHpClampPatch` | 敌人当前 HP 限制 |
| `BuildCreatorEncounterReplacementPatch` | 构筑者遭遇替换 |
| `BuildCreatorEnemySetupPatch` | 构筑者敌人设置 |
| `BuildCreatorTurnHealPatch` | 构筑者回合治疗 |
| `CustomModifierListPatch` | 自定义修正器列表 |
| `CustomModifierListSyncPatch` | 自定义修正器列表同步 |
| `CustomModifierRunStateSyncPatch` | 运行状态同步 |
| `CustomModifierSerializationPatch` | 修正器序列化 |
| `CustomModifierUiRefreshPatch` | 修正器 UI 刷新 |
| `EndlessModeKillPlayersPatch` | 无尽模式杀死玩家 |
| `EndlessModeWinRunPatch` | 无尽模式获胜 |
| `EndlessModeArchitectPatch` | 无尽模式架构者 |
| `EndlessModeArchitectWinRunPatch` | 无尽模式架构者获胜 |
| `EndlessModeNeowRoomPatch` | 无尽模式涅奥房间 |
| `EndlessModeNeowOptionsPatch` | 无尽模式涅奥选项 |
| `EndlessModeEnterActPatch` | 无尽模式进入幕 |
| `EndlessModeHpScalingPatch` | 无尽模式 HP 缩放 |
| `EndlessModeUiRefreshPatch` | 无尽模式 UI 刷新 |
| `EndlessActBannerPatch` | 无尽幕横幅 |

---

## 🪞 反射工具 (Reflection Helpers)

### GameReflection

核心反射工具类，用于访问游戏私有/内部 API。

**关键方法分组**：

**地图相关**：
- `GetRouteStartPoint()` — 当前路线起点
- `GetMapPointChildren(object point)` — 地图点子节点
- `GetMapPointType(object point)` — `MapPointType` 枚举（Monster/Elite/Shop/Rest/Treasure/Boss/Mystery）
- `GetMapPointCoord(object point)` — 坐标

**生物/实体相关**：
- `HasActiveModifier(string entry)` — 检查当前运行是否包含某修正器
- `IsAttackCard(object card)` — 是否攻击卡牌
- `GetCardDisplayedDamage(object card)` — 显示伤害
- `GetCardMultiPlayCount(object card)` — 连击次数
- `GetCreatureEntity(object node)` — 从节点提取实体
- `IsAnyPlayerCreature(object)` — 是否任意玩家生物
- `IsLocalPlayerObject(object)` / `IsLocalPlayerCreature(object)` — 是否本地玩家
- `GetPlayerStateKey(object player)` — 玩家状态键
- `GetPlayerCreature(object player)` — 玩家实体
- `GetCurrentHp(object)` / `GetCreatureCurrentHp(object)` — 当前 HP
- `GetMaxHp(object)` — 最大 HP
- `GetCurrentBlock(object)` — 当前格挡
- `HasHealthAccess(object)` — 是否有 HP 访问权限
- `ResolveOwningPlayer(object)` — 解析所有者玩家
- `GetIntentTotalDamage(object intent, object[] targets, object owner)` — 意图总伤害
- `GetCreatureIntentAnchor(object node)` — 意图悬挂点
- `ResolveHealthHolders(object instance)` — 解决 HP 持有者

**运行状态**：
- `IsRunActive()` — 运行是否活跃
- `IsMultiplayerRun()` — 是否联机
- `GetRunState()` — 获取运行状态
- `GetSeedString(object state)` — 种子字符串

**动态变量**：
- `ResolveDynamicVarBase(object dynamicVar)` — 动态变量基值
- `AddDynamicVarBase(object dynamicVar, decimal amount)` — 动态变量加法

### MultiplayerReflection

联机角色检测：
- `MarkHost()` / `MarkClient()` / `ClearRole()`
- `IsInteractionLocked(Node? context)` — 交互是否锁定（Client 端）
- `IsMultiplayerRun()` — 是否联机运行
- `DescribeCurrentService()` — 当前联机服务状态描述

### RuntimeTypeResolver

通过名称寻找运行时类型（支持 `FindType(string typeName)` / `FindTypeByName(string simpleName)`）。

---

## 🌐 本地化 (Localization)

使用游戏内置 LocString + 自定义文本表：

```csharp
public override LocString Title => new LocString("modifiers", Id.Entry + ".title");
```

本地化通过 `PlusLoc.MergeIntoModifiersTable()` 合并到游戏 `modifiers` 表。

支持语言：**English** (默认) + **Türkçe** (土耳其语，通过 `TranslationServer.GetLocale()` 检测)

关键方法：
- `Text(string key)` — 获取当前语言的文本
- `Format(string key, params object[] args)` — 格式化
- `ModifierTitleKey(string entry)` / `ModifierDescriptionKey(string entry)` — 标题/描述键映射
- `ActNumber(int actNumber)` — 幕名
- `MergeIntoModifiersTable()` — 合并到游戏本地化表

---

## 👥 联机支持 (Multiplayer)

### 架构

- 自动检测 Host/Client 角色
- Host 端可修改规则，Client 端被锁定
- 修正器选择在联机时通过消息同步

### MultiplayerSafety

```csharp
internal static class MultiplayerSafety
{
    public static bool IsGameplayRuleSelectionLocked(Node? context);
}
```

### 消息协议

| 消息类 | 用途 |
|--------|------|
| `QuickRestartBeginMessage` | 快速重新开始通知 |
| `QuickRestartRequestedMessage` | 快速重新开始请求 |
| `RuleSelectionSyncMessage` | 规则选择同步 |

### 同步流程

1. 修改器选择 → emit modifiers changed
2. `CustomModifierListSyncPatch` 捕获变更 → 发送 `RuleSelectionSyncMessage`
3. Host 端接收 → 应用到全局状态
4. 新游戏/存档加载时从 `SerializableModifier` 恢复

---

## 🧩 特征追踪器 (AppliedTracker)

追踪已在当前运行中应用的效果，防止重复应用：

```
GiantCreatureSet     — 已被倍增的 HP 生物
HardEliteSet         — 已被强化的精英
EndlessScaledSet    — 已被无尽缩放
HardEliteRelicRewardSet — 已被发放额外遗物的 (房间, 玩家) 对
GlassCannonRewardSet     — 已被发放金币加成的 (房间, 玩家) 对
AttackDefenseSet    — 已被加成攻击防御的实例
GlassCannonSet      — 已被处理的玻璃大炮玩家
```

`Reset()` — 运行开始时清除所有追踪。

---

## 🛠️ 构建流程

### 构建脚本 (build.ps1)

```powershell
.\build.ps1                                # 仅构建
.\build.ps1 -Install                       # 构建并安装到游戏目录
.\build.ps1 -GameDir "D:\STS2" -Install    # 指定游戏目录
```

**环境变量**：`$env:STS2_GAME_DIR`

**构建流程**：
1. 解析游戏目录（参数 → 环境变量 → 默认 Steam 路径）
2. 验证 `sts2.dll` / `GodotSharp.dll` / `0Harmony.dll`
3. 检查 `dotnet` 可用
4. `dotnet build -c Release -o out/STS2Plus`
5. 可选：复制 DLL + manifest 到 `<GameDir>/Mods/STS2Plus/`

### .csproj 关键点

```xml
<TargetFramework>net9.0</TargetFramework>
<OutputType>Library</OutputType>
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>

<!-- 引用游戏 DLL（不复制到输出） -->
<Reference Include="sts2" Private="false" />
<Reference Include="GodotSharp" Private="false" />
<Reference Include="0Harmony" Private="false" />
```

---

## 🎯 开发指南

### 添加新的自定义修正器

1. 在 `STS2Plus.Modifiers` 创建类继承 `SyncedModifierModel`
2. 在 `CustomModifierCatalog` 的 `IsKnownId` 和 `GetCanonical` switch 中添加
3. 在 `PlusState` 添加标志 + new/restore/sync 方法
4. 在 `AppliedTracker` 添加追踪集（如需）
5. 写对应的 Harmony Patches 实现功能逻辑
6. 在 `PlusLoc` 添加本地化文本（英 + 土）
7. 在 `MoreRulesUi` 添加行常量 + 创建/刷新/点击处理
8. 在 `mod_manifest.json` 更新版本号

### Harmony Patch 分类

```csharp
[HarmonyPatchCategory("Core")]           // 基础功能
[HarmonyPatchCategory("MoreRules")]      // 规则修正器
[HarmonyPatch(typeof(目标类), "方法名")]
internal static class MyPatch
{
    static void Prefix(...) { }
    static void Postfix(...) { }
    static IEnumerable<MethodBase> TargetMethods() { ... }
}
```

### 动态目标方法

```csharp
[HarmonyTargetMethods]
private static IEnumerable<MethodBase> TargetMethods()
{
    // 通过反射在运行时匹配方法
}
```

---

## 📝 常见模式

### 检查修正器活跃

```csharp
// 两路检查：本地选择状态 + 运行状态
if (PlusState.IsGiantCreaturesActive()) { ... }
```

### 跟踪已处理实例（防止重复应用）

```csharp
// AppliedTracker 标记
if (!AppliedTracker.MarkGiantCreature(instance))
    return; // 已标记过，跳过
```

### 访问游戏私有属性/字段

```csharp
object value = AccessTools.Field(typeof(TargetClass), "_fieldName")?.GetValue(instance);
// 或
object value = AccessTools.Property(typeof(TargetClass), "PropertyName")?.GetValue(instance);
```

### 运行时类型匹配

```csharp
Type type = RuntimeTypeResolver.FindType("MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapScreen")
    ?? RuntimeTypeResolver.FindTypeByName("NMapScreen");
```

### Godot 节点操作

```csharp
Node child = node.GetNodeOrNull<Node>((NodePath)"NodeName");
Control ctrl = (Control)(object)((child is Control) ? child : null);
((CanvasItem)node).Visible = true;
```

### 创建一个简单的 UI 控件

```csharp
var label = new Label
{
    Text = "Hello",
    HorizontalAlignment = HorizontalAlignment.Center
};
((Control)label).AddThemeFontSizeOverride((StringName)"font_size", 16);
((Control)label).AddThemeColorOverride((StringName)"font_color", Colors.White);
```

---

## 🔗 参考链接

- **STS2Plus 仓库**: https://github.com/StephenSHorton/STS2Plus
- **Harmony 文档**: https://harmony.pardeike.net/
- **Godot C# API**: https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/
- **杀戮尖塔 2**: Steam 平台
