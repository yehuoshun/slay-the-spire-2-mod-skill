# BaseLib 集成指南

> BaseLib 是 Alchyr 开发的 STS2 模组标准库，提供便捷的基类、工具和注册机制。
> 源码：https://github.com/Alchyr/BaseLib-StS2

---

## 概述

BaseLib 提供了一套 `Custom*Model` 抽象类，封装了原生 API 的常见模式。使用 BaseLib 可以大幅减少模板代码，但不是必须的——纯原生也能实现同样功能。

> 📌 记录版本：**BaseLib 3.4.5**（2026-08-14 发布）。本文含 3.4.x 新增内容：`CustomResource` 自定义资源系统、`CustomCalculatedVar.Create()`、`CustomPotionModel.CustomLargeImagePath`。

---

## 项目配置

### csproj

```xml
<Project Sdk="Godot.NET.Sdk/4.5.1" InitialTargets="CheckDependencyPaths">
    <Import Project=".\Sts2PathDiscovery.props"/>
    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <ImplicitUsings>true</ImplicitUsings>
        <Nullable>enable</Nullable>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
        <GodotDisabledSourceGenerators>GodotPluginsInitializer</GodotDisabledSourceGenerators>
    </PropertyGroup>

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

    <ItemGroup>
        <PackageReference Include="Alchyr.Sts2.BaseLib" Version="*" PrivateAssets="All"/>
        <PackageReference Include="Krafs.Publicizer" Version="2.3.0" PrivateAssets="All"/>
        <PackageReference Include="Alchyr.Sts2.ModAnalyzers" Version="*" />
        <AdditionalFiles Include="ModName/localization/**/*.json"/>
    </ItemGroup>
</Project>
```

### Sts2PathDiscovery.props

自动检测 STS2 安装路径（Windows/Linux/macOS），放在项目根目录：

```xml
<Project>
    <PropertyGroup>
        <IsLinux>false</IsLinux>
        <IsOSX>false</IsOSX>
        <IsLinux Condition="$([MSBuild]::IsOSPlatform('Linux'))">true</IsLinux>
        <IsOSX Condition="$([MSBuild]::IsOSPlatform('OSX'))">true</IsOSX>
    </PropertyGroup>

    <PropertyGroup Condition="'$(IsLinux)' != 'true' And '$(IsOSX)' != 'true'">
        <RegistrySts2Path>$([MSBuild]::GetRegistryValueFromView('HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 2868840', 'InstallLocation', '', RegistryView.Registry64, RegistryView.Registry32))</RegistrySts2Path>
        <AutoSteamPath>$(registry:HKEY_CURRENT_USER\Software\Valve\Steam@SteamPath)\steamapps</AutoSteamPath>
        <Sts2Path Condition="'$(Sts2Path)' == '' and Exists('$(AutoSteamPath)/common/Slay the Spire 2')">$(AutoSteamPath)/common/Slay the Spire 2</Sts2Path>
        <Sts2Path Condition="'$(Sts2Path)' == '' and Exists('$(RegistrySts2Path)/data_sts2_windows_x86_64')">$(RegistrySts2Path)</Sts2Path>
        <Sts2Path Condition="'$(Sts2Path)' == ''">$(AutoSteamPath)/common/Slay the Spire 2</Sts2Path>
        <ModsPath>$(Sts2Path)/mods/</ModsPath>
        <Sts2DataDir>$(Sts2Path)/data_sts2_windows_x86_64</Sts2DataDir>
    </PropertyGroup>

    <PropertyGroup Condition="'$(IsLinux)' == 'true'">
        <SteamLibraryPath>$(HOME)/.local/share/Steam/steamapps</SteamLibraryPath>
        <Sts2Path>$(SteamLibraryPath)/common/Slay the Spire 2</Sts2Path>
        <ModsPath>$(Sts2Path)/mods/</ModsPath>
        <Sts2DataDir>$(Sts2Path)/data_sts2_linuxbsd_x86_64</Sts2DataDir>
    </PropertyGroup>

    <PropertyGroup Condition="'$(IsOSX)' == 'true'">
        <SteamLibraryPath>$(HOME)/Library/Application Support/Steam/steamapps</SteamLibraryPath>
        <Sts2Path>$(SteamLibraryPath)/common/Slay the Spire 2</Sts2Path>
        <ModsPath>$(Sts2Path)/SlayTheSpire2.app/Contents/MacOS/mods/</ModsPath>
        <Sts2DataDir>$(Sts2Path)/SlayTheSpire2.app/Contents/Resources/data_sts2_macos_x86_64</Sts2DataDir>
    </PropertyGroup>
</Project>
```

