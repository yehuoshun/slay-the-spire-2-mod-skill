# Transpiler：泛型匹配与确定性排序

> 实战验证（YuWanCard 2026-10-04 增量：序列化缓存去重排序补丁）。

> ⚠️ 命名空间：**`OpCodes` 在 `System.Reflection.Emit`**（Harmony 2.x 的 `CodeInstruction.opcode` 是 `System.Reflection.Emit.OpCode`，**不是** Mono.Cecil），`CodeInstruction`/`HarmonyPatch`/`AccessTools` 在 `HarmonyLib`。写 `using Mono.Cecil.Cil;` 会报 CS0122 `OpCodes is inaccessible`（2026-10-04 CI 真实报错、已回流）。

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

### 插入而非替换：`Dup` 保留原调用结果

想在某个原生调用「之后」再叠一层自己的判定，别直接改 `instruction.operand`：

```csharp
if (code[i].Calls(original))
{
    code.Insert(i++, new CodeInstruction(OpCodes.Dup));                    // 复制原生结果
    code.Insert(++i, new CodeInstruction(OpCodes.Call, combineMethod));    // 与自己的判定合并
}
```

`combineMethod` 签名形如 `static bool Combine(SomeEnum value, bool originalResult) => MyRegistry.Contains(value) || originalResult;`，并加 `[MethodImpl(MethodImplOptions.NoInlining)]`（否则 JIT 内联后 IL 指纹可能对不上，也便于浮点数/枚举边界调试）。

**为什么不能替换**：把 operand 换成自己的方法等于抹掉原生结果，其它 mod 基于同一个调用打的补丁全部失效，而且这类冲突只在装了别的 mod 时才复现，极难查。`Dup` 保留原结果再或上自己的判定，天然兼容。

注意索引位移：`Insert(i++, dup)` 之后原指令整体后移，第二次插入用 `++i` 才能落在正确位置；改完用 `Harmony.Debug` 或打印 `code` 长度自检一遍。

## 演进路线

- 2026-10-04 新增（YuWanCard 增量：泛型 operand 结构匹配 + 排序确定性）。
