# Transpiler 实战：自定义能力音效

> 纯原生实现（零第三方依赖）。灵感来源：BaseLib v3.4.7 `IPlayCustomPowerSfx`（2026-09-11）。

## 需求

原生能力施加时，Buff 和 Debuff 各自播放一个默认音效。如果想让某个能力播放专属音效（如大招语音、元素特效音），需要拦截默认行为。

BaseLib 用 `IPlayCustomPowerSfx` 接口 + Harmony Transpiler 实现。**纯原生写法完全等价**——自己写接口 + 自己写 Transpiler，零第三方依赖。

## 原理

`NCreature.OnPowerIncreased` 执行时，内部逻辑大致是：

```
if (power.ShouldPlayVfx)
    播放 Buff/Debuff 默认音效
// 之后继续 apply 逻辑
```

Transpiler 在 `ShouldPlayVfx` 判断之后、默认音效播放之前插一段代码：检查当前 power 是否实现了 `ICustomPowerSfx` 接口，如果实现了就让它播放自定义音效，播了就不播默认的。

## 实现

### 1. 定义接口（放在 Mod 公共命名空间）

```csharp
public interface ICustomPowerSfx
{
    /// <param name="amount">本次施加的层数</param>
    /// <param name="isBuff">true=即将播 Buff 音效, false=即将播 Debuff 音效</param>
    /// <returns>true=已播放自定义音效(跳过默认), false=继续走默认</returns>
    bool PlayCustomSfx(int amount, bool isBuff);
}
```

### 2. 写 Transpiler Patch

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
            // 找到 callvirt PowerModel.get_ShouldPlayVfx()
            if (codes[i].opcode == OpCodes.Callvirt &&
                codes[i].operand is MethodInfo mi &&
                mi == shouldPlayVfx)
            {
                // 往下一个 branch 指令，它的跳转标签就是"跳过默认音效"的目标
                for (int j = i + 1; j < codes.Count; j++)
                {
                    if (codes[j].Branches(out var label))
                    {
                        var inject = new List<CodeInstruction>
                        {
                            // 参数: PowerModel power (arg1), int amount (arg2)
                            new CodeInstruction(OpCodes.Ldarg_1),
                            new CodeInstruction(OpCodes.Ldarg_2),
                            // 局部变量: bool isBuff (loc1)
                            new CodeInstruction(OpCodes.Ldloc_1),
                            // 调用检查方法
                            new CodeInstruction(OpCodes.Call,
                                AccessTools.Method(typeof(CustomPowerSfxPatch),
                                    nameof(TryPlayCustomSfx))),
                            // 如果返回 true(已播自定义), 跳过默认
                            new CodeInstruction(OpCodes.Brtrue_S, label),
                        };
                        codes.InsertRange(j, inject);
                        goto patched; // 只打一个点，打完退
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

> **ASM 对齐说明**：`Ldarg_1` = `PowerModel power`（OnPowerIncreased 第一个参数）、`Ldarg_2` = `int amount`（第二个参数）、`Ldloc_1` = `bool isBuff`（第一个局部变量）。如果游戏更新改变了签名/局部变量顺序，需要对应调整索引。

### 3. 在 ModEntry 中注册

```csharp
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        var harmony = new Harmony("mymod");
        harmony.PatchAll(); // 自动扫描并注册 CustomPowerSfxPatch
        // ... 其他初始化
    }
}
```

`PatchAll()` 会自动找到带 `[HarmonyPatch]` 的类，无需单独注册。

## 在能力中使用

给能力模型实现 `ICustomPowerSfx` 接口，用 `SfxCmd.Play(string, float)` 播自定义音效：

```csharp
using MegaCrit.Sts2.Core.Commands;

public class MyPower : PowerModel, ICustomPowerSfx
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public bool PlayCustomSfx(int amount, bool isBuff)
    {
        // 只有高额施加才播专属音效，否则走默认
        if (amount >= 3)
        {
            SfxCmd.Play("res://mymod/sfx/my_power_sound.ogg", 1.0f);
            return true; // 跳过默认 Buff 音效
        }
        return false; // 走默认
    }
}
```

`SfxCmd.Play(string path, float busVolume)` 参数：
- `path`：音频资源路径（推荐 `.ogg` 格式，Godot 原生支持）
- `busVolume`：音量倍率（1.0 = 原始音量，0.5 = 减半）

## 常见问题

| 问题 | 解决 |
|------|------|
| Transpiler 不执行 | 检查 `PatchAll()` 是否调用；检查类名没有写错 |
| 自定义音效不触发 | 确认 power 类实现了 `ICustomPowerSfx`；`PlayCustomSfx` 返回了 `true` |
| IL 索引偏移 | 游戏版本更新后 `OnPowerIncreased` 签名/局部变量变化 → `Ldarg`/`Ldloc` 索引需同步更新 |
| 音效文件没加载 | 确认音频已打包进 PCK；文件格式用 `.ogg` 或 Godot 支持的格式 |

## 演进路线

- 当前：手动 Transpiler（通用，适配所有基版本）
- 更优：如果 BaseLib 被设为依赖，直接实现 `IPlayCustomPowerSfx` 即可（零代码量），本方案保留为**纯原生等效写法**

## 参见

- [harmony-basics.md](harmony-basics.md) — Transpiler 基础语法
- [power-callbacks.md](../power/power-callbacks.md) — 能力回调总表