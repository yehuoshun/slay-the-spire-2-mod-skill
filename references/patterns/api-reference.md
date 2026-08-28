# API 附录

> 常用 API 速查索引（对照真实 `sts2-res/src/` 反编译源码）。各 API 详细用法见对应 reference 文件。

---

## 命名空间

| 命名空间 | 说明 |
|----------|------|
| `MegaCrit.Sts2.Core.Commands` | 命令类（DamageCmd/CardCmd/CreatureCmd/PowerCmd 等） |
| `MegaCrit.Sts2.Core.Models` | 模型基类（CardModel/RelicModel/PowerModel/PotionModel/EventModel 等） |
| `MegaCrit.Sts2.Core.Models.CardPools` | 卡池（ColorlessCardPool/IroncladCardPool 等） |
| `MegaCrit.Sts2.Core.Models.RelicPools` | 遗物池（IroncladRelicPool/SharedRelicPool 等） |
| `MegaCrit.Sts2.Core.Models.PotionPools` | 药水池（SharedPotionPool 等） |
| `MegaCrit.Sts2.Core.Entities.Cards` | 卡牌实体（CardModel/CardPlay/CardType/CardRarity/TargetType/CardTag/CardKeyword） |
| `MegaCrit.Sts2.Core.Entities.Creatures` | 生物实体（Creature） |
| `MegaCrit.Sts2.Core.Entities.Players` | 玩家实体（Player） |
| `MegaCrit.Sts2.Core.Entities.Powers` | 能力（PowerType/PowerStackType/PowerInstanceType） |
| `MegaCrit.Sts2.Core.Entities.Potions` | 药水（PotionRarity/PotionUsage） |
| `MegaCrit.Sts2.Core.Entities.Relics` | 遗物（RelicRarity） |
| `MegaCrit.Sts2.Core.Entities.Characters` | 角色（CharacterGender） |
| `MegaCrit.Sts2.Core.Combat` | 战斗（ICombatState/CombatSide/CombatRoom） |
| `MegaCrit.Sts2.Core.ValueProps` | 数值属性（ValueProp） |
| `MegaCrit.Sts2.Core.GameActions.Multiplayer` | 玩家选择上下文（PlayerChoiceContext） |
| `MegaCrit.Sts2.Core.Localization` | 本地化（LocString） |
| `MegaCrit.Sts2.Core.Localization.DynamicVars` | 动态变量（DynamicVar/EnergyVar/DamageVar 等） |
| `MegaCrit.Sts2.Core.Rooms` | 房间类型（RoomType） |
| `MegaCrit.Sts2.Core.Runs` | 运行状态（RunState/RunManager） |
| `MegaCrit.Sts2.Core.Modding` | 模组注册（ModHelper/ModInitializer） |
| `MegaCrit.Sts2.Core.MonsterMoves.Intents` | 怪物意图（AbstractIntent/SingleAttackIntent 等） |
| `MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine` | 怪物状态机（MoveState/RandomBranchState/ConditionalBranchState） |
| `MegaCrit.Sts2.Core.Helpers` | 工具（ImageHelper/SceneHelper） |
| `MegaCrit.Sts2.Core.Nodes.Combat` | 战斗节点（NCreatureVisuals/NEnergyCounter） |
| `MegaCrit.Sts2.Core.Nodes.Vfx` | 特效节点（NCardTrailVfx/NCardTrail） |
| `HarmonyLib` | Harmony 补丁库 |
| `Godot` | Godot 引擎 |

---

## 模型构造函数签名

| 模型 | 构造要点 | 文件 |
|------|---------|------|
| `CardModel` | `protected (int canonicalEnergyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary = true)` | card-v3 |
| `RelicModel` | `()` 无参；抽象属性 `RelicRarity Rarity` | relic-v3 |
| `PowerModel` | `()` 无参；抽象属性 `PowerType Type`/`PowerStackType StackType` | power-v3 |
| `PotionModel` | `()` 无参；抽象属性 `PotionRarity Rarity`/`PotionUsage Usage`/`TargetType TargetType` | potion-v3 |
| `EventModel` | `()` 无参；`protected abstract IReadOnlyList<EventOption> GenerateInitialOptions()` | event-v3 |
| `AncientEventModel` | `()` 无参；`protected abstract AncientDialogueSet DefineDialogues()` | ancient-v3 |
| `EncounterModel` | `()` 无参；抽象属性 `RoomType RoomType` | monster-v3 |
| `MonsterModel` | `()` 无参；抽象属性 `MinInitialHp`/`MaxInitialHp` | monster-v3 |
| `ModifierModel` | `()` 无参；Title/Description 自动本地化 | modifier-v3 |
| `CharacterModel` | `()` 无参；抽象属性 `NameColor`/`Gender`/`StartingHp`/`StartingGold` 等 | character-v3 |
| `ActModel` | `()` 无参；抽象属性 `Index`/`IsDefault` 等 | act-v3 |
| `OrbModel` | `()` 无参；抽象属性 `PassiveVal`/`EvokeVal`/`DarkenedColor` | orb-v3 |
| `EnchantmentModel` | `()` 无参；效果 override `Enchant*Additive/Multiplicative` | enchantment-v3 |