### Directory.Build.props

```xml
<Project>
    <PropertyGroup>
        <GodotPath>C:/megadot/MegaDot_v4.5.1-stable_mono_win64.exe</GodotPath>
    </PropertyGroup>
</Project>
```

### mod_manifest.json

```json
{
  "id": "YourModId",
  "name": "YourModName",
  "author": "YourName",
  "description": "Your mod description",
  "version": "v0.0.0",
  "min_game_version": "0.107.0",
  "has_pck": true,
  "has_dll": true,
  "dependencies": [
    {"id": "BaseLib", "min_version": "3.3.0"}
  ],
  "affects_gameplay": true
}
```

### ModEntry.cs

```csharp
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YourModId";
    public const string ResPath = $"res://{ModId}";

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll();
    }
}
```

---

## Custom*Model 基类速查

### ICustomModel

所有 BaseLib 模型的标记接口，用于自动注册检测。

```csharp
public interface ICustomModel;
```

### 自动注册机制

所有 `Custom*Model` 构造函数中调用 `autoAdd = true` 时自动注册：

```csharp
// CustomCardModel / CustomRelicModel 构造函数
public CustomCardModel(bool autoAdd = true) {
    if (autoAdd) CustomContentDictionary.AddModel(GetType());
}
```

无需手动 `AddModelToPool` 或 `ModelDb.Inject`。

### CustomCardModel

```csharp
public abstract class CustomCardModel : CardModel, ICustomModel, ILocalizationProvider
{
    // 构造函数
    public CustomCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true, bool autoAdd = true);

    // 自动推断 GainsBlock（从 DynamicVars 检测 BlockVar/CalculatedBlockVar）
    public override bool GainsBlock { get; }

    // 自定义卡牌边框纹理
    public virtual Texture2D? CustomFrame => null;
    // 自定义卡牌边框着色器材质
    public virtual Material? CreateCustomFrameMaterial => null;
    public virtual Material? CreateCustomBannerMaterial => null;
    public virtual string? CustomBannerMaterialPath => null;

    // 自定义肖像图
    public virtual string? CustomPortraitPath => null;
    public virtual Texture2D? CustomPortrait => null;

    // 内联本地化（推荐用 CardLoc 类）
    public virtual List<(string, string)>? Localization => null;

    // 计算变量辅助方法
    public static IEnumerable<DynamicVar> MakeCalculatedVar(string name, int baseVal, Func<CardModel, Creature?, decimal> bonus, int mult = 1);
    public static IEnumerable<DynamicVar> MakeCalculatedDamage(int baseVal, Func<CardModel, Creature?, decimal> bonus, int mult = 1, ValueProp props = ValueProp.Move);
    public static IEnumerable<DynamicVar> MakeCalculatedBlock(int baseVal, Func<CardModel, Creature?, decimal> bonus, int mult = 1, ValueProp props = ValueProp.Move);
}
```

### CustomRelicModel

```csharp
public abstract class CustomRelicModel : RelicModel, ICustomModel, ILocalizationProvider
{
    public CustomRelicModel(bool autoAdd = true);
    public virtual RelicModel? GetUpgradeReplacement() => null;
    public virtual List<(string, string)>? Localization => null;
}
```

### CustomPowerModel

```csharp
public abstract class CustomPowerModel : PowerModel, ICustomPower, ILocalizationProvider, IHealthBarForecastSource
{
    public virtual string? CustomPackedIconPath => null;    // 64x64
    public virtual string? CustomBigIconPath => null;        // 256x256
    public virtual string? CustomBigBetaIconPath => null;

    public virtual IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context);
    public virtual List<(string, string)>? Localization => null;
}
```

