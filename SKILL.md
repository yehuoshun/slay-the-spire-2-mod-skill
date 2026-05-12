# 杀戮尖塔2 Mod 开发技能

提供杀戮尖塔2（Slay the Spire 2）Mod 开发指导，包括模板使用、代码结构、最佳实践等。

## 触发场景

- 用户想开发杀戮尖塔2 Mod
- 用户询问 StS2 mod 开发相关问题
- 用户提到 BaseLib、ModTemplate、卡牌/遗物/角色开发
- 关键词：杀戮尖塔2、Slay the Spire 2、StS2 mod、BaseLib

## ⚠️ 重要：每次使用必须更新

**使用此 skill 前，必须先访问以下 GitHub 仓库获取最新信息：**

1. **BaseLib-StS2**: https://github.com/Alchyr/BaseLib-StS2
   - 检查最新版本号（API: https://api.github.com/repos/Alchyr/BaseLib-StS2/releases/latest）
   - 检查 Wiki 更新：https://alchyr.github.io/BaseLib-Wiki/
   - 检查 API 变更

2. **ModTemplate-StS2**: https://github.com/Alchyr/ModTemplate-StS2
   - 检查模板结构变化
   - 检查 Setup Wiki 更新：https://github.com/Alchyr/ModTemplate-StS2/wiki/Setup

3. **参考项目（可选）**: https://github.com/YuWan886/Sts2-YuWanCard
   - 查看最新实现案例
   - 学习最佳实践

4. **STS2Plus**: https://github.com/StephenSHorton/STS2Plus
   - 大型 mod 架构参考
   - 反编译重构建流程参考
   - 多人模式安全层参考

**更新流程：**
```
1. 访问上述 GitHub 链接
2. 检查 Release 最新版本
3. 检查 README/Wiki 是否有变更
4. 如有更新，告知用户关键变化
5. 继续执行用户请求
```

## 核心资源

### 1. BaseLib-StS2（基础依赖库）

- **仓库**: https://github.com/Alchyr/BaseLib-StS2
- **作者**: Alchyr
- **最新版本**: v3.1.2（2026-05-08）
- **用途**: StS2 mod 开发的基础库，大多数 mod 的必需依赖
- **Wiki**: https://alchyr.github.io/BaseLib-Wiki/
- **NuGet 模板**: `dotnet new install Alchyr.Sts2.Templates`

**安装方式：**
- 下载 `.dll`, `.pck`, `.json` 文件
- 放置到 `Slay the Spire 2/mods` 目录
- 或下载 `BaseLib.x.x.x.zip` 解压

**项目依赖配置：**
```xml
<PackageReference Include="Alchyr.Sts2.BaseLib" Version="*" />
```

**本地开发：**
- 需调整 csproj 顶部的文件路径
- 生成的 .dll 可作为自己 mod 的依赖

#### v3.1.2 变更（2026-05-08）

- 🐛 beta 版本兼容性修复
- 🐛 AnyPlayerCardTargeting 修复
- 🔄 角色选择滚动优化

#### v3.1.1 变更（2026-05-06）

- 🎵 **ModAudio** — 自定义音频支持
- 🏷️ **CustomReward + 自定义消息** — 网络同步消息系统
- 📜 **ITomeCard** — 典籍卡牌接口
- 🎯 **any-player targeting patch** — 任意玩家目标选择
- 🔄 角色选择界面滚动支持

#### v3.1.0 重要变更（2026-04-28）

- ✨ **CustomActModel** — 自定义幕模型
- 🔄 **AsyncMethodCall + AfterCardPlayedPatch 重做**
- ⚡ **inverted temp powers** — 反转临时能力
- 🏷️ `[ConfigIgnoreRestoreDefaults]` 属性

#### ⚠️ v3.0.9 破坏性变更（2026-04-24）

`CombatState` → `ICombatState`，需要用兼容层：
```csharp
// 旧代码
card.CombatState
// 新代码
BetaMainCompatibility.CardModel_.WrappedCombatState(card)

// 旧代码
runState.IterateHookListeners(card.CombatState)
// 新代码
BetaMainCompatibility.RunState.IterateHookListeners.Invoke<IEnumerable>(
    runState, 
    BetaMainCompatibility.CardModel_.CombatState.Get(card))
```

**建议：只针对一个分支开发，避免兼容性问题。**

#### 近期重要功能一览

- `ModAudio` — 自定义音频加载与播放
- `ITomeCard` — 典籍卡牌接口（每次战斗使用次数限制）
- `CustomRewardModel` — 自定义奖励模型（含网络同步）
- `AnyPlayerTargeting` — 任意玩家目标选择（多人模式）
- `CustomEventModel`, `CustomEnchantmentModel` — 自定义事件/附魔
- `CustomCalculatedVar` — 任意数量计算变量
- `SimplifiedLoc` — 简化本地化（`##` 前缀禁用简化）
- 动态枚举生成（hash 一致性）
- `IMaxHandSizeModifier` — 手牌上限修改
- `ITranscendenceCard` (Archaic Tooth) — 超越卡牌接口
- 全局卡牌描述修改器 `DescriptionOverrides.CustomizeDescription += delegate`
- `DisplayVar` — 自定义显示变量
- 角色选择/随机化开关
- `NRestSiteCharacterFactory` — 休息处角色工厂
- `IAddDumbVariablesToPowerDescription` — 能力描述变量
- 多语言支持：`zhs`, `rus`

### 2. ModTemplate-StS2（Mod 模板）

- **仓库**: https://github.com/Alchyr/ModTemplate-StS2
- **作者**: Alchyr
- **Wiki**: https://github.com/Alchyr/ModTemplate-StS2/wiki/Setup
- **NuGet**: `Alchyr.Sts2.Templates`

**三种模板类型：**

| 模板类型 | 用途 | 适合场景 |
|---------|------|---------|
| Slay the Spire 2 Mod | 空 mod 框架 + BaseLib | 基础功能 mod |
| Slay the Spire 2 Content | 内容 mod | 添加卡牌、遗物、药水等 |
| Slay the Spire 2 Character | 角色 mod | 自定义角色 |

**⚠️ 重要配置：**
创建 solution 时**必须启用** "Put solution and project in same directory"（Godot 项目要求）

#### Setup 完整流程

1. **IDE**: 推荐 Rider（Godot 需要 .sln，VS 逐步放弃 .sln）
2. **下载 Megadot**: https://megadot.megacrit.com/（与游戏版本一致）
3. **安装 .NET SDK**: https://dotnet.microsoft.com/en-us/download
4. **安装 BaseLib**: 下载最新 Release，放入 mods 文件夹
5. **安装模板**: `dotnet new install Alchyr.Sts2.Templates`
6. **创建项目**: File → New Solution → 选模板
7. **配置路径**: 如 STS2 路径未自动找到，修改 `Directory.Build.props`
8. **修改清单**: 在 `.json` 中修改 display name（不要改 id）
9. **生成本地化**: Character 模板需 alt+enter → Generate localization
10. **Build vs Publish**: 仅代码修改用 Build（快），非代码修改（图片/场景）必须 Publish

#### Publish 注意事项

- ⚠️ 非代码修改（本地化、图片、场景）必须 Publish 才能生效
- Publish 会调用 Godot 生成 `.pck`，速度较慢
- 如 Godot 路径未配置，会出现错误
- 纯代码修改点 Build（锤子按钮）即可，更快
- 需要 `.tscn` 场景文件时，Godot Publish 是必须的

### 3. 参考项目：Sts2-YuWanCard

