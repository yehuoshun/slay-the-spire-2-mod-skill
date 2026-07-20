# API 附录

> 常用 API 速查索引。各 API 详细用法见对应 reference 文件。

---

## 命名空间

| 命名空间 | 说明 |
|----------|------|
| `MegaCrit.Sts2.Core.Commands` | 命令类（DamageCmd/CardPileCmd 等） |
| `MegaCrit.Sts2.Core.Models` | 所有模型基类（CardModel/RelicModel/PowerModel 等） |
| `MegaCrit.Sts2.Core.Models.Cards` | 卡牌相关 |
| `MegaCrit.Sts2.Core.Models.CardPools` | 卡池（ColorlessCardPool/IroncladCardPool 等） |
| `MegaCrit.Sts2.Core.Models.Relics` | 遗物相关 |
| `MegaCrit.Sts2.Core.Models.Potions` | 药水相关 |
| `MegaCrit.Sts2.Core.Models.Powers` | 能力相关 |
| `MegaCrit.Sts2.Core.Combat` | 战斗相关（CombatState/CombatManager） |
| `MegaCrit.Sts2.Core.Entities.Cards` | 卡牌实体（CardModel 等） |
| `MegaCrit.Sts2.Core.Entities.Creatures` | 生物实体（Creature） |
| `MegaCrit.Sts2.Core.Entities.Players` | 玩家实体（Player） |
| `MegaCrit.Sts2.Core.Entities.Powers` | 能力实体 |
| `MegaCrit.Sts2.Core.Localization` | 本地化（LocString/LocManager） |
| `MegaCrit.Sts2.Core.Localization.DynamicVars` | 动态变量（DynamicVar/EnergyVar 等） |
| `MegaCrit.Sts2.Core.Rooms` | 房间类型（CombatRoom/RoomType） |
| `MegaCrit.Sts2.Core.Runs` | 运行状态（RunManager/RunState） |
| `MegaCrit.Sts2.Core.Modding` | 模组注册（ModHelper/ModInitializer） |
| `MegaCrit.Sts2.Core.Saves.Runs` | 存档序列化 |
| `MegaCrit.Sts2.Core.Multiplayer.Game` | 多人游戏 |
| `MegaCrit.Sts2.Core.Multiplayer.Serialization` | 多人序列化 |
| `MegaCrit.Sts2.Core.Helpers` | 工具（ImageHelper） |
| `MegaCrit.Sts2.Core.Nodes.Screens` | 游戏界面 |
| `MegaCrit.Sts2.Core.Nodes.Rooms` | 房间节点 |
| `MegaCrit.Sts2.Core.Nodes.Combat` | 战斗节点 |
| `MegaCrit.Sts2.Core.Map` | 地图节点 |
| `MegaCrit.Sts2.Core.Events` | 事件系统 |
| `MegaCrit.Sts2.Core.MonsterMoves` | 怪物行动 |
| `MegaCrit.Sts2.Core.MonsterMoves.Intents` | 怪物意图 |
| `HarmonyLib` | Harmony 补丁库 |
| `Godot` | Godot 引擎 |

---

## 模型构造函数签名

| 模型 | 构造函数 | 文件 |
|------|---------|------|
| `CardModel` | `(int baseCost, CardType type, CardRarity rarity, TargetType target, bool showInCardLibrary = true)` | card.md |
| `RelicModel` | `()` 无参数 | relic.md |
| `PowerModel` | `()` 无参数 | power.md |
| `PotionModel` | `()` 无参数 | potion.md |
| `EventModel` | `()` 无参数 | event.md |
| `AncientEventModel` | `()` 无参数 | ancient.md |
| `EncounterModel` | `()` 无参数 | monster.md |
| `MonsterModel` | `()` 无参数 | monster.md |
| `ModifierModel` | `()` 无参数 | modifier.md |
| `CharacterModel` | `()` 无参数 | character.md |
| `ActModel` | `()` 无参数 | act.md |
| `OrbModel` | `()` 无参数 | orb.md |
| `EnchantmentModel` | `()` 无参数 | enchantment.md |

---

## 命令类

| 命令类 | 主要方法 | 说明 |
|--------|---------|------|
| `DamageCmd` | `Attack(val)` | 创建伤害操作 |
| `AttackCommand` | `.FromCard()` `.Targeting()` `.Execute()` | 伤害链式调用 |
| `CardPileCmd` | `Draw(state, count)` | 抽牌 |
| `CardSelectCmd` | `FromHand()` `FromHandForUpgrade()` `FromHandForDiscard()` | 选牌 |
| `CardCmd` | `Obtain()` `Discard()` `Exhaust()` | 卡牌操作 |
| `BlockCmd` | `GainBlock(creature, amount)` | 获得格挡 |
| `PowerCmd` | `Apply<T>(creature, amount)` | 施加能力 |
| `PlayerCmd` | `GainEnergy(state, amount)` | 玩家操作 |
| `RelicCmd` | `Obtain()` `Remove()` | 遗物操作 |