### CustomPotionModel

```csharp
public abstract class CustomPotionModel : PotionModel, ICustomModel, ILocalizationProvider
{
    public CustomPotionModel(bool autoAdd = true);
    public virtual string? CustomPackedImagePath => null;
    public virtual string? CustomPackedOutlinePath => null;
    public virtual string? CustomLargeImagePath => null;  // 大药水图（3.4.5+，原版暂未使用）
    public virtual List<(string, string)>? Localization => null;
}
```

### CustomCardPoolModel

```csharp
public abstract class CustomCardPoolModel : CardPoolModel, ICustomModel, ICustomEnergyIconPool
{
    // 卡牌边框颜色（HSV）
    public virtual Color ShaderColor => new("FFFFFF");
    public virtual float H => ShaderColor.H;
    public virtual float S => ShaderColor.S;
    public virtual float V => ShaderColor.V;
    public virtual Texture2D? CustomFrame(CustomCardModel card);

    // 能量图标
    public virtual string? BigEnergyIconPath => null;
    public virtual string? TextEnergyIconPath => null;

    // 共享池（非角色专属）
    public virtual bool IsShared => false;
    public virtual bool SeenByDefault => false;
}
```

### CustomRelicPoolModel / CustomPotionPoolModel

同上模式，`IsShared` / `SeenByDefault` / `BigEnergyIconPath` / `TextEnergyIconPath`。

### CustomCharacterModel

```csharp
public abstract class CustomCharacterModel : CharacterModel, ICustomModel, ILocalizationProvider, ISceneConversions
{
    public CustomCharacterModel();  // 自动注册

    public virtual bool HideFromVanillaCharacterSelect => false;
    public virtual bool HideInCompendium => false;

    // 场景路径（不设置则用默认路径 res://scenes/...）
    public virtual string? CustomVisualPath => null;
    public virtual string? CustomTrailPath => null;
    public virtual string? CustomIconTexturePath => null;
    public virtual string? CustomIconPath => null;
    public virtual Control? CustomIcon => null;
    public virtual string? CustomEnergyCounterPath => null;
    public virtual string? CustomRestSiteAnimPath => null;
    public virtual string? CustomMerchantAnimPath => null;
    public virtual string? CustomCharacterSelectBg => null;
    public virtual string? CustomCharacterSelectIconPath => null;
    public virtual string? CustomCharacterSelectLockedIconPath => null;
    public virtual string? CustomCharacterSelectTransitionPath => null;
    public virtual string? CustomMapMarkerPath => null;
    public virtual string? CustomAttackSfx => null;
    public virtual string? CustomCastSfx => null;
    public virtual string? CustomDeathSfx => null;

    // 动画状态机
    public virtual NCreatureVisuals? CreateCustomVisuals();
    public virtual CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller);
    public static CreatureAnimator SetupAnimationState(MegaSprite controller, string idleName, ...);

    // 联机手势纹理
    public virtual string? CustomArmPointingTexturePath => null;
    public virtual string? CustomArmRockTexturePath => null;
    public virtual string? CustomArmPaperTexturePath => null;
    public virtual string? CustomArmScissorsTexturePath => null;

    public virtual List<(string, string)>? Localization => null;
}
```

### PlaceholderCharacterModel

简化角色创建——使用铁甲战士默认资源作为占位图：

```csharp
public class MyCharacter : PlaceholderCharacterModel
{
    public const string CharacterId = "MyCharacter";
    public static readonly Color Color = new("ffffff");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 70;
    public override IEnumerable<CardModel> StartingDeck => [ ModelDb.Card<StrikeIronclad>(), ... ];
    public override IReadOnlyList<RelicModel> StartingRelics => [ ModelDb.Relic<BurningBlood>() ];
    public override CardPoolModel CardPool => ModelDb.CardPool<MyCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<MyRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<MyPotionPool>();
}
```

### CustomEnchantmentModel

