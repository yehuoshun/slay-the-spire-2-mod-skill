# 自定义能量图标

> 纯原生实现（零第三方依赖）。灵感来源：BaseLib v3.4.7 `ICustomEnergyIconPool`。

## 需求

默认卡池的 `EnergyColorName` 决定了图标颜色和纹理路径（`ui_atlas.sprites/card/energy_<name>.tres`）。如果想用完全不同的图标（图案、特效图标、自定义绘制），需要拦截原生图标解析。

## 原理

游戏通过 `EnergyIconHelper.GetPath(string prefix)` 查找图标资源路径，`prefix` 就是 `EnergyColorName` 的值。在文本中通过 `EnergyIconsFormatter` 渲染内联图标。

BaseLib 的做法是：让卡池的 `EnergyColorName` 返回一个编码了 ModelId 的唯一字符串（格式 `Category∴Entry`），然后拦截 `GetPath` 和 `TryEvaluateFormat`——看到是自己的编码就返回自定义路径，不是就放行走原生逻辑。

## 纯原生实现

### 1. 定义接口

放在你的 Mod 公共命名空间，池模型实现这个接口即可获得自定义图标。

```csharp
public interface ICustomEnergyIcon
{
    /// <summary>大图标路径（卡牌左上角），返回 null 走原生</summary>
    string? BigIconPath { get; }

    /// <summary>文本内联图标路径（描述里的 [icon] 标签），返回 null 走原生</summary>
    string? TextIconPath { get; }
}
```

### 2. 定义分隔符与辅助方法

分隔符用于把 ModelId 编码进 `EnergyColorName`，Patch 端据此判断是否是自定义池。

```csharp
public static class EnergyIconHelper
{
    /// <summary>模型 ID 编码分隔符（选一个不会出现在普通 EnergyColorName 里的字符）</summary>
    public const char Delimiter = '∴';

    /// <summary>获取编码后的 EnergyColorName</summary>
    public static string EncodePoolId(ModelId id) => $"{id.Category}{Delimiter}{id.Entry}";

    /// <summary>通过编码解析出自定义池模型</summary>
    public static T? DecodePool<T>(string prefix) where T : AbstractModel
    {
        int idx = prefix.IndexOf(Delimiter);
        if (idx < 0) return null;
        return ModelDb.GetById<T>(new ModelId(prefix[..idx], prefix[(idx + 1)..]));
    }
}
```

### 3. 写大图标 Patch（Prefix）

拦截 `EnergyIconHelper.GetPath`，当 `prefix` 是编码过的自定义池 ID 时返回自定义大图标路径。

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

[HarmonyPatch(typeof(EnergyIconHelper), nameof(EnergyIconHelper.GetPath), typeof(string))]
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

### 4. 写文本内联图标 Patch（Transpiler）

> **注意**：`EnergyIconsFormatter.TryEvaluateFormat` 是 private 方法。如果反编译后找不到这个类名，改用 `TargetMethod` 动态定位。

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
            "MegaCrit.Sts2.Core.Localization.Formatters.EnergyIconsFormatter");
        return AccessTools.Method(type, "TryEvaluateFormat");
    }

    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();

        // 找到最后一个 String.Concat(string, string, string) 调用 + stloc.3
        // 把拼接结果替换为自定义文本图标路径
        var concatThree = AccessTools.Method(typeof(string), nameof(string.Concat),
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
                    new CodeInstruction(OpCodes.Ldloc_0),    // prefix 参数
                    new CodeInstruction(OpCodes.Ldloc_3),    // 刚拼接好的文本
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(CustomEnergyTextIconPatch),
                            nameof(ResolveTextIcon))),
                    new CodeInstruction(OpCodes.Stloc_3),    // 替换回去
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

> **ASM 对齐说明**：假定游戏内 `TryEvaluateFormat` 的 IL 中：`Ldloc_0` = prefix 参数、`stloc.3` = 最终拼接结果。如果游戏版本更新改变了局部变量索引或调用位置，需要对应调整。

### 5. 在卡池模型中使用

```csharp
public class MyCardPool : CardPoolModel, ICustomEnergyIcon
{
    // EnergyColorName 必须是唯一值，不能和其他池撞
    // 用 EncodePoolId 确保唯一
    public override string EnergyColorName =>
        EnergyIconHelper.EncodePoolId(Id);

    public string? BigIconPath =>
        "res://mymod/images/energy/my_energy_icon.png";

    public string? TextIconPath =>
        "res://mymod/images/energy/my_energy_text.png";
}
```

### 6. 在 ModEntry 注册 Patch

```csharp
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        var harmony = new Harmony("mymod");
        harmony.PatchAll(); // 自动注册两个 Patch
    }
}
```

## 图标资源准备

### 大图标

放在 `res://<模组ID>/images/energy/` 下，推荐 32x32 或 64x64 PNG，Godot 会自动处理。

如果希望用 `.tres` 纹理资源（支持着色/动画），参照游戏默认 `ui_atlas.sprites/card/energy_red.tres`：

```bash
# 简单的 PNG 即可工作，注意路径与 BigIconPath 一致
images/energy/my_energy_icon.png
```

### 文本图标

内联图标在 BBCode 中显示尺寸受文本行高限制，推荐 16x16 或 20x20 PNG。

## 常见问题

| 问题 | 解决 |
|------|------|
| GetPath Patch 不触发 | 确认 `prefix` 参数类型是 `string`；检查 PatchAll 是否执行 |
| 分隔符冲突 | 普通 `EnergyColorName` 确保不包含 `∴`（该字符在 C# string 中几乎不会出现） |
| 文本图标不替换 | Transpiler 匹配点可能变了，反编译 `TryEvaluateFormat` 确认 IL 结构 |
| 其他池也被影响了 | Patch 里加了 `DecodePool` 判断，不是你的编码就放行 |
| 多人模式 | 客户端和服务端都需要有相同资源，图标路径依赖 PCK 打包 |

## 演进路线

- 当前：手动 Patch，插两个点实现接口
- 更优：如果 BaseLib 被设为依赖，直接让池模型继承 `CustomCardPoolModel` 即可，自动获得自定义图标支持
- 纯原生优点：完全可控，轻量，不背依赖

## 参见

- [energy.md](energy.md) — 能量模块导航
- [harmony-basics.md](../harmony/harmony-basics.md) — Prefix / Transpiler 语法
- [harmony-guide.md](../harmony/harmony-guide.md) — 常用 Patch 目标
- [harmony-custom-power-sfx.md](../harmony/harmony-custom-power-sfx.md) — 同类 Transpiler 模式