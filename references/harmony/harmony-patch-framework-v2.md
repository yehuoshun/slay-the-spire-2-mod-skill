# 属性式 Patch 框架 v2（IL 指纹守卫 + 冲突拓扑分析）

> 实战验证（sts2mod 更好的角色遗物 BetterCharacterRelics v1.1.4，2026-10-07）。框架升级版（对比 [harmony-attribute-patcher.md](harmony-attribute-patcher.md) v1）：版本守卫从「成员存在性」升级为「**IL 指纹冻结**」，冲突检测从「共享列表」升级为「**拓扑跳过分析**」。

## ① 更严格的声明与应用（DeclaredOnly + 契约检查）

```csharp
// 目标查找 DeclaredOnly：不向基类回退（原版删 override 时不会意外扩大目标）
MethodInfo? target = declaringType.GetMethod(name,
    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly,
    null, info.argumentTypes ?? Type.EmptyTypes, null);
// prefix handler 必须返回 bool（契约）
if (handler.IsDefined(typeof(HarmonyPrefix)) && handler.ReturnType != typeof(bool))
    throw new InvalidOperationException("Unexpected prefix contract");
// 结果必须恰好 1 个 target
var result = harmony.CreateClassProcessor(type).Patch();
if (result == null || result.Count != 1) throw new InvalidOperationException("Expected exactly one patched target");
```

- duplicate patch ID 检查（HashSet）；失败计数启动汇总。
- v1 允许「声明了但没目标」静默跳过；v2 全 fail-fast。

## ② VanillaGuard：IL 指纹版本守卫（冻结原版方法体）

```csharp
private static string Fingerprint(MethodInfo method)
{
    // async 入口只创建状态机；结算逻辑在 MoveNext，必须一同冻结
    MethodInfo? moveNext = method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType
        .GetMethod("MoveNext", ...);
    string bodies = string.Join("|", new[] { method, moveNext }.Where(i => i != null)
        .Select(i => Convert.ToHexString(i!.GetMethodBody()?.GetILAsByteArray() ?? [])));
    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(bodies)));
}
internal static void Verify(MethodInfo target)
{
    if (!ReadFrozen().TryGetValue(Key(target), out string? expected))
        throw new InvalidOperationException("Missing frozen target");   // 快照没有 = 版本结构变了
    if (expected != Fingerprint(target))
        Log.Warn($"[Guard] DRIFT {Key(target)}");   // 方法体变 = 降级警告（不炸）
}
```

- **嵌入快照**：`Assembly.GetManifestResourceStream("vanilla-il.txt")`（构建时生成：`完整签名=IL哈希` 每行；泛型 FullName 含程序集标识，按行末 `=` 切分）。
- 语义：签名对但方法体变（悄悄改逻辑）→ DRIFT 警告；签名都没了 → fail-fast。

## ③ BaseModel 路由校验（patch AbstractModel 时的安全网）

```csharp
// 打 AbstractModel 基类方法时，白名单 consumer 类型必须仍路由到同一方法
foreach (Type consumer in consumers)
{
    MethodInfo? route = consumer.GetMethod(declaration.Target.Name, paramTypes);
    if (route == null || route.Module != declaration.Target.Module || route.MetadataToken != declaration.Target.MetadataToken)
        throw new InvalidOperationException($"Base hook route changed for {consumer.Name}.{name}");
}
```

- 场景：patch `AbstractModel.AfterEnergyResetLate` 影响 DivineRight/Destiny——若原版让 consumer override 掉路由，patch 静默失联；**MetadataToken 对比**（同一方法 = 同模块内 token 一致）立刻发现。

## ④ 冲突检测 v2：拓扑跳过分析

```csharp
// Harmony 实际拓扑序（含 before/after），不能只看数值优先级
var order = PatchProcessor.GetSortedPatchMethods(target, info.Prefixes.ToArray());
int skipping = order.FindIndex(m => info.Prefixes.Any(p => p.PatchMethod == m
    && p.owner == HarmonyId && m.ReturnType == typeof(bool)));
foreach (var method in order.Skip(skipping + 1))
    foreach (var patch in info.Prefixes.Where(p => p.PatchMethod == method && p.owner != HarmonyId))
        Log.Warn($"{patch.owner}.{method.Name} follows our skipping prefix; may be skipped.");
```

- 自己的 bool prefix 拓扑序靠前且返回 false → **后面其他 mod 的 prefix 被跳过**——启动日志提前告知。
- 动态重检：`ModManager.OnModDetected` 事件，后加载的 mod 补测。

## ⑤ 可选第三方类型解析（观者双版本兼容）

- 两版观者 mod 并存：Specifications 表（遗物 Entry + 完整类型名 + 程序集名）；运行时 Match：`category=="RELIC" && assemblyName=="Watcher"` + FullName 全等。
- `ResolveMantra`：`ModelDb.GetByIdOrNull<PowerModel>` + FullName + Assembly 双重校验；**绝不直接引用**可选类型，没装就不生效；发真言 `applier` 必须自身。

## 相关 API 速查

| API | 说明 |
|-----|------|
| `MethodBase.GetMethodBody()?.GetILAsByteArray()` / `Assembly.GetManifestResourceStream` | IL 指纹素材 / 嵌入快照读取 |
| `ModManager.OnModDetected` | mod 加载事件 |
| `PatchProcessor.GetSortedPatchMethods(target, prefixes)` | Harmony 拓扑排序 |
| `RelicModel.IsMelted/HasBeenRemovedFromState/IsUsedUp` | 遗物状态判定 |