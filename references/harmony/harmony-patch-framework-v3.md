# 属性式 Patch 框架 v3（测试护栏 + AsyncLocal 作用域）

> 实战验证（sts2mod 海克斯符文 HextechRunes，独立仓库 9.2 万行，2026-10-07）。框架终版（对比 [v2](harmony-patch-framework-v2.md)）：补丁元数据成体系 + **CI 测试冻结补丁清单** + AsyncLocal 作用域规范。

## ① 元数据完整版（HextechPatch）

```csharp
internal sealed class HextechPatchAttribute : Attribute
{
    public HextechPatchAttribute(string id, string feature, string content) { ... }
    public string Id { get; }          // 唯一 ID
    public string Feature { get; }     // 功能描述
    public string Content { get; }     // 涉及内容名
    public bool Optional { get; init; }              // 目标缺失只 Info
    public bool CopiesVanillaLogic { get; init; }    // 不跳过原方法、却在 Postfix 复制了原方法内部步骤 → 进原版拷贝守卫
}
// 纯表现补丁（飞踢尸体击飞、音效）不设 Rune=：补丁失败时不把玩法符文标为不可用
```

- 应用方 `HextechPatcher.ApplyAll`：逐类应用、成败可见；**声明了却找不到目标必须抛异常交给 Patcher 归因**（自己吞掉会让启动摘要误报成功）。
- 需要运行时枚举目标的类只带 `[HextechPatch]` + 声明 `static void Apply(Harmony)`。

## ② AsyncLocal 作用域补丁规范（三件套）

```csharp
// 作用域型 prefix（如「只在本 mod 的 PowerCmd.Apply 窗口内生效」）：
// Prefix 入栈 → Postfix 同步出栈并把 ref __state 清零 → Finalizer 只在异常且 __state 仍持有作用域时出栈
private static void Prefix(..., out object? __state) { AsyncLocalScope.Push(...); __state = ...; }
private static void Postfix(..., object? __state)
{
    AsyncLocalScope.Pop();
    __state = null;   // ref __state 清零
}
private static void Finalizer(Exception? __exception, object? __state)
{
    if (__exception != null && AsyncLocalScope.IsActive) AsyncLocalScope.Pop();  // 异常兜底出栈
}
```

- 异步续体只清账（async 方法 Postfix 在 await 点后执行，作用域需 AsyncLocal 穿透）。

## ③ 版本差异（整文件 #if 分部）

- 版本差异写成**整文件 `#if` 的分部文件**（`HextechSavedPropertyBootstrap.Legacy.cs` / `.Official.cs`、`HextechCreatureVisualsCompat.V110.cs` / `.V111.cs`），共享代码不写 `#if`。
- 行内 `#if` 只允许：原版虚方法签名随版本变化的覆写处 + `[HarmonyPatch]` 目标签名随版本变化的特性声明。预处理指令顶格写。

## ④ 三道护栏（CI 测试冻结，非启动日志）

| 护栏 | 内容 |
|------|------|
| `patch_manifest.<target>.txt` | 冻结补丁目标与优先级（`HEXTECH_WRITE_PATCH_MANIFEST=1` 重生成） |
| `static_state_manifest.<target>.txt` | 冻结可变静态字段清单 |
| `vanilla_copy_guard.<target>.txt` | 冻结可跳过原方法的 bool 前缀目标 + `CopiesVanillaLogic` 目标的 **IL 哈希**；异步目标连 MoveNext 一起冻结（入口只是桩，原版改动几乎都落在 MoveNext） |

- 测试只追加缺失行；**漂移行只报告不刷新**（游戏更新后 headless 日志 `[VanillaCopyGuard] DRIFT` 或测试报漂移 → 人工对照原版复核，刷新快照不能代替行为审查）。
- 目标清单来自 0.111.0 导出，各版本行由测试 `VanillaCopyGuardFreezesEntriesAndAsyncBodies` 补齐。

## ⑤ 补丁保留裁决机制

- `docs/architecture.md` 维护「已裁决保留的补丁」清单：每条带**理由 + 涉及类名**（「不要再翻案；要改先拿出新的原版证据」）——防重复评估，也记录「为什么不用官方 Hook」（如 `CardPileCmd.Draw` 检视替换、`RunManager.OnEnded` 补历史、跳过型 prefix 不能改 `ModifyBlock=0` 因为原版 0 格挡仍播演出）。
- 跳过型 prefix 统一登记：**只在本模组内容/本局启用条件下生效**，其余 `return true`；默认 `Priority.Low`；进原版拷贝守卫。

## 相关 API 速查

| API | 说明 |
|-----|------|
| `HarmonyPrepare/Finalizer/__state` | 作用域三件套 |
| `HEXTECH_WRITE_PATCH_MANIFEST=1` | 重生成护栏清单（测试专用） |
| `VanillaCopyGuardFreezesEntriesAndAsyncBodies` | 守卫测试（含 MoveNext 冻结） |