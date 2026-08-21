# 自定义卡牌

> 参考：[杀戮尖塔2模组开发教程03 - 自定义卡牌 - 哔哩哔哩](https://www.bilibili.com/opus/1179979923167641608)（from 烟汐忆梦_YM）

---

## 概述

所有卡牌继承 `CardModel` 抽象类。卡牌通过构造函数定义基础属性，通过回调实现行为逻辑。

---

## 构造函数

```csharp
public class MyCard : CardModel
{
    public MyCard() : base(
        1,              // CanonicalEnergyCost — 基础耗能
        CardType.Attack, // Type — 卡牌类型（决定肖像框样式）
        CardRarity.Common, // Rarity — 稀有度（决定边框样式+出现逻辑）
        TargetType.TargetedEnemy, // TargetType — 目标类型
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
| `NoTarget` | 无目标 |
| `TargetedEnemy` | 指定一个敌人 |
| `TargetedAlly` | 指定一个友军（非自身，全军覆没时无法打出） |
| `AllEnemies` | 所有敌人 |
| `AllAllies` | 所有友军 |
| `Self` | 自身 |
| `TargetedNoCreature` | 非玩家/敌人（如污浊药水目标商人） |
| `Osty` | 亡灵契约师宠物奥斯提 |
| `RandomEnemy` | 随机敌人 |

---

## 创建基础攻击卡牌

```csharp
using System.Threading.Tasks;
using STS2;
using STS2.Cards;
using STS2.Combat;
using STS2.Core.Cards;
using Godot;

[CardPool(CardPools.Colorless)]  // 添加到无色卡池
public class ExampleStrike : CardModel
{
    // 定义动态变量
    public static readonly DynamicValue<int> Damage = new DynamicValue<int>(2, "D");

    public ExampleStrike() : base(
        0,
        CardType.Attack,
        CardRarity.Common,
        TargetType.TargetedEnemy,
        true
    ) { }

    public override Task OnPlay(PlayerChoiceContext context, CardPlayState playState)
    {
        // 判断目标是否存在
        if (context.Target == null) return Task.CompletedTask;
        // 获取目标生物
        var target = context.Target.Creature;
        // 执行伤害
        return DamageCmd.Attack(Damage.Get(this))
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("res://animations/slash/slash.glb")
            .Execute(playState.CombatState);
    }

    public override void OnUpgrade()
    {
        // 升级：伤害+2
        base.Damage.UpgradeValueBy(2);
    }
}
```

---

## 核心 API

### DamageCmd — 伤害操作

```csharp
// 创建伤害操作
DamageCmd.Attack(<伤害值>)
    .FromCard(this)                    // 指定来源为卡牌
    .FromOsty(creature, this)          // 指定来源为奥斯提宠物
    .FromMonster(monster)              // 指定来源为怪物
    .Targeting(creature)               // 指定单一目标
    .TargetingAllOpponents(state)      // 攻击所有对手
    .TargetingRandomOpponents(state)   // 攻击随机目标，可选参数是否允许重复
    .WithHitFx(vfxPath, sfxPath, tmpSfxPath)  // 指定特效/音效
    .WithHitCount(3)                   // 指定攻击次数
    .Execute(state);                   // 执行（末尾必须调用），返回 Task<AttackCommand>
```

| 方法 | 说明 |
|------|------|
| `FromCard(CardModel)` | 伤害来源为卡牌 |
| `FromOsty(Creature, CardModel)` | 伤害来源为奥斯提宠物 |
| `FromMonster(MonsterModel)` | 伤害来源为怪物 |
| `Targeting(Creature)` | 指定单一目标 |
| `TargetingAllOpponents(CombatState)` | 攻击全部对手 |
| `TargetingRandomOpponents(CombatState, bool)` | 攻击随机目标（第二个参数：是否允许重复） |
| `WithHitFx(string, string, string)` | 特效/音效：vfx=Spine资源路径(res://animations/)，sfx=FMOD音效(.bank)，tmpSfx=调试音频(res://debug_audio/) |
| `WithHitCount(int)` | 攻击次数 |
| `Execute(CombatState)` | 执行攻击，返回 Task，可用 await 等待结束后再执行后续 |

### 卡牌回调

| 回调 | 触发时机 | 签名 |
|------|---------|------|
| `OnPlay` | 出牌时 | `Task OnPlay(PlayerChoiceContext, CardPlayState)` |
| `OnUpgrade` | 升级时 | `void OnUpgrade()` |
| `OnTurnEndInHand` | 回合结束时该牌仍在手牌 | `Task OnTurnEndInHand(PlayerChoiceContext)` |
| `IsPlayable` | 判断是否可打出 | 重写属性 `Func<bool>` |
| `ShouldGlowGoldInternal` | 判断是否发金光提示 | 回调 |

### 卡牌操作

```csharp
// 抽牌
CardPileCmd.Draw(state, 2);  // 抽2张牌

// 从手牌选择
CardSelectCmd.FromHand(context, player, prefs, filter, source);

// 从手牌选牌升级
CardSelectCmd.FromHandForUpgrade(context, player, filter, source);
// 返回 CardModel，需手动调用 UpgradeInternal()

// 从手牌选牌丢弃
CardSelectCmd.FromHandForDiscard(context, player, filter, source);
// 需手动调用 CardCmd.Discard(card)
```

#### CardSelectorPrefs 构造函数

```csharp
// 方式1：选择固定数量
new CardSelectorPrefs("选择一张牌", selectCount: 1);

// 方式2：选择数量范围
new CardSelectorPrefs("选择卡牌", minCount: 1, maxCount: 3);
```

#### 选择过滤器

```csharp
// 过滤器：接受 CardModel 参数，返回 bool
card => card is ExampleStrike  // 只选择某类卡牌
card => card.CanDiscard()      // 只选可丢弃的
```

### 卡牌标签（CardTag）

```csharp
// 指定卡牌标签（如"打击"类）
public override CardTag[] Tags => new[] { CardTag.Strike };
```

### 卡牌关键词（CanonicalKeywords）

```csharp
// 覆盖关键词，如"保留"
public override CardKeyword[] CanonicalKeywords => new[] { CardKeyword.Retain };
```

---

## 卡牌效果

在 `OnPlay` 中为角色施加效果：

```csharp
public override Task OnPlay(PlayerChoiceContext context, CardPlayState playState)
{
    if (context.Target == null) return Task.CompletedTask;
    var target = context.Target.Creature;

    return DamageCmd.Attack(Damage.Get(this))
        .FromCard(this)
        .Targeting(target)
        .Execute(playState.CombatState);
}
```

---

## 使用条件

```csharp
// 重写 IsPlayable 属性
public override bool IsPlayable(PlayerChoiceContext context)
{
    // 持有者金币 >= 100 才可打出
    return context.Player.Gold >= 100;
}

// 合适时机发金光提示
public override bool ShouldGlowGoldInternal(PlayerChoiceContext context)
{
    return context.Player.Gold >= 100;
}
```

---

## 添加卡牌到卡池

```csharp
// 通过属性添加到卡池
[CardPool(CardPools.Colorless)]  // 添加到无色卡池
public class ExampleStrike : CardModel { }

// 手动注册（在 ModEntry 中）
ModHelper.AddModelToPool(typeof(ExampleStrike), CardPools.Colorless);
```

常用卡池名：

| 池 | 说明 |
|-----|------|
| `CardPools.Colorless` | 无色 |
| `CardPools.Ironclad` | 铁甲战士 |
| `CardPools.Silent` | 静默猎手 |
| `CardPools.Defect` | 故障机器人 |
| `CardPools.Watcher` | 观者 |
| `CardPools.Necrobinder` | 亡灵契约师 |
| `CardPools.Regent` | 摄政者 |
| `CardPools.Amnesiac` | 迷失者 |

---

## 自定义卡牌肖像图

```
卡牌裁切纹理：res://images/atlases/card_atlas.sprites/<卡池名称>/<卡牌ID小写>.tres
卡牌大图源：  res://images/packed/card_portraits/<卡池名称>/<卡牌ID小写>.png
```

推荐分辨率：1000x760（或同比例）

---

## 卡牌本地化

路径：`res://<模组ID>/localization/<语言代码>/cards.json`

```json
{
  "ExampleStrike": {
    "name": "示例打击",
    "description": "造成 {D:diff()} 点伤害。"
  }
}
```

`{D:diff()}` 表示显示带差异（升级变化）的动态变量值。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 卡牌不显示 | 检查 `[CardPool]` 属性或手动调 `AddModelToPool` |
| 肖像图不显示 | PNG 路径/命名与卡池和卡牌 ID 一致 |
| 本地化不生效 | 必须 Publish 而非 Build |
| 卡牌无法打出 | 检查 `IsPlayable` 回调 + `TargetType` 是否匹配 |
| 升级数值不变 | 用 `UpgradeValueBy()` 而非直接改字段 |
| OnPlay 空引用 | 加 `if (context.Target == null) return;` 空值检查 |
| 特效不显示 | Spine 资源必须放在 `res://animations/` 下 |
| 音效文件 | FMOD 使用 `.bank` 文件；调试用 `res://debug_audio/` 下的路径 |

---

## 演进路线

- 当前写法：手动 `DamageCmd.Attack().FromCard().Targeting().Execute()` 链式调用
- 更优方案：可封装辅助方法简化重复链式调用（如 `DealDamage(this, target, value, fx)` 一行完成）
- 卡池注册：当前用 `[CardPool]` 属性，可参考 YuWanCard 的 Builder 模式 + 反射一键注册