- **仓库**: https://github.com/YuWan886/Sts2-YuWanCard
- **作者**: YuWan886
- **最新版本**: v0.4.9（2026-05-08）
- **NexusMods**: https://www.nexusmods.com/slaythespire2/mods/149
- **备用下载**: https://pan.quark.cn/s/734161e964f3

**项目特点：**
- 完整的角色 Mod（猪猪角色，80血）
- AI 辅助开发
- 大量自定义卡牌、遗物、状态效果
- 多人模式支持
- 新增敌人、事件、充能球机制

#### ⚠️ v0.4.9 重要变更（2026-05-08）

- beta 版本对应游戏 `+0.105.0`（v0.4.8 为 `+0.104.0`）

#### 最新提交的新功能（截至 2026-05-10）

- ✨ **手牌发光规则系统** — 支持自定义金/红光规则
- ✨ **猪付款 / 猪钓猪 卡牌** — 新卡牌机制
- ✨ **「假如」系列遗物** — 条件触发型遗物
- 🎵 音频资源自定义路径支持
- 🔄 保存属性注册机制重构，优化缓存同步

#### ⚠️ v0.4.8 重要变更（2026-05-03）

- **模组互操作框架** — 内容注册系统和生命周期管理
- **卡牌文件结构重构** — 优化文件组织
- **恶魔形态改为遗物** — 从卡牌迁移到遗物系统
- **呼朋引伴卡牌** — 新卡牌功能
- **重构角色初始遗物系统** — 补丁文件结构优化
- **修复建筑师对话卡住** — 空引用问题修复
- **多人模式购物车同步** — 数据同步修复
- 下载时注意版本匹配：正式版用 `+0.103.2`，beta 版用 `+0.104.0`

**联系方式：**
- QQ: 752913553
- Discord: https://discord.gg/tJT3a95Y8y
- 官网: https://www.pighub.top/

### 4. B站开发教程

- **教程链接**: https://www.bilibili.com/opus/1179300682687053826
- **标题**: 《杀戮尖塔2》模组开发教程 01 - 环境搭建
- **内容**: 从零开始的完整环境搭建教程

### 5. STS2Plus（完整 Mod 参考）

- **仓库**: https://github.com/StephenSHorton/STS2Plus
- **作者**: StephenSHorton（Fork，原始作者 utkuk + Codex）
- **许可**: GPL-3.0
- **技术栈**: .NET 9.0, C# 12, Harmony, 裸模组（无 BaseLib）
- **用途**: 反编译重构建的完整功能增强 mod

**项目结构（10 个子模块）：**

| 模块 | 职责 | 关键文件 |
|------|------|---------|
| `STS2Plus`（核心） | 入口、状态、安全层 | `ModEntry.cs`, `PlusState.cs`, `MultiplayerSafety.cs`, `PatchCategories.cs` |
| `STS2Plus.Config` | JSON 配置读写 | `ConfigManager.cs`, `PlusConfig.cs` |
| `STS2Plus.Features` | 各功能模块 | SpeedControl, RouteAdvisor, QuickRestart 等 |
| `STS2Plus.Localization` | 本地化字符串 | 多语言支持（含中文 zhs） |
| `STS2Plus.Modifiers` | 自定义修改器系统 | `CustomModifierCatalog.cs` — 序列化/同步/UI |
| `STS2Plus.Modules` | 玩法规则模块 | `MoreRulesModule.cs` |
| `STS2Plus.Multiplayer` | 多人同步 | 数据同步/状态共享 |
| `STS2Plus.Patches` | Harmony 补丁库（~30+ 文件） | 按功能分组：CompactRelicDrawer, CustomModifier, BuildCreator, AttackDefense 等 |
| `STS2Plus.Reflection` | 反射辅助 | GameReflection, MultiplayerReflection |
| `STS2Plus.Ui` | UI 组件 | 自定义界面元素 |

**架构特点：**

1. **裸模组（无 BaseLib）** — 直接引用 `sts2.dll` + `GodotSharp.dll` + `0Harmony.dll`
2. **PatchCategory 组织** — 用 `[HarmonyPatchCategory("Core")]` 分类，`harmony.PatchCategory()` 按需加载
3. **位标志状态管理** — `PlusState` 用 int bit flags 追踪 10+ 个规则开关
4. **多人安全层** — `MultiplayerSafety` 检查 `IsInteractionLocked` / `IsMultiplayerRun` 决定补丁是否生效
5. **自定义修改器系统** — 完整的序列化/反序列化/多人同步生命周期
6. **Unity-style Init 入口** — `[ModInitializer("Initialize")]` + `[ScriptPath]` 属性标记

**Build 系统：**
```powershell
.\build.ps1 -GameDir "D:\SteamLibrary\..." -Configuration Release -Install
```
- 支持环境变量 `STS2_GAME_DIR`
- .csproj 中 `STS2GameDir` 属性可被命令行覆盖
- `-Install` 自动复制 DLL 和 manifest 到 Mods 目录

**多人安全层实现（关键学习点）：**

```csharp
internal static class MultiplayerSafety
{
    // 选择前的锁检查（主机锁定后客户端不可改）
    public static bool IsGameplayRuleSelectionLocked(Node? context = null)
        => MultiplayerReflection.IsInteractionLocked(context);

    // 只有主机可以注入玩法规则
    public static bool ShouldInjectGameplayRules(Node? context = null)
        => !MultiplayerReflection.IsInteractionLocked(context);

    // 权威补丁：非多人局 或 主机端
    public static bool ShouldApplyAuthoritativeGameplayPatches(Node? context = null)
        => !MultiplayerReflection.IsMultiplayerRun() 
           || !MultiplayerReflection.IsInteractionLocked(context);

    // 本地补丁：权威端 或 本地玩家对象
    public static bool ShouldApplyLocalPlayerGameplayPatches(object? target, Node? context = null)
        => ShouldApplyAuthoritativeGameplayPatches(context) 
           || GameReflection.IsLocalPlayerObject(target);
}
```

**配置系统模式：**
```csharp
// 配置路径: %AppData%/SlayTheSpire2/ModConfig/STS2Plus.json
internal static class ConfigManager
{
    public static PlusConfig Current { get; private set; } = new();
    
    public static void Load()
    {
        Directory.CreateDirectory(ConfigDirectory);
        if (!File.Exists(ConfigPath))
            { Current = new(); Save(); }  // 首次自动生成默认配置
        else
            Current = JsonSerializer.Deserialize<PlusConfig>(...);
    }
    
    public static void Save()
    {
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(Current, JsonOptions));
    }
}
```

**PatchCategory 注册模式：**
```csharp
public static void Initialize()
{
    harmony = new Harmony("sts2plus.core");
    PatchCategory("Core");
    PatchCategory("MoreRules");
    // 每个 category 独立 try-catch，一个失败不影响其他
}

private static void PatchCategory(string category)
{
    try { harmony.PatchCategory(typeof(ModEntry).Assembly, category); }
    catch (Exception ex) { Logger.Error($"Failed: {category}"); }
}
```

**状态管理模式（位标志）：**
```csharp
// 用 const int 位标志，一个 int 存 10+ 个 bool
private const int AttackDefenseFlag = 1;      // 0b0001
private const int IronSkinFlag = 4;           // 0b0100
private const int GiantCreaturesFlag = 8;     // 0b1000
// ... 最多 32 个状态用一个 int 搞定
```

### 6. 用户项目参考

- **STS2-ShunMod**: https://github.com/yehuoshun/STS2-ShunMod
- **架构**: 裸模组（不使用 BaseLib），直接引用 sts2.dll + 0Harmony
- **卡牌基类**: `ShunCard` — 封装 CardModel 抽象成员，链式配置
- **自动注册**: `[Pool]` 属性 + `ContentRegistry.RegisterAll()` — 加牌无需改 MainFile
- **事件注册**: Harmony Patch `ModelDb.AllSharedEvents` + EventRegistry
- **技术栈**: .NET 9.0, Godot.NET.Sdk/4.5.1, Harmony, BSchneppe.StS2.PckPacker