---

## 命令类（真实签名）

| 命令类 | 主要方法 | 说明 |
|--------|---------|------|
| `DamageCmd` | `Attack(decimal)` / `Attack(CalculatedDamageVar)` | 创建伤害操作（返回 `AttackCommand`） |
| `AttackCommand` | `.FromCard(CardModel)` `.FromMonster(MonsterModel)` `.FromOsty(Creature, CardModel)` `.Targeting(Creature)` `.TargetingAllOpponents(ICombatState)` `.TargetingRandomOpponents(ICombatState, bool)` `.WithHitFx(vfx, sfx, tmpSfx)` `.WithHitCount(int)` `.WithAttackerAnim(...)` `.Execute(PlayerChoiceContext?)` | 伤害链式调用 |
| `CardPileCmd` | `Draw(PlayerChoiceContext, decimal count, Player, bool fromHandDraw = false)` / `Add(CardModel, PileType)` / `AddCurseToDeck<T>(Player)` / `RemoveFromDeck(...)` | 抽牌/进牌组 |
| `CardSelectCmd` | `FromHand(context, player, CardSelectorPrefs, filter, source)` / `FromHandForUpgrade(context, player, source)` / `FromHandForDiscard(context, player, prefs, filter, source)` / `FromCombatPile(...)` / `FromChooseACardScreen(...)` | 选牌 |
| `CardCmd` | `Discard(PlayerChoiceContext, CardModel)` / `Exhaust(PlayerChoiceContext, CardModel, bool, bool)` / `Upgrade(CardModel)` / `Enchant<T>(CardModel, decimal)` / `Transform(...)` / `PreviewCardPileAdd(...)` | 卡牌操作 |
| `CreatureCmd` | `GainBlock(Creature, decimal, ValueProp, CardPlay?, bool)` / `GainBlock(Creature, BlockVar, CardPlay?, bool)` / `Heal(Creature, decimal, bool)` / `Damage(PlayerChoiceContext, target/targets, ...)` / `GainMaxHp(...)` / `LoseMaxHp(...)` | 生物操作（**格挡在这里，无 BlockCmd**） |
| `PowerCmd` | `Apply<T>(PlayerChoiceContext, Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false)` / `Decrement(PowerModel)` / `Remove(PowerModel?)` | 施加/移除能力 |
| `PlayerCmd` | `GainEnergy(decimal, Player)` / `GainGold(decimal, Player, bool wasStolenBack = false)` | 玩家操作 |
| `RelicCmd` | `Obtain(RelicModel, Player, int index = -1)` / `Remove(RelicModel)` / `Replace(...)` / `Melt(RelicModel)` | 遗物操作 |
| `VfxCmd` | `PlayOnCreature(Creature, string vfxPath)` | 特效 |

---

## 回调签名（真实）

