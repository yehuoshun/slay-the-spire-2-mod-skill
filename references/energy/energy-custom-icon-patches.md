# 自定义能量图标：Harmony Patch 实现

> 纯原生实现（零第三方依赖）。

## 大图标 Patch（Prefix）

拦截 `EnergyIconHelper.GetPath`，当 `prefix` 是编码过的自定义池 ID 时返回自定义大图标路径。

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

[HarmonyPatch(typeof(EnergyIconHelper),
    nameof(EnergyIconHelper.GetPath), typeof(string))]
public static class CustomEnergyBigIconPatch
{
    private static bool Prefix(string prefix, ref string __result)
    {
        var pool = EnergyIconHelper.DecodePool<AbstractModel>(prefix);
        if (pool is ICustomEnergyIcon { BigIconPath: string path })
        {
            __result = path;
            return false; // 跳过原生逻辑
        }
        return true; // 不是自定义池，走原生
    }
}
```

> **return false** 表示跳过原方法，`__result` 即是方法的最终返回值。

## 文本内联图标 Patch（Transpiler）

> **注意**：`EnergyIconsFormatter.TryEvaluateFormat` 是 private 类型 + private 方法，用 `TargetMethod` 动态定位。

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

[HarmonyPatch]
public static class CustomEnergyTextIconPatch
{
    // 动态定位 private 类型
    private static MethodBase? TargetMethod()
    {
        var type = RuntimeTypeResolver.FindType(
            "MegaCrit.Sts2.Core.Localization.Formatters." +
            "EnergyIconsFormatter");
        return AccessTools.Method(type, "TryEvaluateFormat");
    }

    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();

        // 找到最后一个 String.Concat(string, string, string) 调用
        // 把拼接结果替换为自定义文本图标路径
        var concatThree = AccessTools.Method(typeof(string),
            nameof(string.Concat),
            [typeof(string), typeof(string), typeof(string)]);

        for (int i = codes.Count - 1; i >= 0; i--)
        {
            if (codes[i].opcode == OpCodes.Call &&
                codes[i].operand is System.Reflection.MethodInfo mi &&
                mi == concatThree &&
                i + 1 < codes.Count &&
                codes[i + 1].opcode == OpCodes.Stloc_3)
            {
                var inject = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_0), // prefix
                    new CodeInstruction(OpCodes.Ldloc_3), // 刚拼好的文本
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(
                            typeof(CustomEnergyTextIconPatch),
                            nameof(ResolveTextIcon))),
                    new CodeInstruction(OpCodes.Stloc_3), // 替换回去
                };
                codes.InsertRange(i + 1, inject);
                break;
            }
        }
        return codes.AsEnumerable();
    }

    private static string ResolveTextIcon(string prefix, string oldText)
    {
        var pool = EnergyIconHelper.DecodePool<AbstractModel>(prefix);
        if (pool is ICustomEnergyIcon { TextIconPath: string path })
            return $"[img]{path}[/img]"; // BBCode 图片标签
        return oldText;
    }
}
```

### ASM 对齐说明

| 指令 | 对应 | 说明 |
|------|------|------|
| `Ldloc_0` | prefix 参数 | `TryEvaluateFormat` 的第一个局部变量 |
| `stloc.3` | 拼接结果 | `String.Concat` 的返回值储存位置 |
| `Call String.Concat` | 三次 string 拼接 | BaseLib 通过该调用定位插入点 |

如果游戏版本更新改变了 `TryEvaluateFormat` 的 IL 结构（局部变量顺序或 Concat 调用位置变化），需要反编译该方法确认并调整匹配点。

## 参见

- [energy-custom-icon-core.md](energy-custom-icon-core.md) — 接口定义与用法
- [harmony-basics.md](../harmony/harmony-basics.md) — Prefix / Transpiler 语法