### 7. YuWanCard 新功能参考

> v0.4.9 后新增的有价值功能模式，可学习借鉴。

- **手牌发光规则系统** — 卡牌满足条件时自动金/红发光（类似原版 ShouldGlowGoldInternal 但更灵活）
- **「假如」系列遗物** — 条件触发型遗物系列设计模式
- **猪付款 / 猪钓猪 卡牌** — 消耗金币/钓鱼机制的新卡牌玩法
- **音频自定义路径** — AudioStreamPlayer 动态加载自定义音频资源
- **保存属性注册重构** — 优化卡牌属性缓存同步机制

## 开发流程

### 步骤 1：环境准备

1. 下载 Megadot（https://megadot.megacrit.com/）
2. 安装 .NET SDK（检查游戏 global.json 确定版本）
3. 安装 IDE（推荐 Rider）
4. 下载 BaseLib 最新 Release（如需要）

### 步骤 2：创建项目

1. 通过 `dotnet new install Alchyr.Sts2.Templates` 安装模板
2. 通过模板创建（File → New Solution）
3. 或手动搭建：csproj + manifest.json + 项目代码
4. 创建 solution 时勾选 "Put solution and project in same directory"
5. 配置 BaseLib 依赖（可选）

### 步骤 3：模组文件结构

每个模组由 3 个文件组成（放在 `mods/<ModId>/` 下）：

| 文件 | 必需 | 说明 |
|------|------|------|
| `<ModId>.json` | ✅ 必须 | 模组清单，描述模组元信息 |
| `<ModId>.dll` | 按需 | 程序集，has_dll=true 时需要 |
| `<ModId>.pck` | 按需 | 资源包，has_pck=true 时需要 |

**模组清单格式（截止 2026-03-13）：**

```json
{
  "id": "ModId",
  "name": "显示名称",
  "author": "作者",
  "description": "描述",
  "version": "v0.0.0",
  "has_pck": true,
  "has_dll": true,
  "dependencies": [],
  "affects_gameplay": true
}
```

- `affects_gameplay`: false = 客户端模组（美化包），联机不校验；true = 影响玩法，联机需校验
- 模组图标：`res://<modid>/mod_image.png`

### 步骤 4：裸模组 vs BaseLib 模组

**裸模组（不使用 BaseLib）：**
- 直接引用 `sts2.dll` + `0Harmony.dll`
- 需要自己处理 CardModel 抽象成员
- 注册用 `ModHelper.AddModelToPool`
- 优点：无外部依赖，更轻量
- 缺点：缺少 BaseLib 提供的便利 API

**BaseLib 模组：**
- 引用 BaseLib NuGet 包
- 提供 CustomCard、CustomRelic 等便捷基类
- 更完善的配置系统、本地化支持
- 优点：开发效率高，社区标准
- 缺点：依赖 BaseLib 版本

### 步骤 5：开发内容

根据 mod 类型开发：
- **卡牌**: 定义卡牌属性、效果、目标、关键词
- **遗物**: 定义遗物效果、触发条件
- **角色**: 定义角色属性、初始卡组、专属机制
- **状态效果**: 定义 buff、debuff 机制
- **充能球**: 定义充能球效果
- **敌人**: 定义敌人行为、血量、技能
- **事件**: 定义事件选项、奖励

### 步骤 6：测试与打包

1. 编译生成 .dll（Build 或 Publish）
2. 自动复制到 `mods/<ModId>/` 目录
3. 游戏内测试
4. 查看日志：`%AppData%\SlayTheSpire2\logs\`
5. 打包发布

## 常见开发点

### 卡牌开发

所有卡牌继承 `CardModel` 抽象类，构造函数接受 5 个参数：

```csharp
public CardModel(int baseCost, CardType type, CardRarity rarity, 
                 TargetType target, bool shouldShowInCardLibrary = true)
```

| 参数 | 说明 |
|------|------|
| `baseCost` | 基础耗能，赋值给 `CanonicalEnergyCost` |
| `type` | 卡牌类型，决定卡面边框样式 |
| `rarity` | 稀有度，决定边框样式、出现逻辑、商店售价 |
| `target` | 目标类型 |
| `shouldShowInCardLibrary` | ⚠️ 是否在卡牌大全显示，默认 true |

#### 卡牌稀有度与获取

| 稀有度 | 随机卡池 | 变牌 | 说明 |
|--------|:--:|:--:|------|
| `Common` / `Uncommon` / `Rare` | ✅ | ✅ | 正常获取 |
| `Basic` | ❌ | — | 基础牌（初始卡组） |
| `Event` | ❌ | ❌ | 仅通过事件获取 |
| `Ancient` | ❌ | ❌ | 仅通过特殊方式获取 |
| `Token` | ❌ | — | 衍生牌（小刀、巨石等） |
| `Status` / `Curse` / `Quest` | ❌ | — | 状态/诅咒/任务牌 |

#### 目标类型

| TargetType | 说明 |
|------------|------|
| `Self` | 自身 |
| `Enemy` | 任意敌人 |
| `Ally` | 存活的友军单位（全部阵亡时无法打出） |
| `TargetedNoCreature` | 非玩家/敌人目标（如商人） |
| `Osty` | 亡灵契约师的宠物奥斯提 |

#### 裸模组卡牌基类

```csharp
// 卡牌基类 — 封装 CardModel 抽象成员
public abstract class ShunCard : CardModel
{
    private readonly List<CardKeyword> _keywords = [];
    private int? _costUpgrade;

    protected ShunCard(int baseCost, CardType type, CardRarity rarity, TargetType target)
        : base(baseCost, type, rarity, target) { }

    protected sealed override IEnumerable<DynamicVar> CanonicalVars => [];
    public sealed override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;
    protected sealed override HashSet<CardTag> CanonicalTags => [];
    protected sealed override IEnumerable<IHoverTip> ExtraHoverTips => /* ... */;

    protected void WithKeywords(params CardKeyword[] keywords) { _keywords.AddRange(keywords); }
    protected void WithCostUpgradeBy(int amount) { _costUpgrade = amount; }
}
```

#### 攻击卡牌示例

```csharp
public class ExampleAttack : ShunCard
{
    public ExampleAttack()
        : base(baseCost: 0, type: CardType.Attack, rarity: CardRarity.Common, target: TargetType.Enemy)
    {
        // DynamicVars 用于动态显示伤害数值
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        DynamicVars.Create(DynamicVarType.Damage, 2);

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2); // 升级后 +2 伤害
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = choiceContext.CreatureTargets.First();
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(target)
            .Execute();
    }
}
```

#### DamageCmd / AttackCommand 链式调用

```csharp
// 基本攻击
await DamageCmd.Attack(6)
    .FromCard(this)                    // 伤害来源：这张卡
    .Targeting(target)                 // 目标
    .Execute();                        // 必须最后调用

// AOE 攻击
await DamageCmd.Attack(4)
    .FromCard(this)
    .TargetingAllOpponents(combatState)
    .Execute();

// 随机目标
await DamageCmd.Attack(3)
    .FromCard(this)
    .TargetingRandomOpponents(combatState, allowRepeat: false)
    .Execute();

// 多次攻击 + 自定义特效
await DamageCmd.Attack(2)
    .FromCard(this)
    .Targeting(target)
    .WithHitFx(vfx: "res://animations/slash.spine", sfx: null, tmpSfx: null)
    .WithHitCount(3)
    .Execute();
