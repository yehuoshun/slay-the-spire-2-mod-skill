# 自定义牌堆：PileType 枚举注入

> 自定义 PileType 注入 + 自定义 PileType 定义。继承 CardPile 与注册见 [pile-base.md](pile-base.md)。

流程同 `RewardType`（见 [reward-core.md](../reward/reward-core.md)），用 `[CustomPileType]` Attribute + `ModelDb.Init` Prefix。

```csharp
using MegaCrit.Sts2.Core.Models;

[AttributeUsage(AttributeTargets.Field)]
public sealed class CustomPileTypeAttribute : Attribute { }

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public static class PileTypeInjector
{
    [HarmonyPrefix]
    public static void InjectPileTypes()
    {
        int baseValue = Enum.GetValues<PileType>().Max(v => Convert.ToInt32(v)) + 1;
        int offset = 0;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in asm.GetTypes())
            {
                foreach (var field in type.GetFields(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!field.IsDefined(typeof(CustomPileTypeAttribute))) continue;
                    if (field.FieldType != typeof(PileType)) continue;
                    field.SetValue(null, Enum.ToObject(typeof(PileType), baseValue + offset));
                    offset++;
                }
            }
        }
    }
}
```

### 定义自定义 PileType

```csharp
// 放在你的奖励类里
public static class MyPileTypes
{
    [CustomPileType]
    public static PileType VoidPile;  // 虚空堆

    [CustomPileType]
    public static PileType ReservePile; // 储备堆
}
```

> 每个自定义 PileType 配一个自定义牌堆类。**一个 PileType 对应一个类**。

## 参见

- [pile-base.md](pile-base.md) — 继承 CardPile + 注册 Registry
- [pile-patches.md](pile-patches.md) — 5 个必要 Harmony Patch
- [reward-core.md](../reward/reward-core.md) — enum 注入详细说明