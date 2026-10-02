# 多版本兼容：loader + 变体机制

> 实战项目验证（YuWanCard）。游戏大版本更新时 `sts2.dll` API 会变（如 0.110.x 给伤害修正 Hook 加 `CardPlay` 参数、`BeforeTurnEnd`→`BeforeSideTurnEnd`），按旧版编译的内容 DLL 在新版可能无法加载。loader 机制让**一个安装包同时覆盖多个游戏版本**。

## 目录结构

```
mods/MyMod/
├─ MyMod.dll              ← loader（游戏入口，带 [ModInitializer]，只引用极稳定 API）
├─ MyMod.json             ← 清单含 min_game_version
├─ MyMod.pck              ← Godot 资源包（各变体共享）
├─ variants.manifest      ← 变体清单（sha256 + compat-target）
└─ lib/
   ├─ 0.107.1/MyMod.Content.dll
   └─ 0.110.0/MyMod.Content.dll   ← 每个游戏版本一个内容变体
```

## 加载流程

1. 游戏加载 loader DLL（`[ModInitializer]`）
2. loader 先装 2 个 Harmony 桥：
   - `ReflectionHelper.ModTypes` 追加内容变体类型（游戏只扫 `mod.assembly` = loader，不追加就发现不了变体模型）
   - `PatchAll` 遇到加载不了的 patch 类型跳过而非中断（见 [harmony-patches.md](../harmony/harmony-patches.md) PatchAllSafe）
3. 解析宿主版本（`ReleaseInfoManager` 主源，回退 `release_info.json` 等）
4. 读 manifest，选「≤ 宿主版本的最新变体」；无匹配用最新变体兜底
5. `AssemblyLoadContext.LoadFromAssemblyPath` 载入内容 DLL，反射调内容 `[ModInitializer]`

## 硬约束

- **loader 只引用极稳定 API**：`ModInitializerAttribute`、`Logger`、`ReleaseInfoManager`、`ReflectionHelper`、`ModManager`、Harmony——不要引用易变 API
- 内容 DLL 在 `lib/<version>/` 下，mod 根目录（找 `MyMod.json`）需**向上回溯定位**
- 单程序集只能 override 一种签名，同时支持两版差异 API 需条件编译（`#if COMPAT_110`）；**建议默认只维护当前游戏版本**，大更新时一次性适配

## 构建

- 开发期单变体：`dotnet build` 自动部署到 `mods/MyMod/lib/<当前版本>/` 并重生成 manifest
- 多版本：每版本一个 `sts2.dll` 快照目录 + 构建脚本逐个编译部署（loader 用**最老**快照编译，保证跨版本可加载）
