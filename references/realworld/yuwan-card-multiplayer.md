# 多人模式（YuWanCard 提炼）

> STS2 原生支持多人（host/join），mod 内容需显式声明多人行为，否则可能出 bug。skill 原文档几乎空白，本文件补齐。

## 1. 内容多人约束

```csharp
// 卡牌上覆写
public override CardMultiplayerConstraint MultiplayerConstraint
    => CardMultiplayerConstraint.MultiplayerOnly;   // 仅多人
// 或 SingleplayerOnly（仅单人）
// 默认 None = 无限制
```

## 2. 玩家身份检查（LocalContext）

多人下同一段代码会在所有客户端执行，只对本地玩家生效的操作要包判断：

```csharp
if (LocalContext.IsMe(player))
{
    await CreatureCmd.GainMaxHp(player.Creature, 10m);
}
```

命名空间 `MegaCrit.Sts2.Core.GameActions.Multiplayer`。适用于修改器（Neow 选项）、遗物、卡牌中所有「本地玩家专属」副作用。

## 3. 自定义网络消息（INetMessage）

跨端同步自定义数据（如「替队友付钱」）需实现消息接口：

```csharp
// 结构体实现 4 个接口：INetMessage, IPacketSerializable, IRunLocationTargetedMessage
// 成员：ShouldBroadcast(bool) / Mode(NetTransferMode.Reliable) / LogLevel / ShouldBuffer
// 序列化：Serialize(PacketWriter) / Deserialize(PacketReader)
//   writer.WriteInt/WriteULong/WriteString/WriteEnum/Write(Location)
//   reader.ReadInt/ReadULong/ReadString/ReadEnum<T>()
// 定位：RunLocation Location（IRunLocationTargetedMessage 必须）
```

要点：消息结构体用 `required` 属性；`ShouldBroadcast => false` 表示仅定向发送；`NetTransferMode.Reliable` 保证送达。注册与处理在 `ModEntry` 初始化阶段完成（`TeammatePayMessageHandler.Register()`）。

## 4. 多人起始遗物

角色实现接口可指定多人初始遗物：`IReadOnlyList<RelicModel> MultiplayerStartingRelics => [...]`。

## 5. 多人+本地化

能力 `remoteDescription` 字段：多人模式下其他玩家看到的能力描述（支持动态变量）。单人时游戏忽略该字段。

## 6. 战斗结束判定

能力/遗物里做循环效果（如每回合多次施加）时，真实代码每轮检查：

```csharp
if (CombatManager.Instance?.IsEnding != false) break;          // 战斗结束中
if (await CombatManager.Instance.CheckWinCondition()) break;   // 已满足胜利
```

防止战斗结算后效果继续跑导致报错。

## 7. 纯原生落地清单

- 卡牌/遗物：覆写 `MultiplayerConstraint`
- 副作用：`LocalContext.IsMe` 守卫
- 跨端数据：自实现 `INetMessage`（上面 4 接口 + 序列化）
- 网络注册：`ModEntry.Initialize` 阶段注册 handler
