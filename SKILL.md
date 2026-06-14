# 杀戮尖塔 2 Mod 开发 — AI 工作流

> 🦞 STS2 Mod 开发指南。依赖 `0Harmony.dll` + `sts2.dll`，不依赖第三方模组。
> 主文件只是索引，具体模式见 references/。

---

## 🚦 总工作流

```
用户说"帮我做 X"
    │
    ├─ 1. 确定类型：卡牌/遗物/药水/能力/附魔/事件/先古之民/怪物/角色/Patch/设置界面？
    │
    ├─ 2. 查 API（强制）
    │      ls ~/.openclaw/workspace/code/sts2-res/src/ 2>/dev/null ||
    │        git clone --depth 1 https://github.com/yehuoshun/sts2-res ~/.openclaw/workspace/code/sts2-res
    │      grep -rn "ClassName\|MethodName" ~/.openclaw/workspace/code/sts2-res/src/
    │
    ├─ 3. 查 references/<对应模式文件> 获取代码模板
    │
    ├─ 4. 写代码（C# + 本地化 JSON）
    │
    ├─ 5. 构建 → 部署 → 测试
    │
    └─ 6. 提交推送
```

---

## 📦 项目脚手架（新建模组的完整步骤）

### 环境要求

| 工具 | 用途 |
|------|------|
| .NET 9.0 SDK | 编译 |
| Megadot 编辑器 | 打包 PCK / 创建资源 |
| Rider / VS Code | 写代码 |

### 1. 创建 Godot 项目

```
打开 Megadot → 新建项目 → 名称用模组ID（英文）
→ 兼容渲染器 → 创建C#解决方案（项目→工具→C#→创建C#解决方案）
```

### 2. 目录结构

```
MyMod/
├── assets/
│   ├── MyMod.json            ← 模组清单
│   ├── images/
│   │   ├── cards/
│   │   ├── relics/
│   │   ├── powers/
│   │   ├── potions/
│   │   ├── enchantments/
│   │   └── events/
│   └── localization/
│       └── zhs/              ← 简体中文
│           ├── cards.json
│           ├── relics.json
│           ├── powers.json
│           ├── potions.json
│           ├── enchantments.json
│           ├── events.json
│           ├── ancients.json
│           ├── monsters.json
│           └── encounters.json
├── src/
│   ├── MyMod.csproj
│   ├── ModEntry.cs           ← 模组入口
│   ├── ModInfo.cs            ← 常量
│   ├── Cards/
│   ├── Relics/
│   ├── Powers/
│   ├── Potions/
│   ├── Enchantments/
│   ├── Events/
│   ├── Monsters/
│   └── Patches/
├── libs/                     ← 依赖 DLL
│   ├── 0Harmony.dll
│   └── sts2.dll
├── tools/                    ← 构建脚本
├── build/                    ← 输出目录
├── project.godot
└── MyMod.sln
```

### 3. 添加依赖 DLL

从游戏目录 `data_sts2_<platform>/` 复制 `0Harmony.dll` 和 `sts2.dll` 到 `libs/`，然后在 .csproj 中添加引用，或在 Rider 中右键依赖项→添加项目引用→浏览添加。

### 4. ModInfo.cs

```csharp
namespace MyMod;

internal static class ModInfo
{
    public const string Id = "MyMod";
    public const string DisplayName = "我的模组";
    public const string TargetGameVersion = "0.103.2";
}
```

### 5. ModEntry.cs（最简入口）

```csharp
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace MyMod;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    private const string HarmonyId = "Author.MyMod";
    private static Harmony? _harmony;

    public static void Initialize()
    {
        _harmony = new Harmony(HarmonyId);
        _harmony.PatchAll(Assembly.GetExecutingAssembly());
        Log.Info($"[{ModInfo.Id}] Loaded for {ModInfo.TargetGameVersion}");
    }
}
```

### 6. 模组清单 assets/MyMod.json

```json
{
  "id": "MyMod",
  "name": "我的模组",
  "version": "1.0.0",
  "author": "作者名",
  "description": "模组描述",
  "has_pck": true,
  "has_dll": true,
  "affects_gameplay": true,
  "dependencies": []
}
```

### 7. .csproj 关键配置

```xml
<Project Sdk="Godot.NET.Sdk/4.5.1">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>13</LangVersion>
    <RootNamespace>MyMod</RootNamespace>
    <Nullable>enable</Nullable>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="0Harmony"><HintPath>../libs/0Harmony.dll</HintPath></Reference>
    <Reference Include="sts2"><HintPath>../libs/sts2.dll</HintPath></Reference>
  </ItemGroup>
  <Target Name="CopyDllToBuild" AfterTargets="Build">
    <Copy SourceFiles="$(OutputPath)$(AssemblyName).dll" DestinationFolder="../build/" />
  </Target>
</Project>
```

