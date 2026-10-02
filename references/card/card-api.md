# 自定义卡牌：核心 API 与使用条件

## 章节导航

| 内容 | 文件 |
|------|------|
| 卡牌效果完整示例 | [card-api-effects.md](card-api-effects.md) |

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

### PowerCmd — 施加能力

```csharp
// 真实签名（silent 有默认值，可省略）
await PowerCmd.Apply<T>(choiceContext, target, amount, applier, cardSource);
// = Apply<T>(PlayerChoiceContext, Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false)

// 非泛型版（运行时才知道能力类型）：
await PowerCmd.Apply(choiceContext, powerInstance, target, amount, applier, cardSource);
```

> ⚠️ 第一参必是 `PlayerChoiceContext`。无上下文（如遗物/修改器钩子里）用 `new ThrowingPlayerChoiceContext()`（实战项目 YuWanCard 真实用法）。

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

> 选择器（CardSelectorPrefs / 过滤器 / 完整片段）→ [card-api-select.md](card-api-select.md)

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

## 使用条件

```csharp
// 重写 IsPlayable 属性（protected virtual 属性，不是方法）
protected override bool IsPlayable => Owner.Gold >= 100;

// 合适时机发金光提示
protected override bool ShouldGlowGoldInternal => Owner.Gold >= 100;
```

> 拿玩家用 `Owner`（CardModel.Owner → Player），`context.Player` 不存在。