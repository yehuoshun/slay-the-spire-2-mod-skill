# MonoMod RuntimeDetour（DetourHook）模式

> 实战验证（sts2mod 更多进阶挑战 MoreAscensionChallenge v0.1.1，2026-10-07）。本仓库唯一用 **MonoMod.RuntimeDetour**（非 Harmony）的 mod。Harmony 之外的第二种运行时改写方案。

## 基本形态

```csharp
using DetourHook = MonoMod.RuntimeDetour.Hook;

// ① 定义原方法签名的委托（参数与目标方法一致，无 __instance）
private delegate void OrigSetMaxAscension(NAscensionPanel self, int maxAscension);
private static DetourHook? _setMaxAscensionHook;

// ② detour 方法：第一个参数是 orig 委托（类型 = 上面定义的 delegate），后续参数与原方法一致
private static void SetMaxAscensionDetour(OrigSetMaxAscension orig, NAscensionPanel self, int maxAscension)
{
    if (AllowsExtraAscensionSelection(self))
    {
        maxAscension = MaxExtraAscension;   // 改参数
    }

    orig(self, maxAscension);               // 调原方法（手动控制是否/何时调用）
}

// ③ 安装：new DetourHook(目标方法, detour 方法组)（detour 用方法名匹配 delegate 签名）
_setMaxAscensionHook = new DetourHook(
    RequireMethod(typeof(NAscensionPanel), nameof(NAscensionPanel.SetMaxAscension),
        BindingFlags.Instance | BindingFlags.Public, typeof(int)),
    SetMaxAscensionDetour);
```

- **detour 签名约定**：`(OrigDelegate orig, 原参数...)`——orig 委托的类型决定方法匹配；返回类型与原方法一致。
- 与原版 Harmony 的差异：没有 prefix/postfix 概念，一个 detour 全权接管；手动调 `orig(...)`（可改参、可替换逻辑、可多次调用）。
- 安装即全局生效；`DetourHook` 字段持有引用防 GC（detour 生命周期=字段生命周期）。

## 全重写模式（序列化/反序列化）

```csharp
private delegate void OrigClientLobbyJoinResponseSerialize(ref ClientLobbyJoinResponseMessage self, PacketWriter writer);
private static void ClientLobbyJoinResponseSerializeDetour(OrigClientLobbyJoinResponseSerialize orig,
    ref ClientLobbyJoinResponseMessage self, PacketWriter writer)
{
    // 完全重写：不调 orig，自己写全部字段（原版格式 + 需要的额外字段）
    writer.WriteList(self.playersInLobby, 3);
    writer.WriteBool(self.dailyTime.HasValue);
    if (self.dailyTime.HasValue) writer.Write(self.dailyTime.Value);
    writer.WriteBool(self.seed != null);
    if (self.seed != null) writer.WriteString(self.seed);
    writer.WriteInt(self.ascension);
    writer.WriteList(self.modifiers);
}
// Deserialize 同理：完整 Read 一遍（必须与原版写入顺序完全一致）
```

- **多人消息格式改动 = 序列化/反序列化必须成对全重写**（原版消息没有「额外进阶」字段，要加就得接管整个读写）。
- ref 参数（`ref ClientLobbyJoinResponseMessage self`）：detour 委托支持 ref struct 消息类型。

## 依赖 DLL 预加载（MonoMod 运行时依赖）

```csharp
private static void PreloadDependencyAssemblies()
{
    var loadContext = AssemblyLoadContext.GetLoadContext(asm) ?? AssemblyLoadContext.Default;
    foreach (var dllPath in Directory.GetFiles(modDirectory, "*.dll"))
    {
        if (dllPath == selfPath) continue;
        loadContext.LoadFromAssemblyPath(dllPath);   // MonoMod.RuntimeDetour.dll 等随包 DLL 预加载
    }
}
```

- MonoMod 系列 DLL 不随游戏分发 → 必须随 mod 打包并在初始化时用 `AssemblyLoadContext.LoadFromAssemblyPath` 预加载，否则类型解析失败。

## Harmony vs MonoMod 选型

| 维度 | Harmony | MonoMod Detour |
|------|---------|----------------|
| 侵入性 | prefix/postfix 组合，精细 | 一个 detour 全权接管（含 orig 手动调用） |
| 多 mod 协同 | Priority + 短路 | 无内建优先级（同点多个 detour 顺序不保证） |
| 适合 | 大部分场景 | 全重写/改参/多次调用场景 |
| 生态 | 游戏自带 0Harmony.dll | 需随包带 MonoMod 依赖 + 预加载 |

> 建议：默认 Harmony（skill 主推）；仅当需要「完整接管 + 手动控制原方法调用时机」（如消息序列化改写）时考虑 MonoMod。