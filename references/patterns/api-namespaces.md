# API 附录：命名空间与构造函数签名

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
| `CardModel` | `protected (int canonicalEnergyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary = true)` | card |
| `RelicModel` | `()` 无参；抽象属性 `RelicRarity Rarity` | relic |
| `PowerModel` | `()` 无参；抽象属性 `PowerType Type`/`PowerStackType StackType` | power |
| `PotionModel` | `()` 无参；抽象属性 `PotionRarity Rarity`/`PotionUsage Usage`/`TargetType TargetType` | potion |
| `EventModel` | `()` 无参；`protected abstract IReadOnlyList<EventOption> GenerateInitialOptions()` | event |
| `AncientEventModel` | `()` 无参；`protected abstract AncientDialogueSet DefineDialogues()` | ancient |
| `EncounterModel` | `()` 无参；抽象属性 `RoomType RoomType` | monster |
| `MonsterModel` | `()` 无参；抽象属性 `MinInitialHp`/`MaxInitialHp` | monster |
| `ModifierModel` | `()` 无参；Title/Description 自动本地化 | modifier |
| `CharacterModel` | `()` 无参；抽象属性 `NameColor`/`Gender`/`StartingHp`/`StartingGold` 等 | character |
| `ActModel` | `()` 无参；抽象属性 `Index`/`IsDefault` 等 | act |
| `OrbModel` | `()` 无参；抽象属性 `PassiveVal`/`EvokeVal`/`DarkenedColor` | orb |
| `EnchantmentModel` | `()` 无参；效果 override `Enchant*Additive/Multiplicative` | enchantment |