```

#### 其他回调

```csharp
// 回合结束时仍在手牌触发（如：失去生命）
protected override async Task OnTurnEndInHand(PlayerChoiceContext ctx)
{
    await PlayerCmd.Damage(Owner!, 3);
}

// 是否可打出（如：需要 ≥100 金币）
public override bool IsPlayable =>
    base.IsPlayable && Owner?.Gold >= 100;

// 可打出时发光提示
public override bool ShouldGlowGoldInternal =>
    Owner?.Gold >= 100;
```

#### 卡牌标签与关键词

```csharp
// 标签（打击/防御等，影响完美打击等联动）
protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

// 关键词（保留、虚无、消耗等）
public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
```

**常见关键词：** 消耗、保留、重放、忠诚、虚无、再生、覆甲、易伤、虚弱、中毒、锻造、力量、敏捷

#### 抽牌 / 选牌 / 升级 / 丢弃

```csharp
// 抽牌
await CardPileCmd.Draw(Owner!, 2);

// 从手牌选牌消耗
var selected = await CardSelectCmd.FromHand(
    choiceContext, Owner,
    new CardSelectorPrefs("选择一张牌消耗", selectCount: 1),
    filter: _ => true,
    source: this);
CardCmd.Exhaust(selected);

// 选择手牌升级
var card = await CardSelectCmd.FromHandForUpgrade(choiceContext, Owner);
card.UpgradeInternal();

// 选择手牌丢弃
var card = await CardSelectCmd.FromHandForDiscard(choiceContext, Owner);
CardCmd.Discard(card);
```

#### 施加效果

```csharp
// 施加虚弱
await CardCmd.ApplyDebuff(target, DebuffType.Weak, 1, this);
```

#### 典籍卡牌（ITomeCard）

> v3.1.1 新增。每次战斗有使用次数限制的特殊卡牌。

```csharp
public class MyTomeCard : ShunCard, ITomeCard
{
    public int UsesPerCombat => 3;  // 每场战斗最多使用 3 次
    
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cp)
    {
        // 使用次数由 BaseLib 自动管理
        await DamageCmd.Attack(10).FromCard(this).Targeting(/*...*/).Execute();
    }
}
```

#### 自定义奖励（CustomReward）

> v3.1.1 新增。支持网络同步的自定义奖励模型。

```csharp
public class MyCustomReward : CustomRewardModel
{
    // 定义奖励逻辑（含网络消息同步）
}
```

#### 自定义音频（ModAudio）

> v3.1.1 新增。加载和播放自定义音频资源。

```csharp
// 播放自定义音效
ModAudio.Play("res://<ModId>/audio/my_sound.wav");
```

#### 注册与卡牌大全

```csharp
// MainFile.cs — 添加到无色卡池
ModHelper.AddModelToPool(typeof(ColorlessCardPool), typeof(ExampleAttack));
// 或角色专属池
ModHelper.AddModelToPool(typeof(IroncladCardPool), typeof(ExampleAttack));
```

#### ⚠️ 卡牌大全（Compendium）不显示的常见原因

1. **`shouldShowInCardLibrary = false`** — 构造函数第 5 个参数设为 false
2. **稀有度问题** — `Event`/`Token`/`Status`/`Curse`/`Quest` 不会出现在随机卡池中
3. **缺少肖像图片** — `PortraitPath` 指向的图片不存在，导致卡牌初始化失败
4. **缺少本地化** — 需要在 `localization/<lang>/cards.json` 中配置

#### 卡牌肖像图

| 路径 | 用途 | 推荐尺寸 |
|------|------|---------|
| `res://images/atlases/card_atlas.sprites/<卡池名>/<小写id>.tres` | AtlasTexture 裁切资源 | — |
| `res://images/packed/card_portraits/<卡池名>/<小写id>.png` | 大图纹理源 | 1000×760 |

#### 卡牌本地化

```json
{
  "EXAMPLE_ATTACK.title": "示例攻击",
  "EXAMPLE_ATTACK.description": "造成 {Damage} 点伤害。{Weak:applyWeaknessTooltip}"
}
```

- `{DynamicVarName}` — 显示动态变量值
- `{DynamicVarName:diff()}` — 显示升级前后的差异值
- `{Keyword:tooltipMethod}` — 显示关键词提示

#### 升级所有卡牌示例

```csharp
// 注意：Owner 是 Creature? 可空，OnPlay 时一定非空，加 ! 断言
foreach (CardModel allCard in base.Owner!.PlayerCombatState.AllCards)
{
    if (allCard != this && allCard.IsUpgradable)
        CardCmd.Upgrade(allCard);
}
// 升级牌组
var deckCards = PileType.Deck.GetPile(Owner!).Cards.Where(c => c.IsUpgradable);
foreach (var card in deckCards)
    CardCmd.Upgrade(card, CardPreviewStyle.None);
```

### 能力开发（Buff/Debuff）

所有能力/Buff 效果继承 `PowerModel` 抽象类。

#### 关键属性

| 属性 | 说明 |
|------|------|
| `Type` | `PowerType.Buff` / `PowerType.Debuff`，决定增益还是减益 |
| `StackType` | `PowerStackType.Stackable`（有层数，如中毒）/ 非堆叠（存在/不存在，如壁垒） |
| `IsInstanced` | true = 每次施加新建独立实例；false = 只存在一个实例（默认 false） |
| `AllowNegative` | true = 允许负数层（如力量/敏捷）；false = 层数为 0 时自动移除 |

#### 基础 Buff 示例（出牌加格挡，回合结束减层）

```csharp
public class MyBuffPower : PowerModel
{
    public MyBuffPower()
    {
        Type = PowerType.Buff;
        StackType = PowerStackType.Stackable;
    }

    // 持有者打出任意卡牌后
    protected override async Task AfterCardPlayed(
        CardModel card, CardPlay cardPlay)
    {
        await CardCmd.Block(Owner!, Amount);
    }

    // 持有者回合结束时，层数 -1
    protected override async Task AfterSideTurnEnd(
        CombatSide side, ICombatState state)
    {
        if (side != Owner!.Creature.Side) return;
        Amount -= 1;
    }
}
```

#### 施加 Buff

```csharp
// 施加 N 层
CardCmd.ApplyBuff(target, new MyBuffPower(), layers: 3);
```

#### 能力本地化

路径：`res://<ModId>/localization/<lang>/powers.json`

```json
{
  "MY_BUFF_POWER.title": "我的Buff",
  "MY_BUFF_POWER.description": "简略描述",
  "MY_BUFF_POWER.smartDescription": "出牌时获得 {Amount} 点格挡。每回合层数-1。"
}
```

- `description` — 普通简介
- `smartDescription` — 带动态变量信息的智能描述

#### 能力图标

| 路径 | 用途 |
|------|------|
| `res://images/atlases/power_atlas.sprites/<小写id>.tres` | 战斗图标裁切资源 |
| `res://images/powers/<小写id>.png` | 大图纹理（256×256） |

#### 控制台测试

