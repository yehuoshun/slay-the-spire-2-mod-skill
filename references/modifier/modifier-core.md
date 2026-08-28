# 自定义 Modifier：模板与可覆盖成员

## 基础模板

```csharp
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

public class MyModifier : ModifierModel
{
    // 可选：开局清空牌组
    public override bool ClearsPlayerDeck => false;

    // 运行创建时：改全局配置（真实写法，对照 DeadlyEvents）
    protected override void AfterRunCreated(RunState runState)
    {
        // 例如：runState.Odds.UnknownMapPoint.EliteOdds = 0.1f;
    }

    // 效果示例：所有攻击伤害 +2（Modify 钩子直接 override）
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer?.Player != null && props.IsPoweredAttack())
            return amount + 2m;
        return amount;
    }
}
```

> ⚠️ 旧版写的 `Alignment`/`MutuallyExclusiveGroup`/`SortOrder`/`ModifierAlignment` 是 **BaseLib `CustomModifierModel`** 的，原生 `ModifierModel` 不存在。Custom Run UI 的"正面/负面"分类由引擎根据规则性质自动处理。

---

## 可覆盖成员（真实存在）

| 成员 | 类型 | 说明 |
|------|------|------|
| `Title` / `Description` | `public virtual LocString` | 自动从 `modifiers` 本地化表生成，无需手写 |
| `NeowOptionTitle` / `NeowOptionDescription` | `public virtual LocString` | Neow 选项标题/描述 |
| `IconPath` | `protected virtual string` | 图标路径（命名约定 `packed/modifiers/<id小写>.png`） |
| `ClearsPlayerDeck` | `public virtual bool` | 开局清空牌组 |
| `GenerateNeowOption(EventModel)` | `public virtual Func<Task>?` | 生成 Neow 选项（AllStar 用它发卡） |
| `AfterRunCreated(RunState)` | `protected virtual void` | 运行创建时改配置 |
| `AfterRunLoaded(RunState)` | `protected virtual void` | 读档后改配置 |
| `IsEquivalent(ModifierModel)` | `public virtual bool` | 判定是否等价（避免重复） |

> 效果类 `Modify*` 钩子（伤害/格挡/地图/奖励）来自 `AbstractModel`，见下文。

