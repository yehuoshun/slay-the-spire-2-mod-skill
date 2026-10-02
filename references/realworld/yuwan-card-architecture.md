# 工程架构与工具链（YuWanCard 提炼）

## 1. 注册系统（ContentRegistry）

YuWan 自研的自动注册框架，思想与本 skill design-patterns 模式1 一致，多几个工程细节：

1. `RegisterAll(assembly)` 安全扫描（捕获 `ReflectionTypeLoadException`，Android/Mono 兼容）
2. `[Pool]` Attribute 类型立即 `ModHelper.AddModelToPool`；事件/先古/球/怪/附魔/单例/角色类型先收集
3. `ModelDb.Init` 阶段（Patch 内）创建规范实例并注册到 `CustomEventRegistry` 等
4. `Freeze()` 冻结：`ModelDb.Init` 后阻止晚期注册（AddModel 变 no-op + 警告）

工程价值：**注册时机分层**（扫描→延迟到 Init→冻结）比一次性注册稳，纯原生可抄。

## 2. ModPatcher.PatchAllSafe

逐个类 try-catch 的 `PatchAll` 替代品——某个 patch 类型加载失败只跳过该类型，不中断整个初始化。Android/Mono AOT 下关键（静态构造函数 NRE 会炸 PatchAll）。外加「手动排除集合」：平台条件 patch 不批量打，单独 `ApplySingle` 包 try-catch。

## 3. 多版本 loader（重点）

游戏大版本 API 变化（0.110.x 给伤害 Hook 加 `CardPlay` 参数、`BeforeTurnEnd`→`BeforeSideTurnEnd`）会让内容 DLL 跨版本失效。loader 机制让**一个安装包覆盖多版本**：

```
mods/YuWanCard/
├─ YuWanCard.dll          ← loader（[ModInitializer]，只引用极稳定 API）
├─ YuWanCard.json         ← 清单含 min_game_version
├─ yuwan-variants.manifest← 变体清单（sha256 + compat-target）
└─ lib/
   ├─ 0.107.1/YuWanCard.Content.dll
   └─ 0.110.0/YuWanCard.Content.dll
```

流程：loader `Initialize()` → 装 2 个 Harmony 桥（`ReflectionHelper.ModTypes` 追加变体类型，游戏才能发现变体模型；`PatchAll` 跳过坏类型）→ 解析宿主版本 → 读 manifest 选「≤宿主版本的最新变体」→ `AssemblyLoadContext.LoadFromAssemblyPath` 载入 → 反射调内容 `[ModInitializer]`。

**注意事项**：
- loader 只引用极稳定 API（`ModInitializerAttribute`、`Logger`、`ReleaseInfoManager`、`ReflectionHelper`、`ModManager`、Harmony）
- 内容 DLL 不在 mod 根目录 → 根目录需向上回溯定位（找 `YuWanCard.json`）
- 构建：`build-variants.ps1` 对每个版本快照编一次内容变体；开发期单变体 `dotnet build` 自动部署

## 4. ModInterop（Harmony Transpiler 互操作）

编译期零依赖调用其他 mod 的 API：定义「存根类」→ `[ModInterop("目标modId")]` + `[InteropTarget("命名空间.类型", "成员名")]` → 初始化时 `ModInteropProcessor.Process(harmony, assembly)` 扫描 → 目标 mod 已加载则 Transpiler 把存根方法体替换为对目标 API 的直接 IL 调用；未加载则空实现 fallback。比反射调用性能好、比硬引用不会崩。

## 5. 构建工具链（构建期依赖，非运行时）

| 工具 | 作用 |
|------|------|
| `Alchyr.Sts2.ModAnalyzers` | 官方注册源码生成器（编译期扫描生成注册代码） |
| `Krafs.Publicizer` | 把 sts2.dll 的 internal 成员 publicize，mod 可直接访问游戏内部 API |
| `BSchneppe.StS2.PckPacker` | 构建时打包 Godot `.pck` 资源 |
| `Sts2PathDiscovery.props` | 跨平台自动探测游戏路径（Windows 注册表 AppID 2868840 / Linux `~/.local/share/Steam/steamapps` / macOS），本地 `local.props` 覆盖 |
| `Godot.NET.Sdk/4.5.1` | 项目 SDK |

`Publicize` 项：`<Publicize Include="sts2" IncludeVirtualMembers="false" IncludeCompilerGeneratedMembers="false" />`——只 publicize 非 virtual 成员，减少碰撞。

## 6. mod.json 字段补充

`min_game_version`（最低游戏版本）、`has_pck`、`has_dll`、`affects_gameplay`——skill 清单 JSON 文档未覆盖 `min_game_version`。

## 7. 移动端兼容

Android/iOS 上部分 Patch 触发游戏类型静态构造函数 NRE（Mono AOT）→ 平台检测（`Godot.OS.GetName()`）后**桌面独有 patch 单独应用**；本地化前缀丢失用 `LocTable.GetRawText` 异常重试兜底。
