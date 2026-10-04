# Transpiler：泛型匹配与确定性排序

> 实战验证（YuWanCard 2026-10-04 增量：序列化缓存去重排序补丁）。

## 1. 匹配泛型方法：用结构判定，别用引用相等

定位"要替换的调用"时，**不要**拿 `AccessTools.Method` 的 `MethodInfo` 去和指令 `operand` 比引用：

```csharp
// ❌ 脆弱：IL 里 operand 是「已闭合泛型实例」，与开放泛型 MemberInfo 不 Equals
private static readonly MethodInfo SortMethod = AccessTools.Method(
    typeof(List<(Type, Mod)>), nameof(List<(Type, Mod)>.Sort), [typeof(Comparison<(Type, Mod)>)])!;
if (instruction.opcode == OpCodes.Callvirt && Equals(instruction.operand, SortMethod)) { ... }
```

改为按**方法名 + 泛型定义**判定，跨任意实例都命中：

```csharp
// ✅ 结构判定
if (instruction.opcode == OpCodes.Callvirt
    && instruction.operand is MethodInfo method
    && method.Name == nameof(List<(Type, Mod)>.Sort)
    && method.DeclaringType?.IsGenericType == true
    && method.DeclaringType.GetGenericTypeDefinition() == typeof(List<>))
{
    instruction.opcode = OpCodes.Call;          // 静态替换实现：callvirt → call
    instruction.operand = ReplacementMethod;
}
```

> 要点：`List<T>` / `Dictionary<K,V>` 等泛型方法的 IL operand 是**闭合实例**，与 `AccessTools.Method` 的开放泛型不相等。判 `DeclaringType.GetGenericTypeDefinition()` 最稳。

## 2. 自定义 Comparison：相等必须返回 0

替换 `List<T>.Sort(Comparison<T>)` 时，若自定义比较器对"业务键相等"的两项返回非 0，会打乱顺序，且**各端结果可能不一致**（索引映射错位 → 联机 desync）。相等键一律返回 0，保持稳定：

```csharp
entries.Sort((left, right) =>
    string.Equals(GetTypeIdentityKey(left.Type), GetTypeIdentityKey(right.Type), StringComparison.Ordinal)
        ? 0
        : comparison(left, right));
```

## 3. 什么时候才上 Transpiler

Transpiler 最脆（依赖 IL 形态，游戏更新易失效）。能用 Postfix（改返回值）/ Prefix（跳过原逻辑）解决就别上；必须改方法体指令时才用，并在 `## 演进路线` 备注脆弱点。

## 演进路线

- 2026-10-04 新增（YuWanCard 增量：泛型 operand 结构匹配 + 排序确定性）。
