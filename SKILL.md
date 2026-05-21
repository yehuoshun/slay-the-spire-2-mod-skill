---
name: slay-the-spire-2-mod-skill
description: 杀戮尖塔2（Slay the Spire 2 / STS2）模组开发实用指南。当用户提到杀戮尖塔2、杀戮尖塔、STS2、STS Mod、写模组、模组开发、卡牌、遗物、药水、能力、事件、角色、怪物、补丁、Harmony、Godot模组、Mod开发时使用。
---

# 杀戮尖塔 2 Mod 开发技能

## 源码参考

> ⚠️ **写任何 patch 前必须做的事**：
> 1. 检查本地有无 sts2-res：`ls ~/.openclaw/workspace/code/sts2-res/src/ 2>/dev/null || git clone --depth 1 https://github.com/yehuoshun/sts2-res ~/.openclaw/workspace/code/sts2-res`
> 2. `grep -rn "目标类名" ~/.openclaw/workspace/code/sts2-res/src/` 查真实签名
> 3. 对着真实签名写 patch，别猜

- **游戏反编译源码**: [yehuoshun/sts2-res](https://github.com/yehuoshun/sts2-res)（私有，仅本人可访问）— 最新游戏版本 decompile
- **Mod 项目源码**: [yehuoshun/STS2-ShunMod](https://github.com/yehuoshun/STS2-ShunMod) — 本章节 Mod 的完整源码
- **第三方参考**: [STS2Plus](https://github.com/StephenSHorton/STS2Plus) / [YuWanCard](https://github.com/YuWan886/Sts2-YuWanCard)

---

## 0. 快速入门：从零到模组运行

### 环境准备

1. **安装 .NET 9.0 SDK** — `dotnet-sdk-9.0`（与游戏 `global.json` 一致）
2. **安装 C# IDE** — Rider / VS Code / Visual Studio
3. **下载 Megadot** — [megadot.megacrit.com](https://megadot.megacrit.com)，STS2 专用 Godot 分支，不可用官方 Godot
4. **安装模组模板**：
   ```bash
   dotnet new install Alchyr.Sts2.Templates
   ```

### 创建项目

Rider: `File → New Solution`，选择模板：

| 模板 | 用途 |
|------|------|
| Slay the Spire 2 Mod | 空白模组 |
| Slay the Spire 2 Content | 内容模组（卡牌/遗物/药水等） |
| Slay the Spire 2 Character | 角色模组 |

> 项目名不能含空格、必须是 `.sln` 格式、勾选「Put solution and project in same directory」。

### 构建与发布

| 操作 | 功能 | 使用场景 |
|------|------|---------|
| **Build** | 仅编译 `.dll` → 复制到 mods | 代码改动 |
| **Publish** | 编译 + 生成 `.pck` + 复制全部文件 | 非代码改动（本地化/图片/场景） |

> Publish 需要 `Directory.Build.props` 正确配置 Godot 路径。日常开发优先 Build（快）。

### 模组文件结构

模组由至多三个文件构成（三者同名、同目录置于 `mods/` 下）：

| 文件 | 必需 | 说明 |
|------|------|------|
| `ModName.json` | ✅ | 清单 |
| `ModName.dll` | 有代码时 | 编译的 C# 程序集 |
| `ModName.pck` | 有资源时 | Godot 资源包 |


## 1. 环境与项目

### 关键依赖

- **.NET 9.0 SDK** — 与游戏 global.json 一致
- **[Megadot](https://megadot.megacrit.com)** — STS2 自研 Godot 分支，不可用官方 Godot
- `0Harmony.dll` — 游戏自带，运行时 IL 补丁框架
- **[BaseLib](https://github.com/Alchyr/BaseLib-StS2)** (NuGet，**可选**) — 社区 STS2 Mod 框架，提供 `CustomCardModel`、ModConfig 设置面板、图像路径工具等便利封装
- **Krafs.Publicizer** (NuGet) — 可选，访问游戏私有/受保护成员
- **BSchneppe.StS2.PckPacker** (NuGet) — 可选，构建时自动生成 .pck
- **Alchyr.Sts2.ModAnalyzers** (NuGet) — 可选，代码分析

### BaseLib：用还是不用？

| | 用 BaseLib | 纯自研 |
|---|---|---|
| **上手速度** | 快，有封装好的 CustomModel 系列 | 慢，需自己读源码、手写注册逻辑 |
| **模组依赖** | 用户必须同时装 BaseLib | 无外部依赖 |
| **更新风险** | 游戏更新后等 BaseLib 更新 | 补丁直接打在游戏上，自己维护即可 |
| **扩展性** | 受限于框架提供的接口 | 完全自由，想怎么改怎么改 |
| **ID 冲突** | 自动加命名空间前缀 | 需自己注意类名唯一性 |
| **设置面板** | `ConfigSlider`/`ConfigTickbox` 声明即用 | 需自己写 Godot UI |

**结论**：新手或快速原型用 BaseLib；成品 Mod 追求零依赖、深度定制用自研。两者也可以混用——用 BaseLib 的 ModConfig 面板，卡牌/遗物自己手动实现。本指南两种方式都会给出代码。

### .csproj（两种方案）

**方案 A：Godot.NET.Sdk（推荐，适配 Megadot 项目）**

```xml
<Project Sdk="Godot.NET.Sdk/4.5.1">
  <Import Project=".\Sts2PathDiscovery.props"/>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  <ItemGroup Condition="Exists('$(Sts2DataDir)')">
    <Reference Include="0Harmony"><HintPath>$(Sts2DataDir)/0Harmony.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="sts2"><HintPath>$(Sts2DataDir)/sts2.dll</HintPath><Private>false</Private></Reference>
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Alchyr.Sts2.BaseLib" Version="*" PrivateAssets="All"/>
    <PackageReference Include="Krafs.Publicizer" Version="2.3.0"/>
  </ItemGroup>
</Project>
```

`Sts2PathDiscovery.props` 定义游戏路径。支持自动检测 Steam 安装路径（Windows 注册表 / Linux / macOS），也可用 `local.props` 覆盖（gitignore）：

```xml
<!-- local.props -->
<Project><PropertyGroup>
  <Sts2Path>D:\Steam\steamapps\common\Slay the Spire 2</Sts2Path>
</PropertyGroup></Project>
```

```xml
<Project>
  <PropertyGroup>
    <Sts2DataDir>C:\path\to\Slay the Spire 2\data_sts2_windows_x86_64</Sts2DataDir>
    <ModsPath>C:\path\to\Slay the Spire 2\Mods\</ModsPath>
    <GodotPath>C:\path\to\Megadot\megadot.exe</GodotPath>
  </PropertyGroup>
</Project>
```

**方案 B：Microsoft.NET.Sdk（不依赖 Godot 项目）**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <OutputType>Library</OutputType>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="sts2"><HintPath>libs\sts2.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="0Harmony"><HintPath>libs\0Harmony.dll</HintPath><Private>false</Private></Reference>
  </ItemGroup>
</Project>
```

方案 A 适合在 Megadot 中开发的 Godot 项目，自动处理 PCK 导出、Mods 目录复制。方案 B 适合纯 DLL Mod（如 STS2Plus），不依赖 Godot 编辑器。

### Godot 项目

1. Megadot 新建项目，项目名 = 模组 ID
2. 菜单：项目 → 工具 → C# → 创建 C# 解决方案
3. 在 VS/Rider 中打开 `.sln`

### Godot 脚本修复

模组中带 C# 脚本的 Godot 场景无法被游戏检索到，初始化中加：

```csharp
Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
```

### mod_manifest.json（完整格式）

```json
{
  "id": "MyMod",
  "name": "My Mod",
  "author": "you",
  "description": "A test mod.",
  "version": "v0.1.0",
  "has_dll": true,
  "has_pck": false,
  "min_game_version": "0.105.0",
  "dependencies": [
    { "id": "BaseLib", "min_version": "3.1.2" }
  ],
  "affects_gameplay": true
}
```

- `id`：唯一标识，不可修改。推荐格式：作者_模组名
- `has_dll`：是否有代码程序集。纯材质/美化包可设为 false
- `has_pck`：是否有资源包
- `min_game_version`：最低游戏版本要求（0.105+ 新增）
- `dependencies`：前置模组及其最低版本
- `affects_gameplay`：影响玩法则 true（联机校验）；纯美化/信息类 false

### 模组入口 — 四阶段初始化

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    static readonly Harmony harmony = new("mymod");
    public static void Initialize()
    {
        harmony.PatchAll();                                     // Phase 1: Harmony 补丁
        ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly());  // Phase 2: 内容注册
        ConfigRegistrar.Register(new MyModConfig());             // Phase 3: 配置
        PreloadAssets();                                         // Phase 4: 资源预加载
    }

    static void PreloadAssets()
    {
        foreach (var path in new[] { "res://MyMod/images/icon.png" })
            if (ResourceLoader.Exists(path))
                ResourceLoader.Load<Texture2D>(path);
    }
}
```

### 调试

Steam 版无控制台输出，用 Megadot 运行项目调试。在 Megadot 编辑器可执行文件目录下创建 `mods/` 文件夹，放入模组文件即可加载。

Harmony 报错 `.NET 版本不兼容` 时，编辑 Megadot 目录下的 `GodotSharp/Api/Debug/GodotPlugins.runtimeconfig.json`，强制 .NET 9.0：

```json
{
  "runtimeOptions": {
    "framework": { "name": "Microsoft.NETCore.App", "version": "9.0.0" }
  }
}
```

### BaseLib ModConfig — 模组设置 UI

BaseLib 提供开箱即用的设置面板，声明属性即生成 UI 控件：

```csharp
public class MyModConfig : SimpleModConfig
{
    [ConfigSlider("Speed", 1, 10, Step = 0.5f)]  // 滑块
    public float Speed { get; set; } = 2f;

    [ConfigTickbox("Enable Feature")]              // 勾选框
    public bool FeatureEnabled { get; set; } = true;

    [ConfigDropdown("Mode", ["A", "B", "C"])]     // 下拉菜单
    public string Mode { get; set; } = "A";
}

// 注册（Initialize 中）
ModConfigRegistry.Register(new MyModConfig());
```

支持的控件：`ConfigSlider`、`ConfigTickbox`、`ConfigDropdown`、`ConfigColorPicker`、`ConfigLineEdit`、`ConfigButton`、`ConfigCollapsibleSection`。

---

## 2. Harmony 补丁

### 固定目标

```csharp
[HarmonyPatch(typeof(T), nameof(T.Method))]
static void Postfix(ref int __result) => __result = 99;
```

### 动态目标

```csharp
[HarmonyPatch]
static IEnumerable<MethodBase> TargetMethods()
{
    foreach (var t in typeof(BaseClass).Assembly.GetTypes())
    {
        if (t.IsAbstract || !typeof(BaseClass).IsAssignableFrom(t)) continue;
        var g = AccessTools.PropertyGetter(t, "Prop");
        if (g?.DeclaringType == t) yield return g;
    }
}
```

### 替换方法体

```csharp
[HarmonyPatch(typeof(T), "Method")]
static bool Prefix(ref int __result) { __result = 42; return false; }
```

### ModPatcher 错误隔离（YuWanCard 模式）

每个补丁独立 try-catch，平台缺失方法不崩全局：

```csharp
var patcher = new ModPatcher(ModId);

// 批量打补丁（每个类型独立 try-catch）
patcher.PatchAllSafe(Assembly.GetExecutingAssembly(), excludeTypes);

// 单个补丁隔离
patcher.ApplySingle(h => SomePatch.Apply(h), "PatchName");

// 平台条件补丁
if (!IsMobilePlatform())
    patcher.ApplySingle(h => DesktopOnlyPatch.Apply(h), "DesktopOnly");

static bool IsMobilePlatform()
{
    try { return Godot.OS.GetName() is "Android" or "iOS"; }
    catch { return false; }
}
```

---

## 3. 自定义卡牌

### 卡牌基类 — CardModel 构造函数

```csharp
CardModel(int cost, CardType type, CardRarity rarity, TargetType target, bool showInLibrary = true)
```

| 参数 | 说明 |
|------|------|
| `cost` | 基础能耗，赋值给 `CanonicalEnergyCost`，最终用于 `EnergyCost` |
| `type` | 卡牌类型（Attack/Skill/Power/Status/Curse），决定肖像框样式 |
| `rarity` | 稀有度（Common/Uncommon/Rare 可随机生成；Event/Ancient/Token/Status/Curse/Quest 不会进随机池） |
| `target` | 目标类型（Opponent/Ally/Self/AllOpponents/Everyone/TargetedNoCreature/Osty 等） |
| `showInLibrary` | 是否在图鉴中显示 |

### 卡牌关键词（CardKeyword）

覆盖 `CanonicalKeywords` 设置卡牌规则标识：

```csharp
public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [CardKeyword.Exhaust, CardKeyword.Ethereal, CardKeyword.Retain, CardKeyword.Innate];
```

常用关键词：`Exhaust`（消耗）、`Ethereal`（虚无）、`Retain`（保留）、`Innate`（固有）、`Unplayable`（不可打出）。

### 卡牌标记（CardTag） 

覆盖 `CanonicalTags` 设置类别标签，影响联动（如「完美打击」会统计所有标记为 Strike 的卡牌）：

```csharp
public override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
```

### 打出条件

```csharp
// 条件不够时灰色不可打出
public override bool IsPlayable => Owner?.Gold >= 100;

// 条件满足时发出金光提示
public override bool ShouldGlowGoldInternal => Owner?.Gold >= 100;
```

### 回调钩子

```csharp
// 回合结束还在手牌时触发
public override async Task OnTurnEndInHand(PlayerChoiceContext ctx)
{
    await PlayerCmd.Damage(Owner, 3);
}
```

### BaseLib CustomCardModel（推荐）

`Alchyr.Sts2.BaseLib` NuGet 包提供了 `CustomCardModel`，构造函数 `autoAdd: true` 自动注册，无需 `ContentRegistry`。

```csharp
public class MyCard : CustomCardModel
{
    public MyCard() : base(cost: 2, CardType.Skill, CardRarity.Rare, TargetType.Self) {}

    // 图像路径（自动拼接 res://ModId/images/...）
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(6).FromCard(this).Targeting(ctx.Targets.First()).Execute();
    }
}
```

### 手动模式（不依赖 BaseLib）

```csharp
public abstract class MyCardBase : CardModel
{
    readonly List<CardKeyword> _keywords = []; int? _costUpgrade;
    protected MyCardBase(int cost, CardType type, CardRarity rarity, TargetType tgt) : base(cost, type, rarity, tgt) {}
    protected sealed override IEnumerable<DynamicVar> CanonicalVars => [];
    public sealed override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;
    protected sealed override HashSet<CardTag> CanonicalTags => [];
    protected override void OnUpgrade() { if (_costUpgrade.HasValue) EnergyCost.UpgradeBy(_costUpgrade.Value); }
    protected void WithKeywords(params CardKeyword[] k) => _keywords.AddRange(k);
    protected void WithCostUpgradeBy(int a) => _costUpgrade = a;
}
```

### 卡牌实现

```csharp
[Pool(typeof(ColorlessCardPool))]
public class MyCard : MyCardBase
{
    public MyCard() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { WithKeywords(CardKeyword.Exhaust); WithCostUpgradeBy(-1); }
    public override string PortraitPath => "res://MyMod/cards/my_card.png";
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // ctx.Targets 获取选择的目标
        // play 获取打出状态（耗费资源、所在牌堆等）
    }
}
```

### 伤害链

```csharp
DamageCmd.Attack(damage)
    .FromCard(this)                      // 伤害来源
    .Targeting(target)                   // 目标 Creature
    .WithHitCount(count)                 // 连击次数
    .WithHitFx("res://animations/vfx_slash.tscn", sfx: null, tmpSfx: null)  // 特效/音效
    .Execute();                          // 执行（必须 await）
```

`FromMonster` 用于怪物伤害；`TargetingAllOpponents(combatState)` 打全体；`TargetingRandomOpponents(combatState, allowRepeats)` 随机目标。

### 抽牌与选牌

```csharp
CardPileCmd.Draw(owner, 2);                                    // 抽 2 张
CardSelectCmd.FromHand(ctx, owner, prefs, filter, source);     // 从手牌选→消耗
CardSelectCmd.FromHandForUpgrade(ctx, owner, prefs, filter);   // 选牌升级（需手动调 UpgradeInternal）
CardSelectCmd.FromHandForDiscard(ctx, owner, prefs, filter);   // 选牌丢弃（需手动调 CardCmd.Discard）
```

`CardSelectorPrefs` 构造函数：
- `CardSelectorPrefs(LocString prompt, int selectCount)` — 固定选牌数
- `CardSelectorPrefs(LocString prompt, int minCount, int maxCount)` — 范围选牌

prompt 常用取值（原版）：`ChooseCardPrompt.Discard`、`ChooseCardPrompt.Exhaust`、`ChooseCardPrompt.Upgrade` 等。

### 升级回调

```csharp
protected override void OnUpgrade()
{
    base.OnUpgrade();  // 先调基类
    DynamicVars.Damage.UpgradeValueBy(3);
}
```

### 图标与本地化

| 类型 | 路径 |
|------|------|
| 裁切纹理 | `images/atlases/card_atlas.sprites/<卡池>/<小写ID>.tres` |
| 大图 | `images/packed/card_portraits/<卡池>/<小写ID>.png` (推荐 1000×760) |
| 本地化 | `<modid>/localization/<lang>/cards.json` |

卡池名称对应 `CardPoolModel.Title` 属性。`{Dmg:diff()}` 显示带强化差异的动态变量值。

### 自动注册

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class PoolAttribute(Type poolType) : Attribute { public Type PoolType => poolType; }

public static class ContentRegistry
{
    public static void RegisterAll(Assembly a)
    {
        foreach (var t in GetTypes(a))
        { if (t.IsAbstract) continue; var p = t.GetCustomAttribute<PoolAttribute>();
          if (p != null) ModHelper.AddModelToPool(p.PoolType, t); }
    }
    static Type[] GetTypes(Assembly a) {
        try { return a.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).Cast<Type>().ToArray(); }
    }
}
```

---

## 4. 自定义遗物

### 稀有度与获取方式

`Rarity` 类型为 `Rarity` 枚举（并非独立枚举），取值影响获取途径：

| 稀有度 | 获取方式 |
|--------|---------|
| `Starter` | 仅初始遗物/事件，不可交易 |
| `Event` | 仅问号事件，不可交易 |
| `Ancient` | 仅先古之民事件 |
| `Common` / `Uncommon` / `Rare` | 精英奖励、商店、宝箱 |
| `Shop` | 仅商店 |

### 动态变量（DynamicVar）

所有 `AbstractModel` 通过 `CanonicalVars` 声明动态变量，用于本地化文本中的数值显示（`{Energy:amount}` 等），数值改变时 UI 同步更新：

```csharp
protected sealed override IEnumerable<DynamicVar> CanonicalVars => [DynamicVars.Energy.WithBase(1)];
// 使用: DynamicVars.Energy.BaseValue
// 修改: DynamicVars.Energy.BaseValue = 3;
```

常用 DynamicVar：`Energy`、`Gold`、`Damage`、`Block`、`HitCount`、`Heal`、`Draw`。完整列表查看游戏源码 `DynamicVarSet.cs`。

### 基础遗物 Hook

```csharp
public override async Task AfterSideTurnStart(Side side, CombatState combatState)
{
    // side 值: Side.Player / Side.Opponent
    if (side == Owner.Creature.Side)
    {
        await Flash();  // 闪烁提示
        await PlayerCmd.GainEnergy(Owner, DynamicVars.Energy.BaseValue);
    }
}
```

### BaseLib CustomRelicModel（推荐）

```csharp
public class MyRelic : CustomRelicModel
{
    public MyRelic() : base(autoAdd: true) {}  // 自动注册

    protected override Rarity Rarity => Rarity.Starter;
    protected sealed override IEnumerable<DynamicVar> CanonicalVars => [DynamicVars.Energy.WithBase(1)];

    public override async Task AfterSideTurnStart(Side side, CombatState combatState)
    { if (side == Owner.Creature.Side) { await Flash(); await PlayerCmd.GainEnergy(Owner, DynamicVars.Energy.BaseValue); } }
}
```

### 手动模式

```csharp
internal sealed class MyRelic : RelicModel
{
    protected override Rarity Rarity => Rarity.Starter;
    // ...
}
// + 手动 ModHelper.AddModelToPool(...)
```

### 遗物池

| 池 | 获取方式 |
|----|---------|
| `IroncladRelicPool` 等角色池 | 精英/商店（并入玩家随机遗物袋） |
| `SharedRelicPool` | 精英/商店/宝箱（两路：遗物袋+宝箱袋） |
| `EventRelicPool` | 仅问号事件 |
| `FallbackRelicPool` | 兜底（基本只有 Circlet） |
| `DeprecatedRelicPool` | 废弃/存档兼容，不参与获取 |

### 修改初始遗物

```csharp
[HarmonyPatch(typeof(Ironclad), "StartingRelics", MethodType.Getter)]
static void Postfix(ref IReadOnlyList<RelicModel> __result)
{ var list = __result.ToList(); list.Add(ModelDb.GetByIdOrNull<RelicModel>(ModelDb.GetId(typeof(MyRelic)))); __result = list; }
```

### 图标与本地化

| 类型 | 路径 |
|------|------|
| 大图 | `images/relics/<小写ID>.png` (256×256) |
| 剪影 | `images/relics/<小写ID>_outline.png` |
| 裁切 | `images/atlases/relic_atlas.sprites/<小写ID>.tres` |
| 剪影裁切 | `images/atlases/relic_outline_atlas.sprites/<小写ID>.tres` |
| 本地化 | `<modid>/localization/<lang>/relics.json` |

> **图标原理**：游戏优先找裁切纹理（AtlasTexture），找不到才回退到大图。每个遗物图标资源需要手动创建 `.tres` 文件，引用对应 PNG 并设置 Region 大小。

---

## 5. 自定义药水

### 稀有度

`PotionRarity` 枚举：`Common` / `Uncommon` / `Rare`，决定能否在药水池被随机获取。

### 使用时机

`PotionUsage` 枚举：
- `CombatOnly`：仅战斗中使用
- `AnyTime`：任意场景使用（如鲜血药水/混沌药水）
- `ProgrammaticOnly`：仅代码触发，玩家不能主动用（如瓶中精灵）

### 三种模式

```csharp
// 伤害药水
public override async Task OnUse(PlayerChoiceContext ctx, PotionTarget target)
{ await DamageCmd.Attack(30).FromCard(this).TargetingAllOpponents(combatState).Execute(); }

// 效果药水（自损换 Buff）
public override async Task OnUse(PlayerChoiceContext ctx, PotionTarget target)
{ await PlayerCmd.Damage(Owner, 10); await PlayerCmd.ApplyPower(Owner, PowerDb.Get<IntangiblePower>(), 2); }

// 自动消耗药水（复活）
public override PotionUsage Usage => PotionUsage.ProgrammaticOnly;
public override bool CanBeGeneratedInCombat => false;  // 不会随机生成（如炼制药水卡牌）
public override async Task<bool> ShouldDie(Creature creature)
{ return creature != Owner.Creature || !IsUsable; }
public override async Task AfterPreventingDeath(Creature creature)
{ await Consume(); await PlayerCmd.Heal(Owner, 10); }
```

### 药水池

```csharp
ModHelper.AddModelToPool(typeof(SharedPotionPool), typeof(MyPotion));
```

游戏本体药水池：`IroncladPotionPool`、`SilentPotionPool`、`DefectPotionPool`、`NecrobinderPotionPool`、`RegentPotionPool`、`SharedPotionPool`。

### 图标与本地化

| 类型 | 路径 |
|------|------|
| 大图 | `images/potions/<小写ID>.png` |
| 裁切 | `images/atlases/potion_atlas.sprites/<小写ID>.tres` |
| 描边裁切 | `images/atlases/potion_outline_atlas.sprites/<小写ID>.tres` |
| 本地化 | `<modid>/localization/<lang>/potions.json` |

---

## 6. 自定义能力 (Buff/Debuff)

### 核心属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Type` | `PowerType` | `Buff`（增益）/ `Debuff`（减益）/ `Neutral`（中性） |
| `StackType` | `PowerStackType` | `Intensity`（有层数，如力量3层）/ `Duration`（只存在/不存在，如壁垒） |
| `IsInstanced` | `bool` | `true`：每次施加建独立实例；`false`（默认）：只有1个实例、叠加数值 |
| `AllowNegative` | `bool` | 层数是否允许为负（如力量/敏捷可以负数）。`false` 时层数为 0 自动移除 |

### BaseLib CustomPowerModel（推荐）

```csharp
public class MyPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "powers/power.png".ImagePath();      // 64×64
    public override string? CustomBigIconPath => "powers/big/power.png".ImagePath();      // 256×256

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Intensity;

    public override async Task AfterCardPlayed(CardModel card)
    { await PlayerCmd.GainBlock(Owner, Amount); }
}
```

### 手动模式

```csharp
internal sealed class MyPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Intensity;
    public override bool AllowNegative => false;
    // ...
}
```

### 图标与本地化

| 类型 | 路径 |
|------|------|
| 战斗图标裁切 | `images/atlases/power_atlas.sprites/<小写ID>.tres` |
| 大图 | `images/powers/<小写ID>.png` (推荐 256×256) |
| 本地化 | `<modid>/localization/<lang>/powers.json` |

`smartDescription` 用于带动态变量的描述；`description` 用于普通文本。

### 调试

控制台命令（按 `` ` `` 打开）：

```
power apply <target> <power_type>
```
单人游戏中 target=0 表示玩家角色。

---

## 7. 自定义附魔

### 附魔加成

```csharp
internal sealed class MyEnchant : EnchantmentModel
{
    // 伤害加成
    public override decimal EnchantDamageAdditive => Amount * 3;
    // 格挡加成
    public override decimal EnchantBlockAdditive => Amount * 2;

    // 附魔时修改卡牌
    public override void ModifyCard()
    { Card.WithKeywords(); Card.Keywords.Remove(CardKeyword.Exhaust); }
}

// 给卡牌附魔
await CardCmd.Enchant(card, new MyEnchant { Amount = 2 });
```

### 控制台测试

```
enchant hand <index> <enchant_type>
```

### 图标与本地化

| 路径 | 说明 |
|------|------|
| `images/enchantments/<小写ID>.png` | 图标 |
| `<modid>/localization/<lang>/enchantments.json` | 本地化 |

---

## 8. 自定义事件

### 基础事件

```csharp
internal sealed class MyEvent : EventModel
{
    public override IEnumerable<EventOption> GenerateInitialOptions(PlayerChoiceContext ctx)
    {
        return [
            new EventOption(
                ctx,
                InitialOptionKey("OPT_1"),                    // 本地化键：<事件ID>.pages.INITIAL.options.OPT_1
                async () => { await GiveCard(typeof(MyCard)); SetEventFinished(L10NLookup("pages.RECHARGE.description")); }
            ),
            new EventOption(ctx, InitialOptionKey("OPT_2"),
                async () => { await PlayerCmd.GainGold(Owner!, 50); SetEventFinished(L10NLookup("pages.GOLD.description")); }
            )
        ];
    }
}
```

`EventOption` 可选的第四个参数指定一个 `RelicModel`，让选项显示遗物图标。

### 多页选项

通过 `SetEventState` 切换页面：

```csharp
async Task NextPage(PlayerChoiceContext ctx) {
    SetEventState([
        new EventOption(ctx, "PAGE2_OPT1", async () => { /* ... */ SetEventFinished(...); }),
        new EventOption(ctx, "PAGE2_OPT2", async () => { /* ... */ SetEventFinished(...); })
    ]);
}
```

### 添加到地图

```csharp
[HarmonyPatch(typeof(Overgrowth), "AllEvents", MethodType.Getter)]
static void Postfix(ref IReadOnlyList<EventModel> __result)
{ var list = __result.ToList(); list.Add(ModelDb.GetByIdOrNull<EventModel>(ModelDb.GetId(typeof(MyEvent)))); __result = list; }
```

### 先古之民事件（AncientEventModel）

先古之民出现在每一章开头，继承 `AncientEventModel`：

```csharp
internal sealed class MyAncient : AncientEventModel
{
    // 定义对话配置
    protected override AncientDialogueSet DefineDialogues()
    {
        return new AncientDialogueSet(
            [
                new AncientDialogue("event:/sfx/ancients/ancient_greet", // 第一段对话音效
                    "talk.firstVisitEver.0-0",   // 本地化键
                    AncientDialogueType.Ancient),
                new AncientDialogue(null,          // 无音效
                    "talk.firstVisitEver.0-1",
                    AncientDialogueType.Character),
            ],
            [InitialOptionKey("OPT_1")]
        );
    }

    // 所有可能的选项
    protected override IReadOnlyList<EventOption> AllPossibleOptions(PlayerChoiceContext ctx)
    {
        return [
            new EventOption(ctx, InitialOptionKey("OPT_1"),
                async () => { await GiveCard(typeof(MyCard)); SetEventFinished(L10NLookup("pages.DONE.description")); }
            )
        ];
    }
}
```

### 先古之民对话文本键格式

```
<先古ID>.title                    → 显示名
<先古ID>.epithet                  → 称号
<先古ID>.talk.firstVisitEver.<对话组>-<行索引> → 初见对话
<先古ID>.talk.<角色ID>.<对话组>-<行索引> → 与特定角色的特殊对话
<先古ID>.talk.ANY.<对话组>-<行索引>r → 通用对话
```

对话类型：`ancient`（先古发言）、`next`（选项分支）、`char`（玩家发言）。

### 先古之民资源（缺一不可）

| 资源 | 路径 |
|------|------|
| 事件背景图 | `images/events/<小写ID>.png` (3440×1613) |
| 地图图标 | `images/packed/map/ancients/ancient_node_<小写ID>.png` |
| 地图描边 | `images/packed/map/ancients/ancient_node_<小写ID>_outline.png` |
| 历史对话图标 | `images/ui/run_history/<小写ID>.png` |
| 历史对话描边 | `images/ui/run_history/<小写ID>_outline.png` |
| 背景场景 | `scenes/events/background_scenes/<小写ID>.tscn` |

### 事件本地化

格式：`<事件ID>.<目标类型>.<项ID>.<键>`

```json
{
  "MY_EVENT.title": "事件标题",
  "MY_EVENT.pages.INITIAL.description": "初始页面描述",
  "MY_EVENT.pages.INITIAL.options.OPT_1.title": "选项1标题",
  "MY_EVENT.pages.INITIAL.options.OPT_1.description": "选项1描述",
  "MY_EVENT.pages.RECHARGE.description": "选择后的文本"
}
```

### 事件与先古背景图

| 类型 | 路径 |
|------|------|
| 普通事件背景 | `images/events/<小写ID>.png` (3440×1613) |
| 先古事件背景 | 同上 |

---

## 9. 自定义怪物

### 怪物类与状态机

怪物核心是 `MonsterMoveStateMachine`，由 `GenerateMoveStateMachine()` 生成。状态机 = 状态节点列表 + 初始状态 ID。

### MoveState 构造

```csharp
MoveState(string stateId, Func<PlayerChoiceContext, Task> onPerform, List<Intent> intents)
```

| 参数 | 说明 |
|------|------|
| `stateId` | 状态唯一 ID |
| `onPerform` | 进入该状态时执行的异步回调 |
| `intents` | 显示的意图列表（`AttackIntent(dmg)` / `BuffIntent` / `DefendIntent` / `DebuffIntent` 等） |

```csharp
internal sealed class MyMonster : MonsterModel
{
    public override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var attackState = new MoveState("ATTACK",
            async (ctx) => { await DamageCmd.Attack(6).FromMonster(this).Targeting(ctx.GetPlayer()).Execute(); },
            [new AttackIntent(6)]);
        attackState.FollowUpState = attackState;               // 循环自身
        return new MonsterMoveStateMachine([attackState], "ATTACK");
    }
}
```

### 多状态循环

```csharp
var attack = new MoveState("ATTACK", AttackAction, [new AttackIntent(8)]);
var buff = new MoveState("BUFF", BuffAction, [new BuffIntent()]);
attack.FollowUpState = buff;
buff.FollowUpState = attack;
return new MonsterMoveStateMachine([attack, buff], "ATTACK");
```

### 随机分支（RandomBranchState）

```csharp
var randBranch = new RandomBranchState("RANDOM");
randBranch.AddBranch(attackState, cooldown: 0, repeat: RepeatType.NoConsecutive, weight: 2);
randBranch.AddBranch(defendState, cooldown: 2, repeat: new MaxRepeats(3), weight: 1);
```

- `cooldown`：选中后冷却回合数
- `repeat`：`RepeatType.NoRestriction` / `RepeatType.NoConsecutive`（不能连续）/ `RepeatType.NoRepeat`（不可重复）/ `MaxRepeats(n)`
- `weight`：权重，越大越容易被选中

### 条件分支

```csharp
var condBranch = new ConditionalBranchState("COND");
condBranch.AddBranch(bigAttack, ctx => ctx.GetPlayer().Hp <= 10);  // 条件 true 走此分支
condBranch.AddBranch(normalAttack, ctx => true);                     // 兜底
```

### 遭遇类与添加

```csharp
// 遭遇定义
internal sealed class MyEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;  // Monster/Elite/Boss
    public override IEnumerable<(MonsterModel, string?)> GenerateMonsters()
    { return [(ModelDb.GetByIdOrNull<MonsterModel>(ModelDb.GetId(typeof(MyMonster))).ToMutable(), null)]; }
}

// 添加到地图
[HarmonyPatch(typeof(Overgrowth), "GenerateAllEncounters")]
static void Postfix(ref IEnumerable<EncounterModel> __result)
{ __result = __result.Append(ModelDb.GetByIdOrNull<EncounterModel>(ModelDb.GetId(typeof(MyEncounter)))); }
```

### 怪物资源

| 资源 | 路径 |
|------|------|
| 动画场景 | `scenes/creature_visuals/<小写ID>.tscn` |
| 遭遇站位场景 | `scenes/encounters/<小写ID>.tscn`（Marker2D 节点名=槽位名） |
| 攻击音效 | `event:/sfx/enemy/enemy_attacks/<小写ID>/<小写ID>_attack` |
| 施法音效 | `event:/sfx/enemy/enemy_attacks/<小写ID>/<小写ID>_cast` |
| 死亡音效 | `event:/sfx/enemy/enemy_attacks/<小写ID>/<小写ID>_die` |
| 本地化 | `<modid>/localization/<lang>/monsters.json` |
| 遭遇本地化 | `<modid>/localization/<lang>/encounters.json` |

> 怪物场景结构：根节点必须是 `NCreatureVisuals` 类型。`%Bounds` 子节点定义选择框大小、血条长度、意图图标位置。

### 调试

控制台命令：

```
encounter <encounter_id>  → 触发某场遭遇
```

---

## 10. 自定义角色（完整流程）

### 角色需要定义的内容

1. 角色卡池（`CardPoolModel`）
2. 角色遗物池（`RelicPoolModel`）
3. 角色药水池（`PotionPoolModel`）
4. 角色类（`CharacterModel`）
5. 大量场景/贴图资源

### 自定义角色卡池（CardPoolModel）

```csharp
internal sealed class MyCardPool : CardPoolModel
{
    public override string Title => "mypool";                    // 卡池名，影响卡面图资源路径
    public override string EnergyColorName => "mypool";          // 能量图标名
    public override Color DeckEntryCardColor => Colors.Purple;   // 缩略卡底色
    public override Color EnergyOutlineColor => Colors.Purple;   // 能量数字描边色
    public override string CardFrameMaterialPath => "card_frame_purple";  // 边框材质路径
    public override bool IsColorless => false;

    public override IReadOnlyList<CardModel> GenerateAllCards() => [/* 卡牌列表 */];
}
```

| 属性 | 说明 |
|------|------|
| `Title` | 决定 `card_atlas.sprites/<Title>/` 路径 |
| `EnergyColorName` | 决定 `ui_atlas.sprites/card/energy_<Name>.tres` 路径 |
| `CardFrameMaterialPath` | 决定 `materials/cards/frames/<Path>_mat.tres` 路径（HSV 着色器） |
| `EnergyOutlineColor` | 卡牌左上角能量数字描边颜色 |
| `DeckEntryCardColor` | 打出后缩略卡牌底色 |

能量图标缩略图（富文本中）需要：`images/packed/sprite_fonts/<EnergyColorName>_energy_icon.png`

### 卡池边框着色器（HSV 偏移）

```gdshader
shader_type canvas_item;
uniform float h = 0.0;
uniform float s = 0.0;
uniform float v = 0.0;

void fragment() {
    vec4 c = texture(TEXTURE, UV);
    vec3 hsv = rgb2hsv(c.rgb);
    hsv.x = mod(hsv.x + h, 1.0);
    hsv.y = clamp(hsv.y + s, 0.0, 1.0);
    hsv.z = clamp(hsv.z + v, 0.0, 1.0);
    c.rgb = hsv2rgb(hsv);
    COLOR = c;
}
```

### 自定义遗物池与药水池

```csharp
internal sealed class MyRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "mypool";
    public override Color LabOutlineColor => Colors.Purple;
    public override IReadOnlyList<RelicModel> GenerateAllRelics() => [/* 遗物列表 */];
}

internal sealed class MyPotionPool : PotionPoolModel
{
    public override string EnergyColorName => "mypool";
    public override IReadOnlyList<PotionModel> GenerateAllPotions() => [/* 药水列表 */];
}
```

### 角色类骨架

```csharp
internal sealed class MyChar : CharacterModel
{
    public override CharacterGender Gender => CharacterGender.Female;
    public override CharacterModel? UnlocksAfterRunAs => null;  // null=无需解锁

    public override CardPoolModel CardPool => ModelDb.GetByIdOrNull<CardPoolModel>(ModelDb.GetId(typeof(MyCardPool)));
    public override RelicPoolModel RelicPool => ModelDb.GetByIdOrNull<RelicPoolModel>(ModelDb.GetId(typeof(MyRelicPool)));
    public override PotionPoolModel PotionPool => ModelDb.GetByIdOrNull<PotionPoolModel>(ModelDb.GetId(typeof(MyPotionPool)));

    public override IReadOnlyList<CardModel> StartingDeck => [/* 初始卡组 */];
    public override IReadOnlyList<RelicModel> StartingRelics => [/* 初始遗物 */];
}
```

`CharacterGender`：`Male` / `Female` / `Unknown`。

### 注册角色

```csharp
[HarmonyPatch(typeof(ModelDb), "AllCharacters", MethodType.Getter)]
static void Postfix(ref IReadOnlyList<CharacterModel> __result)
{ var list = __result.ToList(); list.Add(ModelDb.GetByIdOrNull<CharacterModel>(ModelDb.GetId(typeof(MyChar)))); __result = list; }
```

同样方法注册自定义卡池到 `ModelDb.AllCardPools`、遗物池到 `ModelDb.AllRelicPools`、药水池到 `ModelDb.AllPotionPools`。

### 角色完整资源清单

| 资源 | 路径 |
|------|------|
| 待机动画场景 | `scenes/creature_visuals/<小写ID>.tscn`（根节点 `NCreatureVisuals`） |
| 头像缩略图场景 | `scenes/ui/character_icons/<小写ID>_icon.tscn` |
| 能量计数器场景 | `scenes/combat/energy_counters/<小写ID>_energy_counter.tscn`（根节点 `NEnergyCounter`） |
| 商店动画场景 | `scenes/merchant/characters/<小写ID>_merchant.tscn` |
| 篝火场景 | `scenes/rest_site/characters/<小写ID>_rest_site.tscn` |
| 卡牌拖尾特效 | `scenes/vfx/card_trail_<小写ID>.tscn`（根节点 `NCardTrailVfx`） |
| 头像图标 | `images/ui/top_panel/character_icon_<小写ID>.png` |
| 头像描边 | `images/ui/top_panel/character_icon_<小写ID>_outline.png` |
| 选择背景场景 | `scenes/screens/char_select/char_select_bg_<小写ID>.tscn` |
| 选择界面底部图 | `images/packed/character_select/char_select_<小写ID>.png` |
| 选择界面锁定图 | `images/packed/character_select/char_select_<小写ID>_locked.png` |
| 地图标记箭头 | `images/packed/map/icons/map_marker_<小写ID>.png` |
| 联机手势贴图 | `images/ui/hands/multiplayer_hand_<小写ID>_point/rock/paper/scissors.png` |
| 过场着色器 | `materials/transitions/<小写ID>_transition_mat.tres` |

> **角色场景结构规则**：根节点必须是挂载 `NCreatureVisuals` 脚本的 Node2D；子节点 `%Visuals`（设唯一名称访问）；`%Bounds` 定义选择框。

> **能量计数器场景规则**：根节点必须是 `NEnergyCounter` 类型；内部 Label 必须是 `MegaLabel`（可继承占位或导入 `addons/mega_text` 插件）。

> **卡牌拖尾场景规则**：根节点必须是 `NCardTrailVfx` 类型；内含 `NCardTrail` 类型的 Line2D 节点。

### Godot 脚本检索修复（关键！）

模组程序集通过 `AssemblyLoadContext.LoadFromAssemblyPath` 加载，绕过了 Godot 的脚本映射。必须在初始化中调用扫描：

```csharp
typeof(Node).Assembly.GetType("GodotPlugins.Game")?.GetMethod("ScanAssembly", BindingFlags.NonPublic|BindingFlags.Static)
    ?.Invoke(null, [Assembly.GetExecutingAssembly()]);
```

### Spine 动画插件

Godot 不原生识别 Spine 格式，需要安装 Godot-Spine GDExtension 插件：

1. 在 Mod 根目录创建 `bin/` 文件夹
2. 下载合适版本的 Godot-Spine GDExtension 放在 `bin/` 下
3. 重启编辑器加载插件
4. JSON 格式的 Spine 文件需改后缀名为 `.spine-json` 才能被识别

---

## 11. 解包与逆向工程

### 反编译游戏源码

在 Rider 中：
- `Shift+Shift` 全局搜索（包括 STS2 源码和 BaseLib），如搜 `CloakAndDagger`
- `Ctrl+Click` 任意 STS2/BaseLib 类型跳转到反编译源码
- `Find Usages` 查看调用位置

### 外部反编译工具

- **ILSpy**（最可读，有 VS Code 扩展）
- **dnSpy**
- **dotPeek**（与 Rider 内置基本相同）

加载 `sts2.dll`（位于 `data_sts2_<platform>/`），搜索类型/方法。

### 反编译代码常见问题

| 反编译输出 | 应写成 |
|-----------|--------|
| `new DynamicVar("Damage", 0.75M)` | `new("Damage", 0.75)` |
| `new \u003C\u003Ez__ReadOnlySingleElementList<DynamicVar>(...)` | `[new("Damage", 0.75)]` |
| 展开的 getter + `[OriginalAttributes]` | `=>` 表达式体 |
| `HistoryCourse historyCourse = this;` 在方法开头 | async 编译器处理，正常 |
| `// ISSUE: reference to compiler-generated method` | lambda 反编译失败 |

### 提取游戏资源

用 **GDRE Tools**（[GDRETools/gdsdecomp](https://github.com/GDRETools/gdsdecomp)）：
1. 运行 → RE Tools → Recover Project
2. 打开 `SlayTheSpire2.pck`
3. 浏览资源：`localization/`（文本）、`src/Core/`（反编译源码）、各目录（图片/场景/音效）

---

## 12. UI 开发

```csharp
var layer = new CanvasLayer { Name = "MyLayer" };
((SceneTree)Engine.GetMainLoop()).Root.AddChild(layer);
bool blocked = tree.Paused || (NOverlayStack.Instance?.ScreenCount > 0);
```

---

## 13. 联机适配

```csharp
bool isClient = RunManager.Instance?.NetService?.Type == NetGameType.Client;
control.MouseFilter = isClient ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
```

---

## 14. 反射与工具模式

```csharp
AccessTools.Property(typeof(T), "P").GetValue(instance);
typeof(CardModel).Assembly.GetType("Namespace.Class");

// 防重复
static readonly HashSet<int> seen = [];
if (!seen.Add(RuntimeHelpers.GetHashCode(instance))) return;

// 弱引用
static readonly ConditionalWeakTable<CardModel, Data> extras = new();
extras.GetOrCreateValue(card);
```

---

## 15. 本地化

```json
{ "MY_CARD.title": "Title", "MY_CARD.description": "{Dmg:amount} damage." }
```

```csharp
new LocString("cards", "MY_CARD.title");
LocManager.Instance.GetTable("modifiers").MergeWith(dict);
```

BaseLib 会在本地化缺失时打印错误而非崩溃，方便调试。

各内容类型的本地化路径：

| 内容类型 | 本地化路径 |
|----------|-----------|
| 卡牌 | `<modid>/localization/<lang>/cards.json` |
| 遗物 | `<modid>/localization/<lang>/relics.json` |
| 药水 | `<modid>/localization/<lang>/potions.json` |
| 能力 | `<modid>/localization/<lang>/powers.json` |
| 附魔 | `<modid>/localization/<lang>/enchantments.json` |
| 事件 | `<modid>/localization/<lang>/events.json` |
| 先古 | `<modid>/localization/<lang>/ancients.json` |
| 怪物 | `<modid>/localization/<lang>/monsters.json` |
| 遭遇 | `<modid>/localization/<lang>/encounters.json` |
| 角色 | `<modid>/localization/<lang>/characters.json` |

---

## 16. 常见坑

| 坑 | 对策 |
|----|------|
| 图标不显示 | 只能用内置图标或规范路径；确保 `.tres` AtlasTexture 已创建并引用正确 PNG |
| 联机状态丢失 | 双路检查：本地 + `HasActiveModifier` |
| IL2CPP 崩溃 | 捕获 `ReflectionTypeLoadException` |
| 节点销毁后崩 | `_ExitTree` 取消事件订阅 |
| Steam 无日志 | Megadot 项目模式运行调试 |
| Harmony 报错 | 编辑 `GodotPlugins.runtimeconfig.json` 强制 .NET 9.0 |
| 模组场景找不到脚本 | `ScanAssembly` 修复 Godot 脚本映射（或 `LookupScriptsInAssembly`） |
| 本地化内容不生效 | Publish（非 Build），生成 .pck 才能打包非代码资源 |
| 自定义角色选择界不出现 | patch `ModelDb.AllCharacters` Getter，且需要 patch 对应 CardPool/RelicPool/PotionPool |
| Spine 动画不加载 | 安装 Godot-Spine GDExtension 插件，JSON 格式改 `.spine-json` 后缀 |
| BaseLib 报错 | 更新到最新版；Beta 分支可能暂时不兼容 |
| dotnet SDK 找不到 | 检查 `DOTNET_ROOT` 环境变量；Rider 切换 MSBuild 版本 |
| ModAnalyzers 本地化报错 | Alt+Enter → Generate localization，移到正确 json 文件 |

## 17. API 速查

| 类型 | 命名空间/路径 | 用途 |
|------|---------|------|
| `ModInitializer` | `Core.Modding` | 入口 |
| `ModHelper.AddModelToPool` | `Core.Modding` | 入池 |
| `CardModel` | `Core.Models` | 卡牌 |
| `RelicModel` | `Core.Models` | 遗物 |
| `PotionModel` | `Core.Models` | 药水 |
| `PowerModel` | `Core.Models` | 能力/Buff |
| `EnchantmentModel` | `Core.Models` | 附魔 |
| `EventModel` | `Core.Models` | 事件 |
| `AncientEventModel` | `Core.Models` | 先古之民 |
| `CharacterModel` | `Core.Models` | 角色 |
| `CardPoolModel` | `Core.Models` | 卡池 |
| `MonsterModel` | `Core.Models` | 怪物 |
| `EncounterModel` | `Core.Models` | 遭遇 |
| `DynamicVars` | `Core.Localization.DynamicVars` | 动态变量 |
| `PlayerCmd` | `Core.Commands` | 玩家操作（能量/HP/金币等） |
| `CardCmd` | `Core.Commands` | 卡牌操作（附魔/丢弃/升级） |
| `DamageCmd` | `Core.Commands` | 伤害链 |
| `CardPileCmd.Draw` | `Core.Commands` | 抽牌 |
| `CardSelectCmd` | `Core.Commands` | 选牌 |
| `RunManager` | `Core.Runs` | 运行状态 |
| `ModelDb` | `Core.Models` | 模型数据库 |
| `LocString/LocManager` | `Core.Localization` | 本地化 |
| `NCombatRoom` | `Core.Nodes.Rooms` | 战斗场景 |

## 18. BaseLib 进阶特性

### 自定义目标类型

`DynamicEnumValueMinter` 动态创建 `TargetType` 枚举值：

```csharp
static readonly DynamicEnumValueMinter<TargetType> Minter = new();
public static TargetType Everyone { get; } = Minter.Mint("MODID_TARGETTYPE_EVERYONE");
```

### 自定义音频

```csharp
var audio = ResourceLoader.Load<AudioStream>("res://ModId/sounds/effect.ogg");
```

### ITomeCard — 指定尘书卡牌

```csharp
public class MyTomeCard : CustomCardModel, ITomeCard { }
```

### MakeCalculated 快捷方式

```csharp
MakeCalculated<int>(name: "Bonus", baseVal: 0, calc: card => card.Owner.Strength);
```

### 其他特性

- **`[SavedProperty]`**：属性自动注册到游戏序列化系统
- **`SpireField<T>`**：为已有类添加额外字段
- **`AddedNode`**：为已有场景动态添加子节点
- **`ICustomUiModel`**：为卡牌/遗物/药水添加额外 UI
- **`DescriptionOverrides.CustomizeDescription`**：全局描述文本修改
- **`RelicImageOverridePatch.AddOverride`**：遗物图像路径重映射
- **`CustomCharacterModel`**：自动注册角色
- **`CommonActions`**：常用卡牌命令快捷封装

## 19. 参考架构：YuWanCard

### Builder Pattern 卡牌基类

链式构造，比直接继承 `CardModel` 更灵活：

```csharp
public class MyCard : YuWanCardModel
{
    public MyCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.Opponent)
    {
        WithDamage(8, upgrade: 3)
            .WithBlock(5)
            .WithKeywords(CardKeyword.Exhaust)
            .WithKeyword(CardKeyword.Ethereal, UpgradeType.Remove)
            .WithHandGlowGold(card => card.Owner?.Gold >= 100);
    }
}
```

链式 API：`WithDamage()` `WithBlock()` `WithCards()` `WithEnergy()` `WithHeal()` `WithPower<T>()` `WithKeywords()` `WithKeyword(kw, upgradeType)` `WithTags()` `WithTip()` `WithHandGlow()` `WithVar()` `WithVars()` `WithCalculatedDamage()`

### 分阶段初始化

```csharp
ModLifecycle.Publish(ModLifecyclePhase.Initializing);
patcher.PatchAllSafe(Assembly.GetExecutingAssembly(), excludeTypes);  // 补丁
patcher.ApplySingle(h => SpecificPatch.Apply(h), "PatchName");         // 条件补丁
ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly());          // 内容
ConfigRegistrar.TryDeferredRegister();                                  // 配置
AssetPreloader.Preload();                                               // 资源
ModLifecycle.Publish(ModLifecyclePhase.Initialized);
```

### 声明式手牌发光

```csharp
WithHandGlowGold(card => card.Owner?.Gold >= 100);  // 实例级
CardHandGlowRegistry.Register<SomeCard>(CardHandGlowRules.Gold(...));  // 类型级
```

### SavedProperty 序列化

```csharp
public class MyRelic : YuWanRelicModel
{
    static MyRelic() => SavedPropertyRegistration.RegisterType(typeof(MyRelic));
    [SavedProperty] public string CustomData { get; set; } = "";
}
```

### 模组图标

模组在安装页面的显示图标：`res://<modid>/mod_image.png`

---

## 附录A：游戏源码速查

> 不用每次 grep sts2-res。关键类的真实签名、字段、属性都在这里。
> 写 Patch 前先查本节，签名对不上再去看反编译源码。

### A.1 CardModel — 卡牌模型

```csharp
// ── 构造函数 ──
CardModel(int cost, CardType type, CardRarity rarity, TargetType target, bool showInLibrary = true)

// ── 可 Patch 的属性/方法 ──
public virtual int MaxUpgradeLevel => 1;           // 升级上限，patch 这个实现无限升级
public bool IsUpgradable                            // 是否可升级
{
    get { if (CurrentUpgradeLevel >= MaxUpgradeLevel) return false; return true; }
}
public bool IsUpgraded => CurrentUpgradeLevel > 0;  // 是否已升级
public int CurrentUpgradeLevel                       // 当前升级等级
public int BaseReplayCount                           // 原生重放次数（默认 0）
public EnchantmentModel? Enchantment                 // 附魔（null = 无附魔）
public CardUpgradePreviewType UpgradePreviewType     // 升级预览类型

// ── 可重写的方法 ──
protected virtual IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();
public virtual IEnumerable<CardKeyword> CanonicalKeywords  // 关键词
protected virtual HashSet<CardTag> CanonicalTags           // 标签
public virtual string PortraitPath                          // 肖像路径
public virtual bool IsPlayable                              // 打出条件
public virtual bool ShouldGlowGoldInternal                  // 金光提示
protected virtual void OnUpgrade()                          // 升级回调
protected virtual async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)  // 打出效果
public virtual async Task OnTurnEndInHand(PlayerChoiceContext ctx)           // 回合结束在手牌

// ── 描述相关 ──
public string GetDescriptionForPile(PileType pileType, Creature? target = null)
// 内部: private string GetDescriptionForPile(PileType, DescriptionPreviewType, Creature?)
public string GetDescriptionForUpgrade()

// ── 数值相关 ──
public int GetEnchantedReplayCount()  // = Enchantment?.EnchantPlayCount(BaseReplayCount) ?? BaseReplayCount
public int ResolveXValue()            // X 费卡当前 X 值
public int ResolveStarXValue()        // 辉星卡 X 值
public DynamicVarSet DynamicVars      // 动态变量集合（.TryGetValue(key, out dv)）

// ── 序列化/克隆 ──
public static CardModel FromSerializable(SerializableCard save)
public CardModel ToMutable()
protected override void DeepCloneFields()

// ── 内部方法（Patch 时可能用到）──
public void UpgradeInternal()          // 执行升级（CurrentUpgradeLevel++）
public void EnchantInternal(EnchantmentModel m, decimal amount)
public void FinalizeUpgradeInternal()  // 升级后刷新数值
```

### A.2 RelicModel — 遗物模型

```csharp
// ── 核心属性 ──
protected virtual Rarity Rarity    // 稀有度：Starter/Event/Ancient/Common/Uncommon/Rare/Shop

// ── 回合钩子 ──
public virtual async Task AfterSideTurnStart(Side side, CombatState combatState)
public virtual async Task BeforeSideTurnStart(Side side, CombatState combatState)
// Side 枚举: Side.Player / Side.Opponent

// ── 战斗钩子 ──
public virtual async Task OnCombatStart(CombatState combatState)
public virtual async Task OnCombatEnd()
public virtual async Task OnPlayerDeath()

// ── 获取钩子 ──
public virtual async Task OnObtained()
public virtual async Task OnRemoved()

// ── 工具方法 ──
protected Task Flash()               // 闪烁提示（触发遗物动画）
public static RelicModel FromSerializable(SerializableRelic save)
```

### A.3 PotionModel — 药水模型

```csharp
// ── 枚举 ──
enum PotionRarity { Common, Uncommon, Rare }
enum PotionUsage { CombatOnly, AnyTime, ProgrammaticOnly }

// ── 核心 ──
public TargetType TargetType
public PotionUsage Usage
public virtual bool CanBeGeneratedInCombat  // 战斗中能否随机生成
public virtual bool ShouldDie               // 复活药水用
public virtual async Task AfterPreventingDeath(Player player)
public virtual async Task OnUse(PlayerChoiceContext ctx, PotionTarget target)

// ── 序列化 ──
public static PotionModel FromSerializable(SerializablePotion save)
public PotionModel ToMutable()
```

### A.4 EnchantmentModel — 附魔模型

```csharp
// ── 核心方法 ──
public virtual bool CanEnchant(CardModel card)         // 签名: (CardModel) 非无参
public virtual bool CanEnchantCardType(CardType cardType)
public virtual void ModifyCard()                        // 应用附魔效果
public int EnchantPlayCount(int baseCount)              // 计算附魔重放次数
public int Amount                                       // 层数

// ── 序列化 ──
public static EnchantmentModel FromSerializable(SerializableEnchantment save)
```

### A.5 PowerModel — 能力/Buff/Debuff

```csharp
enum PowerType { Buff, Debuff, Neutral }
enum PowerStackType { Intensity, Duration }

// ── 核心 ──
protected virtual PowerType Type
protected virtual PowerStackType StackType
public virtual bool IsInstanced      // true = 每个实例独立，false = 同类型不叠加
public virtual bool AllowNegative    // 允许负层数
public virtual string smartDescription  // 动态描述的 key

// ── 战斗钩子 ──
public virtual async Task OnApplied(Creature target)
public virtual async Task OnRemoved(Creature target)
public virtual async Task AfterSideTurnStart(Side side, CombatState state)
public virtual async Task OnDamageReceived(DamageInfo info)
public virtual async Task OnDamageDealt(DamageInfo info)
public virtual decimal ModifyDamage(DamageInfo info, decimal damage)  // 伤害修正
```

### A.6 EventModel — 事件模型

```csharp
// ── 核心 ──
public virtual string EventId
public virtual async Task OnEnter(RoomModel room, Player player)
public virtual async Task OnOptionSelected(int optionIndex, Player player)

// ── AncientEventModel（先古之民）──
public virtual AncientDialogueSet DialogueSet
public virtual async Task OnAncientEnter(Player player)
public virtual async Task OnAncientOptionSelected(int option, Player player)
```

### A.7 MonsterModel + 遭遇

```csharp
// ── MonsterModel ──
public virtual async Task<MoveState> GetInitialState(CombatState combat)
public virtual string PortraitPath

// ── MoveState ──
public class MoveState
{
    public LocString IntentText       // 意图文本
    public int IntentAmount           // 意图数值
    public IntentType IntentType      // Attack/Defend/Buff/Debuff/...
    public Func<Task<MoveState>> Action  // 执行动作，返回下一个 MoveState
}

// ── EncounterModel ──
public virtual string EncounterId
public virtual IReadOnlyList<MonsterModel> GetMonsters(Player player)
```

### A.8 CharacterModel — 角色模型

```csharp
public virtual string CharacterId
public virtual IReadOnlyList<RelicModel> StartingRelics
public virtual CardPoolModel CardPool
public virtual RelicPoolModel RelicPool
public virtual PotionPoolModel PotionPool
public virtual string PortraitPath
public virtual string SelectScreenPath
public virtual int MaxHp
```

### A.9 Core Commands — 命令类

```csharp
// ── CardCmd ──
static void Upgrade(CardModel card, CardPreviewStyle style = ...)
static void Enchant(EnchantmentModel m, CardModel card, decimal amount)
static void Discard(CardModel card)

// ── DamageCmd ──
static AttackCommand Attack(decimal damage)
    .FromCard(CardModel)
    .FromMonster(Creature)
    .Targeting(Creature)
    .TargetingAllOpponents(CombatState)
    .TargetingRandomOpponents(CombatState, bool allowRepeats)
    .WithHitCount(int)
    .WithHitFx(string scenePath, ...)
    .Execute()  // → Task

// ── PlayerCmd ──
static Task GainEnergy(Player player, int amount)
static Task GainGold(Player player, int amount)
static Task GainBlock(Player player, int amount)
static Task Damage(Player player, int amount)
static Task Heal(Player player, int amount)

// ── CardPileCmd ──
static Task Draw(Player, int count)

// ── CardSelectCmd ──
static Task<CardModel?> FromHand(PlayerChoiceContext ctx, Player owner,
    CardSelectorPrefs prefs, Func<CardModel,bool>? filter, CardModel? source)
static Task<CardModel?> FromHandForUpgrade(..., filter)
static Task<CardModel?> FromHandForDiscard(..., filter)
// CardSelectorPrefs: new(prompt, count) 或 new(prompt, minCount, maxCount)

// ── PotionCmd ──
static async Task<PotionProcureResult> TryToProcure(PotionModel potion, Player player, int slotIndex = -1)
static async Task<PotionProcureResult> TryToProcure<T>(Player player) where T : PotionModel
```

### A.10 NPotionContainer / NPotionHolder

```csharp
// ── NPotionContainer ──
class NPotionContainer : Control
{
    private Player? _player;                           // 当前玩家
    private readonly List<NPotionHolder> _holders;     // 药水栏位列表
    public void Initialize(IRunState runState);
    private void RemoveUsed(PotionModel potion);       // 使用后移除（可 Patch）
    private void Discard(PotionModel potion);           // 丢弃（可 Patch）
}

// ── NPotionHolder ──
class NPotionHolder : NClickableControl
{
    public NPotion? Potion { get; private set; }       // 当前药水节点
    public bool HasPotion => Potion != null;
    private TextureRect _emptyIcon;                     // 空栏位图标
    public void AddPotion(NPotion potion);              // 放入药水
    public void RemoveUsedPotion();                     // 使用后清理
    public void DiscardPotion();                        // 丢弃后清理
}
```

### A.11 其他常用类型

```csharp
// ── PileType ──
enum PileType { None, Hand, Draw, Discard, Deck, Exhaust, ... }

// ── CardType ──
enum CardType { Attack, Skill, Power, Status, Curse }

// ── CardRarity ──
enum CardRarity { Common, Uncommon, Rare, Event, Ancient, Token, Status, Curse, Quest, Shop }

// ── TargetType ──
enum TargetType { Opponent, Ally, Self, AllOpponents, Everyone, TargetedNoCreature, Osty, ... }

// ── CardKeyword ──
enum CardKeyword { Exhaust, Ethereal, Retain, Innate, Unplayable, ... }

// ── CardTag ──
enum CardTag { Strike, ... }

// ── CardPoolModel ──
class CardPoolModel
{
    public string Title               // 卡池名
    public Color BorderColor           // 边框颜色
    public Color ThumbnailCardColor    // 缩略卡颜色
    public string EnergyIconPath       // 能量图标路径
}

// ── ModelDb ──
static class ModelDb
{
    static Id GetId(Type type)
    static T GetByIdOrNull<T>(Id id) where T : AbstractModel
    static T Potion<T>() where T : PotionModel
}

// ── ModHelper ──
static class ModHelper
{
    static void AddModelToPool(Type poolType, Type modelType)
}

// ── RunManager / LocalContext ──
class RunManager
{
    static RunManager Instance
    IRunState RunState
    HoveredModelTracker HoveredModelTracker
}
static class LocalContext
{
    static Player GetMe(IRunState runState)
    static bool IsMine(AbstractModel model)
}

// ── TaskHelper ──
static class TaskHelper
{
    static Task RunSafely(Task task)   // 火后即忘，异常静默吞
}

// ── PotionFactory ──
static class PotionFactory
{
    static IReadOnlyList<PotionModel> GetPotionOptions(Player player, PotionModel[] exclude)
}
```