```csharp
public abstract class CustomEnchantmentModel : EnchantmentModel, ICustomModel, ILocalizationProvider
{
    public virtual string? CustomIconPath => null;
    public virtual List<(string, string)>? Localization => null;
}
```

### CustomEventModel

```csharp
public abstract class CustomEventModel : EventModel, ICustomModel, ILocalizationProvider
{
    public virtual string? CustomInitialPortraitPath => null;
    public virtual string? CustomBackgroundScenePath => null;
    public virtual string? CustomVfxPath => null;
    public virtual List<(string, string)>? Localization => null;

    // 选项创建辅助（自动从方法名生成本地化键）
    protected EventOption Option(Func<PlayerChoiceContext, Task> action, RelicModel? relic = null);
}
```

### CustomEncounterModel

```csharp
public abstract class CustomEncounterModel : EncounterModel, ICustomModel
{
    public CustomEncounterModel(RoomType roomType, bool autoAdd = true);
    public virtual bool IsValidForAct(int act);
    public virtual string? CustomScenePath => null;
    public virtual Texture2D? CustomEncounterBackground();
    public virtual string? CustomRunHistoryIconPath => null;
}
```

### CustomMonsterModel

```csharp
public abstract class CustomMonsterModel : MonsterModel, ICustomModel, ILocalizationProvider, ISceneConversions
{
    public virtual string? CustomVisualPath => null;
    public virtual NCreatureVisuals? CreateCustomVisuals();
    public virtual CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller);
    public static CreatureAnimator SetupAnimationState(MegaSprite controller, string idleName, ...);
    public virtual List<(string, string)>? Localization => null;
}
```

### CustomModifierModel

```csharp
public abstract class CustomModifierModel : ModifierModel, ICustomModel, ILocalizationProvider
{
    public CustomModifierModel(bool autoAdd = true);
    public virtual ModifierAlignment Alignment => ModifierAlignment.Good;
    public virtual string? MutuallyExclusiveGroup => null;
    public virtual int SortOrder => 0;
    public virtual List<(string, string)>? Localization => null;
}
```

### CustomAncientModel

```csharp
public abstract class CustomAncientModel : AncientEventModel, ICustomModel, ILocalizationProvider
{
    // 选项池系统（加权随机）
    public virtual AncientOptionPools OptionPools { get; }
    protected static AncientOptionPools MakePool();
    protected static AncientOption<T> AncientOption<T>() where T : AbstractModel;
    public virtual bool IsValidForAct(int act);
    public virtual bool ShouldForceSpawn(int act);
    public virtual List<(string, string)>? Localization => null;
}
```

### CustomTemporaryPowerModel

```csharp
public abstract class CustomTemporaryPowerModel : PowerModel
{
    public Func<Creature, int>? ApplyPowerFunc { get; set; }
    // 指定一个能力在回合结束时自动施加
    public PowerModel? InternallyAppliedPower { get; set; }
}
```

### CustomActModel

```csharp
public abstract class CustomActModel : ActModel, ICustomModel, ILocalizationProvider
{
    public virtual string? CustomMapBackgroundPath => null;
    public virtual string? CustomMapScenePath => null;
    public virtual string? CustomCombatBackgroundPath => null;
    public virtual string? CustomShopScenePath => null;
    public virtual string? CustomRestSiteScenePath => null;
    public virtual string? CustomTreasureScenePath => null;
    public virtual string? CustomBossChestScenePath => null;
    public virtual string? CustomMusicPath => null;
    public virtual string? CustomBossMusicPath => null;
    public virtual List<(string, string)>? Localization => null;
}
```

### CustomPetModel

```csharp
public abstract class CustomPetModel(bool visibleHp) : CustomMonsterModel
{
    // 不生成 AI 状态机，固定不行动
}
```

### CustomOrbModel

```csharp
public abstract class CustomOrbModel : OrbModel, ICustomModel, ILocalizationProvider
{
    public virtual string? CustomIconPath => null;
    public virtual string? CustomSpritePath => null;
    public virtual bool IncludeInRandomPool => false;
    public virtual Node2D? CreateCustomSprite() => null;
    public virtual List<(string, string)>? Localization => null;
}
```

