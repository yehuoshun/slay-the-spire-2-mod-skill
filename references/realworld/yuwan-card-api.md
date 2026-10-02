# 真实 API 校验结论（YuWanCard）

> 对照反编译源码 `sts2-res/src/` 与 YuWanCard 真实代码，逐条确认/纠错本 skill 的 API 知识。

## ✅ 已确认一致（放心使用）

| API | 真实签名（sts2-res 反编译验证） |
|-----|------|
| `DamageCmd.Attack` | `Attack(decimal)` / `Attack(CalculatedDamageVar)`，返回 `AttackCommand` |
| 伤害链 | `.FromCard(CardModel)` `.Targeting(Creature)` `.WithHitFx(string)` `.WithHitCount(decimal)` `.Execute(PlayerChoiceContext)` |
| `PowerCmd.Apply<T>` | `Apply<T>(PlayerChoiceContext, Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent=false)` |
| `PowerCmd.Apply`（非泛型） | `Apply(PlayerChoiceContext, PowerModel power, Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent=false)` |
| `CreatureCmd` | `GainBlock` / `Heal` / `GainMaxHp` / `SetMaxHp` / `Damage` 均确认 |
| 能力钩子 | `AfterSideTurnStart(CombatSide, IReadOnlyList<Creature>, ICombatState)` 确认（旧名 `BeforeTurnEnd` 在 0.110.x 改名 `BeforeSideTurnEnd`） |
| 卡牌钩子 | `OnPlay(PlayerChoiceContext, CardPlay)` / `OnUpgrade()` / `CanPlay` 确认 |
| 卡牌池 | `MegaCrit.Sts2.Core.Models.CardPools` 命名空间确认 |
| `[Pool]` 注册 | `ModHelper.AddModelToPool` + Attribute 扫描注册可行（YuWan 自研 ContentRegistry 即此思路） |
| `PowerStackType` | 引擎只有 `Counter` / `Single` / `None` 三种 |
| `PowerType` | `Buff` / `Debuff` / `Neutral` 三种 |

## ⚠️ 纠错/补充（skill 原有内容不准或缺失）

| 主题 | skill 旧说法 | 真实情况 |
|------|-------------|---------|
| `PowerCmd.Apply<T>` 参数 | 曾写 4 参（target, amount, applier, card） | **第一参必是 `PlayerChoiceContext`**，共 6-7 参。无上下文时可 `new ThrowingPlayerChoiceContext()` |
| `TargetType` | 缺项 | 完整 10 种：`None/Self/AnyEnemy/AllEnemies/RandomEnemy/AnyAlly/AllAllies/AnyPlayer/TargetedNoCreature/Osty` |
| `CardRarity` | 缺项 | 完整 10 种：`Basic/Common/Uncommon/Rare/Ancient/Event/Token/Curse/Status/Quest` |
| `CardKeyword` | 缺项 | 内置含 `Exhaust/Ethereal/Innate/Unplayable/Retain/Sly/Eternal`，自定义用 `ModCardTagRegistry` |
| 能力本地化 | 只提 `smartDescription` | 三字段分工：`description`（静态，图鉴）、`smartDescription`（动态变量，战斗悬浮）、`remoteDescription`（多人对方视角）。**卡牌 description 自动注入 DynamicVar，能力 description 不注入** |
| 遗物稀有度 | 缺项 | 含 `Starter/Common/Uncommon/Rare/Shop/Event/Ancient/None(+CustomRarity)` |
| 升级机制 | 「OnUpgrade 必须调 UpgradeValueBy」 | 真实框架用 Harmony 后置 Patch `UpgradeInternal` 自动应用所有 DynamicVar 升级值；`OnUpgrade` 只写额外逻辑。纯原生下仍建议在 `OnUpgrade` 手写（原生无自动机制） |

## 🔍 新发现（skill 空白，详见各模式文件）

- `WithKeyword(keyword, UpgradeType.Add/Remove)` 声明式关键字升级
- `WithCalculatedDamage` + `CalculationBaseVar/ExtraDamageVar/CalculatedDamageVar` 三变量
- `BaseReplayCount` + `DeckVersion` + `[SavedProperty]` 跨战斗持久化
- `CardMultiplayerConstraint`（MultiplayerOnly/SingleplayerOnly）
- `LocalContext.IsMe(player)` 多人身份检查
- 自定义遗物稀有度（`RelicRarity.None` + 6 个 UI Patch）
- `IHealthBarForecastSource` 生命条预测
- `[SavedProperty]` 属性命名建议带模组前缀（如 `YUWANCARD_`），避免警告

## 引用注意

真实代码片段多依赖 YuWan 自研基类（`YuWanCardModel` 等），**只取原生 API 调用部分**，基类替换为原生 `CardModel`/`PowerModel`/`RelicModel` 后即可用。