### 8. 构建 & 部署

```bash
# 编译 DLL
dotnet build src/MyMod.csproj

# 打包 PCK（在 Megadot 中：项目→导出→导出PCK/ZIP→保存到 build/MyMod.pck）

# 三个文件放一起：
# build/MyMod.dll
# build/MyMod.pck
# build/MyMod.json

# 复制到游戏 mods/ 目录测试
cp build/MyMod.* /path/to/SlayTheSpire2/mods/
```

---

## 📋 核心 API 速查

| API | 说明 |
|-----|------|
| `CardModel(cost, type, rarity, target)` | 卡牌基类构造 |
| `RelicModel` | 遗物基类。`Rarity` 决定获取途径 |
| `PotionModel` | 药水基类。`Usage` 决定使用场景 |
| `PowerModel` | 能力基类。`Type`/`StackType` 决定行为 |
| `EnchantmentModel` | 附魔基类。`ModifyCard()` 改卡牌属性 |
| `EventModel` | 事件基类。`GenerateInitialOptions()` 返回选项 |
| `AncientEventModel` | 先古之民。`DefineDialogues()` 返回对话 |
| `CharacterModel` | 角色基类。`CardPool`/`RelicPool`/`StartingDeck` |
| `MonsterModel` | 怪物基类。`GenerateMoveStateMachine()` |
| `EncounterModel` | 遭遇基类。`GenerateMonsters()` |
| `CardPoolModel` | 卡池基类。`GenerateAllCards()` |
| `DynamicVars` | 动态变量集合。`["Name"].UpgradeValueBy(n)` |
| `PlayerCmd` | 玩家操作：`GainEnergy`/`GainBlock`/`GainGold`/`Damage`/`Heal`/`ApplyPower` |
| `CardCmd` | 卡牌：`Enchant`/`Discard`/`Upgrade` |
| `DamageCmd` | 伤害链：`Attack().FromCard().Targeting().Execute()` |
| `CardPileCmd` | 牌堆：`Draw` |
| `CardSelectCmd` | 选牌：`FromHandGeneric`/`FromDeckGeneric` |
| `ModelDb` | 反射注册：`GetId`/`GetByIdOrNull`/`AllCards`/`AllRelics` |
| `ModHelper.AddModelToPool<T>()` | 注册模型到对应池 |
| `SavedPropertiesTypeCache.InjectTypeIntoCache()` | 自定义属性序列化缓存注入 |
| `ScriptManagerBridge.LookupScriptsInAssembly` | 场景脚本映射（有自定义场景时必须） |

---

## 📂 模式速查

| 需求 | 文件 |
|------|------|
| 卡牌 | [references/modes-card.md](references/modes-card.md) |
| 遗物 | [references/modes-relic.md](references/modes-relic.md) |
| 药水 | [references/modes-potion.md](references/modes-potion.md) |
| 能力（Buff/Debuff） | [references/modes-power.md](references/modes-power.md) |
| 附魔 | [references/modes-enchantment.md](references/modes-enchantment.md) |
| 事件 & 先古之民 | [references/modes-event.md](references/modes-event.md) |
| Harmony 补丁 | [references/modes-harmony.md](references/modes-harmony.md) |
| 角色 | [references/modes-character.md](references/modes-character.md) |
| 怪物 & 遭遇 | [references/modes-monster.md](references/modes-monster.md) |
| 序列化 & 注册 | [references/modes-serialization.md](references/modes-serialization.md) |
| 真实项目写法模式 | [references/real-code-patterns.md](references/real-code-patterns.md) |
| 项目构建 & 部署 | [references/project-scaffold.md](references/project-scaffold.md) |
| 设置界面 | [references/modes-settings-ui.md](references/modes-settings-ui.md) |
| 附录（枚举/本地化/常见坑） | [references/appendices.md](references/appendices.md) |

---

## ⚠️ 常见坑速览

| 问题 | 解决 |
|------|------|
| 图标不显示 | PNG 未打包进 PCK；检查路径与代码一致 |
| 本地化不生效 | 必须 **Publish**（非 Build），本地化是资源文件 |
| Harmony 报 .NET 版本 | `GodotPlugins.runtimeconfig.json` → `"version": "9.0.0"` |
| 场景脚本找不到 | `ScriptManagerBridge.LookupScriptsInAssembly` |
| Mod 不加载 | `assets/MyMod.json` 的 `id` 和文件名一致 |
| 自定义属性不保存 | 没调 `SavedPropertiesTypeCache.InjectTypeIntoCache()` |
| `AddModelToPool` 泛型报错 | 用 `ModHelper.AddModelToPool(poolType, modelType)` 反射重载 |
| 遗物 `Rarity=Starter` 但池里不出现 | Starter 稀有度不走随机池，需 Patch 或用事件给 |
| Harmony PatchAll 异常 | 单类 try-catch 包裹，防止一个类炸了全挂 |
| 设置 UI 自己写容易出 bug | 参考 modes-settings-ui.md 或接入 ModConfig 框架 |

