# 自定义奖励：RewardType 注入

> 奖励基类见 [reward-base.md](reward-base.md)。

## Step 1: 注入新 RewardType

`RewardType` 是原生 enum（在 `MegaCrit.Sts2.Core.Rewards` 命名空间）。不能直接加新成员，必须运行时注入。

### 原理

用 Harmony Prefix 在 `ModelDb.Init` 执行前扫描所有程序集，找到标记了 `[RewardType]` 的静态字段，通过反射把新 enum 值注入进去。

### 定义自定义 RewardType

```csharp
using MegaCrit.Sts2.Core.Rewards;

public static class MyRewardTypes
{
    /// <summary>自定义奖励类型：卡牌变形</summary>
    [RewardType]
    public static RewardType CardTransform;

    /// <summary>自定义奖励类型：卡牌升级</summary>
    [RewardType]
    public static RewardType CardUpgrade;

    /// <summary>自定义奖励类型：联动选择</summary>
    [RewardType]
    public static RewardType LinkedReward;
}
```

`[RewardType]` 是你的自定义标记 Attribute：

```csharp
[AttributeUsage(AttributeTargets.Field)]
public sealed class RewardTypeAttribute : Attribute { }
```

### 注入逻辑（Harmony Prefix on ModelDb.Init）

```csharp
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public static class RewardTypeInjector
{
    [HarmonyPrefix]
    public static void InjectRewardTypes()
    {
        var rewardTypeEnum = typeof(RewardType);
        var underlyingType = Enum.GetUnderlyingType(rewardTypeEnum);
        var allValues = Enum.GetValues<RewardType>();

        // 找到原生最大值
        int maxValue = 0;
        foreach (var val in allValues)
        {
            int iv = Convert.ToInt32(val);
            if (iv > maxValue) maxValue = iv;
        }
        BaseValue = maxValue + 1; // 基础偏移

        // 扫描所有已加载程序集的静态 [RewardType] 字段
        int offset = 0;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in asm.GetTypes())
            {
                foreach (var field in type.GetFields(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!field.IsDefined(typeof(RewardTypeAttribute))) continue;
                    if (field.FieldType != rewardTypeEnum) continue;

                    int enumValue = BaseValue + offset;
                    field.SetValue(null, Enum.ToObject(rewardTypeEnum, enumValue));
                    RegisteredTypes[field.Name] = enumValue;
                    offset++;
                }
            }
        }
    }

    public static int BaseValue { get; private set; }
    public static Dictionary<string, int> RegisteredTypes { get; } = new();
}
```

### 冲突避免

`BaseValue = maxValue + 1` 确保不覆盖原生值。各 Mod 从同一起点递增，天然隔离。

