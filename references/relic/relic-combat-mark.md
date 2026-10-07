# 战斗标记追踪（冷却标记 + 全量刷新 + 体型增长 + 自播音频）

> 实战验证（sts2mod 心之钢 Heartsteel v0.2.0，2026-10-07）。遗物机制的完整战斗态管理：多敌人冷却/标记、标记数值全局同步、攻击者体型随成长变化、本地音频自播。RitsuLib 的 AssetProfile 资源声明一句话带过（纯原生对照见 character-asset-hooks.md）。

## 战斗态追踪结构

```csharp
private Dictionary<Creature, int>? _enemyCooldowns;   // 每个敌人 3 回合冷却
private HashSet<Creature>? _markedEnemies;            // 已标记（庞然吞食）敌人

// 懒初始化 + DeepCloneFields 重置（战斗瞬态不入存档）
private Dictionary<Creature, int> EnemyCooldowns => _enemyCooldowns ??= new();
private HashSet<Creature> MarkedEnemies => _markedEnemies ??= new();
protected override void DeepCloneFields()
{
    base.DeepCloneFields();
    _enemyCooldowns = new(); _markedEnemies = new();
}
```

- 键用 `Creature` 实例（战斗实体引用），`PruneTrackedEnemies(aliveEnemies)` 每回合清死掉的（`Keys.Where(k => !aliveSet.Contains(k))` + `RemoveWhere`）。
- 进入战斗：`BeforeCombatStart` 重置 + `AfterCreatureAddedToCombat` 给新敌人补条目（`TryAdd`）。

## 冷却 → 标记流转（回合驱动）

```csharp
public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
{
    if (side != CombatSide.Player) return;
    foreach (Creature enemy in aliveEnemies)
    {
        EnemyCooldowns.TryAdd(enemy, 0);
        if (MarkedEnemies.Contains(enemy)) continue;
        int updated = EnemyCooldowns[enemy] + 1;
        if (updated >= CooldownTurns)
        {
            EnemyCooldowns[enemy] = CooldownTurns;   // 封顶
            await ApplyDevourMark(enemy);            // 上标记（power + Flash + 状态点亮）
        }
        else EnemyCooldowns[enemy] = updated;
    }
    RefreshStatus();   // Status = 有标记 ? Active : Normal
}
```

## 触发 + 全量刷新标记数值

```csharp
// AfterDamageGiven：dealer=自己 + 敌人 + 被标记 + 攻击卡 → 触发
//   清冷却/去标记/移除 power → Flash + 音效 → 额外伤害（5+10% 自己MaxHp）→ GainMaxHp
// 自己 MaxHp 变化后所有标记敌人 power 数额同步刷新：
foreach (Creature enemy in MarkedEnemies.Where(e => e.IsAlive).ToList())
{
    HeartsteelDevourPower? power = enemy.GetPower<HeartsteelDevourPower>();
    if (power != null && power.Amount != bonus)
        await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), power,
            bonus - power.Amount, Owner.Creature, cardSource: null, silent: true);
}
```

- 标记 power 的 `Amount` 直接存「当前加成伤害」，数值变了全量 `ModifyAmount` 差值同步。
- `ThrowingPlayerChoiceContext`：非玩家决策场景的默认 context（同前例）。

## 体型增长（MaxHp 成长可视化）

```csharp
private void ApplyOwnerScale(float duration)
{
    NCreature? node = NCombatRoom.Instance?.GetCreatureNode(Owner.Creature);
    if (node == null) return;
    float gained = Math.Max(0, Owner.Creature.MaxHp - Owner.Character.StartingHp);
    float t = Mathf.Clamp(gained / 60f, 0f, 1f);          // 每 +60 最大生命涨满
    node.ScaleTo(Mathf.Lerp(1f, 1.75f, t), duration);      // 1.0 → 1.75 倍
}
```

- `NCreature.ScaleTo(float size, double duration)` public（真实 API）；`NCombatRoom.Instance.GetCreatureNode(Creature?)`。
- 调用时机：`AfterObtained`（初始归 1）/ `AfterRoomEntered`（进房重置）/ 每次触发后 `ApplyOwnerScale(0.15f)` 平滑增长。

## 本地音频自播（不依赖 SfxCmd）

```csharp
// AudioStreamMP3.LoadFromFile(path) 缓存复用 + 随机路径 Random.Shared（纯表现不需确定性 RNG）
// 播放器：NGame.Instance 下挂 AudioStreamPlayer { Bus="Master", VolumeDb=-7 }
// 每次触发：_player.Stop() → Stream = cached → Play()
```

- 自挂 `AudioStreamPlayer` + `IsInstanceValid` 防生命周期失效——比 patch SfxCmd 轻量（音效少时）。

## 相关 API 速查

| API | 说明 |
|-----|------|
| `CreatureCmd.GainMaxHp(Creature, decimal)` | 加最大生命（回当前血） |
| `PowerCmd.ModifyAmount(context, power, offset, applier, cardSource, silent)` | 差值改层数 |
| `AfterCreatureAddedToCombat(Creature)` | 生物进战斗回调（补追踪） |
| `AfterRoomEntered(AbstractRoom)` | 进房回调（重置体型） |
| `NCreature.ScaleTo(float, double)` | 节点缩放动画 |
| `AudioStreamMP3.LoadFromFile(string)` | 本地 MP3 加载 |

> ℹ️ 心之钢的 `RelicAssetProfile/IModPowerAssetOverrides`（RitsuLib）集中声明图标路径——纯原生对照：`PackedIconPath` 等虚属性（relic-core.md）+ AssetHooks getter 覆写（character-asset-hooks.md）。