---

## 🧠 活跃参考仓库学习笔记（2026-06-14）

### YuWanCard — Builder 模式 + 反射注册体系

**卡牌 Builder 链式构造**（`YuWanCardModel`）：
```csharp
// 构造函数中自动注册到 ContentRegistry
class MyCard : YuWanCardModel {
    public MyCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.Enemy)
        .WithDamage(7, 3)           // DamageVar(7) + 升级+3
        .WithBlock(5)               // BlockVar(5)
        .WithVar("custom", 3, 1)    // 自定义 DynamicVar
        .WithPower<MyPower>(2)      // PowerVar + 自动 HoverTip
        .WithKeywords(CardKeyword.Exhaust)
        .WithHandGlowGold(c => ...) // 手牌金色高亮条件
        .WithCostUpgradeBy(-1)      // 升级减费
    { }
}
```

**注册体系**（`ContentRegistry`）：
- `[Pool]` 属性 → `ModHelper.AddModelToPool` 自动注册
- `[RegisterEvent]` / `[RegisterAncient]` / `[RegisterBoss]` / `[RegisterCharacter]` / `[RegisterOrb]` / `[RegisterMonster]` / `[RegisterEnchantment]` / `[RegisterSingleton]` 等属性
- `AssemblyScanner.GetLoadableTypes` 安全类型扫描（Android/Mono 兼容）
- `Freeze()` 机制：ModelDb.Init 后冻结，防止延迟注册
- `InitDeDuplicationPatch`：SafeInit 去重构造，跳过已存在的 ModelId

**Patch 管理**（`ModPatcher`）：
- `PatchAllSafe`：单类 try-catch，一个类炸了不影响其他
- `ApplySingle`：手动控制特定 Patch 的加载（如平台条件 Patch）
- `ModLifecycle.Publish(phase)`：生命周期事件发布

**跨模组通信**（`ModInterop`）：
- `[ModInterop]` 属性标记接口方法
- `ModInteropProcessor` 自动扫描并连接

### BaseLib — Hooks 接口 + 工具集

**Hooks 接口扩展**：
- `IMaxHandSizeModifier` — 修改最大手牌数
- `IHealAmountModifier` — 修改治疗量
- `IAfterCardDowngraded` — 卡牌降级后回调
- `IHealthBarForecastSource` — 血条预测数据源

**工具集**：
- `ReflectionUtils` / `CustomCharacterUtils` / `MonsterActions` / `CustomLocTableManager` / `NodeFactory`
- `Patching/AsyncMethodCall` — 异步方法 IL 级 Patch 辅助
- `ModConfigRegistry.Register` — 注册到 ModConfig 框架

### ModTemplate — 双模板脚手架

- **ContentMod**：纯内容模组（卡牌/遗物/药水等），最简入口
- **CharacterMod**：角色模组，含 `CardPool` / `RelicPool` / `PotionPool` / `Character`
- 模板自带 `ScriptManagerBridge.LookupScriptsInAssembly` 注释（有自定义场景时取消注释）

### ModConfig v0.2.2 — 设置框架

**控件类型**：Toggle / Slider / Dropdown / KeyBind / TextInput / ColorPicker / Button / Header / Separator

**公共 API**（`ModConfigApi`）：
```csharp
ModConfigApi.Register(modId, displayName, entries);
ModConfigApi.Register(modId, displayName, displayNames, entries); // 多语言
ModConfigApi.GetValue<T>(modId, key);
ModConfigApi.SetValue(modId, key, value);
```

**关键设计**：
- 零 Harmony 注入（`SceneTree.NodeAdded` 信号检测 `NSettingsTabManager`）
- `ModConfigManager` 持久化 debounce（`ProcessFrame` 合并写入）
- `I18n` 多语言支持（`LabelKey` / `Labels` 字典）
- `CollapsedMods` UI 折叠状态持久化
- `ColorPicker` 颜色规范化（`NormalizeColorHexOrDefault`）
- `ConfigEntry.Validator` 输入校验（TextInput 红框反馈）
- `Button` 类型支持（可绑定回调动作）

---
