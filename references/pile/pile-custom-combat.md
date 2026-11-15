# 自定义战斗牌堆（CustomPile）

> 参考 BaseLib v3.4.8 `Abstracts/CustomPile.cs` + `Patches/Content/CustomPilePatches.cs`。纯原生主语仍见 [pile-patches.md](pile-patches.md)；本文件补它们没覆盖的两件事：卡面节点/过场为什么会缺，以及 transpiler 怎么改才不跟别的 mod 打架。

## 原生层为什么不够

`CardPileCmd.Add` 系列把 `PileTypeExtensions.IsCombatPile()` 的结果**内联**用来判断「要不要造 NCard、走哪条 tween」。只补 `PlayerCombatState.AllPiles` / `CardPile.Get` 而不管这里，自定义堆里的卡就没有卡面节点、也没有过场动画——卡"消失"在逻辑里。

## 抽象成员（照 BaseLib 签名）

| 成员 | 说明 |
|---|---|
| `CustomPile(PileType pileType)` | 继承 `CardPile`，构造传枚举值 |
| `abstract bool CardShouldBeVisible(CardModel card)` | 该卡在这个堆里是否可见 |
| `virtual bool NeedsCustomTransitionVisual => false` | 是否需要自定义过场 |
| `abstract Vector2 GetTargetPosition(CardModel model, Vector2 size)` | 卡停在哪 |
| `virtual NCard? GetNCard(CardModel card)` | 自己造卡面节点（默认不造） |
| `virtual bool CustomTween(Tween tween, CardModel card, NCard cardNode, CardPile? oldPile)` | 自定义动画 |
| `virtual string? IconPath` / `virtual LocString? Name` | 堆图标与名称 |

注册：`CustomPiles.RegisterCustomPile(pileType, () => new MyPile(pileType))`，必须在**战斗创建之前**（模组初始化早期）调用；运行中注册不影响已存在的战斗。

## 要补的 Patch 点

- `GetCombatPile(PileType, Player)` 前缀：命中自定义堆就 `__result = GetCustomPile(state, type)`。
- `IsCombatPile(PileType)` 前缀：自定义堆返回 true。
- `CardPileCmd.Add` 的两个 async 状态机 transpiler：接上 `IsCombatPileForTransition(pileType, wasCombatPile)`——**插入不替换**（`Dup` 保留原生结果），详见 [design-patterns-compat.md](../baselib/design-patterns-compat.md) 第四节。
- `GetNCardPile` / `GetPilePosition` / `SpecialPileInCombat`：让 UI 能找到卡面节点与落点。

## 坑

① 只支持**战斗内**牌堆；战斗外（地图、事件、商店）用自定义堆拿不到 UI 与位置。② 注册表在首次访问 `PlayerCombatState.Piles` 时被快照，晚注册无效。③ 补丁目标签名随游戏版本变，跨版本必须分档、且「找不到目标方法就 Warn 后 return」。④ 自定义 `PileType` 枚举值的存档跨版本稳定性未验证——涉及持久化时优先用字符串 id。

## 演进路线

- 当前：静态注册表 + 三处 Patch + transpiler 插入式改写，已足够支撑「暂存堆 / 牌桌堆」
- 后续：过场动画与卡面节点若做成可配置（构造参数而非 override），能少写一半子类
