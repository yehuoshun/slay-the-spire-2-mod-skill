# 自定义能力音效：接口、Transpiler 与注册

## 接口定义

```csharp
public interface ICustomPowerSfx
{
    bool PlayCustomSfx(int amount, bool isBuff);
}
```

## Transpiler Patch

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

[HarmonyPatch(typeof(NCreature), nameof(NCreature.OnPowerIncreased))]
public static class CustomPowerSfxPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var shouldPlayVfx = AccessTools.PropertyGetter(
            typeof(PowerModel), nameof(PowerModel.ShouldPlayVfx));

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Callvirt &&
                codes[i].operand is MethodInfo mi && mi == shouldPlayVfx)
            {
                for (int j = i + 1; j < codes.Count; j++)
                {
                    if (codes[j].Branches(out var label))
                    {
                        var inject = new List<CodeInstruction>
                        {
                            new CodeInstruction(OpCodes.Ldarg_1),
                            new CodeInstruction(OpCodes.Ldarg_2),
                            new CodeInstruction(OpCodes.Ldloc_1),
                            new CodeInstruction(OpCodes.Call,
                                AccessTools.Method(typeof(CustomPowerSfxPatch),
                                    nameof(TryPlayCustomSfx))),
                            new CodeInstruction(OpCodes.Brtrue_S, label),
                        };
                        codes.InsertRange(j, inject);
                        goto patched;
                    }
                }
            }
        }
        patched:
        return codes.AsEnumerable();
    }

    private static bool TryPlayCustomSfx(PowerModel power, int amount, bool isBuff)
    {
        if (power is ICustomPowerSfx custom)
            return custom.PlayCustomSfx(amount, isBuff);
        return false;
    }
}
```

## ASM 对齐

| 指令 | 对应 | 说明 |
|------|------|------|
| `Ldarg_1` | 参数 1 | `PowerModel power` |
| `Ldarg_2` | 参数 2 | `int amount` |
| `Ldloc_1` | 局部 1 | `bool isBuff` |

游戏版本更新后签名/局部变量顺序可能变化，需要同步调整。

## ModEntry 注册

```csharp
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        var harmony = new Harmony("mymod");
        harmony.PatchAll();
    }
}
```

## 参见

- [harmony-custom-power-sfx-usage.md](harmony-custom-power-sfx-usage.md) — 使用示例与常见问题