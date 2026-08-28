# 自定义卡牌：进阶写法与资源

## 进阶：链式辅助方法

> 从 BaseLib `ConstructedCardModel` Builder 模式提炼。原生 API 本就支持链式（`DamageCmd.Attack().FromCard().Targeting().Execute()`），再包一层静态辅助，把高频操作收敛成一行。

### CardFx 静态辅助类

```csharp
public static class CardFx
{
    // 攻击指定目标
    public static Task DealDamage(this CardModel card, PlayerChoiceContext ctx,
        CardPlay play, int damage, int hits = 1, string? hitFx = null)
    {
        ArgumentNullException.ThrowIfNull(play.Target, "play.Target");
        var cmd = DamageCmd.Attack(damage).FromCard(card).Targeting(play.Target)
            .WithHitCount(hits);
        if (hitFx != null) cmd = cmd.WithHitFx(hitFx);
        return cmd.Execute(ctx);
    }

    // 攻击所有敌人
    public static Task DealDamageAll(this CardModel card, PlayerChoiceContext ctx,
        CardPlay play, int damage)
        => DamageCmd.Attack(damage).FromCard(card)
            .TargetingAllOpponents(card.CombatState).Execute(ctx);

    // 抽牌（真实签名：Draw(PlayerChoiceContext, decimal count, Player, bool)）
    public static Task Draw(this CardModel card, PlayerChoiceContext ctx, int count)
        => CardPileCmd.Draw(ctx, count, card.Owner, false);

    // 对目标施加能力（真实签名：Apply<T>(ctx, Creature, decimal, applier, cardSource)）
    public static async Task ApplyPower<T>(this CardModel card, PlayerChoiceContext ctx,
        CardPlay play, int amount) where T : PowerModel
    {
        if (play.Target == null) return;
        await PowerCmd.Apply<T>(ctx, play.Target, amount, play.Target, card);
    }
}

// 用法：攻击 + 抽牌
protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
{
    ArgumentNullException.ThrowIfNull(play.Target, "play.Target");
    await Task.WhenAll(
        this.DealDamage(ctx, play, 6),
        this.DealDamage(ctx, play, 3));
}
```

### 便捷 override（转译 BaseLib 自动推断）

```csharp
// BaseLib 会从 DynamicVars 自动推断 GainsBlock；纯原生手动 override 即可
public override bool GainsBlock => true;   // 该卡给格挡

// 计算变量辅助（一次生成 Base/Extra/主变量）
protected override IEnumerable<DynamicVar> CanonicalVars =>
    CustomCalculatedVar.Create("Damage", 5, (src, creature) => 0m, 2);
```

> `CustomCalculatedVar.Create` 签名见 [design-patterns.md](../baselib/design-patterns.md)「模式 3」与参考 API 附录。

### 注意

- 辅助方法只是收敛重复，内部仍走原生命令，行为与手写完全一致
- 扩展方法类名带 `CardFx` 前缀，避免与其他 Mod 命名冲突
- 需要原生链式的高级用法（多段伤害、特效、随机目标）时，直接用原生链式即可

## 添加卡牌到卡池

```csharp
// 方式①：attribute + ContentRegistry 自动注册（推荐，硬规则4-①，见 design-patterns 模式1）
[CardPool(typeof(ColorlessCardPool))]  // 添加到无色卡池
public class ExampleStrike : CardModel { }

// 方式②：手动注册（在 ModEntry 中，硬规则4-②）
ModHelper.AddModelToPool(typeof(ColorlessCardPool), typeof(ExampleStrike));
```

常用卡池名：

| 池 | 说明 |
|-----|------|
| `ColorlessCardPool` | 无色 |
| `IroncladCardPool` | 铁甲战士 |
| `SilentCardPool` | 静默猎手 |
| `DefectCardPool` | 故障机器人 |
| `NecrobinderCardPool` | 亡灵契约师 |
| `RegentCardPool` | 摄政者 |
| `EventCardPool` | 事件牌（不随机生成） |
| `TokenCardPool` | 衍生物（小刀/灵魂等） |

> 完整池列表见源码 `Core/Models/CardPools/`（另有 Curse/Status/Quest/Deprecated 等边界池）。

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

