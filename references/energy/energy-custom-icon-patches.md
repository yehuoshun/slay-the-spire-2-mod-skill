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
        var pool = ModEnergyIconCodec.DecodePool<AbstractModel>(prefix);
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

> 文本内联图标 Patch（Transpiler + ASM 对齐）→ [energy-custom-icon-text-patch.md](energy-custom-icon-text-patch.md)

## 参见

- [energy-custom-icon-core.md](energy-custom-icon-core.md) — 接口定义与用法
- [harmony-basics.md](../harmony/harmony-basics.md) — Prefix / Transpiler 语法