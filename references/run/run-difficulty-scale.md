# 自定义难度：怪物缩放 + 设置中心 + 联机同步

> 实战验证（sts2mod 自定义难度 CustomDifficulty v0.2.1，2026-10-07）。滑条调怪物血/攻倍率 + 递进模式 + 联机主机配置 + 无尽跨轮联动。纯原生（零依赖）。

## ① 设置中心（静态配置 + ticks↔倍率换算）

```csharp
internal static class CustomDifficultySettings
{
    public const int MinTicks = 1;      // 1 tick = x0.1
    public const int MaxTicks = 50;     // 50 ticks = x5.0
    public static decimal MonsterHpMultiplier => TicksToMultiplier(_monsterHpTicks);  // ticks/10m
    // 递进：1 + 每房间增量 × 已走房间数，clamp [0.1, 99]
    private static decimal ProgressiveMultiplier(int deltaPercent, int floorIndex)
        => Math.Clamp(1m + deltaPercent / 100m * Math.Max(0, floorIndex), 0.1m, 99m);
    public static event Action? Changed;   // 变更事件（UI 刷新订阅）
    // 三入口：SetLocal / SetRemote / SetPersisted
}
```

- 全部 setter 先 Clamp + 值没变直接 return（防无效广播）；持久化与广播是显式参数。
## ② 缩放挂点（原版多人缩放之后）

```csharp
[HarmonyPatch(typeof(Creature), nameof(Creature.ScaleMonsterHpForMultiplayer))]
internal static class MonsterScalingPatch
{
    private static void Postfix(Creature __instance)
    {
        if (!__instance.IsMonster) return;
        int floorIndex = GetEffectiveFloorIndex();
        ApplyHpMultiplier(__instance, floorIndex);       // 血：SetMaxHpInternal/SetCurrentHpInternal
        ApplyAttackMultiplierPower(__instance, floorIndex); // 攻：挂隐藏 power
    }
}
```

- **挂 `ScaleMonsterHpForMultiplayer` 之后 = 在原版多人缩放结果上再乘**，天然兼容原版（不用重算人数缩放）。
- HP 直接内部设置；攻击用 power（攻击随力量动态变化，乘算最稳）。

## ③ 编码兼容 Power（Amount 存倍率 + 旧档兼容）

```csharp
public sealed class MonsterAttackScalePower : PowerModel
{
    // 双编码区间不重叠：旧版 Amount = 100 + ticks（0.1.x）；新版 Amount = 10000 + 百分比（0.2.0+）
    private const int EncodedAttackTicksOffset = 100;
    private const int EncodedPercentOffset = 10000;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;   // 隐藏

#if STS2_108_OR_NEWER
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
#else
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
#endif
    {
        if (dealer != Owner || target == null || !target.IsPlayer) return 1m;   // 只影响敌人打玩家
        if (!props.IsPoweredAttack()) return 1m;                                // 只乘「攻击」类伤害
        return GetMultiplier() > 0m ? GetMultiplier() : 1m;
    }
}
```

- **编码值存 Amount**（联机序列化免费），单实例 + 隐藏；解码按区间判断（旧档不炸）。
- 条件编译签名（0.108+ 多 cardPlay）。

## ④ 递进房间计数（确定性 + 跨 mod 联动）

```csharp
private static int GetEffectiveFloorIndex()
{
    int totalFloor = 0;
    try
    {
        if (RunManager.Instance?.DebugOnlyGetState() is RunState state)
            totalFloor = Math.Max(0, state.TotalFloor);   // 地图历史条目总数：联机一致、读档自恢复
    }
    catch { }
    return totalFloor + EndlessModeCompat.GetFloorsBeforeCurrentLoop();
}
```

- `RunState.TotalFloor`：确定性计数（不自己数房间，避免两端分叉）。
- **软联动消费 Interop**（run-interop-config.md 的模式被上游实例化）：按程序集名 "EndlessMode" 反射 `EndlessModeInterop.GetTotalFloorsBeforeCurrentLoop`，一次解析缓存 + 兜底 0。

## ⑤ 联机同步（INetGameService 消息）

```csharp
// 消息类：INetMessage + IPacketSerializable（字段全手写读写）
netService.RegisterMessageHandler<CustomDifficultySettingsMessage>(OnSettingsReceived);
netService.SendMessage(CreateMessage());   // 广播（主机变更/新玩家加入时重发）
// 客户端请求主机配置：主机收到 → 重广播；客户端收到主机设置 → SetRemote
```

- **联机以房主配置为准**；`PlayerConnected` patch 里主机重广播；角色选择屏 3 路径 patch（单人/主机/客户端）+ `_Ready` 兜底 + RunManager.Launch 注册。

## ⑥ Profile 持久化

`SaveManager.InitProfileId/SwitchProfileId` postfix → `LoadCurrentProfile()`（profile-scoped JSON，同 run-interop-config.md）；联机远程配置不落盘。

## 相关 API 速查

| API | 说明 |
|-----|------|
| `Creature.ScaleMonsterHpForMultiplayer` | 原版多人血量缩放（挂其后乘倍率） |
| `Creature.SetMaxHpInternal/SetCurrentHpInternal` | 直接改生命内部（真实 API） |
| `PowerModel.ModifyDamageMultiplicative(...)` | 攻击乘算（0.108+ 签名带 CardPlay） |
| `RunState.TotalFloor` | 确定性房间计数 |