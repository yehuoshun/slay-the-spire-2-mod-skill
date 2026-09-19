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