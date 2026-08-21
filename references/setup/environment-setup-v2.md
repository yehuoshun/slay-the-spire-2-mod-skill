# 环境搭建 & 创建项目

> 参考：[烟汐忆梦_YM 的 B站教程](https://www.bilibili.com/opus/1179300682687053826)
> v2：新增「生产级项目骨架」（学自 Alchyr/ModTemplate-StS2 的工程化思想，纯原生落地，零第三方依赖）

---

## 模组构成

一个完整的模组由三个文件组成（需同名、同目录）：

| 文件 | 说明 | 是否必须 |
|------|------|---------|
| `<ModId>.json` | 模组配置清单 | ✅ 必须 |
| `<ModId>.pck` | 数据包（资源、本地化、图像） | ❌ 可选 |
| `<ModId>.dll` | 程序集（C# 代码） | ❌ 可选 |

---

## 环境要求

| 工具 | 用途 | 下载 |
|------|------|------|
| **Megadot 编辑器** | STS2 定制的 Godot 编辑器 | [megadot.megacrit.com](https://megadot.megacrit.com) |
| **.NET 9.0 SDK** | 编译 C# 代码 | 微软官网 |
| **Rider / VS** | 写代码 | 按需 |

**推荐使用 Megadot 而非官方 Godot**，因为 STS2 的核心依赖库版本可能与官方分支不一致，用官方分支可能触发兼容性问题。

---

## 创建项目

1. 打开 Megadot → 新建项目
2. 项目名称用英文，与模组 ID 一致（影响命名空间和导出文件名）
3. 渲染器选择 **兼容渲染器**（2D 场景渲染快，不影响主程序设置）
4. 创建完成后，设置模组图像：`res://<modid>/mod_image.png`（模组安装页面的显示图）

---

## 生产级项目骨架（v2）

> 学自 ModTemplate 的工程化思想：资源/代码分离、目录规范、自动检测。骨架本身不依赖任何第三方，直接纯原生可用。

### 标准目录结构

```
MyMod/
├── MyMod/                    ← Godot 资源目录（与代码分离）
│   ├── images/
│   │   ├── card_portraits/   ← 卡牌肖像（big/ 大图 1000x760）
│   │   ├── potions/          ← 药水（outline/ 描边）
│   │   ├── powers/           ← 能力图标（big/ 256x256）
│   │   ├── relics/           ← 遗物（big/ + outline/ 描边）
│   │   └── charui/           ← 角色 UI（大能量图标、选择图标、地图标记）
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
├── Sts2PathDiscovery.props   ← 自动检测游戏路径（见下）
└── project.godot
```

**规范要点**：
- 资源目录与代码目录**分离**（`MyMod/` vs `MyModCode/`），避免 Godot 把 .cs 当资源、路径清晰
- 本地化按文件类型拆分（cards/relics/powers/...），比单文件更清晰
- 图标路径约定：大图 `big/`、描边 `outline/`，与各模块 reference 的路径规则一致

### 跨平台路径自动检测（Sts2PathDiscovery.props）

放在项目根目录。自动探测游戏安装路径（Windows 注册表 + Steam 库 + Linux/macOS），无需手动复制 DLL：

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

配合 csproj 顶部 `<Import Project=".\Sts2PathDiscovery.props"/>`，自动引用 `$(Sts2DataDir)` 下的 `sts2.dll` / `0Harmony.dll`，免手动配置。

### 生产级 csproj（含构建期自动打 PCK）

```xml
<Project Sdk="Godot.NET.Sdk/4.5.1" InitialTargets="CheckDependencyPaths">
    <Import Project=".\Sts2PathDiscovery.props"/>
    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <ImplicitUsings>true</ImplicitUsings>
        <Nullable>enable</Nullable>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
        <GodotDisabledSourceGenerators>GodotPluginsInitializer</GodotDisabledSourceGenerators>
        <!-- 抑制架构不匹配警告（mod 应跨平台构建为 MSIL） -->
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

    <!-- 构建时自动生成 .pck（简单资源时可用，复杂资源仍建议手动导出） -->
    <Target Name="BuildPck" AfterTargets="Build" Condition="Exists('$(GodotPath)')">
        <Exec Command="&quot;$(GodotPath)&quot; --headless --export-pack &quot;Windows&quot; &quot;../build/$(AssemblyName).pck&quot;" />
    </Target>
</Project>
```

> 自动打 PCK target 需在 `Directory.Build.props` 配置 `$(GodotPath)`（Megadot 可执行文件路径）。不可用时仍走下方手动导出步骤。

### 生产级入口 MainFile.cs

```csharp
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "MyMod";              // 资源文件路径用
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        // 若 mod 场景用到 C# 脚本，取消注释：
        // Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        Harmony harmony = new(ModId);
        harmony.PatchAll();
    }
}
```

> `Logger` 集中管理日志，比散落 `Log.Info` 更规范，且带 ModId 前缀方便排查。

### 角色资源清单（charui）

角色 mod 需要全套 UI 资源（纯原生对应 `CharacterModel` 的路径属性）：

| 资源 | 默认路径约定 | 对应属性 |
|------|-------------|---------|
| 角色选择图标 | `images/charui/character_icon_<id>.png` | `CustomIconTexturePath` |
| 角色名（锁定）图标 | `images/charui/char_select_char_name.png` | `CustomCharacterSelectLockedIconPath` |
| 大地图标记 | `images/charui/map_marker_<id>.png` | `CustomMapMarkerPath` |
| 大能量图标 | `images/charui/big_energy.png` | `CustomEnergyCounterPath` |
| 文字能量图标 | `images/charui/text_energy.png` | — |

> 明细以 [character.md](../character/character.md) 为准，此处给出通用目录约定。

---

## 模组清单

模组清单是一个与模组 ID 同名的 JSON 文件，放在 `build/` 目录下。

```json
{
  "id": "MyCustomMod",
  "name": "My Custom Mod",
  "version": "1.0.0",
  "author": "Author",
  "description": "模组描述",
  "has_pck": true,
  "has_dll": true,
  "affects_gameplay": true,
  "dependencies": []
}
```

**关键字段说明：**

| 字段 | 说明 |
|------|------|
| `has_pck` | 有无数据包。纯美化包可设为 `true`，`has_dll` 设为 `false` |
| `has_dll` | 有无代码。纯资源包可不包含 DLL |
| `affects_gameplay` | 是否影响游戏玩法。**联机模式关键**：为 `false` 时不校验，为 `true` 时所有联机玩家必须安装相同模组，否则可能同步问题 |

---

## PCK 数据包

PCK 保存资源（本地化、图像、音效、Spine 动画等）。路径与游戏本体一致则会替换资源（材质包制作方法）。

**手动打包步骤：**

1. 编辑器左上角：**项目 → 导出**
2. 添加导出方案 → 选 Windows
3. 点击 **导出 PCK/ZIP**
4. 保存到 `build/` 目录，文件名 `<ModId>.pck`
5. 取消勾选「使用调试导出」和「导出为补丁」
6. 清单中 `has_pck` 设为 `true`

> 有 `$(GodotPath)` 且资源简单时，可用上方 csproj 的「构建期自动打 PCK」target 替代手动导出。

---

## C# 开发环境

### 创建 C# 解决方案

编辑器：**项目 → 工具 → C# → 创建 C# 解决方案**

### 检查 .NET 版本

打开 `.csproj` 文件，确认目标框架为 `net9.0`，语言版本 `C# 13`。

### 配置依赖库（两种方式）

**方式 A（推荐）：Sts2PathDiscovery.props 自动检测**（见上方「生产级项目骨架」），csproj 里直接引用 `$(Sts2DataDir)` 下的 DLL。

**方式 B（备选，props 不可用时）**：

- 从游戏目录 `data_sts2_<platform>/` 复制 `sts2.dll` + `0Harmony.dll` 到项目 `libs/` 目录
- IDE 中：**右键依赖项 → 添加项目引用 → 浏览 → 选择两个 DLL**

### 声明模组入口

```csharp
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace MyCustomMod;

[ModInitializer(nameof(Initialize))]
public static class MyCustomModInitializer
{
    public static void Initialize()
    {
        Log.Info("[MyCustomMod] 模组加载成功！");
    }
}
```

**规则：**
- 入口类必须是 **静态类**
- 初始化方法必须是 **无参数、无返回值、静态方法**
- 用 `[ModInitializer]` 特性指定初始化方法
- 生产级可用上方 MainFile.cs 模式（常量 + Logger + 脚本检索）

### 构建

右键项目 → **生成**（Build）。成功后 `build/` 目录会得到：
- `MyCustomMod.dll`
- `MyCustomMod.pck`（如果有资源）
- `MyCustomMod.json`（清单）

---

## 调试

将 `build/` 下的三个文件复制到游戏目录的 `mods/` 下，或 Megadot 可执行文件同目录下的 `mods/` 目录。

游戏启动后：
- 右下角提示模组载入
- 日志输出（通过 `Log.Info` 或 `GD.Print`）

---

## 演进路线

- 当前：手动创建项目 + 手动配置依赖
- v2：**生产级骨架**（目录规范 + props 自动检测 + 构建期打 PCK + 生产级入口），学自 ModTemplate 工程化思想，纯原生
- 终极：**把纯原生骨架打包成 `dotnet new` 模板**，一行生成项目（学自 ModTemplate 的模板机制 `Alchyr.Sts2.Templates`）
- 无关 BaseLib：模板机制、目录规范、props 全是原生，BaseLib 的 Custom*Model 示例内容不采用

---

## 已知问题

| 问题 | 解决 |
|------|------|
| `.NET SDK` 版本不匹配 | 编译报 `CS1705`，检查 `global.json` 或 `.csproj` 目标框架 |
| 模组不加载 | 检查清单 `id` 与文件名一致 |
| DLL 引用报错 | 检查 `Sts2PathDiscovery.props` 路径检测是否命中，或改用手动复制 |
| 本地化不生效 | 必须 **Publish**（非 Build），本地化是资源文件 |
| 联机同步问题 | `affects_gameplay` 设置错误 |
| 自动打 PCK 失败 | `$(GodotPath)` 未配置或路径不对，改用手动导出 |
