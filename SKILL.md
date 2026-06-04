---
name: slay-the-spire-2-mod-skill
description: 杀戮尖塔2（Slay the Spire 2 / STS2）模组开发实用指南。当用户提到杀戮尖塔2、杀戮尖塔、STS2、STS Mod、写模组、模组开发、卡牌、遗物、药水、能力、事件、角色、怪物、补丁、Harmony、Godot模组、Mod开发时使用。
---

# 杀戮尖塔 2 Mod 开发 — AI 工作流

> 你不是在查字典，你是在干活。按流程走，别跳步。

---

## 🚦 总工作流（收到请求后强制执行）

```
用户说"帮我做 X"
    │
    ├─ 1. 确定类型：卡牌/遗物/药水/能力/附魔/事件/怪物/角色/Patch？
    │
    ├─ 2. 查真实 API（强制）
    │      ls ~/.openclaw/workspace/code/sts2-res/src/ 2>/dev/null ||
    │        git clone --depth 1 https://github.com/yehuoshun/sts2-res ~/.openclaw/workspace/code/sts2-res
    │      grep -rn "目标类名\|目标方法" ~/.openclaw/workspace/code/sts2-res/src/
    │
    ├─ 3. 参考已有代码（多渠道参考）
    │      # ShunMod 自有实现
    │      grep -rn "关键词" ~/.openclaw/workspace/code/STS2-ShunMod/STS2-ShunModCode/
    │      # STS2-Mods 参考合集（Natsuki，12 个独立模组）
    │      curl -sL "https://raw.githubusercontent.com/s1f102500012/sts2mod/main/<模组目录>/src/<文件>.cs"
    │      # STS2Plus 参考（Stephen）
    │      curl -sL "https://raw.githubusercontent.com/StephenSHorton/STS2Plus/master/<路径>"
    │      # YuWanCard 参考（生产级角色 Mod，Builder/SavedProperty）
    │      curl -sL "https://raw.githubusercontent.com/YuWan886/Sts2-YuWanCard/master/<路径>"
    │
    ├─ 4. 写代码（查对应模式文件 → references/）
    │
    └─ 5. 提交推送（git add -A && git commit -m "中文提交信息" && git push）
```

---

## 🏗️ 模式零：新建模组脚手架（第一步）

写任何代码之前先搭好目录和配置文件。

### 目录结构

```
MyMod/
├── assets/
│   ├── MyMod.json          ← 模组清单
│   ├── images/
│   │   ├── cards/
│   │   ├── relics/
│   │   ├── powers/
│   │   └── events/
│   └── localization/
│       ├── zhs/            ← 简体中文
│       │   ├── cards.json
│       │   ├── relics.json
│       │   ├── powers.json
│       │   └── events.json
│       └── eng/            ← 英文
│           └── ...
├── src/
│   ├── MyMod.csproj
│   ├── ModEntry.cs         ← 入口：[ModInitializer]
│   ├── ModInfo.cs          ← 常量（ModId/版本/资源路径）
│   ├── Cards/
│   ├── Relics/
│   ├── Powers/
│   ├── Events/
│   └── Patches/
└── tools/                  ← 构建脚本
    ├── build_and_deploy.sh
    ├── pack_mod.gd
    └── project.godot
```

### ModInfo.cs 模板

```csharp
namespace MyMod;

internal static class ModInfo
{
    public const string Id = "MyMod";
    public const string DisplayName = "我的模组";
    public const string TargetGameVersion = "0.103.2";
    public const string LogPrefix = $"[{Id}]";

    // 资源路径（供 AssetHooks 和卡牌/遗物用）
    public const string CardPortraitPath = $"res://{Id}/images/cards/{{0}}.png";
    public const string RelicIconPath = $"res://{Id}/images/relics/{{0}}.png";
    public const string PowerIconPath = $"res://{Id}/images/powers/{{0}}.png";
}
```

### ModEntry.cs 模板

```csharp
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MyMod;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    private const string HarmonyId = "Author.MyMod";
    private static Harmony? _harmony;

    public static void Initialize()
    {
        // 1. 注入自定义模型的序列化缓存（有自定义属性时必做）
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MyRelic));

        // 2. 注册模型到游戏池
        ModHelper.AddModelToPool<SharedRelicPool, MyRelic>();

        // 3. 安装 Harmony 补丁
        _harmony = new Harmony(HarmonyId);
        _harmony.PatchAll();

        // 4. 安装资源钩子（有自定义贴图时）
        // AssetHooks.Install(_harmony);

        Log.Info($"[{ModInfo.Id}] Loaded for STS2 {ModInfo.TargetGameVersion}.");
    }
}
```

### 快速脚手架命令

```bash
# 推荐：用官方模板一键创建（需先安装 dotnet new 模板）
dotnet new install Alchyr.Sts2.Templates
dotnet new sts2mod -n MyMod          # 空模组（BaseLib 依赖）
dotnet new sts2content -n MyMod      # 内容模组（卡牌/遗物/能力）
dotnet new sts2character -n MyMod    # 角色模组（完整角色）

# 手动创建完整目录（不用模板时）
mkdir -p MyMod/{assets/{images/{cards,relics,powers,events},localization/{zhs,eng}},src/{Cards,Relics,Powers,Events,Patches},tools}

# 创建 assets/MyMod.json（参考 references/appendices.md 附录 D）
# 创建 src/MyMod.csproj（参考 references/appendices.md 附录 E）
# 写 ModInfo.cs + ModEntry.cs（参考上面模板）
# 写卡牌类 → Cards/
# 写本地化 → assets/localization/zhs/cards.json（参考 references/appendices.md 附录 C）
```

---

