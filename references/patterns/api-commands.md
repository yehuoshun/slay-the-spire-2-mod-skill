# API 附录：命令类（真实签名）

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

