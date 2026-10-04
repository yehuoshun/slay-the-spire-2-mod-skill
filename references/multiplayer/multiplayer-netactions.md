# 多人：网络行动与交互状态防护

> 实战验证（YuWanCard 2026-10-04 增量）。跨端第二条路线：走**行动队列**（`GameAction` + `INetAction`），与 `INetMessage` 对等消息互补。

## 1. 原生行动骨架

游戏里"玩卡/用药/结束回合"都走 `GameAction`：主机权威排序，各端按同一顺序执行。入口 `RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(action)`（host 直接入队；client 发请求给 host）。

```csharp
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;

public sealed class MyAction(Player player) : GameAction
{
    public Player Player { get; } = player;
    public override ulong OwnerId => Player.NetId;                       // 归属玩家
    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;
    protected override async Task ExecuteAction() { /* 效果 */ }
    public override INetAction ToNetAction() => /* 网络封包 */ null!;
}

RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new MyAction(player));
```

`INetAction : IPacketSerializable` 负责 `Serialize`/`Deserialize` + `ToGameAction(Player)`。

## 2. GameActionType 语义（决定取消/入队时机）

| 值 | 语义 |
|----|------|
| `Combat` | 战斗结束或战斗外入队 → 取消 |
| `CombatPlayPhaseOnly` | 同上；且在非 PlayPhase 请求会**延迟到玩家 PlayPhase**（非丢弃）——玩卡/用药用它 |
| `NonCombat` | 结束战斗不取消；战斗中入队要等战斗结束才执行 |
| `Any` | 任意时机可执行，不因战斗结束取消 |

> `RequestEnqueue` 只对 `CombatPlayPhaseOnly` + `NotPlayPhase` 做延迟，其余立即派发。

## 3. 阶段门控（判断此刻能否发起交互）

战斗阶段读 `RunManager.Instance.ActionQueueSynchronizer.CombatState`，枚举 `ActionSynchronizerCombatState`：`NotInCombat` / `PlayPhase`（玩家完全可控）/ `EndTurnPhaseOne` / `NotPlayPhase`（敌方回合+抽牌）。

```csharp
static bool CanActNow()
    => RunManager.Instance?.ActionQueueSynchronizer.CombatState == ActionSynchronizerCombatState.PlayPhase;
```

玩家是否已按"结束回合"：`CombatManager.Instance.IsPlayerReadyToEndTurn(player)`。
⚠️ 别再用 `CombatManager.Instance.IsEnding` 当"能否操作"判据——结束回合有多个子阶段，用 `CombatState` / `IsPlayerReadyToEndTurn` 更准。

## 4. 执行失败必须抛出

自定义行动/交互的执行 catch 里**记录后要 `throw;`**，不能吞：

```csharp
catch (Exception ex) { MainFile.Logger.Error($"action failed: {ex}"); throw; }
```

吞掉 = 各端状态静默发散（desync）且无感知。（游戏自身的 Hook 回调另有统一异常策略，照游戏约定走。）

## 5. 交互前后查模型状态

右键/点击类交互是异步的，执行期间模型可能已被移除（打出/消耗/死亡）。执行后必须复查，否则会对已移除模型重复执行或误触发结束回调：

```csharp
static bool IsModelStillInState(AbstractModel model) => model switch
{
    CardModel card     => !card.HasBeenRemovedFromState,
    RelicModel relic   => !relic.HasBeenRemovedFromState && relic.Owner.Relics.Contains(relic),
    PowerModel power   => power.Owner.Powers.Contains(power),
    PotionModel potion => !potion.HasBeenRemovedFromState && potion.Owner.Potions.Contains(potion),
    _ => true
};
```

- `HasBeenRemovedFromState` 是 `CardModel`/`RelicModel`/`PotionModel` 的公共属性；**`PowerModel` 没有**，改用 `Owner.Powers` 集合判断。
- 循环执行多个绑定：每步后 `if (!IsModelStillInState(model)) break;`；全部完成后再 `InvokeExecutionFinished()`。

## 演进路线

- 2026-10-04 新增（学自 YuWanCard 增量：PlayPhase 门控 / 模型状态防护 / 网络行动异常 rethrow）。
- ⚠️ **自定义 `INetAction` 子类型不被源码生成器注册**（`[GenerateSubtypes]` 只覆盖游戏程序集），纯 mod 自定义网络行动的序列化需 patch 序列化环节（YuWan 做法 hook `TryWriteNetAction`/`ReadNetAction`）→ 进阶高风险；能用 `INetMessage`（[multiplayer-core.md](multiplayer-core.md)）就别自造行动。
