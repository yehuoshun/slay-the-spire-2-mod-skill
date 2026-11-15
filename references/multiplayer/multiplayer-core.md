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


### 自定义网络消息：lobby 阶段就要注册

接收 handler 如果只挂在「战斗开始」这类时机上，lobby 里发的消息就收不到——网络服务在 lobby 阶段早已创建。v3.4.8 的解法是双层注册：

1. 给 `NetHostGameService` / `NetClientGameService` 的**构造器**打 Postfix，构造完立刻 `Register(service)`；
2. 再在 `RunManager.InitializeShared` 的 Postfix 里先 `Unregister(service)` 再 `Register(service)`，防止同一连接被注册两次。

自定义消息类型要点：

- 类型必须**静态存在于 mod 程序集**里（启动期反射扫描收集）；
- 消息 id 由类型 `FullName` 的哈希决定 —— **改类名或命名空间 = 联机不兼容**；
- lobby 阶段 `RunManager.Instance.NetService` 未必可用，发送时把服务显式传进去；
- 抄 Postfix 时核对参数类型：`[HarmonyPatch]` + `TargetMethod()` 的目标类型必须与 Postfix 第一个参数一致，否则静默失效（BaseLib 自己的 client 版就写成了 host 类型）。
