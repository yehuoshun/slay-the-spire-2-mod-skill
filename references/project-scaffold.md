# 项目搭建 & 部署

> 纯原生 Mod 脚手架。零第三方依赖，只靠 `0Harmony.dll` + `sts2.dll`。

---

## 环境要求

| 工具 | 用途 |
|------|------|
| .NET 9.0 SDK | 编译 |
| Megadot 编辑器 | 打包 PCK / 创建资源 |
| Rider / VS Code | 写代码 |

---

## 1. 创建 Godot 项目

```
打开 Megadot → 新建项目 → 名称用模组ID（英文 PascalCase）
→ 兼容渲染器 → 创建C#解决方案（项目→工具→C#→创建C#解决方案）
```

---

## 2. 目录结构

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
│   │   ├── events/
│   │   ├── characters/
│   │   ├── monsters/
│   │   └── ui/
│   ├── localization/
│   │   └── zhs/              ← 简体中文
│   │       ├── cards.json
│   │       ├── relics.json
│   │       ├── powers.json
│   │       ├── potions.json
│   │       ├── enchantments.json
│   │       ├── events.json
│   │       ├── ancients.json
│   │       ├── monsters.json
│   │       ├── characters.json
│   │       └── encounters.json
│   ├── scenes/               ← 自定义场景（角色视觉、VFX、休息点等）
│   └── audio/
├── src/
│   ├── MyMod.csproj
│   ├── ModEntry.cs           ← 模组入口
│   ├── Cards/                ← 卡牌代码
│   ├── Relics/               ← 遗物代码
│   ├── Powers/               ← 能力代码
│   ├── Potions/              ← 药水代码
│   ├── Enchantments/         ← 附魔代码
│   ├── Events/               ← 事件代码
│   ├── Monsters/             ← 怪物 & 遭遇
│   ├── Characters/           ← 角色
│   ├── Patches/              ← Harmony 补丁
│   ├── Settings/             ← 设置系统（可选）
│   └── Core/                 ← 核心工具（反射、注册、属性等）
├── libs/
│   ├── 0Harmony.dll
│   └── sts2.dll
├── build/                    ← 输出目录
├── project.godot
└── MyMod.sln
```

---

## 3. 依赖 DLL

从游戏目录 `data_sts2_<platform>/` 复制 `0Harmony.dll` 和 `sts2.dll` 到 `libs/`。

---

## 4. ModEntry.cs（入口）

生产级写法——三阶段初始化 + try-catch 保护：

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
    private static readonly object Lock = new();
    private static bool _initialized;
    private static Harmony? _harmony;

    public static void Initialize()
    {
        lock (Lock)
        {
            if (_initialized) return;
            _initialized = true;
        }

        Log.Info($"[MyMod] Initializing...");

        // Phase 1: Harmony patches（try-catch 防止一个类炸了全挂）
        _harmony = new Harmony(HarmonyId);
        try { _harmony.PatchAll(Assembly.GetExecutingAssembly()); }
        catch (Exception e) { Log.Error($"[MyMod] Harmony failed: {e}"); }

        // Phase 2: 内容注册（属性扫描 + 反射注入）
        ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly());

        // Phase 3: 设置系统（如果有）
        // SettingsManager.Initialize();

        Log.Info($"[MyMod] Initialized for {ModInfo.TargetGameVersion}");
    }
}
```

**关键教训**：
- `lock` + `_initialized` 防止重复初始化（学了海克斯符文）
- `PatchAll` 外层 try-catch 兜底，防止一个 Patch 类报错导致整个 Mod 挂掉
- 分阶段初始化，方便排查问题

---

## 5. ModInfo.cs（常量）

```csharp
namespace MyMod;

internal static class ModInfo
{
    public const string Id = "MyMod";
    public const string DisplayName = "我的模组";
    public const string TargetGameVersion = "0.103.2";
}
```

**多版本兼容**（学海克斯符文）：

```csharp
#if STS2_107_OR_NEWER
    public const string TargetGameVersion = "0.107.0";
#elif STS2_106_OR_NEWER
    public const string TargetGameVersion = "0.106.1";
#else
    public const string TargetGameVersion = "0.103.2";
#endif
```

---

## 6. ContentRegistry.cs（属性扫描注册）

学 YuWanCard 的思路，用自定义 Attribute 标记模型，初始化时反射扫描自动注册。不用手动调 `AddModelToPool`。

```csharp
using System.Reflection;
using MegaCrit.Sts2.Core.Modding;

namespace MyMod.Core;

public static class ContentRegistry
{
    public static void RegisterAll(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract) continue;

            // 卡牌池
            var cardAttr = type.GetCustomAttribute<CardPoolAttribute>();
            if (cardAttr != null)
            {
                ModHelper.AddModelToPool(cardAttr.PoolType, type);
                continue;
            }

            // 遗物池
            var relicAttr = type.GetCustomAttribute<RelicPoolAttribute>();
            if (relicAttr != null)
            {
                ModHelper.AddModelToPool(relicAttr.PoolType, type);
                continue;
            }
        }
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class CardPoolAttribute : Attribute
{
    public Type PoolType { get; }
    public CardPoolAttribute(Type poolType) => PoolType = poolType;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RelicPoolAttribute : Attribute
{
    public Type PoolType { get; }
    public RelicPoolAttribute(Type poolType) => PoolType = poolType;
}
```

**用法**：在卡牌/遗物类上加 `[CardPool(typeof(RedPool))]` 即可自动注册。

---

## 7. .csproj 配置

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

---

## 8. 模组清单 assets/MyMod.json

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

---

## 9. 构建 & 部署

```bash
# 编译 DLL
dotnet build src/MyMod.csproj -c Release

# 打包 PCK（Megadot 中：项目→导出→导出PCK/ZIP→保存到 build/MyMod.pck）

# 最终产物：
# build/MyMod.dll
# build/MyMod.pck
# assets/MyMod.json  ← 复制到 build/

# 部署到游戏 mods/ 目录
cp build/MyMod.* /path/to/SlayTheSpire2/mods/
```

---

## 10. 推荐项目结构对比

| 方案 | 适用场景 | 参考 |
|------|---------|------|
| 最简 Scaffold | 1-2 个遗物/卡牌的小 Mod | [ModTemplate](https://github.com/Alchyr/ModTemplate-StS2) |
| Builder 模式 | 卡牌/遗物数量多，需要流式代码 | [YuWanCard](https://github.com/YuWan886/Sts2-YuWanCard) |
| 属性扫描注册 | 内容型 Mod，想自动发现注册 | 本指南 ContentRegistry |
| 生产级分 Hooks 目录 | 大型 Mod，需要清晰的补丁组织 | [海克斯符文](https://github.com/s1f102500012/sts2mod) |