### CustomCalculatedVar — 计算变量（3.4.5+）

`CustomCalculatedVar.Create()` 静态便捷方法，一次生成计算变量的三个子变量，计算值 = `baseVal + (bonus * mult)`：

```csharp
// 返回三个变量：{Name}Base（基础值）、{Name}Extra（倍率）、主变量
// 签名：bonus 为加成计算函数 Func<DynamicVarSource, Creature?, decimal>
public static IEnumerable<DynamicVar> Create(
    string name, int baseVal,
    Func<DynamicVarSource, Creature?, decimal> bonus, int mult = 1);

// 用法示例（在 CanonicalVars 中）：基础 5，倍率 2，加成由 bonus 函数计算
protected override IEnumerable<DynamicVar> CanonicalVars =>
    CustomCalculatedVar.Create("Damage", 5, (src, creature) => /* 你的加成 */ 0m, 2);
```

相比 `CustomCardModel.MakeCalculatedVar`（只适用于卡牌），`CustomCalculatedVar.Create` 不依赖卡牌类型，通用性更强。

---

## [Pool] 属性

自动将模型关联到指定池：

```csharp
[Pool(typeof(MyCardPool))]
public class MyCard : CustomCardModel { ... }

[Pool(typeof(MyRelicPool))]
public class MyRelic : CustomRelicModel { ... }

[Pool(typeof(MyPotionPool))]
public class MyPotion : CustomPotionModel { ... }
```

不再需要手动 `AddModelToPool`。

---

## ConstructedCardModel — Builder 模式

```csharp
// 使用 Builder 模式一行定义卡牌
public class MyCard : ConstructedCardModel
{
    public MyCard() : base(
        new ConstructedCard("MyCard")
            .WithCost(1)
            .WithType(CardType.Attack)
            .WithRarity(CardRarity.Common)
            .WithTarget(TargetType.TargetedEnemy)
            .WithDamage(6)
            .WithUpgrade(card => card.WithDamage(9))
    ) { }
}
```

完整 Builder API：

| 方法 | 说明 |
|------|------|
| `.WithCost(int)` | 设置耗能 |
| `.WithType(CardType)` | 设置卡牌类型 |
| `.WithRarity(CardRarity)` | 设置稀有度 |
| `.WithTarget(TargetType)` | 设置目标类型 |
| `.WithDamage(int)` | 添加伤害动态变量 |
| `.WithBlock(int)` | 添加格挡动态变量 |
| `.WithPower<T>(int)` | 添加能力施加 |
| `.WithKeywords(params CardKeyword[])` | 添加关键词 |
| `.WithTags(params CardTag[])` | 添加标签 |
| `.WithCalculatedDamage(string, int, Func<...>)` | 计算伤害 |
| `.WithCalculatedBlock(string, int, Func<...>)` | 计算格挡 |
| `.WithCalculatedVar(string, int, Func<...>)` | 自定义计算变量 |
| `.WithTip(string, string)` | 添加提示文本 |
| `.WithCardPool(Type)` | 指定卡池 |
| `.WithUpgrade(Action<ConstructedCard>)` | 升级变化 |
| `.WithCustomPortrait(string)` | 自定义肖像 |
| `.WithCustomFrame(string)` | 自定义边框 |
| `.WithCustomFrameMaterial(Material)` | 自定义边框材质 |
| `.OnPlay(Func<...>)` | 打出回调 |
| `.OnUpgrade(Action)` | 升级回调 |
| `.IsPlayable(Func<bool>)` | 使用条件 |

---

---

## CardModifier — 卡牌修饰器

BaseLib 提供 `CardModifier` 系统，将修饰器附着到卡牌上修改其行为。类似附魔但更灵活，支持序列化和多人同步。

### 基本用法

```csharp
// 添加修饰器到卡牌
card.AddModifier<MyModifier>();

// 移除修饰器
card.RemoveModifier<MyModifier>();
```

### 自定义修饰器

