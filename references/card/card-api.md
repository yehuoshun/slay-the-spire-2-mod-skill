# 自定义卡牌：核心 API 与使用条件

## 核心 API

### DamageCmd — 伤害操作

```csharp
// 创建伤害操作
DamageCmd.Attack(<伤害值>)
    .FromCard(this)                    // 指定来源为卡牌
    .FromOsty(creature, this)          // 指定来源为奥斯提宠物
    .FromMonster(monster)              // 指定来源为怪物
    .Targeting(creature)               // 指定单一目标
    .TargetingAllOpponents(combatState)      // 攻击所有对手（ICombatState）
    .TargetingRandomOpponents(combatState)   // 攻击随机目标，可选参数是否允许重复
    .WithHitFx(vfxPath, sfxPath, tmpSfxPath)  // 指定特效/音效
    .WithHitCount(3)                   // 指定攻击次数
    .Execute(choiceContext);           // 执行（末尾必须调用，传 PlayerChoiceContext），返回 Task<AttackCommand>
```

| 方法 | 说明 |
|------|------|
| `FromCard(CardModel)` | 伤害来源为卡牌 |
| `FromOsty(Creature, CardModel)` | 伤害来源为奥斯提宠物 |
| `FromMonster(MonsterModel)` | 伤害来源为怪物 |
| `Targeting(Creature)` | 指定单一目标 |
| `TargetingAllOpponents(ICombatState)` | 攻击全部对手 |
| `TargetingRandomOpponents(ICombatState, bool)` | 攻击随机目标（第二个参数：是否允许重复，默认 true） |
| `WithHitFx(string, string, string)` | 特效/音效：vfx=Spine资源路径，sfx=FMOD音效(.bank)，tmpSfx=调试音频 |
| `WithHitCount(int)` | 攻击次数 |
| `Execute(PlayerChoiceContext?)` | 执行攻击，返回 Task，可用 await 等待结束后再执行后续 |

### 卡牌回调

| 回调 | 触发时机 | 签名 |
|------|---------|------|
| `OnPlay` | 出牌时 | `protected virtual Task OnPlay(PlayerChoiceContext, CardPlay)` |
| `OnUpgrade` | 升级时 | `protected virtual void OnUpgrade()` |
| `OnTurnEndInHand` | 回合结束时该牌仍在手牌 | `protected virtual Task OnTurnEndInHand(PlayerChoiceContext)` |
| `IsPlayable` | 判断是否可打出 | `protected virtual bool IsPlayable`（属性） |
| `ShouldGlowGoldInternal` | 判断是否发金光提示 | `protected virtual bool ShouldGlowGoldInternal`（属性） |

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
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

    await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
        .FromCard(this)
        .Targeting(cardPlay.Target)
        .Execute(choiceContext);
}
```

---

## 使用条件

```csharp
// 重写 IsPlayable 属性（protected virtual 属性，不是方法）
protected override bool IsPlayable => Owner.Gold >= 100;

// 合适时机发金光提示
protected override bool ShouldGlowGoldInternal => Owner.Gold >= 100;
```

> 拿玩家用 `Owner`（CardModel.Owner → Player），`context.Player` 不存在。

