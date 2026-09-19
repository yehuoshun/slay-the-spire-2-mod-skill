# 自定义奖励：RewardType 注入与奖励基类

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

## Step 2: 继承 Reward

```csharp
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

public abstract class MyCustomReward : Reward
{
    protected MyCustomReward(Player player) : base(player) { }

    /// <summary>子类必须返回自定义的 RewardType</summary>
    protected abstract override RewardType RewardType { get; }

    /// <summary>奖励显示顺序（原生奖励索引见下方表格）</summary>
    public override int RewardsSetIndex => 9; // 原生奖励之后

    /// <summary>获取本地化字符串</summary>
    public LocString GetLoc(string key)
    {
        return new LocString("gameplay_ui", key);
    }
}
```

### 必须重写的成员

| 成员 | 类型 | 说明 |
|------|------|------|
| `RewardType` | 属性 | 返回注入的自定义枚举值 |
| `Populate()` | 方法 | 准备奖励内容（随机生成等） |
| `IsPopulated` | 属性 | 奖励是否已准备好 |
| `Description` | 属性 | 奖励面板显示的描述文本 |
| `IconPath` | 属性 | 奖励面板显示的图标路径 |
| `ToSerializable()` | 方法 | 将奖励数据转为可存 JSON |
| `RewardsSetIndex` | 属性 | 在面板中的渲染顺序 |

### 原生 RewardsSetIndex 参考

| 索引 | 奖励类型 |
|:----:|---------|
| 0 | 角色专属遗物 |
| 1 | 随机遗物 |
| 2 | 稀有遗物 |
| 3 | 遗物 |
| 4 | 药水 |
| 5 | 稀有卡 |
| 6 | 普通卡 |
| 7 | 无色卡 |
| 8 | 金币 |
| **9+** | **自定义奖励在此之后** |

### 不继承 CustomReward 的说明

BaseLib 的 `CustomReward` 额外封装了：
- `GetLoc()` 自动拼接 key 为 `ModID-ClassName`
- `DeserializeMethod` delegate + `Initialize()` 自动注册

纯原生需要手动实现这些，见 [reward-serialization.md](reward-serialization.md)。

## 参见

- [reward-serialization.md](reward-serialization.md) — 序列化与存档
- [reward-examples.md](reward-examples.md) — 完整示例