```csharp
public class MyModifier : CardModifier
{
    // 修改基础伤害
    public override int ModifyBaseDamageAdditive(CardModel card)
    {
        return Amount; // 增加 Amount 点伤害
    }

    // 修改基础格挡
    public override int ModifyBaseBlockAdditive(CardModel card)
    {
        return Amount; // 增加 Amount 点格挡
    }

    // 保存额外数据
    public override void StoreSaveData(ModifierSave save)
    {
        save.IntProperties["myValue"] = 42;
    }

    // 加载额外数据
    public override void LoadSaveData(ModifierSave save)
    {
        if (save.IntProperties.TryGetValue("myValue", out var val))
            ; // 使用恢复的值
    }
}
```

### 修饰器回调

| 方法 | 说明 |
|------|------|
| `ModifyBaseDamageAdditive(CardModel)` | 修改基础伤害 |
| `ModifyBaseBlockAdditive(CardModel)` | 修改基础格挡 |
| `OnCardPlayed(PlayerChoiceContext, CardPlayState)` | 卡牌打出时 |
| `OnTurnEndInHand(PlayerChoiceContext)` | 回合结束仍在手牌时 |
| `StoreSaveData(ModifierSave)` | 保存额外数据 |
| `LoadSaveData(ModifierSave)` | 加载额外数据 |

### 序列化

CardModifier 自动序列化其 `Amount` + `IntProperties` + `AdditionalProperties`。

### 注意

CardModifier 是 **BaseLib 专属**，原生 API 不支持。

---

## CustomResource — 自定义资源系统（3.4.5 完整支持）

BaseLib 支持自定义资源（类似能量的消耗项）：卡牌可以花费自定义资源打出，资源有独立的数量、每回合重置逻辑和 UI 处理器。

### 快速上手

```csharp
// 1. 定义资源类（继承 BasicCustomResource，setEachTurn 控制每回合重置）
public class ManaResource : BasicCustomResource
{
    public ManaResource() : base("Mana", setEachTurn: 3) { } // 每回合重置为 3
}

// 2. 卡牌消耗该资源
public class MyCard : CustomCardModel
{
    public MyCard() : base(0, CardType.Skill, CardRarity.Common, TargetType.None)
    {
        CustomResources<ManaResource>.SetCanonicalCost(this, 2); // 消耗 2 法力
    }
}

// 3. 战斗中获取资源实例（例如击杀敌人奖励资源）
var mana = CustomResources<ManaResource>.Get(combatState.Player.PlayerCombatState);
mana.ModifyAmount(+1);
```

### CustomResource 基类

```csharp
public abstract class CustomResource(string id)
{
    public string Id { get; protected set; }               // 唯一 ID（用于排序/标识）
    public event Action<int, int>? AmountChanged;          // 数量变化事件（旧值, 新值）

    public virtual int Amount { get; set; }                // 当前可花费数量
    public void ModifyAmount(int change);                  // 增减资源

    public virtual bool ApplySharedModification => true;   // "免费"效果（SetToFreeThisTurn/Combat）是否也作用于本资源
    public virtual bool IsDefaultOptional => false;        // 该资源费用是否默认可选

    // 战斗开始准备（PlayerCombatState 初始化时调用）
    public virtual void PrepForCombat(PlayerCombatState state) { }
    // 回合开始重置（3.4.5+：玩家能量重置后、AfterEnergyReset 钩子前调用）
    public virtual void StartOfTurnReset(PlayerCombatState state, ICombatState combatState) { }

    // 花费资源（成功触发 BaseLibHooks.AfterSpendCustomResource<T>）
    public virtual async Task<bool> Spend<T>(ICombatState combatState, AbstractModel? spender, int amount, bool optional)
        where T : CustomResource;
    public virtual bool CanAfford(CardModel card, int cost);   // 是否付得起
    public virtual UnplayableReason UnplayableReason { get; }  // 付不起时使用的不可打出原因

    // UI 视觉处理器（3.4.5+：注册时自动调用一次，返回 null 则自行处理视觉）
    public abstract ICustomResourceVisualsHandler? ResourceVisualsHandler();
    public abstract ICustomCostVisualsHandler? CostVisualsHandler();
}
```

### BasicCustomResource — 开箱即用

