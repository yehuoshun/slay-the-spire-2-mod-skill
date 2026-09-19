# 自定义奖励：序列化与存档

> **重要**：序列化是实现自定义奖励最关键的环节。处理不当会导致存档损坏或多人不同步。

## 序列化流程

```mermaid
graph LR
    A[玩家选择奖励] --> B[ToSerializable<br>→ SerializableReward]
    B --> C[存档 JSON]
    C --> D[读档]
    D --> E[Reward.FromSerializable<br>→ Custom Deserializer]
    E --> F[恢复奖励对象]
```

原生 `Reward` 有 `ToSerializable()` 虚方法返回 `SerializableReward`。反序列化依赖 `Reward.FromSerializable(SerializableReward, Player)` ——这是一个静态方法，内部用了 `switch` 或字典查找 `RewardType`。纯原生需要拦截这个方法，加入自定义分支。

## 序列化注册（Harmony Prefix）

```csharp
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

public static class CustomRewardRegistry
{
    private static readonly Dictionary<RewardType, Func<SerializableReward, Player, Reward>> 
        _deserializers = new();

    public static void Register(RewardType type, 
        Func<SerializableReward, Player, Reward> deserializer)
    {
        _deserializers[type] = deserializer;
    }

    /// <summary>在 ModEntry.Initialize 中调用，注册所有自定义奖励</summary>
    public static void RegisterAll()
    {
        foreach (var (type, deserializer) in _deserializers)
        {
            Logger.Info($"Registered deserializer for reward type {type}");
        }
    }
}

[HarmonyPatch(typeof(Reward), nameof(Reward.FromSerializable))]
public static class RewardDeserializationPatch
{
    [HarmonyPrefix]
    private static bool Prefix(SerializableReward save, Player player, ref Reward __result)
    {
        if (CustomRewardRegistry.TryGetDeserializer(save.RewardType, out var deserializer))
        {
            __result = deserializer(save, player);
            return false; // 跳过原生逻辑
        }
        return true; // 不是自定义奖励，走原生
    }
}
```

## 奖励类的序列化模板

```csharp
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

public class MyReward : Reward
{
    public int CustomData { get; set; }  // 需要持久化的自定义字段

    public MyReward(Player player) : base(player) { }

    protected override RewardType RewardType => MyRewardTypes.MyType;

    // 序列化：将自定义数据写入 SerializableReward
    public override SerializableReward ToSerializable()
    {
        var save = base.ToSerializable();
        save.GoldAmount = CustomData; // 复用原生字段（如果没有自定义 save slot）
        // 或用 save.AdditionalData / save.SerializedStrings 等
        return save;
    }

    // 反序列化方法（静态，供注册用）
    public static MyReward CreateFromSave(SerializableReward save, Player player)
    {
        return new MyReward(player)
        {
            CustomData = save.GoldAmount
        };
    }
}

// 注册（在 ModEntry.Initialize 中）
CustomRewardRegistry.Register(MyRewardTypes.MyType, MyReward.CreateFromSave);
```

## 关于 SerializableReward

`SerializableReward` 包含以下可用字段，用于存储自定义数据：

| 字段 | 类型 | 说明 |
|------|------|------|
| `RewardType` | `RewardType` | 奖励类型（必须！用于反序列化匹配） |
| `GoldAmount` | `int` | 金币数，可复用 |
| `CardIds` | `List<string>` | 卡牌 ID 列表 |
| `RelicIds` | `List<string>` | 遗物 ID 列表 |
| `PotionIds` | `List<string>` | 药水 ID 列表 |
| `SerializedStrings` | `List<string>`? | 通用字符串列表 |
| `AdditionalData` | `string`? | 额外 JSON 字符串 |

存储自定义数据建议用 `SerializedStrings` 或 `AdditionalData`（存 JSON）。避免使用 `GoldAmount` 等有原生含义的字段，除非你的自定义数据本身就是 int 且不会被误解。

## 注册时机

自定义奖励的注册必须在 `ModelDb.Init` 之前完成，否则读档时找不到对应的反序列化器会报错。

```csharp
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        // 顺序：注入→注册→Harmony
        var harmony = new Harmony("mymod");
        harmony.PatchAll(); // RewardTypeInjector + RewardDeserializationPatch

        CustomRewardRegistry.Register(MyRewardTypes.CardTransform, 
            CardTransformReward.CreateFromSave);

        // ... 其他初始化
    }
}
```

## 参见

- [reward-core.md](reward-core.md) — RewardType 注入与奖励基类
- [reward-examples.md](reward-examples.md) — 完整示例