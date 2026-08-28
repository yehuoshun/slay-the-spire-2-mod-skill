# API 附录：回调签名（真实）

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