```csharp
// setEachTurn > 0：每回合开始自动重置为该值；默认 -1 不自动重置
public abstract class BasicCustomResource(string resourceId, int setEachTurn = -1) : CustomResource
{
    public int DefaultMax { get; set; } = setEachTurn;
    // 已实现：PrepForCombat 置 0、StartOfTurnReset 按 DefaultMax 重置、
    //         自带 BasicCostVisualsHandler / BasicResourceVisualsHandler（白=正常/绿=修改/红=付不起/灰=可选付不起）
}
```

### CustomResources<T> 静态 API

| 方法 | 说明 |
|------|------|
| `Get(PlayerCombatState)` | 获取该玩家的资源实例 |
| `TryGet(PlayerCombatState?, out T?)` | 安全获取（未注册/未战斗中返回 false） |
| `SetCanonicalCost(CardModel, int)` | 设置卡牌该项资源的基础费用 |
| `SetXCost(CardModel)` | 将该项资源费用设为 X 费用 |
| `CanonicalCost(CardModel)` | 读取卡牌该项资源的基础费用 |
| `Cost(CardModel)` | 获取 `CustomResourceCost<T>` 实例（费用对象） |
| `AmountSpent(CardPlay)` | 本次打出实际花费的该资源数量 |
| `WasSpent(CardPlay)` | 本次打出是否花费了该资源 |

### CustomResourceCost<T> — 费用对象

```csharp
var cost = CustomResources<ManaResource>.Cost(card);
cost.MakeOptional();          // 强制该费用可选（忽略资源默认 IsDefaultOptional）
cost.IsOptional(player);      // 查询当前是否可选
// X 费用：SetXCost(card) 后，CapturedXValue / ResolveXValue() 读写 X 值
```

### 挂钩子

```csharp
// 战斗中修改资源费用（对应原生 TryModifyEnergyCostInCombat）
public interface IModifyResourceCostInCombat<T> where T : CustomResource
{
    decimal ModifyResourceCostInCombat(CardModel card, T resource, decimal originalCost);
}

// 资源被花费后响应
public interface IAfterSpendResource<T> where T : CustomResource
{
    Task AfterSpendResource(ICombatState combatState, T resource, AbstractModel? spender, int amount);
}
```

### 常见问题

| 问题 | 解决 |
|------|------|
| 资源每回合不重置 | 重写 `StartOfTurnReset`，或用 `BasicCustomResource` 的 `setEachTurn` 参数 |
| 资源不显示 UI | 实现 `ResourceVisualsHandler()` / `CostVisualsHandler()` 返回处理器 |
| "免费"效果不作用于本资源 | `ApplySharedModification` 返回 false |
| 卡牌付不起资源费用 | 检查 `CanAfford`、`UnplayableReason`、`IsDefaultOptional` 配置 |
| 需要 X 费用 | `CustomResources<T>.SetXCost(card)` + `CapturedXValue` |

---

## BaseLib 工具类

### NodeFactory — Godot 节点工厂

从资源路径创建节点实例，自动处理类型转换：

```csharp
// 从场景资源创建节点
var icon = NodeFactory<Control>.CreateFromResource("res://path/to/scene.tscn");
icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

// 从 NEnergyCounter 资源创建
var counter = NodeFactory<NEnergyCounter>.CreateFromResource(counter);
```

### ShaderUtils — 着色器工具

生成 HSV 偏移着色器材质，用于自定义卡牌颜色：

```csharp
// 生成 HSV 着色器材质
var material = ShaderUtils.GenerateHsv(hue: 1f, saturation: 1f, value: 1f);

// 返回 ShaderMaterial，可直接应用到卡牌池
```

### CustomEnergyCounter — 自定义能量计数器

```csharp
public readonly struct CustomEnergyCounter(
    Func<int, string> pathFunc,  // 根据层数返回图标路径
    Color outlineColor,          // 能量数字描边颜色
    Color burstColor             // 能量爆发颜色
);

// 使用示例
public override CustomEnergyCounter? CustomEnergyCounter =>
    new CustomEnergyCounter(
        layer => $"res://images/energys/layer_{layer}.png",
        new Color(1f, 0.8f, 0.2f),  // 金色描边
        new Color(1f, 1f, 1f)        // 白色爆发
    );

// 或使用场景路径版本
public override string? CustomEnergyCounterPath =>
    "res://MyMod/scenes/energy_counter.tscn";
```