```
进入战斗 → 按 ` 打开控制台 → 输入命令给予指定能力
```

### 事件开发

所有事件继承 `EventModel` 抽象类，玩家进入问号房间后随机触发。

#### 基础事件示例（双选项）

```csharp
public class MyCustomEvent : EventModel
{
    // 生成初始选项
    protected override IEnumerable<EventOption> GenerateInitialOptions()
    {
        return
        [
            // 选项1：给一张卡牌
            new EventOption(
                title: L10NLookup("pages.INITIAL.options.OPTION_1.title"),
                key: InitialOptionKey("OPTION_1"),
                selectedCallback: async () =>
                {
                    await CardCmd.ObtainCard(Owner!, new MyCustomCard());
                    SetEventFinished(L10NLookup("pages.OPTION_1_COMPLETE.description"));
                }),

            // 选项2：给50金币
            new EventOption(
                title: L10NLookup("pages.INITIAL.options.OPTION_2.title"),
                key: InitialOptionKey("OPTION_2"),
                selectedCallback: async () =>
                {
                    PlayerCmd.GainGold(Owner!, 50);
                    SetEventFinished(L10NLookup("pages.OPTION_2_COMPLETE.description"));
                }),
        ];
    }
}
```

**EventOption 参数：**
- `title` — 选项显示文本（用 `L10NLookup` 查本地化）
- `key` — 唯一键（用 `InitialOptionKey("ID")` 构建）
- `selectedCallback` — 选择后的回调，null = 不可选
- 可选：指定遗物图标 → 选项旁显示遗物图标

#### 事件本地化

路径：`res://<ModId>/localization/<lang>/events.json`

格式：`<事件ID>.<目标类型>.<项ID>.<键>`

```json
{
  "MY_CUSTOM_EVENT.title": "自定义事件",
  "MY_CUSTOM_EVENT.pages.INITIAL.description": "你遇到了一个神秘的事件...",
  "MY_CUSTOM_EVENT.pages.INITIAL.options.OPTION_1.title": "[获得卡牌] 接受馈赠",
  "MY_CUSTOM_EVENT.pages.INITIAL.options.OPTION_2.title": "[获得50金币] 拿走金币",
  "MY_CUSTOM_EVENT.pages.OPTION_1_COMPLETE.description": "你获得了一张强大的卡牌！",
  "MY_CUSTOM_EVENT.pages.OPTION_2_COMPLETE.description": "你获得了50金币。"
}
```

#### 事件背景图

`res://images/events/<小写id>.png`（原版分辨率 3440×1613）

#### 多页选项

```csharp
// 切换到新一页选项
SetEventState(newPages: newPages, isTutorial: false);
```

通过 `SetEventState` 打开新的一组选项，实现多页事件流程。

#### 添加事件到事件列表

事件硬编码在 `ActModel.AllEvents` 中，通过 Harmony Patch 注入：

```csharp
// 注入到第一章密林
[HarmonyPatch(typeof(Overgrowth), nameof(Overgrowth.AllEvents), MethodType.Getter)]
static class Overgrowth_AllEvents_Patch
{
    static void Postfix(ref IReadOnlyList<EventModel> __result)
    {
        __result = [.. __result, new MyCustomEvent()];
    }
}
```

#### 先古之民事件

继承 `AncientEventModel`，出现在每一章开头。额外要求：

```csharp
public class MyAncient : AncientEventModel
{
    // 所有可能出现的选项
    protected override IEnumerable<EventOption> AllPossibleOptions => [/* ... */];

    // 定义对话配置
    protected override AncientDialogueSet DefineDialogues()
    {
        return new AncientDialogueSet(
            dialogues: [
                new AncientDialogue(sfx: "event:/Ancient/First"),
                new AncientDialogue(),  // 无音效
                new AncientDialogue(sfx: "event:/Ancient/Third"),
            ]
        );
    }
}
```

**先古之民本地化：** `res://<ModId>/localization/<lang>/ancients.json`

| 键格式 | 说明 |
|--------|------|
| `<ID>.title` | 先古之民名字 |
| `<ID>.epithet` | 描述 |
| `<ID>.talk.firstVisitEver.<组>-<行>` | 首次对话 |
| `<ID>.talk.<角色ID>.<组>-<行>` | 角色特殊对话 |
| `<ID>.talk.ANY.<组>-<行>r` | 通用对话（`r` 后缀） |

**对话行后缀：** `ancient` = 先古之民说话 / `next` = 选项分支 / `char` = 角色说话

**先古之民注册：**

```csharp
[HarmonyPatch(typeof(Hive), nameof(Hive.AllAncients), MethodType.Getter)]
static class Hive_AllAncients_Patch
{
    static void Postfix(ref IReadOnlyList<AncientEventModel> __result)
    {
        __result = [new MyAncient()]; // 强制锁定
    }
}
```

**先古之民素材（缺一不可）：**

| 路径 | 用途 |
|------|------|
| `res://images/events/<小写id>.png` | 背景图 |
| `res://images/packed/map/ancients/ancient_node_<小写id>.png` | 地图图标 |
| `res://images/packed/map/ancients/ancient_node_<小写id>_outline.png` | 地图图标描边 |
| `res://images/ui/run_history/<小写id>.png` | 历史对话头像 |
| `res://images/ui/run_history/<小写id>.png` | 历史对话头像描边 |
| `res://scenes/events/background_scenes/<小写id>.tscn` | 背景场景 |

### 药水开发

所有药水继承 `PotionModel` 抽象类。

#### 药水类关键属性

| 属性 | 说明 |
|------|------|
| `Rarity` | `PotionRarity.Common` / `Uncommon` / `Rare`，决定随机获取的概率 |
| `Usage` | 使用时机：`CombatOnly`（仅战斗）/ `AnyTime`（任意场景）/ `TriggeredOnly`（代码触发，如瓶中精灵） |

#### 伤害药水示例

```csharp
public class MyAoePotion : PotionModel
{
    public MyAoePotion() : base(PotionRarity.Common)
    {
        Usage = PotionUsage.CombatOnly;
    }

    protected override async Task OnUse(
        PlayerChoiceContext choiceContext, PotionTarget target)
    {
        await DamageCmd.Attack(30)
            .FromPotion(this)
            .TargetingAllOpponents(Owner!.CombatState!)
            .Execute();
    }
}
```

#### 效果药水示例

```csharp
public class MyBuffPotion : PotionModel
{
    public MyBuffPotion() : base(PotionRarity.Uncommon)
    {
        Usage = PotionUsage.AnyTime;
    }

    protected override async Task OnUse(
        PlayerChoiceContext choiceContext, PotionTarget target)
    {
        // 失去 10 点生命，获得 2 层无实体
        await PlayerCmd.Damage(Owner!, 10);
        await CardCmd.ApplyBuff(Owner!, BuffType.Intangible, 2, this);
    }
}
```

#### 自动消耗药水（死亡阻止 + 复活）

```csharp
public class MyRevivePotion : PotionModel
{
    public MyRevivePotion() : base(PotionRarity.Rare)
    {
        Usage = PotionUsage.TriggeredOnly; // 代码触发，不可手动使用
    }

    // 阻止死亡
    public override bool ShouldDie(Creature creature, ICombatState state)
    {
        if (creature != Owner?.Creature) return true;
        return false; // 返回 false 阻止死亡
    }

    // 阻止死亡后触发
    protected override async Task AfterPreventingDeath(
        Creature creature, ICombatState state)
    {
        await Consume();                          // 消耗药水
        await PlayerCmd.Heal(Owner!, 10);         // 回复 10 HP
    }

    // 战斗中不会被随机生成（如炼制药水卡牌）
    public override bool CanBeGeneratedInCombat => false;
}
```

#### 药水池注册

```csharp
// 共享药水池 — 所有角色可获取
ModHelper.AddModelToPool(typeof(SharedPotionPool), typeof(MyAoePotion));
```

游戏本体药水池类型：`IroncladPotionPool` / `SilentPotionPool` / `DefectPotionPool` / `NecrobinderPotionPool` / `RegentPotionPool` / `SharedPotionPool` / `EventPotionPool`

#### 药水图标