| 回调 | 对象 | 签名 |
|------|------|------|
| `OnPlay` | CardModel | `protected virtual Task OnPlay(PlayerChoiceContext, CardPlay)` |
| `OnUpgrade` | CardModel | `protected virtual void OnUpgrade()` |
| `OnTurnEndInHand` | CardModel | `protected virtual Task OnTurnEndInHand(PlayerChoiceContext)` |
| `IsPlayable` | CardModel | `protected virtual bool IsPlayable`（**属性**） |
| `ShouldGlowGoldInternal` | CardModel | `protected virtual bool ShouldGlowGoldInternal`（**属性**） |
| `AfterSideTurnStart` | AbstractModel（遗物/能力/卡牌通用） | `Task AfterSideTurnStart(CombatSide, IReadOnlyList<Creature>, ICombatState)` |
| `BeforeSideTurnStart` | AbstractModel | `Task BeforeSideTurnStart(PlayerChoiceContext, CombatSide, IReadOnlyList<Creature>, ICombatState)` |
| `AfterSideTurnEnd` | AbstractModel | `Task AfterSideTurnEnd(PlayerChoiceContext, CombatSide, IEnumerable<Creature>)` |
| `AfterCombatEnd` / `AfterCombatVictory` | AbstractModel | `Task ...(CombatRoom)` |
| `AfterCardPlayed` | AbstractModel | `Task AfterCardPlayed(PlayerChoiceContext, CardPlay)` |
| `AfterDamageReceived` | AbstractModel | `Task AfterDamageReceived(PlayerChoiceContext, Creature target, DamageResult, ValueProp, Creature?, CardModel?)` |
| `ModifyDamageAdditive` | AbstractModel | `decimal ModifyDamageAdditive(Creature?, decimal, ValueProp, Creature?, CardModel?)` |
| `ModifyDamageMultiplicative` | AbstractModel | 同上 5 参 |
| `ModifyBlockAdditive` | AbstractModel | `decimal ModifyBlockAdditive(Creature, decimal, ValueProp, CardModel?, CardPlay?)` |
| `OnUse` | PotionModel | `protected virtual Task OnUse(PlayerChoiceContext, Creature? target)` |
| `OnPlay` | EnchantmentModel | `Task OnPlay(PlayerChoiceContext, CardPlay?)` |
| `EnchantDamageAdditive` | EnchantmentModel | `decimal EnchantDamageAdditive(decimal, ValueProp)` |
| `EnchantBlockAdditive` | EnchantmentModel | `decimal EnchantBlockAdditive(decimal)` |
| `GenerateInitialOptions` | EventModel | `protected abstract IReadOnlyList<EventOption> GenerateInitialOptions()` |
| `GenerateMoveStateMachine` | MonsterModel | `protected abstract MonsterMoveStateMachine GenerateMoveStateMachine()` |
| `Passive` | OrbModel | `Task Passive(PlayerChoiceContext, Creature?)` |
| `Evoke` | OrbModel | `Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext)` |

---

## 注册点

| 目标 | 方法 | 文件 |
|------|------|------|
| 卡牌到池 | `ModHelper.AddModelToPool<TPoolType, TModelType>()` 或 `(Type, Type)`；或 `[CardPool(typeof(池类))]` | serialization-v3 |
| 遗物到池 | `ModHelper.AddModelToPool<IroncladRelicPool, MyRelic>()`；或 `[RelicPool]` | serialization-v3 |
| 药水到池 | `ModHelper.AddModelToPool<SharedPotionPool, MyPotion>()`；或 `[PotionPool]` | serialization-v3 |
| 能力/球体/章节到 ModelDb | `ModelDb.Inject(typeof(X))` | serialization-v3 |
| 角色 | Patch `ModelDb.AllCharacters` getter（ref IEnumerable） | character-v3 |
| 事件 | Patch act 子类 `Overgrowth.AllEvents` getter（ref IEnumerable） | event-v3 |
| 先古之民 | Patch act 子类 `Hive.AllAncients` getter（ref IEnumerable） | ancient-v3 |
| 遭遇 | Patch act 子类 `Overgrowth.GenerateAllEncounters()`（实例方法） | monster-v3 |
| Modifier | Patch `NCustomRunModifiersList.GetModifiersTickedOn` | modifier-v3 |
| 序列化 | `SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(X))` | serialization-v3 |

> ⚠️ `ActModel.AllEvents`/`ActModel.AllAncients` 是抽象基类 getter，**Patch 基类不拦截子类实现**——要 Patch 具体 act 子类（`Overgrowth`/`Hive`/`Glory`/`Underdocks`）。池的注册由角色类 `CardPool`/`RelicPool`/`PotionPool` 属性关联，**无需** Patch `ModelDb.All*Pools` getter。

---

## 文件索引

| 文件 | 内容 |
|------|------|
| `references/card/card.md` | 自定义卡牌（v3 当前） |
| `references/relic/relic.md` | 自定义遗物（v3） |
| `references/power/power.md` | 自定义能力（v3） |
| `references/potion/potion.md` | 自定义药水（v3） |
| `references/enchantment/enchantment.md` | 自定义附魔（v3） |
| `references/event/event.md` | 自定义事件（v3） |
| `references/event/ancient.md` | 先古之民事件（v3） |
| `references/character/character.md` | 自定义角色（v3） |
| `references/monster/monster.md` | 自定义敌怪 & 遭遇（v3） |
| `references/modifier/modifier.md` | 自定义 Modifier（v3） |
| `references/orb/orb.md` | 自定义球体（v3） |
| `references/act/act.md` | 自定义章节（v3） |
| `references/pet/pet.md` | 自定义宠物（v3） |
| `references/harmony/harmony.md` | Harmony 补丁模式 |
| `references/serialization/serialization.md` | 序列化与注册（v3） |
| `references/settings/settings.md` | 设置界面（BaseLib，需转纯原生） |
| `references/baselib/design-patterns.md` | 纯原生设计模式总纲 |
| `references/setup/environment-setup.md` | 环境搭建 |
| `references/setup/rider.md` | Rider 开发环境配置 |
| `references/patterns/code-patterns.md` | 实战写法模式（v3 待校） |
