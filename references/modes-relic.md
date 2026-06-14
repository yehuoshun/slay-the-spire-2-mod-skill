# 自定义遗物

> 学海克斯符文的 RelicBase 抽象 + YuWanCard 的自动注册。

---

## 最简遗物

```csharp
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyMod.Relics;

[RelicPool(typeof(SharedPool))]
public sealed class LuckyCoinRelic : RelicModel
{
    public override Rarity Rarity => Rarity.Common;
    public override string BigIconPath => "res://MyMod/images/relics/lucky_coin.png";
    public override string PackedIconPath => BigIconPath;
    protected override string PackedIconOutlinePath => BigIconPath;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("GoldPerCombat", 10m)
    ];

    public override async Task AfterCombatEnd(PlayerChoiceContext context)
    {
        if (Owner == null) return;
        Flash();
        await PlayerCmd.GainGold(context, DynamicVars["GoldPerCombat"].IntValue);
    }
}
```

---

## Rarity 决定获取途径

| Rarity | 获取方式 |
|--------|---------|
| `Common` | 普通遗物池 |
| `Uncommon` | 稀有遗物池 |
| `Rare` | BOSS 遗物池 |
| `Starter` | **不走随机池**，需 Patch 或用事件给 |
| `Shop` | 商店遗物池 |
| `Special` | 特殊事件获取 |
| `Event` | 事件专属 |

---

## 常用生命周期钩子

| 钩子 | 触发时机 |
|------|---------|
| `AfterCombatEnd(context)` | 战斗结束后 |
| `AfterPlayerTurnStart(context, player)` | 玩家回合开始 |
| `AfterPlayerTurnEnd(context, player)` | 玩家回合结束 |
| `AfterCardPlayed(context, cardPlay)` | 打出卡牌后 |
| `OnPlayerDamaged(context, damageTaken)` | 玩家受伤时 |
| `OnPlayerKill(context, target)` | 玩家击杀敌人时 |
| `BeforePlayerBlockLost(context, amountLost)` | 格挡消失前 |
| `AfterPlayerHeal(context, amountHealed)` | 玩家治疗后 |

---

## 角色限定遗物

```csharp
public sealed class IroncladOnlyRelic : RelicModel
{
    public override bool IsAvailableForPlayer(Player player)
    {
        return player.Character is Ironclad;
    }
}
```

---

## 带计数器的遗物

```csharp
public sealed class CounterRelic : RelicModel
{
    // SavedProperty 确保计数器序列化保存
    [SavedProperty] public int KillCount { get; set; }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("KillsNeeded", 5m)
    ];

    public override async Task OnPlayerKill(PlayerChoiceContext context, Creature target)
    {
        KillCount++;
        if (KillCount >= DynamicVars["KillsNeeded"].IntValue)
        {
            KillCount = 0;
            Flash();
            await PlayerCmd.Heal(context, 10);
        }
    }
}
```

---

## 战斗开始/战斗结束遗物

```csharp
public sealed class BattleRageRelic : RelicModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(1m)
    ];

    public override async Task AfterCombatStart(PlayerChoiceContext context)
    {
        Flash();
        await PowerCmd.Apply<StrengthPower>(
            Owner.Creature, DynamicVars["StrengthPower"].BaseValue, Owner.Creature, this);
    }
}
```

---

## 遗物本地化

`assets/localization/zhs/relics.json`：

```json
{
  "LuckyCoin": {
    "NAME": "幸运硬币",
    "FLAVOR": "一面是字，一面是花——但两面都写着赢。",
    "DESCRIPTIONS": ["每场战斗结束后获得 !GoldPerCombat! 金币。"]
  }
}
```

---

## Builder 模式遗物（学 YuWanCard）

```csharp
public sealed class LuckyCoinRelic : YuWanRelicModel
{
    // 构造函数中 autoAdd = true → 自动注册
    public LuckyCoinRelic() : base(autoAdd: true)
    {
        Rarity = Rarity.Common;
    }

    public override async Task AfterCombatEnd(PlayerChoiceContext context)
    {
        Flash();
        await PlayerCmd.GainGold(context, 10);
    }
}
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| Starter 遗物不出现 | Starter 不走随机池，需手动 Patch 给 |
| 计数器不保存 | 加 `[SavedProperty]` 特性 |
| 图标模糊 | PNG 分辨率 128x128 以上 |
| `IsAvailableForPlayer` 不生效 | 检查 `Owner` 是否已初始化 |