| 路径 | 用途 |
|------|------|
| `res://images/atlases/potion_atlas.sprites/<小写id>.tres` | 裁切纹理资源 |
| `res://images/atlases/potion_outline_atlas.sprites/<小写id>.tres` | 描边裁切纹理资源 |
| `res://images/potions/<小写id>.png` | 大图纹理 |

#### 药水本地化

路径：`res://<ModId>/localization/<lang>/potions.json`

```json
{
  "MY_AOE_POTION.title": "爆炸药水",
  "MY_AOE_POTION.description": "对所有敌人造成 30 点伤害。"
}
```

### 遗物开发

所有遗物继承 `RelicModel` 抽象类，类名必须唯一（会按大驼峰 → `UPPER_SNAKE_CASE` 生成遗物ID）。

#### 遗物稀有度与获取方式

| 稀有度 | 获取方式 |
|--------|---------|
| `Rarity.Starter` | 初始遗物，不在宝箱/精英/商店刷新 |
| `Rarity.Common` | 宝箱、精英、商店 |
| `Rarity.Uncommon` | 宝箱、精英、商店 |
| `Rarity.Rare` | 宝箱、精英、商店 |
| `Rarity.Shop` | 仅在商店刷新 |
| `Rarity.Event` | 仅通过事件获取 |
| `Rarity.Ancient` | 仅通过事件/特殊方式获取 |

**遗物池（AddModelToPool）：**

| 池 | 说明 |
|----|------|
| `IroncladRelicPool` / `SilentRelicPool` / `DefectRelicPool` / `NecrobinderRelicPool` / `RegentRelicPool` | 角色专属池，影响精英奖励和商店 |
| `SharedRelicPool` | 公共池，影响精英、商店、宝箱三类来源 |
| `EventRelicPool` | 事件池，只能通过问号事件获取 |
| `FallbackRelicPool` | 兜底池，仅 Circlet（头环） |
| `DeprecatedRelicPool` | 废弃内容/兼容存档 |

#### 基础遗物示例

```csharp
public class MyCustomRelic : RelicModel
{
    public MyCustomRelic() : base(Rarity.Starter)
    {
        // DynamicVars 用于动态显示数值
    }

    // 动态变量 — 数值变化时 UI 同步更新
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        DynamicVars.Create(DynamicVarType.Energy, 1);

    // 回合开始时触发
    protected override async Task AfterSideTurnStart(
        CombatSide side, ICombatState combatState)
    {
        if (side != Owner!.Creature.Side) return;
        
        Flash(); // 遗物生效闪烁提示
        await PlayerCmd.GainEnergy(
            Owner, DynamicVars.Energy.BaseValue);
    }
}
```

#### 修改角色初始遗物

```csharp
// Harmony Patch 篡改起始遗物列表
[HarmonyPatch(typeof(Ironclad), "StartingRelics", MethodType.Getter)]
static class Ironclad_StartingRelics_Patch
{
    static void Postfix(ref IReadOnlyList<RelicModel> __result)
    {
        __result = [.. __result, new MyCustomRelic()];
    }
}
```

#### 遗物本地化

路径：`res://<ModId>/localization/<lang>/relics.json`

```json
{
  "MY_CUSTOM_RELIC.title": "我的遗物",
  "MY_CUSTOM_RELIC.description": "每回合开始时获得 {Energy} 点能量。",
  "MY_CUSTOM_RELIC.flavor": "一段引言文本"
}
```

#### 遗物图标

| 路径 | 用途 | 尺寸 |
|------|------|------|
| `res://images/relics/<小写id>.png` | 大图标（百科大全） | 256×256 |
| `res://images/relics/<小写id>_outline.png` | 描边图标（未发现时） | 256×256 |
| `res://images/atlases/relic_atlas.sprites/<小写id>.tres` | AtlasTexture 图标资源 | — |
| `res://images/atlases/relic_outline_atlas.sprites/<小写id>.tres` | AtlasTexture 描边资源 | — |

**AtlasTexture 创建步骤：**
1. 在 Godot 中右键文件夹 → 新建 → 资源 → 搜索 `AtlasTexture`
2. 命名为 `<小写遗物ID>.tres`
3. 双击资源，右侧属性检查器拖入 PNG → 设置 Region (w, h) = 256×256
4. 描边纹理同理，放在 `relic_outline_atlas.sprites` 目录

若未找到 AtlasTexture，游戏会降级使用 `images/relics/` 下的大图标。

#### 常见错误

**HarmonyLib .NET 版本不匹配：**
若运行时报错，可能是 Godot 4.5.1 的 .NET 版本过高。修改编辑器目录下的：
```
GodotSharp/Api/Debug/GodotPlugins.runtimeconfig.json
```
强制使用 .NET 9.0 框架。

### 附魔开发

所有附魔继承 `EnchantmentModel` 抽象类，核心是计算动态变量数值 + 监听目标卡牌。

#### 附魔攻击卡示例（去消耗 + 加攻 + 加格挡）

```csharp
public class MyEnchantment : EnchantmentModel
{
    // 只能附魔到攻击卡
    public override bool CanEnchant(CardModel card) =>
        card.Type == CardType.Attack;

    // 附魔后去掉消耗关键词
    protected override void OnEnchant(CardModel card)
    {
        card.Keywords.Remove(CardKeyword.Exhaust);
    }

    // 攻击伤害加成（Amount = 附魔层数）
    public override int EnchantDamageAdditive(CardModel card, int baseDamage)
        => Amount;

    // 格挡加成
    public override int EnchantBlockAdditive(CardModel card, int baseBlock)
        => Amount;
}
```

#### 为卡牌附魔

```csharp
// 给指定卡牌附魔 X 层
CardCmd.Enchant(card, new MyEnchantment(), layers: 3);
```

#### 控制台测试

按 `` ` `` 键打开控制台，输入命令为手牌第 N 张牌附魔。

#### 附魔本地化

路径：`res://<ModId>/localization/<lang>/enchantments.json`

```json
{
  "MY_ENCHANTMENT.title": "我的附魔",
  "MY_ENCHANTMENT.description": "增加 {Amount} 点伤害和格挡。"
}
```

#### 附魔图标

`res://images/enchantments/<小写id>.png`

### 角色开发

自定义角色需要同时定义：角色类（`CharacterModel`）+ 卡池（`CardPoolModel`）+ 遗物池（`RelicPoolModel`）+ 药水池（`PotionPoolModel`）。

#### 角色卡池

```csharp
public class WatcherCardPool : CardPoolModel
{
    public override string Title => "watcher";           // 决定卡面图资源路径
    public override string EnergyColorName => "watcher"; // 能量图标名称
    public override string CardFrameMaterialPath => "card_frame_purple"; // 卡框着色器材质
    public override Color DeckEntryCardColor => Colors.Purple;   // 缩略卡底色
    public override Color EnergyOutlineColor => Colors.Purple;   // 能量数字描边色
    public override bool IsColorless => false;

    public override IEnumerable<CardModel> GenerateAllCards()
    {
        return [/* 此池所有卡牌 */];
    }
}
```

**卡池资源路径规则：**

| 资源 | 路径 |
|------|------|
| 卡面裁切 | `res://images/atlases/card_atlas.sprites/<Title>/<卡牌id>.tres` |
| 能量图标裁切 | `res://images/atlases/ui_atlas.sprites/card/energy_<EnergyColorName>.tres` |
| 能量图标大图 | 自定义路径，无限制 |
| 富文本能量缩略图 | `res://images/packed/sprite_fonts/<EnergyColorName>_energy_icon.png` |
| 卡框着色器材质 | `res://materials/cards/frames/<CardFrameMaterialPath>_mat.tres` |

**卡框着色器（HSV 偏移 ShaderMaterial）：** 需创建 `ShaderMaterial` → 内嵌或引用 HSV 着色器 → 调节 HSV 参数调色。