---

## 回调签名

| 回调 | 对象 | 签名 |
|------|------|------|
| `OnPlay` | CardModel | `Task OnPlay(PlayerChoiceContext, CardPlayState)` |
| `OnUpgrade` | CardModel | `void OnUpgrade()` |
| `OnTurnEndInHand` | CardModel | `Task OnTurnEndInHand(PlayerChoiceContext)` |
| `IsPlayable` | CardModel | `bool IsPlayable(PlayerChoiceContext)` |
| `AfterSideTurnStart` | RelicModel | `Task AfterSideTurnStart(Side, ICombatState)` |
| `AfterCombatEnd` | RelicModel | `Task AfterCombatEnd(PlayerChoiceContext)` |
| `AfterCardPlayed` | RelicModel | `Task AfterCardPlayed(PlayerChoiceContext, CardPlayState)` |
| `OnPlayerDamaged` | RelicModel | `Task OnPlayerDamaged(PlayerChoiceContext, int)` |
| `OnCardPlayed` | PowerModel | `Task OnCardPlayed(PlayerChoiceContext, CardPlayState)` |
| `OnTurnStart` | PowerModel | `Task OnTurnStart(PlayerChoiceContext)` |
| `OnTurnEnd` | PowerModel | `Task OnTurnEnd(PlayerChoiceContext)` |
| `OnTakeDamage` | PowerModel | `Task OnTakeDamage(PlayerChoiceContext, DamageInfo)` |
| `OnUse` | PotionModel | `Task OnUse(PlayerChoiceContext, PotionTarget)` |
| `ShouldDie` | PotionModel | `Task<bool> ShouldDie(PlayerChoiceContext, Creature)` |
| `GenerateInitialOptions` | EventModel | `Task<List<EventOption>> GenerateInitialOptions()` |
| `GenerateMoveStateMachine` | MonsterModel | `MonsterMoveStateMachine GenerateMoveStateMachine()` |
| `Passive` | OrbModel | `Task Passive(CombatState)` |
| `Evoke` | OrbModel | `Task Evoke(CombatState)` |

---

## 注册点

| 目标 | 方法 | 文件 |
|------|------|------|
| 卡牌到池 | `ModHelper.AddModelToPool<T>(typeof(X))` | serialization.md |
| 遗物到池 | `ModHelper.AddModelToPool<T>(typeof(X))` | serialization.md |
| 药水到池 | `ModHelper.AddModelToPool<T>(typeof(X))` | serialization.md |
| 能力到 ModelDb | `ModelDb.Inject(typeof(X))` | serialization.md |
| 角色 | Patch `ModelDb.AllCharacters` getter | character.md |
| 卡池 | Patch `ModelDb.AllCardPools` getter | character.md |
| 遗物池 | Patch `ModelDb.AllRelicPools` getter | character.md |
| 药水池 | Patch `ModelDb.AllPotionPools` getter | character.md |
| 事件 | Patch `ActModel.AllEvents` getter | event.md |
| 先古之民 | Patch `ActModel.AllAncients` getter | ancient.md |
| 遭遇 | Patch `GenerateAllEncounters` | monster.md |
| Modifier | Patch `GetModifiersTickedOn` | modifier.md |
| 序列化 | `SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(X))` | serialization.md |

---

## 文件索引

| 文件 | 内容 |
|------|------|
| `references/card/card.md` | 自定义卡牌 |
| `references/relic/relic.md` | 自定义遗物 |
| `references/power/power.md` | 自定义能力 |
| `references/potion/potion.md` | 自定义药水 |
| `references/enchantment/enchantment.md` | 自定义附魔 |
| `references/event/event.md` | 自定义事件 |
| `references/event/ancient.md` | 先古之民事件 |
| `references/character/character.md` | 自定义角色 |
| `references/monster/monster.md` | 自定义敌怪 & 遭遇 |
| `references/modifier/modifier.md` | 自定义 Modifier |
| `references/orb/orb.md` | 自定义球体 |
| `references/act/act.md` | 自定义章节 |
| `references/pet/pet.md` | 自定义宠物 |
| `references/harmony/harmony.md` | Harmony 补丁模式 |
| `references/serialization/serialization.md` | 序列化与注册 |
| `references/settings/settings.md` | 设置界面 |
| `references/baselib/baselib.md` | BaseLib 集成指南 |
| `references/setup/environment-setup.md` | 环境搭建 |
| `references/setup/rider.md` | Rider 开发环境配置 |
| `references/patterns/code-patterns.md` | 实战写法模式 |