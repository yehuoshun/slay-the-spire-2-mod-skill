# 自定义卡牌：构造函数与基础卡牌

## 构造函数

```csharp
public class MyCard : CardModel
{
    public MyCard() : base(
        1,              // CanonicalEnergyCost — 基础耗能
        CardType.Attack, // Type — 卡牌类型（决定肖像框样式）
        CardRarity.Common, // Rarity — 稀有度（决定边框样式+出现逻辑）
        TargetType.AnyEnemy, // TargetType — 目标类型
        true            // ShouldShowInCardLibrary — 是否在图鉴显示（可选，默认true）
    ) { }
}
```

### 参数说明

| 参数 | 属性 | 说明 |
|------|------|------|
| 1st | `CanonicalEnergyCost` | 基础耗能，初始化后赋值给 `EnergyCost` |
| 2nd | `Type` | 卡牌类型，决定肖像框样式 |
| 3rd | `Rarity` | 稀有度，决定边框样式+出现逻辑+售价 |
| 4th | `TargetType` | 目标类型 |
| 5th | `ShouldShowInCardLibrary` | 是否在图鉴显示（可选，默认true） |

### 卡牌类型（CardType）

| 值 | 说明 |
|-----|------|
| `Attack` | 攻击牌 |
| `Skill` | 技能牌 |
| `Power` | 能力牌 |

### 稀有度（CardRarity）

| 值 | 说明 |
|-----|------|
| `None` | 无（默认） |
| `Basic` | 初始卡（铁甲战士等初始牌组，不随机生成） |
| `Common` | 普通，随机池生成 |
| `Uncommon` | 罕见，随机池生成 |
| `Rare` | 稀有，随机池生成 |
| `Event` | 事件，不随机生成 |
| `Ancient` | 先古，不随机生成 |
| `Token` | 衍生物（小刀/巨石/灵魂），不随机生成 |
| `Status` | 状态，不随机生成 |
| `Curse` | 诅咒，不随机生成 |
| `Quest` | 任务（藏宝图/多尼斯异鸟蛋），不随机生成 |

### 目标类型（TargetType）

| 值 | 说明 |
|-----|------|
| `None` | 无目标 |
| `Self` | 仅自身，无目标选择 |
| `AnyEnemy` | 指定一个敌人 |
| `AllEnemies` | 所有敌人 |
| `RandomEnemy` | 随机敌人 |
| `AnyPlayer` | 指定一个玩家（多人模式；单人无目标选择） |
| `AnyAlly` | 指定一个友军（非自身，多人模式） |
| `AllAllies` | 所有友军 |
| `TargetedNoCreature` | 非玩家/敌人（如污浊药水目标商人） |
| `Osty` | 亡灵契约师宠物奥斯提 |

---

## 创建基础攻击卡牌

```csharp
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

[CardPool(typeof(ColorlessCardPool))]  // 添加到无色卡池（自动注册，见 design-patterns 模式1）
public class ExampleStrike : CardModel
{
    public ExampleStrike() : base(
        0,
        CardType.Attack,
        CardRarity.Common,
        TargetType.AnyEnemy,
        true
    ) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 空值检查（真实源码风格）
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        // 执行伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 升级：伤害+2
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
```

