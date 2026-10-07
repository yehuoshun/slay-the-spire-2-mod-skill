# 奖励卡内容修改（卡奖励附魔 + 商店卡附魔）

> 实战验证（sts2mod 奖励附魔 RewardEnchants v0.3.0，2026-10-07）。不改奖励类型、不建新卡——对**已生成的卡牌奖励**按概率附加附魔。核心 hook：`Hook.TryModifyCardRewardOptions`。

## 核心 hook（卡奖励生成入口）

```csharp
harmony.Patch(
    RequireMethod(typeof(CoreHook), nameof(CoreHook.TryModifyCardRewardOptions),
        BindingFlags.Static | BindingFlags.Public,
        typeof(IRunState), typeof(Player), typeof(List<CardCreationResult>),
        typeof(CardCreationOptions), typeof(List<AbstractModel>).MakeByRefType()),
    postfix: new HarmonyMethod(typeof(ModEntry), nameof(TryModifyCardRewardOptionsPostfix)));
```

- 真实签名：`static bool TryModifyCardRewardOptions(IRunState, Player, List<CardCreationResult>, CardCreationOptions, out List<AbstractModel>)`——`out` 参数反射定位用 `MakeByRefType()`。
- postfix 里 `__result = __result || enchantedAny`：修改过就让原版知道。

## 过滤条件（哪些奖励可以动）

```csharp
if (options.Count == 0) return false;
if (creationOptions.Source != CardCreationSource.Encounter) return false;          // 只要遭遇战奖励
if (creationOptions.Flags.HasFlag(CardCreationFlags.NoModifyHooks)) return false;  // 禁止修改的场合跳过
if (creationOptions.Flags.HasFlag(CardCreationFlags.NoCardModelModifications)) return false;
```

- `CardCreationOptions.Source/Flags/RngOverride` 均 public get（私有 set，With 方法设值）。

## 附魔候选过滤

```csharp
// 只要原版附魔（命名空间过滤，排除其他 mod 的附魔）
ModelDb.DebugEnchantments.Where(e => e.GetType().Namespace == "MegaCrit.Sts2.Core.Models.Enchantments")
// 排除表：Clone（复制卡）、DeprecatedEnchantment（废弃）、TezcatarasEmber（剧情专属）
// 其余：CanEnchant(card) 合法性 + Inky 特判（仅攻击卡）
```

## 修改流程（克隆 → 附魔 → 替换）

```csharp
CardModel enchantedCard = player.RunState.CloneCard(currentCard);   // ① 克隆（不污染原卡）
decimal amount = actIndex + 1;                                      // ② 层数随幕数涨
CardCmd.Enchant(selected.ToMutable(), enchantedCard, amount);       // ③ 附魔（走原版入口，可被其他 mod 拦截）
result.ModifyCard(enchantedCard);                                   // ④ 替换奖励展示
```

- 概率：`Clamp((actIndex+1) * 0.125m, 0, 1)`（每幕 +12.5%，第 8 幕封顶 100%）。

## RNG 纪律（多人一致性）

```csharp
Rng rng = creationOptions.RngOverride ?? player.PlayerRng.Rewards;  // 奖励随机源

// 商店：独立确定性派生源（同种子同结果，多人/读档一致）
int counter = shopsRng.ToSerializable().counter;                     // 0.110+；旧版用 shopsRng.Counter
ulong shopSeed = unchecked(player.PlayerRng.Seed + StringHelper.GetDeterministicHashCode("shops"));
string derivedName = $"RewardEnchants.shop.{counter}.{card.Id.Entry}.{card.CurrentUpgradeLevel}.{card.Enchantment?.Id.Entry ?? "none"}";
Rng localRng = new Rng(shopSeed, derivedName);                      // Rng(uint seed, string name) 派生构造
```

- **不要直接消费公共 `PlayerRng.Shops`**——商店多卡各自派生，避免一次消耗污染后续商店随机。
- 派生名含 卡ID+升级+附魔，保证「同一商店同一卡」永远同结果。
- `Rng.NextFloat()` / `NextItem<T>(IEnumerable<T>)`：核心随机 API。

## 商店卡附魔

```csharp
harmony.Patch(RequireMethod(typeof(MerchantCardEntry), nameof(MerchantCardEntry.Populate),
    BindingFlags.Instance | BindingFlags.Public), postfix: ...);
// postfix: __instance.CreationResult → TryApplyMerchantEnchantment(creationResult, card.Owner)
```

- `MerchantCardEntry.Populate()` public 实例方法，`CreationResult` 属性拿生成结果。

## 条件编译跨版本

```csharp
#if STS2_110_OR_NEWER
    int counter = shopsRng.ToSerializable().counter;   // 0.110+：Rng 有序列化形态
#else
    int counter = shopsRng.Counter;                    // 旧版：直接读
    uint shopSeed = shopsRng.Seed;
#endif
```

- 版本化 DLL 引用共享 `HextechRunes/versioned-dll-backups`；`AssemblyMetadata` 标兼容目标。

## 相关 API 速查

| API | 说明 |
|-----|------|
| `Hook.TryModifyCardRewardOptions` | 卡奖励生成静态分发点（AbstractModel 钩子聚合） |
| `CardCreationResult.ModifyCard(CardModel)` | 替换奖励卡展示 |
| `CardCreationSource.Encounter` / `CardCreationFlags.*` | 奖励来源/标志 |
| `ModelDb.DebugEnchantments` | 全部注册附魔（含原版+mod） |
| `PlayerRngSet.Rewards/Shops/Seed` | 玩家随机源 |
| `Rng(uint, string)` / `NextFloat` / `NextItem<T>` | 确定性派生随机 |
| `MerchantCardEntry.Populate()/CreationResult` | 商店卡生成入口 |