# Harmony 补丁：组织方式与常用目标

## 组织方式

```
Patches/
├── Core/
│   ├── PlusLifecyclePatch.cs
│   ├── SkipIntroLogoPatch.cs
│   └── SpeedControlBootstrapPatch.cs
├── MoreRules/
│   ├── AttackDefenseCardCreationPatch.cs
│   ├── EndlessModeHpScalingPatch.cs
│   └── GlassCannonEnergyPatch.cs
└── ModEntry.cs
```

或按功能平铺（STS2Plus 风格）：

```
Patches/
├── PlusLifecyclePatch.cs           [Core]
├── SkipIntroLogoPatch.cs           [Core]
├── CustomModifierListPatch.cs      [MoreRules]
├── CustomModifierSerializationPatch.cs [MoreRules]
├── AttackDefenseCardCreationPatch.cs  [MoreRules]
├── EndlessModeHpScalingPatch.cs    [MoreRules]
└── GlassCannonEnergyPatch.cs       [MoreRules]
```

---

## 常用 Patch 目标

### 注册 / 模型

| 目标 | 用途 |
|------|------|
| `ModelDb.AllCharacters` getter | 注册自定义角色 |
| `ModelDb.AllCardPools` getter | 注册自定义卡池 |
| `ModelDb.AllRelicPools` getter | 注册自定义遗物池 |
| `ModelDb.AllPotionPools` getter | 注册自定义药水池 |
| `ModifierModel.FromSerializable` | 自定义 Modifier 反序列化 |
| `NCustomRunModifiersList.GetModifiersTickedOn` | 注入运行规则 |

### 战斗

| 目标 | 用途 |
|------|------|
| `CardModel.GetDamage` | 修改卡牌伤害 |
| `CardModel.GetBlock` | 修改卡牌格挡 |
| `CardModel.ToMutable` | 卡牌创建时修改 |
| `Creature.SetCurrentHpInternal` | 限制 HP（private，慎用） |
| `Creature.SetMaxHpInternal` | 限制最大 HP |
| `Creature.AfterAddedToRoom` | 怪物入场时修改 |
| `AbstractModel.BeforeCombatStart` | 战斗开始前修改（真实钩子，`Creature.BeforeCombatStart` 不存在） |
| `CombatRoom` constructor | 替换遭遇（`Core/Rooms/CombatRoom.cs`） |

### UI

| 目标 | 用途 |
|------|------|
| `NMainMenu._Ready` | 主菜单初始化 |
| `NCombatRoom.OnCombatSetUp` | 战斗 UI 设置 |
| `NMapScreen` | 地图 UI 修改 |
| `NEnergyCounter.Create` | 自定义能量计数器 |

### 事件

| 目标 | 用途 |
|------|------|
| `Overgrowth.AllEvents` getter | 注入事件（act 子类，非抽象基类 `ActModel`） |
| `Hive.AllAncients` getter | 注入先古之民（act 子类） |
| `Overgrowth.GenerateAllEncounters()` | 注入遭遇（实例方法，非 getter） |
| `EventModel.BeginEvent` | 事件开始时修改 |
| `EventModel.SetEventFinished` | 事件结束时修改 |

