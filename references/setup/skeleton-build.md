# 项目骨架：构建、入口与角色资源

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