## 📁 本项目结构（ShunMod 实战模板）

```
STS2-ShunMod/
├── STS2-ShunMod.csproj          ← 项目文件
├── STS2-ShunModCode/
│   ├── MainFile.cs              ← 模组入口：[ModInitializer]
│   ├── Core/
│   │   ├── ShunLogger.cs        ← 统一日志
│   │   └── Registration/
│   │       └── ContentRegistry.cs ← 自动注册 [Pool] 标注的类
│   ├── Cards/
│   │   └── *.cs                 ← 卡牌（每文件一个类）
│   ├── Events/
│   │   └── *.cs                 ← 事件
│   ├── Patches/
│   │   └── *.cs                 ← Harmony 补丁（每文件一个或多个 Patch 类）
│   └── images/                  ← 图片资源（按类型分子目录）
└── localization/                 ← 本地化 JSON
```

**写新东西放哪：** Cards/ 放卡牌、Events/ 放事件、Patches/ 放 Harmony 补丁、Core/ 放工具类。

---

## 📦 模组入口（MainFile 模板）

```csharp
using System.Reflection;
using HarmonyLib;
using STS2_ShunMod.Core;

namespace STS2_ShunMod;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "STS2-ShunMod";
    private static readonly Harmony _harmony = new(ModId);

    public static void Initialize()
    {
        ShunLogger.Summary(ModId);
        try { _harmony.PatchAll(); ShunLogger.Info(ModId, "补丁加载完成"); }
        catch (Exception e) { ShunLogger.Error(ModId, $"补丁失败: {e.Message}"); }
        try { ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly()); }
        catch (Exception e) { ShunLogger.Error(ModId, $"注册失败: {e.Message}"); }
    }
}
```

**关键：** `Harmony.PatchAll()` 自动发现所有 `[HarmonyPatch]` 类。`ContentRegistry.RegisterAll` 自动注册所有 `[Pool]` 标注的卡牌/遗物/药水。

---

## 🔧 开发工具链

| 需求 | 工具/命令 |
|------|----------|
| 查游戏源码 | `grep -rn "关键词" ~/.openclaw/workspace/code/sts2-res/src/` |
| 查 ShunMod 已有实现 | `grep -rn "关键词" ~/.openclaw/workspace/code/STS2-ShunMod/STS2-ShunModCode/` |
| 查 Natsuki 参考合集 | `curl -sL https://raw.githubusercontent.com/s1f102500012/sts2mod/main/<目录>/src/<文件>.cs` |
| 查 YuWanCard 参考 | `curl -sL https://raw.githubusercontent.com/YuWan886/Sts2-YuWanCard/master/<路径>` |
| 查 STS2Plus 参考 | `curl -sL https://raw.githubusercontent.com/StephenSHorton/STS2Plus/master/<路径>` |
| 创建新模组 | `dotnet new sts2mod -n MyMod` / `sts2content` / `sts2character`（需先 `dotnet new install Alchyr.Sts2.Templates`） |

---

## 📋 API 速查（最常用）

```
CardModel            → 卡牌基类。构造 (cost, type, rarity, target)
RelicModel           → 遗物基类。Rarity 决定获取途径
PotionModel          → 药水基类。Usage 决定使用场景
PowerModel           → 能力基类。Type/StackType 决定行为
EnchantmentModel     → 附魔基类。ModifyCard() 改卡牌属性
EventModel           → 事件基类。GenerateInitialOptions() 返回选项
AncientEventModel    → 先古之民。DefineDialogues() 返回对话
CharacterModel       → 角色基类。CardPool/RelicPool/StartingDeck
MonsterModel         → 怪物基类。GenerateMoveStateMachine()
EncounterModel       → 遭遇基类。GenerateMonsters()
CardPoolModel        → 卡池基类。GenerateAllCards()
DynamicVars          → 动态变量集合。["Name"].UpgradeValueBy(n)
PlayerCmd            → 玩家操作：GainEnergy/GainBlock/GainGold/Damage/Heal/ApplyPower
CardCmd              → 卡牌：Enchant/Discard/Upgrade
DamageCmd            → 伤害链：Attack().FromCard().Targeting().Execute()
CardPileCmd          → 牌堆：Draw
CardSelectCmd        → 选牌：FromHandGeneric/FromDeckGeneric
ModelDb              → 反射注册：GetId/GetByIdOrNull/AllCards/AllRelics
LocString/LocManager → 本地化
SavedPropertiesTypeCache → 序列化缓存注入
ModHelper             → 注册辅助：AddModelToPool<T>
CoreHook              → 游戏钩子：TryModifyCardRewardOptions
```

---

## 📂 模式速查（按需加载）

| 需求 | 文件 | 内容 |
|------|------|------|
| 卡牌/遗物/药水/能力/附魔/事件 | [references/modes-content.md](references/modes-content.md) | 模式一~六：卡牌、遗物、药水、能力、附魔、事件 |
| Harmony/角色/怪物/高级/注册/牌组/构建 | [references/modes-advanced.md](references/modes-advanced.md) | 模式七~十三：Harmony 补丁、角色、怪物、高级技巧、注册序列化、初始牌组、构建部署 |
| Natsuki 实战模式 | [references/natsuki-patterns.md](references/natsuki-patterns.md) | 从 12 个独立模组提炼的 12 条必知写法 |
| 附录（资源/本地化/清单/生命周期/常见坑） | [references/appendices.md](references/appendices.md) | 附录 A~G：资源路径、本地化、清单 JSON、csproj、生命周期钩子、常见坑 |
| 参考架构 | [references/architecture.md](references/architecture.md) | STS2Plus / Natsuki / YuWanCard 架构分析 |
