# Harmony 补丁模式


> 参考：BaseLib 源码 `Utils/Patching/` + STS2Plus 源码 `Patches/`


## 章节导航

| 内容 | 文件 |
|------|------|
| 基础 Patch 类型与参数 | [harmony-basics.md](harmony-basics.md) |
| PatchCategory 与安全模式 | [harmony-patches.md](harmony-patches.md) |
| 组织方式与常用目标 | [harmony-guide.md](harmony-guide.md) |

## 概述

Harmony 是 STS2 模组开发的必需品。几乎所有自定义效果都要通过 Patch 修改游戏行为。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| Patch 不生效 | 检查 `harmony.PatchAll()` 或 `PatchCategory()` 是否调用 |
| 一个类炸了全挂 | 每个 Patch 类别独立 try-catch |
| 找不到目标方法 | 使用 `RuntimeTypeResolver.FindType()` 反射查找 |
| 类型不匹配 | 用 `AccessTools` 而非直接写类型 |
| Postfix 修改返回值无效 | 参数声明为 `ref int __result` |
| 多人模式下崩溃 | 用 `MultiplayerSafety` 检查后再 Patch |

---

## 演进路线

- 当前：手动 `harmony.PatchAll()` 全量加载
- 更优：`[HarmonyPatchCategory]` + `PatchCategory()` 分类批量加载
