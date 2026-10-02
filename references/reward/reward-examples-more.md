# 自定义奖励：更多示例与注册

## 示例 2：卡牌升级奖励（CardUpgradeReward）

类似从「熔炉」事件选一张牌升级，但作为战斗奖励。

```csharp
public class CardUpgradeReward : Reward
{
    [RewardType] public static RewardType CardUpgrade;

    public int UpgradeCount { get; set; } = 1;

    public CardUpgradeReward(Player player) : base(player) { }

    protected override RewardType RewardType => CardUpgrade;
    public override bool IsPopulated => true;
    public override int RewardsSetIndex => 9;

    public override LocString Description =>
        new LocString("gameplay_ui", "MYSKILL-CARD_UPGRADE_TITLE");

    // 真实签名：protected virtual string?
    protected override string? IconPath =>
        "res://myskill/images/rewards/card_upgrade.png";

    public override void Populate() { }

    public override SerializableReward ToSerializable()
    {
        var save = base.ToSerializable();
        save.GoldAmount = UpgradeCount;
        return save;
    }

    public static CardUpgradeReward CreateFromSave(
        SerializableReward save, Player player)
    {
        return new CardUpgradeReward(player)
        {
            UpgradeCount = save.GoldAmount,
        };
    }
}
```

## 示例 3：随机升级奖励（RandomCardUpgradeReward）

自动随机升级一张卡牌，不需要玩家选择。

```csharp
public class RandomCardUpgradeReward : Reward
{
    [RewardType] public static RewardType RandomUpgrade;

    public RandomCardUpgradeReward(Player player) : base(player) { }

    protected override RewardType RewardType => RandomUpgrade;
    public override bool IsPopulated => false; // 需要 Populate
    public override int RewardsSetIndex => 9;

    public override LocString Description =>
        new LocString("gameplay_ui", "MYSKILL-RANDOM_UPGRADE_TITLE");

    // 真实签名：protected virtual string?
    protected override string? IconPath =>
        "res://myskill/images/rewards/random_upgrade.png";

    public override void Populate()
    {
        // 真实升级 API：IsUpgradable + UpgradeInternal()（CanUpgrade() 不存在）
        var upgradable = Player.Deck.Cards
            .Where(c => c.IsUpgradable && !c.IsUpgraded)
            .ToList();
        if (upgradable.Count > 0)
        {
            var card = upgradable[0];
            card.UpgradeInternal();
        }
    }
}
```

## 添加奖励到战斗

### 方式 1：事件回调

```csharp
public override Task AfterCombatEnd(...)
{
    if (someCondition)
    {
        room.AddExtraReward(Owner.Player,
            new CardTransformReward(Owner.Player) { MaxCards = 3 });
    }
    return Task.CompletedTask;
}
```

### 方式 2：Patch 战斗奖励池

```csharp
[HarmonyPatch(typeof(CombatRoom),
    nameof(CombatRoom.OfferRoomEndRewards))]
public static class AddCustomRewardPatch
{
    private static void Postfix(CombatRoom __instance)
    {
        if (ShouldGiveCustomReward(__instance))
        {
            __instance.AddExtraReward(__instance.Player,
                new CardUpgradeReward(__instance.Player));
        }
    }
}
```

> ⚠️ `AddNonEliteRewards` 是**编造方法**（原生 CombatRoom 不存在）；真实 API 是 `AddExtraReward(Player, Reward)` + `OfferRoomEndRewards()`（后者才是战斗结束发奖入口，Patch 它或 `_extraRewards` 的填充时机）。

## 完整注册流程（ModEntry）

```csharp
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        var harmony = new Harmony("myskill");
        harmony.PatchAll();   // 触发所有 [HarmonyPatch]（含 EnumInjector）
    }
}
```

> ⚠️ 原生 `Reward.FromSerializable` 是硬编码 switch（自定义 RewardType 直接抛 NotImplementedException），
> **不存在 CustomRewardRegistry**。自定义奖励的存档/读档需自己 `[HarmonyPatch] Reward.FromSerializable` 加分支，
> 并在 `ToSerializable()` 里写入自定义字段（SerializableReward 的 GoldAmount/CardIds 等复用字段）。

## 参见

- [reward-core.md](reward-core.md) — RewardType 注入与基类
- [reward-serialization.md](reward-serialization.md) — 序列化详解
- [reward-examples.md](reward-examples.md) — 示例 1：卡牌变形