# 项目模板（ModTemplate-StS2）

> 参考：[Alchyr/ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2)（官方脚手架模板 v2.5.1，2026-08-12）
> 学习时间：2026-08-21

---

## 概述

官方 dotnet 项目模板，一键生成 STS2 mod 的标准目录结构。提供 3 种模板：

| 模板 | shortName | 用途 |
|------|-----------|------|
| Empty Mod | `alchyrsts2mod` | 空 mod |
| Content Mod | `alchyrsts2contentmod` | 内容 mod（卡牌/遗物/能力等） |
| Character Mod | `alchyrsts2charmod` | 角色 mod（含完整角色资源骨架） |

> ⚠️ 模板默认依赖 BaseLib（`Alchyr.Sts2.BaseLib` + manifest dependency）。本 skill 主推纯原生，见下方「纯原生化」移除即可。

---

## 安装与使用

```bash
# 安装模板（NuGet 包 Alchyr.Sts2.Templates）
dotnet new install Alchyr.Sts2.Templates

# 创建内容 mod
dotnet new alchyrsts2contentmod -n MyMod --ModAuthor "YourName"

# 创建角色 mod
dotnet new alchyrsts2charmod -n MyMod --ModAuthor "YourName"
```

**重要**：创建解决方案时必须启用 **"Put solution and project in same directory"**，否则无法与 Godot 正常工作。

### 可用参数

| 参数 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `--ModAuthor` | string | Author | 作者名，替换 manifest 的 author |
| `--PublicizeSts` | bool | false | 是否 publicize sts2.dll（访问私有/受保护成员） |
| `--NullableChecks` | enable/disable | enable | 空值检查严格度 |

---

## 模板文件规范（纯原生可借鉴）

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
        <!-- 抑制架构不匹配警告（mod 应跨平台构建） -->
        <NoWarn>$(NoWarn);MSB3270</NoWarn>
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

    <!-- 可选：publicize sts2.dll（访问原本 private/protected 成员） -->
    <ItemGroup Condition="$(Publicize)">
        <PackageReference Include="Krafs.Publicizer" Version="2.3.0" PrivateAssets="All"/>
        <Publicize Include="sts2" IncludeVirtualMembers="false" IncludeCompilerGeneratedMembers="false" />
    </ItemGroup>
</Project>
```

> 模板默认还有 `Alchyr.Sts2.BaseLib` + `Alchyr.Sts2.ModAnalyzers` 包引用，**纯原生方案删除这两个 PackageReference**。

### 入口 MainFile.cs

```csharp
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "ContentMod";              // 资源文件路径用
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        // 若 mod 场景用到 C# 脚本，需检索程序集：
        // Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        Harmony harmony = new(ModId);
        harmony.PatchAll();
    }
}
```

### manifest（ContentMod.json）

```json
{
  "id": "ContentMod",
  "name": "ContentMod",
  "author": "YourName",
  "description": "...",
  "version": "v0.0.0",
  "min_game_version": "0.107.0",
  "has_pck": true,
  "has_dll": true,
  "dependencies": [],
  "affects_gameplay": true
}
```

> 纯原生：`dependencies` 数组留空（模板默认有 BaseLib）。

### Sts2PathDiscovery.props

自动检测 STS2 安装路径（Windows/Linux/macOS），详见 [environment-setup.md](environment-setup.md) 与 [baselib.md](../baselib/baselib.md)「项目配置」。

---

## 目录结构规范

```
MyMod/
├── MyMod/                    ← Godot 资源目录（与代码分离）
│   ├── images/
│   │   ├── card_portraits/   ← 卡牌肖像（big/ 大图）
│   │   ├── potions/          ← 药水（outline/ 描边）
│   │   ├── powers/           ← 能力图标（big/）
│   │   ├── relics/           ← 遗物（big/ + outline/ 描边）
│   │   └── charui/           ← 角色 UI（大能量图标、选择图标等）
│   ├── localization/
│   │   └── eng/
│   │       ├── cards.json
│   │       ├── relics.json
│   │       ├── powers.json
│   │       ├── potions.json
│   │       ├── ancients.json
│   │       ├── card_keywords.json
│   │       ├── characters.json
│   │       └── static_hover_tips.json
│   └── mod_image.png         ← 模组安装页展示图
├── MyModCode/                ← C# 代码目录（与资源分离）
│   ├── Cards/
│   ├── Potions/
│   ├── Powers/
│   ├── Relics/
│   ├── Character/
│   └── MainFile.cs
├── MyMod.csproj
├── MyMod.json
├── Sts2PathDiscovery.props
├── Directory.Build.props
├── project.godot
└── export_presets.cfg
```

**规范要点**：
- 资源目录与代码目录**分离**（`MyMod/` vs `MyModCode/`），避免 Godot 把 .cs 当资源
- 本地化按文件类型拆分（cards/relics/powers/...），比单文件更清晰
- 图标路径约定：大图 `big/`、描边 `outline/`，与各模块 reference 中的路径规则一致

---

## 纯原生化

模板默认依赖 BaseLib，转纯原生只需两步：

1. `csproj` 删除：`<PackageReference Include="Alchyr.Sts2.BaseLib" .../>`（ModAnalyzers 是编译期检查，可留可删）
2. `manifest` 的 `dependencies` 清空

其余（目录结构、props、MainFile 模式、图像/本地化路径约定）全部原生可用。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 模板创建后 Godot 无法打开 | 创建时启用 "Put solution and project in same directory" |
| 编译报架构警告 | csproj 加 `<NoWarn>MSB3270</NoWarn>` |
| 场景找不到 C# 脚本 | 入口调 `ScriptManagerBridge.LookupScriptsInAssembly` |
| 不想用 BaseLib | 删 csproj 包引用 + manifest dependencies 清空 |

---

## 演进路线

- 当前：本 skill 手动创建项目（见 [environment-setup.md](environment-setup.md)）
- 官方模板：`dotnet new` 一键生成标准骨架，省去手建目录
- 纯原生取舍：模板骨架可直接用，但 BaseLib 依赖要移除，用本 skill 各子项的纯原生写法填充
