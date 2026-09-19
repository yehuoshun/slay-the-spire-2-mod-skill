# CI 构建与打包

> 学自 [yehuoshun/sts2-shunmod](https://github.com/yehuoshun/STS2-ShunMod) 的 `ci.yml`。纯 GitHub Actions 流水线，零本地环境依赖。

---

## 概述

本地构建依赖游戏安装目录（通过 `Sts2PathDiscovery.props` 定位），CI 构建不行——CI 机器上没有 STS2。解决方式：

1. 把 `sts2.dll` + `0Harmony.dll` 上传到 GitHub Release
2. CI 中从 Release 下载，用 `-p:Sts2DataDir=` 指定路径
3. 可选的 Godot 二进制用于打 PCK

---

## 一、上传依赖

手动打一个 Release，标签名如 `0.107.1`，上传两个文件：

```
sts2.dll         ~9 MB
0Harmony.dll     ~2.2 MB
```

> 文件从游戏目录 `data_sts2_*/` 下复制。每次游戏更新后上传新版本。

---

## 二、CI 流水线结构

```yaml
name: CI Build

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  workflow_dispatch:

env:
  DEPS_VERSION: "0.107.1"          # 依赖 Release 标签
  GODOT_VERSION: "4.5.1-stable"    # PCK 打包用

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 9.0.x

      # ── 依赖缓存 ──
      - name: Cache NuGet
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj') }}

      - name: Cache game refs
        id: cache-deps
        uses: actions/cache@v4
        with:
          path: deps
          key: deps-${{ env.DEPS_VERSION }}

      # ── 下载游戏 DLL ──
      - name: Download game refs
        if: steps.cache-deps.outputs.cache-hit != 'true'
        run: |
          mkdir -p deps
          URL="https://github.com/${{ github.repository }}/releases/download/${{ env.DEPS_VERSION }}"
          curl -sL "$URL/sts2.dll" -o deps/sts2.dll
          curl -sL "$URL/0Harmony.dll" -o deps/0Harmony.dll

      # ── 构建 ──
      - name: Build
        run: |
          dotnet build MyMod.csproj \
            -c Release \
            -p:Sts2DataDir="$(realpath deps)"
```

---

## 三、PCK 打包

两种方式。推荐**优先用 PckPacker**（简资源时），复杂资源用 Godot 导出。

### 方式 A：PckPacker（NuGet 包，单体模组适用）

在 csproj 中加入：

```xml
<ItemGroup>
  <PackageReference Include="BSchneppe.StS2.PckPacker" Version="0.1.1" PrivateAssets="All"/>
</ItemGroup>
<PropertyGroup>
  <PckPackerSourceDir>assets/</PckPackerSourceDir>
  <PckPackerResPrefix>$(AssemblyName)</PckPackerResPrefix>
  <PckPackerOutputPath>$(OutputPath)$(AssemblyName).pck</PckPackerOutputPath>
</PropertyGroup>
<ItemGroup>
  <GodotResourceFiles Include="assets\**"/>
</ItemGroup>
```

构建时自动生成 `.pck`，无需下载 Godot。

### 方式 B：Godot 导出（复杂场景/多模块）

```yaml
      - name: Cache Godot
        id: cache-godot
        uses: actions/cache@v4
        with:
          path: godot-bin
          key: godot-${{ env.GODOT_VERSION }}

      - name: Download Godot
        if: steps.cache-godot.outputs.cache-hit != 'true'
        run: |
          GODOT_URL="https://github.com/godotengine/godot/releases/download/\
            ${{ env.GODOT_VERSION }}/\
            Godot_v${{ env.GODOT_VERSION }}_mono_linux_x86_64.zip"
          curl -sL "$GODOT_URL" -o godot.zip
          unzip -q godot.zip -d godot-bin

      - name: Export .pck
        run: |
          GODOT=$(find godot-bin -name "Godot*" -type f -executable | head -1)
          chmod +x "$GODOT"
          "$GODOT" --headless --export-pack "BasicExport" MyMod.pck
```

> 需要 `export_presets.cfg` 和 `project.godot` 在项目根目录，见 [export-presets.md](export-presets.md) 和 [project-godot.md](project-godot.md)。

---

## 四、打包分发的产物

| 产物 | 内容 | 说明 |
|------|------|------|
| `MyMod.dll` | 编译好的 C# DLL | 放入游戏 `Mods/MyMod/` |
| `MyMod.json` | 模组清单 | 同上 |
| `MyMod.pck` | 资源包（可选） | 有图片/场景等资源时需提供 |

推荐打成 ZIP 发布：

```yaml
      - name: Package
        run: |
          mkdir -p publish/MyMod
          cp MyMod.dll publish/MyMod/
          cp MyMod.json publish/MyMod/
          [ -f MyMod.pck ] && cp MyMod.pck publish/MyMod/
          cd publish && zip -r MyMod.zip MyMod/
```

---

## 五、多模块方案（参考 sts2-shunmod）

如果模组分多个独立模块（如 Core + 角色 + 修改包），需要**先构建 Core**，其余模块通过 `ProjectReference` 引用 Core 的 DLL：

```yaml
jobs:
  build-core:
    steps:
      - name: Build Core
        run: dotnet build Core/Core.csproj -c Release -p:Sts2DataDir=...

      - name: Upload Core artifact
        uses: actions/upload-artifact@v4
        with:
          name: build-core
          path: Core/bin/Release/Core.dll

  build-modules:
    needs: build-core
    strategy:
      matrix:
        module: [ModA, ModB]
    steps:
      - name: Download Core artifact
        uses: actions/download-artifact@v4
        with:
          name: build-core

      - name: Place Core.dll
        run: |
          mkdir -p ModA/.godot/mono/temp/bin/Release
          cp Core.dll ModA/.godot/mono/temp/bin/Release/

      - name: Build
        run: dotnet build ModA/ModA.csproj -c Release -p:Sts2DataDir=...
```

---

## 六、csproj 兼容 CI 的关键点

csproj 中的 `<Reference>` Condition 确保本地和 CI 两不误：

```xml
<ItemGroup Condition="Exists('$(Sts2DataDir)/sts2.dll')">
    <Reference Include="0Harmony">
        <HintPath>$(Sts2DataDir)/0Harmony.dll</HintPath>
        <Private>false</Private>
    </Reference>
    <Reference Include="sts2">
        <HintPath>$(Sts2DataDir)/sts2.dll</HintPath>
        <Private>false</Private>
    </Reference>
</ItemGroup>
```

- 本地：`Sts2DataDir` 由 `Sts2PathDiscovery.props` 解析
- CI：`Sts2DataDir` 由 `-p:Sts2DataDir=$(realpath deps)` 覆盖

---

## 常见问题

**Q: CI 里 `dotnet build` 报找不到 Godot SDK？**
A: 因为用了 `Godot.NET.Sdk`。需要网络可访问 NuGet，或提前缓存 NuGet 包（actions/cache）。

**Q: 没有 `export_presets.cfg` 能打 PCK 吗？**
A: 不能，`--export-pack` 需要预设文件。要么加 cfg，要么只用 PckPacker。

**Q: 依赖 Release 怎么更新？**
A: 每次游戏升级后从游戏目录复制新的 `sts2.dll` + `0Harmony.dll`，打新标签上传。修改 `DEPS_VERSION` 环境变量。