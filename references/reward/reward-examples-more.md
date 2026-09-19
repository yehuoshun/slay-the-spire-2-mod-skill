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

    public override string IconPath =>
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

    public override string IconPath =>
        "res://myskill/images/rewards/random_upgrade.png";

    public override void Populate()
    {
        var upgradable = Player.Deck.Cards
            .Where(c => c.CanUpgrade() && !c.IsUpgraded)
            .ToList();
        if (upgradable.Count > 0)
        {
            var card = upgradable[
                Random.Range(0, upgradable.Count)];
            card.Upgrade();
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
    nameof(CombatRoom.AddNonEliteRewards))]
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

## 完整注册流程（ModEntry）

```csharp
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        var harmony = new Harmony("myskill");

        // PatchAll 触发所有 [HarmonyPatch]
        // 包括 RewardTypeInjector + RewardDeserializationPatch
        harmony.PatchAll();

        // 注册自定义奖励反序列化器
        CustomRewardRegistry.Register(
            CardTransformReward.CardTransform,
            CardTransformReward.CreateFromSave);
        CustomRewardRegistry.Register(
            CardUpgradeReward.CardUpgrade,
            CardUpgradeReward.CreateFromSave);
        CustomRewardRegistry.RegisterAll();
    }
}
```

## 参见

- [reward-core.md](reward-core.md) — RewardType 注入与基类
- [reward-serialization.md](reward-serialization.md) — 序列化详解
- [reward-examples.md](reward-examples.md) — 示例 1：卡牌变形