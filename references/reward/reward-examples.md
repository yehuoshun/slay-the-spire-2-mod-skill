# 自定义奖励：完整示例

## 示例 1：卡牌变形奖励（CardTransformReward）

从 BaseLib `CardTransformReward` 转译。效果类似遗物「禁魔咒典」——挑选一张卡牌变成另一张卡牌。

```csharp
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

public class CardTransformReward : Reward
{
    [RewardType] public static RewardType CardTransform;

    /// <summary>可选牌数量</summary>
    public int MaxCards { get; set; } = 1;

    /// <summary>是否升级</summary>
    public bool Upgrade { get; set; } = false;

    public CardTransformReward(Player player) : base(player) { }

    protected override RewardType RewardType => CardTransform;
    public override bool IsPopulated => true;
    public override int RewardsSetIndex => 9;

    public override LocString Description =>
        new LocString("gameplay_ui", "MYSKILL-CARD_TRANSFORM_TITLE");

    public override string IconPath =>
        "res://myskill/images/rewards/card_transform.png";

    public override void Populate() { }

    public override SerializableReward ToSerializable()
    {
        var save = base.ToSerializable();
        save.GoldAmount = MaxCards;
        save.CardIds = new List<string> { Upgrade ? "upgrade" : "no_upgrade" };
        return save;
    }

    public static CardTransformReward CreateFromSave(
        SerializableReward save, Player player)
    {
        return new CardTransformReward(player)
        {
            MaxCards = save.GoldAmount,
            Upgrade = save.CardIds?.Contains("upgrade") ?? false,
        };
    }
}
```

更多示例见 [reward-examples-more.md](reward-examples-more.md)：

- 卡牌升级奖励（CardUpgradeReward）
- 随机升级奖励（RandomCardUpgradeReward）
- 添加奖励到战斗的两种方式
- 完整 ModEntry 注册流程

## 参见

- [reward-core.md](reward-core.md) — RewardType 注入与基类
- [reward-serialization.md](reward-serialization.md) — 序列化详解