#### 角色遗物池

```csharp
public class WatcherRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "watcher";
    public override Color LabOutlineColor => Colors.Purple;
    public override IEnumerable<RelicModel> GenerateAllRelics() => [/* 遗物列表 */];
}
```

#### 角色药水池

```csharp
public class WatcherPotionPool : PotionPoolModel
{
    public override IEnumerable<PotionModel> GenerateAllPotions() => [/* 药水列表 */];
}
```

#### 独立卡池/遗物池/药水池注册（非角色）

如果不为角色创建，需 patch `ModelDb`：

```csharp
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCardPools), MethodType.Getter)]
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllRelicPools), MethodType.Getter)]
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllPotionPools), MethodType.Getter)]
// Postfix 中追加自定义池
```

#### 角色类

```csharp
public class MyCharacter : CharacterModel
{
    public override CharacterGender Gender => CharacterGender.Female;
    public override Type? UnlocksAfterRunAs => typeof(Defect); // 前置解锁角色，null=无条件
    public override CardPoolModel CardPool => new WatcherCardPool();
    public override PotionPoolModel PotionPool => new WatcherPotionPool();
    public override RelicPoolModel RelicPool => new WatcherRelicPool();
    public override IEnumerable<CardModel> StartingDeck => [/* 初始卡组 */];
    public override IReadOnlyList<RelicModel> StartingRelics => [/* 初始遗物 */];

    // 攻击建筑师的 VFX 序列
    protected override IEnumerable<string> GetArchitectAttackVfx() => [
        "vfx_attack_blunt",
        "vfx_heavy_blunt",
        "vfx_attack_slash",
        "vfx_bloody_impact",
        "vfx_rock_shatter"
    ];
}
```

#### 角色资源清单

| 资源类型 | 路径 |
|----------|------|
| 默认待机动画场景 | `res://scenes/creature_visuals/<小写id>.tscn` |
| 头像缩略图图标场景 | `res://scenes/ui/character_icons/<小写id>_icon.tscn` |
| 能量计数器场景 | `res://scenes/combat/energy_counters/<小写id>_energy_counter.tscn` |
| 商店待机动画场景 | `res://scenes/merchant/characters/<小写id>_merchant.tscn` |
| 火堆休息动画场景 | `res://scenes/rest_site/characters/<小写id>_rest_site.tscn` |
| 卡牌拖尾特效场景 | `res://scenes/vfx/card_trail_<小写id>.tscn` |
| 头像缩略图图标纹理 | `res://images/ui/top_panel/character_icon_<小写id>.png` |
| 头像描边纹理 | `res://images/ui/top_panel/character_icon_<小写id>_outline.png` |
| 角色选择背景场景 | `res://scenes/screens/char_select/char_select_bg_<小写id>.tscn` |
| 角色选择底部图像 | `res://images/packed/character_select/char_select_<小写id>.png` |
| 未解锁时的底部图像 | `res://images/packed/character_select/char_select_<小写id>_locked.png` |
| 地图标记箭头图像 | `res://images/packed/map/icons/map_marker_<小写id>.png` |
| 联机手部纹理（手指/石头/布/剪刀） | `res://images/ui/hands/multiplayer_hand_<小写id>_{point/rock/paper/scissors}.png` |
| 过场动画着色器材质 | `res://materials/transitions/<小写id>_transition_mat.tres` |

#### FMOD 音效路径

```
event:/sfx/characters/<小写id>/<小写id>_attack
event:/sfx/characters/<小写id>/<小写id>_cast
event:/sfx/characters/<小写id>/<小写id>_die
```

若无自定义音效，可在角色类中重写使用其他角色音效。

#### 场景结构要点

- **待机动画场景**：根节点挂载 `NCreatureVisuals` 脚本（或继承类），含名为 `Visuals` 的唯一名称子节点
- **能量计数器**：根节点挂载 `NEnergyCounter` 脚本，内含 `MegaLabel`（需导入 `res://addons/mega_text/` 插件或使用继承占位）
- **卡牌拖尾**：根节点挂载 `NCardTrailVfx` 脚本，内含 `NCardTrail` 类型的 `Line2D` 节点

#### Spine 动画导入

Godot 不原生支持 Spine，需下载 **Godot-Spine GDExtension 插件**放到 `bin/` 目录。
JSON 格式骨骼动画后缀改为 `.spine-json`。

#### 程序集脚本检索修复

STS2 通过 `AssemblyLoadContext.LoadFromAssemblyPath` 加载模组 DLL，绕过了 Godot 脚本映射。需在初始化方法中加入：

```csharp
// 让 Godot 检索当前程序集，建立脚本→资源映射
var assembly = Assembly.GetExecutingAssembly();
// 调用 Godot 内部检索方法
```

否则场景实例化时会出现找不���程序集错误。

#### 角色本地化

`res://<ModId>/localization/<lang>/characters.json`

#### 注册角色

```csharp
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)]
static class AllCharacters_Patch
{
    static void Postfix(ref IReadOnlyList<CharacterModel> __result)
    {
        __result = [.. __result, new MyCharacter()];
    }
}
```

参考 Sts2-YuWanCard 项目结构，包含：
- 角色定义（血量、初始遗物）
- 初始卡组
- 专属卡牌
- 专属遗物
- 专属状态效果
- 充能球（可选）

### 敌人开发

自定义敌人需要两个类：`MonsterModel`（怪物） + `EncounterModel`（遭遇）。游戏通过 EncounterModel 刷出战斗中的怪物。

#### 怪物类

所有敌对怪物继承 `MonsterModel`，核心是 `GenerateMoveStateMachine` 回调。

```csharp
public class MyCustomMonster : MonsterModel
{
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var attackState = new MoveState(
            stateId: "ATTACK",
            intents: [MoveIntent.Attack],       // 显示攻击意图
            onPerform: async () =>
            {
                await DamageCmd.Attack(6)
                    .FromMonster(this)
                    .TargetingRandomOpponents(CombatState!)
                    .Execute();
            }
        );

        var buffState = new MoveState(
            stateId: "BUFF",
            intents: [MoveIntent.Buff],         // 显示强化意图
            onPerform: async () => { Amount += 3; }
        );

        // 循环：攻击 → 强化 → 攻击 → ...
        attackState.FollowUpState = buffState;
        buffState.FollowUpState = attackState;

        return new MonsterMoveStateMachine(
            states: [attackState, buffState],
            initialState: attackState
        );
    }
}
```

#### 怪物状态机

**MoveState 构造函数：** `MoveState(stateId, intents, onPerform)`

| 参数 | 说明 |
|------|------|
| `stateId` | 状态唯一标识（字符串） |
| `intents` | 意图：`MoveIntent.Attack` / `Buff` / `Debuff` / `Defend` 等 |
| `onPerform` | 执行回调 |
| `FollowUpState` | 下一状态 |

**随机分支：**

```csharp
var randomBranch = new RandomBranchState("RANDOM");
randomBranch.AddBranch(attackState, cooldown: 2, repeatType: RepeatType.None, weight: 1);
randomBranch.AddBranch(buffState, cooldown: 0, repeatType: RepeatType.None, weight: 1);
```

**条件分支：**

```csharp
var conditional = new ConditionalState("CONDITIONAL");
conditional.AddBranch(
    condition: () => Owner!.PlayerCombatState.Health <= 10,
    state: bigAttackState     // 血少 → 大招
);
conditional.AddBranch(
    condition: () => true,
    state: normalAttackState  // 否则 → 普攻
);
```

#### 怪物资源

