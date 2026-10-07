# 宠物进阶：持久化位置、视觉复制、避让布局

> 实战验证（sts2mod 俄洛伊 Illaoi v0.2.0，2026-10-07）。宠物/召唤物的三个进阶场景：触手（多宠物+位置持久化+避让布局）、灵魂（视觉复制自目标怪物）。基础模板见 [pet.md](pet.md)。

## 宠物位置持久化（SavedProperty 存 Vector2）

`[SavedProperty]` 不能直接存 `Vector2`，拆成两个 float 属性：

```csharp
public sealed class MyTentacleMonster : MonsterModel
{
    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public float Illaoi_VisualOffsetX { get; set; }
    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public float Illaoi_VisualOffsetY { get; set; }

    public Vector2 VisualOffset { get => new(VisualOffsetX, VisualOffsetY); set { VisualOffsetX = value.X; VisualOffsetY = value.Y; } }
}
```

- 存的是 `ModelDb.Monster<T>().ToMutable()` 的可变实例（`AbstractModel.ToMutable()`，整个仓库用这个创建运行时副本，不污染原型）。

## 宠物怪物模板（装饰物级）

```csharp
public override int MinInitialHp => 1;  public override int MaxInitialHp => 1;
public override bool CanChangeScale => true;      // 允许缩放（视觉大小随布局）
public override bool HasDeathSfx => false;  public override bool HasHurtSfx => false;
public override bool IsHealthBarVisible => false;
protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/fallback");
protected override MonsterMoveStateMachine GenerateMoveStateMachine()
{
    MoveState state = new("NOTHING", _ => Task.CompletedTask, new HiddenIntent());
    state.FollowUpState = state;
    return new MonsterMoveStateMachine([state], state);   // 固定不行动
}
```

生成方式（战斗内）：`combatState.CreateCreature(model, player.Creature.Side, slot: null)` + `PlayerCmd.AddPet(tentacle, player)` + `CreatureCmd.SetMaxAndCurrentHp(tentacle, 1m)`。

## 视觉复制怪物（灵魂 = 目标怪物的克隆外观）

灵魂怪物不自己画美术，**复制目标怪物的视觉**：

```csharp
[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
public string Illaoi_VisualSourceCategory { get; set; }   // 存 ModelId 两段，不存对象引用
[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
public string Illaoi_VisualSourceEntry { get; set; }

public void SetVisualSource(MonsterModel sourceModel)
{
    ModelId id = sourceModel.Id;
    Category = id.Category; Entry = id.Entry;
}
public MonsterModel? ResolveVisualSourceModel()
    => ModelDb.GetByIdOrNull<MonsterModel>(new ModelId(Category, Entry));
```

- 运行时：`soulModel.SetVisualSource(body.Monster)` → 视觉层读取源模型场景。
- 灵魂血量按本体比例算：`Max(1, Ceil(bodyHp * 0.5m))`；`CreatureCmd.SetMaxHp` + `SetCurrentHp`。
- 灵魂放在敌人阵营（`CombatSide.Enemy`），带 `IllaoiHuskPower`（3 回合）+ `IllaoiSoulLinkPower`（3 回合）+ 视觉链接线。

## 避让布局（多宠物不重叠）

```csharp
for (int attempt = 0; attempt < 24; attempt++)        // 最多 24 次随机尝试
{
    Vector2 offset = RollCandidate(index, rng);       // 分区随机（index%3==2 换小区域）
    if (NearestDistanceSquared(offset, occupied) >= 92f * 92f)  // 最小间距 92px
        return offset;                                // 满足立即返回
    // 否则记录最佳（最远）继续
}
return best;                                          // 全失败用最远者
```

- 用 `Rng.Chaotic`（战斗随机源，非确定性避免闪跳）。灵魂按 lane 排序摆放（`[0, -1, 1, -2, 2, -3, 3]` 顺序）。
- 战斗外清理：`AfterCombatEnd` 里 `CleanupTentacles` + 遗物计数归零。

## 遗物联动（SpawnsPets + 计数）

```csharp
public override bool ShowCounter => true;   // 遗物上显示计数
public override bool SpawnsPets => true;    // 声明会生成宠物（UI 提示）
public override int DisplayAmount => !IsCanonical ? _tentacles : 0;
[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
public int Illaoi_Tentacles { get => _tentacles; set { _tentacles = Math.Max(0, value); InvokeDisplayAmountChanged(); } }
```

- 计数想持久化就给 `[SavedProperty]`；`DisplayAmount` 里 `IsCanonical` 判断避免原型污染显示。
- 战斗状态（本回合是否命令过/是否获得过格挡/下命令伤害加成）在 `AfterSideTurnStart` 玩家回合重置，`BeforeCombatStart` 全量重置。

## 状态查询辅助

- `player.Creature.Pets.Where(p => p.Monster is MyTentacleMonster && p.IsAlive)` 拿活触手；没有活触手时兜底用遗物计数（死亡触手也算资源，避免全灭后流派瘫痪）。
- `PlayerCmd.AddPet` 签名：`AddPet(Creature pet, Player player)`（及泛型重载 `AddPet<T>(Player)`）。