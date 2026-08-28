# 项目骨架：跨平台路径自动检测

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

