# Async 状态机 Transpiler（改硬编码常量）+ Prefix 短路协同

> 实战验证（sts2mod 手牌上限解除 RemoveHandLimit v0.3.1，2026-10-07）。两类 Harmony 高阶技巧：① async 方法里的常量调用怎么用 transpiler 替换；② 多个 mod 打同一 prefix 时怎么用 Priority + 短路协同。

## ① Async 方法 Transpiler（打 MoveNext 而不是方法本体）

```csharp
private static MethodBase GetAsyncStateMachineTarget(MethodInfo method)
{
    AsyncStateMachineAttribute? attribute = method.GetCustomAttribute<AsyncStateMachineAttribute>();
    if (attribute?.StateMachineType == null) return method;   // 非 async：直接打本体
    MethodInfo? moveNext = attribute.StateMachineType
        .GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    return moveNext ?? throw new InvalidOperationException("Could not find MoveNext");
}
// 使用：harmony.Patch(GetAsyncStateMachineTarget(drawMethod), transpiler: ...)
```

- **async 方法的 IL 在编译器生成的 `<X>d__N.MoveNext` 里**，直接 patch 原方法只有壳；必须反射到状态机类型取 MoveNext。
- 改造逻辑：把方法体内对 `CardPile.MaxCardsInHand` getter 的 `call` 指令替换为常量：

```csharp
private static IEnumerable<CodeInstruction> ReplaceMaxCardsInHandCalls(
    IEnumerable<CodeInstruction> instructions, int expectedCount, MethodBase originalMethod)
{
    List<CodeInstruction> patched = instructions.ToList();
    int count = 0;
    foreach (CodeInstruction instruction in patched)
    {
        if (!instruction.Calls(MaxCardsInHandGetterMethod)) continue;
        instruction.opcode = OpCodes.Ldc_I4;   // call → 推入常量
        instruction.operand = HandLimit;
        count++;
    }

    if (count != expectedCount)   // 数量断言：版本变化导致调用数变化 → 立刻显形
    {
        throw new InvalidOperationException($"Expected {expectedCount} calls, patched {count}.");
    }

    return patched;
}
// 用法：Add 期望 1 处、Draw 期望 3 处、CheckCanDraw 期望 1 处
```

- `CodeInstruction.Calls(MethodInfo)`：精确匹配 call 指令调用的方法。
- **expectedCount 断言**：transpiler 最怕版本更新后调用点消失/增加，静默失效；断言数量让启动直接报错显形。

## ② 双版本签名查找（0.108 新增参数 → 退回旧签名）

```csharp
MethodInfo addCardsMethod =
    typeof(CardPileCmd).GetMethod(nameof(CardPileCmd.Add), BindingFlags.Public | BindingFlags.Static,
        null, new[] { typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition),
                      typeof(AbstractModel), typeof(bool), typeof(bool) }, null)          // 0.108+：多一个 bool
    ?? RequireMethod(typeof(CardPileCmd), nameof(CardPileCmd.Add),
        BindingFlags.Public | BindingFlags.Static,
        typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition),
        typeof(AbstractModel), typeof(bool));                                              // 0.107 签名
```

- `Type.GetMethod(name, flags, binder, types, modifiers)` 带显式参数数组精确重载；找不到返回 null → `??` 退回旧签名。

## ③ Prefix 短路协同（多 mod 撞同一 prefix）

```csharp
// RitsuLib 也给 HandPosHelper.GetPosition 打"超10张单排压缩" prefix；默认优先级下会盖掉双排布局
harmony.Patch(handPosGetPosition,
    prefix: new HarmonyMethod(typeof(ModEntry), nameof(GetPositionPrefix)) { priority = Priority.High });

private static bool GetPositionPrefix(int handSize, int cardIndex, ref Vector2 __result)
{
    if (handSize <= RowLimit) return true;    // 放行原版（及后续 prefix）
    __result = /* 双排计算 */;
    return false;                              // 短路：跳过原版方法体 + 后续 prefix
}
```

- **Harmony prefix 执行序按 Priority 降序**：Priority.High 的先跑。
- **prefix 返回 false = 短路整条链路**（跳过剩余 prefix + 原方法体）——高优先级先决断，低优先级的同类 prefix 不会执行，天然解决撞车。
- 这是「我的实现优先于其他 mod」的通用姿势：需要抢占的行为一律 Priority.First/High + 返回 false。

## 相关 API 速查

| API | 说明 |
|-----|------|
| `AsyncStateMachineAttribute.StateMachineType` | async 状态机类型（`System.Runtime.CompilerServices`） |
| `CodeInstruction.Calls(MethodInfo)` / `opcode` / `operand` | IL 指令检查与改写 |
| `OpCodes.Ldc_I4` | 推入 int32 常量 |
| `HarmonyPriority` / `priority = Priority.High` | prefix/postfix 优先级 |
| `Type.GetMethod(name, flags, binder, types, modifiers)` | 精确重载查找（可返回 null） |