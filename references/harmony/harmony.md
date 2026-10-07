# Harmony 补丁模式


> 参考：BaseLib 源码 `Utils/Patching/` + STS2Plus 源码 `Patches/`


## 章节导航

| 内容 | 文件 |
|------|------|
| 基础 Patch 类型与参数 | [harmony-basics.md](harmony-basics.md) |
| PatchCategory 与安全模式 | [harmony-patches.md](harmony-patches.md) |
| AsyncLocal 异步 Patch | [harmony-async-local.md](harmony-async-local.md) |
| Transpiler：泛型匹配与确定性排序 | [harmony-transpiler.md](harmony-transpiler.md) |
| 组织方式与常用目标 | [harmony-guide.md](harmony-guide.md) |
| 属性式 Patch 框架（元数据/门控/共享检测） | [harmony-attribute-patcher.md](harmony-attribute-patcher.md) |
| Transpiler 实战：自定义能力音效（导航） | [harmony-custom-power-sfx.md](harmony-custom-power-sfx.md) |
| ├ 接口 + Transpiler | [harmony-custom-power-sfx-core.md](harmony-custom-power-sfx-core.md) |
| └ 使用 + 常见问题 | [harmony-custom-power-sfx-usage.md](harmony-custom-power-sfx-usage.md) |

## 概述

Harmony 是 STS2 模组开发的必需品。几乎所有自定义效果都要通过 Patch 修改游戏行为。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| Patch 不生效 | 检查 `harmony.PatchAll()` 或 `PatchCategory()` 是否调用 |
| 一个类炸了全挂 | 每个 Patch 类别独立 try-catch |
| 找不到目标方法 | 使用 `AccessTools.TypeByName()`（或自研 RuntimeTypeResolver 封装）反射查找 |
| 类型不匹配 | 用 `AccessTools` 而非直接写类型 |
| Postfix 修改返回值无效 | 参数声明为 `ref int __result` |
| Transpiler 匹配不到泛型调用 | operand 是闭合实例，按 `DeclaringType.GetGenericTypeDefinition()` 结构判定 |
| Transpiler 报 `OpCodes` 不存在/不可访问 | 补 `using System.Reflection.Emit;`（**不是** Mono.Cecil；CodeInstruction.opcode 用 System 的 OpCode） |
| 多人模式下崩溃 | 用 `MultiplayerSafety` 检查后再 Patch |

---

## 演进路线

- 当前：手动 `harmony.PatchAll()` 全量加载
- 更优：`[HarmonyPatchCategory]` + `PatchCategory()` 分类批量加载
