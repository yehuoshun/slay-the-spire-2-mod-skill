# 多人模式：约束、身份检查与网络消息

> 实战项目验证（YuWanCard）。命名空间：消息/约束在 `MegaCrit.Sts2.Core.GameActions.Multiplayer` 与 `Multiplayer.*`；**`LocalContext` 在 `MegaCrit.Sts2.Core.Context`**（旧版写错）。

## 1. 内容多人约束

```csharp
// 卡牌上覆写
public override CardMultiplayerConstraint MultiplayerConstraint
    => CardMultiplayerConstraint.MultiplayerOnly;   // 仅多人
// 或 SingleplayerOnly（仅单人）
// 默认 None = 无限制
```

## 2. 玩家身份检查（LocalContext）

多人下同一段代码在所有客户端执行，只对本地玩家生效的操作要包判断：

```csharp
using MegaCrit.Sts2.Core.Context;   // LocalContext 在此命名空间

if (LocalContext.IsMe(player))
{
    await CreatureCmd.GainMaxHp(player.Creature, 10m);
}
```

适用于修改器（Neow 选项）、遗物、卡牌中所有「本地玩家专属」副作用。

## 3. 自定义网络消息（INetMessage）

跨端同步自定义数据（如「替队友付钱」）需实现消息结构体：

```csharp
// 结构体实现 4 接口：INetMessage, IPacketSerializable, IRunLocationTargetedMessage
public struct MyMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
    public required int Value { get; set; }
    public required ulong TargetNetId { get; set; }
    public required RunLocation Location { get; set; }

    public bool ShouldBroadcast => false;            // false = 定向发送
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public bool ShouldBuffer => false;

    RunLocation IRunLocationTargetedMessage.Location => Location;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(Value);
        writer.WriteULong(TargetNetId);
        writer.Write(Location);                       // RunLocation 整体写入
    }

    public void Deserialize(PacketReader reader)
    {
        Value = reader.ReadInt();
        TargetNetId = reader.ReadULong();
        Location = reader.Read<RunLocation>();
    }
}
```

要点：`required` 属性；`WriteEnum/ReadEnum<T>()` 传枚举；Handler 注册在 `ModEntry.Initialize` 阶段。

## 4. 多人专用细节

- **角色**：`MultiplayerStartingRelics` 指定多人初始遗物
- **能力本地化**：`remoteDescription` 字段（多人对方玩家视角，支持动态变量；单人忽略）
- **战斗结束判定**：循环效果每轮检查 `CombatManager.Instance?.IsEnding != false` 提前 break，防结算后效果继续跑报错

> 自定义网络行动（`GameAction`/`INetAction`）、PlayPhase 门控、交互状态防护 → [multiplayer-netactions.md](multiplayer-netactions.md)
