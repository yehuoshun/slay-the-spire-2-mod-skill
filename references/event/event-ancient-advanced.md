# 先古之民事件：纯原生注册辅助

## 进阶：纯原生注册辅助

> 从 BaseLib 提炼，零第三方依赖。先古之民走 Patch 注入，用 Attribute 标记 + 反射统一收集。

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RegisteredAncientAttribute : Attribute { }

[HarmonyPatch(typeof(Hive), nameof(Hive.AllAncients), MethodType.Getter)]
public static class AllAncientsPatch
{
    private static void Postfix(ref IEnumerable<AncientEventModel> __result)
    {
        var registered = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<RegisteredAncientAttribute>() != null)
            .Select(t => (AncientEventModel)Activator.CreateInstance(t)!);
        __result = __result.Concat(registered);
    }
}
```

```csharp
[RegisteredAncient]
public class MyAncient : AncientEventModel { ... }   // 自动注册
```

## Act 1 先古：原生层的三个坑（BaseLib v3.4.8 修正）

**① Act 1 的共享先古表是 null。** `ActModel` 上那个「本章所有先古」的私有字段（`_sharedAncientSubset`）只有 Act 2/3 才有值，Act 1 为 null。按「null 就 return」的写法注入，自定义先古永远进不了 Act 1。正确做法是把它当工作列表：`var workingList = field ?? [];` 填完再仅当原字段为 null 且 `workingList.Count > 0` 时回写 `field = workingList`（走 `AccessTools.Field(typeof(ActModel), "_sharedAncientSubset")` 读写；字段被改名即静默失效，升级必查）。另外用 `AccessTools.Field(typeof(ActModel), "_rooms")` 读 `RoomSet.HasAncient` 去重，别重复注入。

**② Act 1 先古要复刻涅奥的「先清零再治疗」。** 玩家进入 Act 1 先古事件时按「从 0 开始回血」处理，否则满血进事件就吃不到治疗：

```csharp
protected override async Task BeforeEventStarted(bool isPreFinished)
{
    if (!isPreFinished && HealFromZero && Owner != null)
        Owner.Creature.SetCurrentHpInternal(0M);   // 先归零
    await base.BeforeEventStarted(isPreFinished);  // base 负责真正治疗，顺序不可颠倒
    TaskHelper.RunSafely(NRun.Instance.GlobalUi.TopBar.Hp.LerpAtNeow()); // 纯视觉，不 await
}
```

`HealFromZero` 的自判据是 `Owner?.RunState.CurrentActIndex == 0`。覆写成 true 却不在 Act 1 会让玩家先归零，务必配「只在 Act 1 有效」的过滤。

**③ 带 modifier 的局不生成自定义先古。** 生成房间时若 `CurrentGeneratingRunState.State?.Modifiers.Count > 0` 且 `act.Index == 0`，直接放弃注入——自定难度/词条的局里 Act 1 先古池是别人管的，硬塞会打架。

**版本分档**：`GenerateRooms` 在 beta 与正式版有两个签名（尾部多一个 bool），跨版本补丁用 `DeclaredMethod("GenerateRooms", [typeof(Rng), typeof(UnlockState), typeof(bool)]) ?? DeclaredMethod(..., [typeof(Rng), typeof(UnlockState), typeof(bool), typeof(bool)])` 兜底。强制出场则在该方法的 Postfix 里，当 `RoomSet.HasAncient` 为真时按 `ShouldForceSpawn(act, rngChosenAncient)` 覆盖 `roomSet.Ancient`。

**1-based 编号**：`ActModel.Index` 是从 0 开始的地图索引，写「第几章」判断用 `actModel.Index >= 0 ? actModel.Index + 1 : actModel.Index`。