| 资源 | 路径 |
|------|------|
| 待机动画场景 | `res://scenes/creature_visuals/<小写id>.tscn` |
| 攻击音效 | `event:/sfx/enemy/enemy_attacks/<小写id>/<小写id>_attack` |
| 施法音效 | `event:/sfx/enemy/enemy_attacks/<小写id>/<小写id>_cast` |
| 死亡音效 | `event:/sfx/enemy/enemy_attacks/<小写id>/<小写id>_die` |

场景结构同角色（`NCreatureVisuals` 根节点 + `Visuals` 唯一名子节点），`%Bounds` 节点决定选择框/血条长度/意图图标位置。

#### 怪物本地化

`res://<ModId>/localization/<lang>/monsters.json`

```json
{
  "MY_CUSTOM_MONSTER.name": "自定义怪物",
  "MY_CUSTOM_MONSTER.moves.ATTACK.title": "攻击",
  "MY_CUSTOM_MONSTER.moves.BUFF.title": "强化"
}
```

#### 遭遇类

遭遇（Encounter）代表地图上的节点，决定怪物池和奖励档位。

```csharp
public class MyCustomEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster; // Monster/Elite/Boss
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        [new MyCustomMonster()];

    protected override IEnumerable<(MonsterModel, string?)> GenerateMonsters()
    {
        return [(ModelDb.Get<MyCustomMonster>().ToMutable(), null)];
    }
}
```

- `RoomType.Monster` / `Elite` / `Boss` 决定奖励档位
- 第二个元组参数 `null` = 自动站位；如需自定义站位，场景 `res://scenes/encounters/<小写遭遇id>.tscn` 中用 `Marker2D` 节点指定槽位名

#### 注册遭遇

```csharp
[HarmonyPatch(typeof(Overgrowth), nameof(Overgrowth.GenerateAllEncounters))]
static class Overgrowth_Encounters_Patch
{
    static void Postfix(ref IEnumerable<EncounterModel> __result)
    {
        __result = [.. __result, new MyCustomEncounter()];
    }
}
```

#### 遭遇本地化

`res://<ModId>/localization/<lang>/encounters.json`

#### 控制台测试

按 `` ` `` 打开控制台，输入命令直接触发某场遭遇。

### 多人模式支持

- 部分卡牌/遗物支持多人模式专属效果
- 使用 `(多人模式)` 标记多人专属内容
- 注意队友交互逻辑

## CSProj 关键配置

### 裸模组 csproj 示例

```xml
<Project Sdk="Godot.NET.Sdk/4.5.1" InitialTargets="CheckDependencyPaths">
    <Import Project=".\Sts2PathDiscovery.props"/>
    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <ImplicitUsings>true</ImplicitUsings>
        <Nullable>enable</Nullable>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    </PropertyGroup>

    <!-- STS2 核心引用 -->
    <ItemGroup Condition="Exists('$(Sts2DataDir)')">
        <Reference Include="0Harmony">
            <HintPath>$(Sts2DataDir)/0Harmony.dll</HintPath>
            <Private>false</Private>
        </Reference>
        <Reference Include="sts2">
            <HintPath>$(Sts2DataDir)/sts2.dll</HintPath>
            <Private>false</Private>
        </Reference>
    </ItemGroup>

    <!-- PCK 打包器（不需要 Godot 时也能简单打包） -->
    <ItemGroup>
        <PackageReference Include="Krafs.Publicizer" Version="2.3.0" PrivateAssets="All"/>
        <PackageReference Include="BSchneppe.StS2.PckPacker" Version="0.1.1" PrivateAssets="All"/>
    </ItemGroup>

    <!-- Build 后自动复制到 mods 目录 -->
    <Target Name="CopyToModsFolderOnBuild" AfterTargets="PostBuildEvent">
        <Copy SourceFiles="$(TargetPath)" DestinationFolder="$(ModsPath)$(MSBuildProjectName)/" />
        <Copy SourceFiles="$(AssemblyName).json" DestinationFolder="$(ModsPath)$(MSBuildProjectName)/" />
    </Target>
</Project>
```

## 最佳实践

1. **始终检查最新版 BaseLib API** — API 可能变化
2. **参考多个项目** — YuWanCard（裸模组）、BaseLib 官方文档
3. **模块化设计** — 卡牌基类、事件注册器、工具类分离
4. **版本管理** — 使用 Git 管理代码
5. **日志调试** — 利用 `%AppData%\SlayTheSpire2\logs\` 排查问题
6. **社区交流** — 加入 Discord/QQ 群获取帮助
7. **可空类型处理** — STS2 中 Creature/CardModel 等属性常为可空，OnPlay 等时机可用 `!` 断言
8. **图片路径** — 所有 `res://` 路径相对于 `STS2_ShunMod/` 资源文件夹
9. **Build vs Publish** — 纯代码改动用 Build（快），改资源用 Publish

## 注意事项

- ⚠️ **必须先访问 GitHub 更新信息** — 每次使用此 skill 时
- API 可能随版本变化，本文档代码仅为示例
- 开发前务必阅读官方 Wiki 最新文档
- 遇到问题时，参考 YuWanCard 等成熟项目
- `CardRarity.Event` 稀有度卡牌不会出现在卡牌大全中
- 模组图标 `mod_image.png` 需放在 `res://<modid>/mod_image.png`

## 相关链接汇总

| 资源 | 链接 |
|------|------|
| BaseLib GitHub | https://github.com/Alchyr/BaseLib-StS2 |
| BaseLib Wiki | https://alchyr.github.io/BaseLib-Wiki/ |
| ModTemplate GitHub | https://github.com/Alchyr/ModTemplate-StS2 |
| ModTemplate Wiki | https://github.com/Alchyr/ModTemplate-StS2/wiki/Setup |
| YuWanCard GitHub | https://github.com/YuWan886/Sts2-YuWanCard |
| YuWanCard NexusMods | https://www.nexusmods.com/slaythespire2/mods/149 |
| B站开发教程 01 - 环境搭建 | https://www.bilibili.com/opus/1179300682687053826 |
| B站开发教程 02 - 自定义遗物 | https://www.bilibili.com/opus/1179604439936270359 |
| B站开发教程 03 - 自定义卡牌 | https://www.bilibili.com/opus/1179979923167641608 |
| B站开发教程 04 - 自定义药水 | https://www.bilibili.com/opus/1180032536494997541 |
| B站开发教程 05 - 自定义附魔 | https://www.bilibili.com/opus/1180713881530531843 |
| B站开发教程 06 - 自定义事件 | https://www.bilibili.com/opus/1180714323922649110 |
| B站开发教程 07 - 自定义能力 | https://www.bilibili.com/opus/1181126133981118470 |
| B站开发教程 08 - 自定义角色 | https://www.bilibili.com/opus/1182961747166756931 |
| B站开发教程 09 - 自定义敌怪 | https://www.bilibili.com/opus/1183380755590414377 |
| Megadot 下载 | https://megadot.megacrit.com/ |
| .NET SDK | https://dotnet.microsoft.com/en-us/download |
| STS2Plus 完整Mod参考 | https://github.com/StephenSHorton/STS2Plus |
| STS2-ShunMod（用户项目） | https://github.com/yehuoshun/STS2-ShunMod |
| 七咒之戒参考 | https://github.com/Aizistral-Studios/Enigmatic-Legacy |
| Spire Codex 参考 | https://github.com/ptrlrd/spire-codex |
| RitsuLib（BaseLib替代） | https://github.com/BAKAOLC/STS2-RitsuLib |

---

**最后更新**: 2026-05-11
**基于**: BaseLib v3.1.2 / YuWanCard v0.4.9+ / B站教程 / STS2Plus / STS2-ShunMod 项目实践