### ISceneConversions — 场景自动转换

将 Godot 原生场景节点自动转换为 STS2 所需的类型：

```csharp
public class MyCharacter : CustomCharacterModel
{
    // 注册场景转换（在构造函数中调用）
    public void RegisterSceneConversions()
    {
        // 将普通场景转为 NCreatureVisuals
        CustomVisualPath?.RegisterSceneForConversion<NCreatureVisuals>();
        // 将普通场景转为 NRestSiteCharacter
        CustomRestSiteAnimPath?.RegisterSceneForConversion<NRestSiteCharacter>();
        // 将普通场景转为 NEnergyCounter
        CustomEnergyCounterPath?.RegisterSceneForConversion<NEnergyCounter>();
    }
}
```

### PreloadManager.Cache — 资源缓存

```csharp
// 缓存获取 Texture2D
var texture = PreloadManager.Cache.GetTexture2D("cards/frame.png".ImagePath());

// 缓存获取任意资源
var resource = PreloadManager.Cache.GetAsset<Resource>(path);
```

### 扩展方法（StringExtensions）

```csharp
public static string ImagePath(this string path) { ... }
public static string CardImagePath(this string path) { ... }
public static string BigCardImagePath(this string path) { ... }
public static string PowerImagePath(this string path) { ... }
public static string BigPowerImagePath(this string path) { ... }
public static string RelicImagePath(this string path) { ... }
public static string BigRelicImagePath(this string path) { ... }
public static string CharacterUiPath(this string path) { ... }
```

所有路径方法都带 `ResourceLoader.Exists()` 回退检查，找不到自动用默认图。

---

## 图像路径与回退模式

BaseLib 推荐使用 `ResourceLoader.Exists()` 检查路径是否存在，不存在则回退到默认图：

```csharp
public static string CardImagePath(this string path)
{
    path = Path.Join(ResPath, "images", "card_portraits", path);
    if (ResourceLoader.Exists(path)) return path;
    return Path.Join(ResPath, "images", "card_portraits", "card.png"); // 回退
}
```

---

## 本地化工具

BaseLib 提供 `Loc` 辅助类，支持在模型类中直接定义本地化：

```csharp
public virtual List<(string, string)>? Localization => new CardLoc("MyCard")
    .WithName("示例卡牌")
    .WithDescription("造成 {D} 点伤害。");
```

| 工具类 | 说明 |
|--------|------|
| `CardLoc(string)` | 卡牌本地化 |
| `RelicLoc(string)` | 遗物本地化 |
| `PowerLoc(string)` | 能力本地化 |
| `PotionLoc(string)` | 药水本地化 |
| `CharacterLoc(string)` | 角色本地化 |
| `OrbLoc(string)` | 球体本地化 |
| `EventLoc(string)` | 事件本地化 |

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 模型不自动注册 | 检查 `autoAdd` 参数是否为 true |
| 自定义资源不重置（BaseLib 3.4.5+） | `BasicCustomResource` 的 `setEachTurn` 或重写 `StartOfTurnReset` |
| `[Pool]` 不生效 | 确保 BaseLib 版本 >= 3.3.0 |
| 图片不显示 | 检查 `ResourceLoader.Exists(path)` 路径是否正确 |
| 角色不显示 | 继承 `CustomCharacterModel` 自动注册，无需 Patch |
| 能量计数器不显示 | 使用 `CustomEnergyCounter` 或 `CustomEnergyCounterPath` |
| 动画不播放 | 使用 `SetupAnimationState` 辅助方法 |

---

## 演进路线

- 当前方案：依赖 BaseLib NuGet 包
- 纯原生方案：参考 `references/` 下各模块的纯原生写法，直接继承原生 `CardModel`/`RelicModel`/`PowerModel` 等
- 两者可以混用：BaseLib 提供便捷基类，但 Harmony Patch 等仍需手写