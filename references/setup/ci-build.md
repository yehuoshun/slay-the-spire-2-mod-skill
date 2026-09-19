# CI 构建与打包

> 学自 [yehuoshun/sts2-shunmod](https://github.com/yehuoshun/STS2-ShunMod) 的 `ci.yml`。纯 GitHub Actions 流水线，零本地环境依赖。

---

## 章节导航

| 内容 | 文件 |
|------|------|
| PCK 打包（PckPacker / Godot 导出） | [ci-build-pck.md](ci-build-pck.md) |
| 打包分发与多模块方案 | [ci-build-package.md](ci-build-package.md) |

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

## 三、csproj 兼容 CI 的关键点

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