# 多版本变体 Loader（一个 loader 按游戏版本选 DLL）

> 实战验证（sts2mod 集成战略事件 IntegratedStrategyEvents v0.5.6，2026-10-07）。同一模组包支持 0.107.1 / 0.110.1 / 0.111.0：**loader 常驻、实现按版本变体**。对比 [loader-variants.md](loader-variants.md)（BepInEx vs 原生单版本）。

## 目录与变体清单

```text
mods/IntegratedStrategyEvents/
  IntegratedStrategyEvents.Loader.dll    # loader（常驻，游戏直接加载）
  IntegratedStrategyEvents.json           # manifest
  lib/0.107.1/IntegratedStrategyEvents.dll   # 各版本变体实现
  lib/0.110.1/IntegratedStrategyEvents.dll
  lib/0.111.0/IntegratedStrategyEvents.dll
  compat-target.txt
```

- loader 用 `integrated-strategy-events-variants.manifest`（或扫描 lib/ 目录）列候选变体，每个候选带 `CompatTarget`。

## 加载流程

```csharp
public static void Initialize()
{
    // 1. 解析主机游戏版本
    HostVersionSnapshot host = ResolveHostVersion();
    // 2. 挑兼容变体（没有匹配 → 抛「No compatible variant for host ...」）
    VariantCandidate? variant = PickVariant(loaderDirectory, libRoot, host.Numeric);
    // 3. 用 loader 自己的 AssemblyLoadContext 加载变体 DLL
    AssemblyLoadContext context = AssemblyLoadContext.GetLoadContext(typeof(LoaderBootstrap).Assembly)
        ?? AssemblyLoadContext.Default;
    Assembly implementation = context.LoadFromAssemblyPath(variant.DllPath);
    // 4. 校验变体
    ValidateVariantAssembly(implementation, variant);
    // 5. 关联到游戏 Mod 系统 + 反射调真实 ModInitializer
    if (AssociateVariantAssemblyWithGame(implementation))
        InvokeRealInitializer(implementation);
}
```

## 变体验证（双重身份校验）

```csharp
private static void ValidateVariantAssembly(Assembly assembly, VariantCandidate variant)
{
    // ① 程序集名必须固定（游戏按名字找模型/注册）
    if (!string.Equals(assembly.GetName().Name, "IntegratedStrategyEvents", StringComparison.Ordinal))
        throw new BadImageFormatException($"Variant identity is ..., expected IntegratedStrategyEvents.");
    // ② 内嵌 AssemblyMetadata 的 CompatibilityTarget 必须与候选一致（防目录错配）
    string? embeddedTarget = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == CompatTargetMetadataKey)?.Value;
    if (!string.Equals(embeddedTarget, variant.CompatTarget, StringComparison.Ordinal))
        throw new BadImageFormatException(...);
}
```

- 编译侧（主项目 csproj）：`AssemblyMetadata Include="IntegratedStrategyEventsCompatibilityTarget" Value="$(IntegratedStrategySts2Target)"`——版本符号进元数据。

## 关联到游戏 Mod 系统（反射桥）

```csharp
// ModManager.AssociateAssemblyWithMod(string, Assembly) 反射调用——把变体程序集挂到 mod 名下
// 旧版路径：Mod.assemblies 字段 / Mod.assembly 字段（Legacy）
// 找不到时降级：订阅 ModManager.OnModDetected（OnLegacyModDetected）等 mod 出现再关联
```

- 关键：loader 自己的程序集和变体程序集都要被游戏识别为「IntegratedStrategyEvents」mod 的一部分（模型注册/存档序列化按 mod id 走）。

## 设计要点

| 点 | 说明 |
|----|------|
| 变体不含 loader 逻辑 | loader 永远用游戏兼容的最小 API 面（抗版本漂移）；变体随便用新 API |
| 失败即抛 | loader 加载失败直接 Log.Error + rethrow（不能静默，玩家能看见问题） |
| 单 DLL 布局不变 | 每个变体仍是单 DLL（对游戏入口友好），多版本=多目录各一份 |
| 存档兼容 | 模型清单变化=联机契约变化（CHANGELOG 注明「不能与 0.5.5 混版本联机」） |

## 相关 API 速查

| API | 说明 |
|-----|------|
| `AssemblyLoadContext.LoadFromAssemblyPath` | 加载变体 DLL（用 loader 自己的 context） |
| `AssemblyMetadataAttribute` | 版本兼容目标元数据 |
| `ModManager.AssociateAssemblyWithMod(string, Assembly)` | 程序集→mod 关联（真实 API，可反射） |
| `ModManager.OnModDetected` | 延迟关联兜底 |