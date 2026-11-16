# 回合末延迟交互 Power（挂载 → 回合末结算）

> 学自 [lf201014/STS2_MarisaMod](https://github.com/lf201014/STS2_MarisaMod) 2026-11-14（`000c33f` → `7d04b1b`，宇宙热寂 `HeatDeathOfUniverse` 重做）。

## 什么时候需要

卡牌效果要在**回合结束时**才向玩家要选择（"回合结束时，每名玩家任选任意张手牌消耗，每消耗一张你获得 1 层蓄能"）。写在 `OnPlay` 里只能在打出瞬间结算；把选择挪进 Power，让引擎在自己的回合末回调里弹界面。

| 方案 | 结算时机 | 适用 |
|------|---------|------|
| 卡 `OnPlay` 里直接 `CardSelectCmd.FromHand` | 打出瞬间 | 立即生效（见 [card-api-select.md](../card/card-api-select.md)） |
| 卡挂 Power，Power 在 `BeforeSideTurnEnd` 里选牌 | 本回合结束时 | 卡面写"回合结束时"，或要让本回合其它结算先走完 |

## 代码模板

### 1) 卡：只负责挂载，不做选择

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    // 每个存活队友的角色各挂一份：多人时每人选自己的手牌，互不串扰
    var playersAvail = CombatState!.GetTeammatesOf(Owner.Creature)
        .Where(c => c is { IsAlive: true, IsPlayer: true })
        .Select(c => c.Player!)
        .Distinct()                        // 同一玩家多角色时防重复弹窗
        .ToList();

    foreach (var player in playersAvail)
    {
        var pow = await PowerCmd.Apply<HeatDeathOfUniversePower>(
            choiceContext, player.Creature, 1, Owner.Creature, this);
        if (pow != null) pow.PowerTarget = player.Creature;   // 用返回值回填参数
    }
}
```

### 2) Power：回合末交互 + 结算完自移除

```csharp
public class HeatDeathOfUniversePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public Creature PowerTarget;      // 由卡在 Apply 之后回填

    // 己方回合结束前：弹选择并结算
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        if (Owner.Player == null) return;      // 挂在怪物身上时没有 Player

        var sum = 0;
        foreach (var item in await CardSelectCmd.FromHand(choiceContext, Owner.Player,
                     new CardSelectorPrefs(SelectionScreenPrompt, 0, 99), null, this))
        {
            await CardCmd.Exhaust(choiceContext, item);
            sum += 1;
        }

        if (PowerTarget != null)
            await PowerCmd.Apply<ChargeUpPower>(choiceContext, PowerTarget, sum, Owner, null);
    }

    // 己方回合结束后：整个移除（一次性开关型 Power）
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == Owner.Side) await PowerCmd.Remove(this);
    }
}
```

### 3) 本地化：Power 自己也要一条 `.selectionScreenPrompt`

选择界面的标题取的是**发起选择那个模型**的 `SelectionScreenPrompt` 属性，所以 Power 要在 `localization/<lang>/powers.json` 里补齐 4 条键（key 前缀按各 mod 的 ID 约定）：

```json
"MARISAMOD-HEAT_DEATH_OF_UNIVERSE_POWER.title": "宇宙热寂",
"MARISAMOD-HEAT_DEATH_OF_UNIVERSE_POWER.description": "回合结束时选择任意牌[gold]消耗[/gold]。",
"MARISAMOD-HEAT_DEATH_OF_UNIVERSE_POWER.smartDescription": "回合结束时选择任意牌[gold]消耗[/gold]。",
"MARISAMOD-HEAT_DEATH_OF_UNIVERSE_POWER.selectionScreenPrompt": "选择任意牌[gold]消耗[/gold]"
```

纯原生的本地化键写法见 [card-advanced.md](../card/card-advanced.md) §卡牌本地化，占位符/条件块见 [card-localization.md](../card/card-localization.md)。

## 要点

- **两个钩子的 `side` 判断方向相反**：`BeforeSideTurnEnd` 里 `side != Owner.Side` 就 `return`（只在己方回合末弹）；`AfterSideTurnEnd` 里 `side == Owner.Side` 才移除。两处写成一个方向，会出现"敌方回合末也弹选择"或"自己回合末不消失"。
- **引擎先 `Before` 后 `After`**：交互发生在移除之前，不需要额外的 pending 标记。
- **`PowerStackType.Single` + 每次 Apply 1 层** = 一次性开关；用 `Counter` 会被叠成没有意义的数字。
- **`PowerCmd.Apply<T>` 返回新挂的实例**（`var pow = await PowerCmd.Apply<T>(...)`）：`if (pow != null) pow.Field = ...` 是把"这张卡针对谁 / 记多少"从卡传给 Power 的轻量做法——不改构造函数、不用 ID 反查、也不依赖全局表。返回 `null` 表示这次没挂上（被拦截/无效目标），回填前必须判空。
- **交互上下文必须用钩子参数 `choiceContext`**，不要 `new ThrowingPlayerChoiceContext()`（那只给同步签名回调兜底，见 [power-advanced.md](power-advanced.md) 增益扩散器）。
- **`cardSource` 传 `this`**：这次施加在日志/统计里归属到这个 Power 模型，而不是卡。
- **卡面文案同步改**：机制从"打出时"改成"回合结束时"，描述必须写"回合结束时"，否则玩家会以为立即结算。上游同批提交把卡面改成"回合结束时，所有玩家选择任意数量手牌消耗。每消耗一张牌，你获得 1 层蓄力。"，并把 Power 描述写成"回合结束时选择任意牌消耗。"

## 常见问题

| 现象 | 原因 / 处置 |
|------|------------|
| 回合末没弹选择 | ① `side != Owner.Side` 方向写反；② Power 挂在怪物侧，`Owner.Player` 为 null |
| 同一玩家弹两次 | 卡里漏了 `Distinct()`（一个玩家带多个角色时各挂一份） |
| 选完了不结算 | `PowerCmd.Apply<资源Power>` 的 target 传错：应传受益者（上游传 `PowerTarget`），不是 `Owner` |
| 选择框标题显示成键名 | `powers.json` 缺 `<Power ID>.selectionScreenPrompt` |
| Power 一直不消失 | `AfterSideTurnEnd` 漏写或 `side` 条件写错；战斗结束不会再走回合末钩子，随战斗清场统一清掉，不必额外写兜底 |
