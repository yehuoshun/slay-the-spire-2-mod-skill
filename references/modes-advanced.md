# 高级模式

---

## 模式八：Harmony 补丁

### 基本语法

```csharp
using HarmonyLib;

namespace MyMod.Patches;

// 前置补丁：在原方法执行前运行
[HarmonyPatch(typeof(GameClass), "MethodName")]
[HarmonyPrefix]
static void Prefix() { /* 在原方法前 */ }

// 后置补丁：在原方法执行后运行
[HarmonyPatch(typeof(GameClass), "MethodName")]
[HarmonyPostfix]
static void Postfix(ref int __result) { /* 可修改返回值 */ }

// 修改 Getter
[HarmonyPatch(typeof(GameClass), "PropertyName", MethodType.Getter)]
[HarmonyPostfix]
static void Postfix(ref string __result) { __result = "new value"; }

// 修改 Setter
[HarmonyPatch(typeof(GameClass), "PropertyName", MethodType.Setter)]
[HarmonyPrefix]
static void Prefix(ref string value) { value = "new input"; }

// Transpiler：修改方法体 IL（高级）
[HarmonyPatch(typeof(GameClass), "MethodName")]
[HarmonyTranspiler]
static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    // 用 CodeMatcher 修改 IL
    return new CodeMatcher(instructions).InstructionEnumeration();
}
```

### 多 Patch 类的安全安装

```csharp
public static void Initialize()
{
    var harmony = new Harmony("MyMod");
    var assembly = Assembly.GetExecutingAssembly();

    // PatchAll 会自动发现所有带 [HarmonyPatch] 的类
    harmony.PatchAll(assembly);
}

// 更安全的方式：逐类 try-catch
public static void PatchAllSafe(Harmony harmony, Assembly assembly)
{
    foreach (var type in assembly.GetTypes())
    {
        try
        {
            if (type.GetCustomAttribute<HarmonyPatch>() != null)
                harmony.CreateClassProcessor(type).Patch();
        }
        catch (Exception ex)
        {
            Log.Warn($"Patch failed: {type.Name}: {ex.Message}");
        }
    }
}
```

---

## 模式九：自定义角色

### 最简角色类

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;

namespace MyMod.Characters;

public class WatcherCharacter : CharacterModel
{
    public override CharacterGender Gender => CharacterGender.Female;
    public override CharacterModel? UnlocksAfterRunAs => null; // 不需要解锁
    public override CardPoolModel CardPool => new WatcherCardPool();
    public override RelicPoolModel RelicPool => new WatcherRelicPool();

    public override IEnumerable<CardModel> StartingDeck => new CardModel[]
    {
        new StrikeCard(),
        new StrikeCard(),
        new StrikeCard(),
        new StrikeCard(),
        new DefendCard(),
        new DefendCard(),
        new DefendCard(),
        new DefendCard(),
        new EruptionCard(),
        new VigilanceCard(),
    };

    public override IEnumerable<RelicModel> StartingRelics => new RelicModel[]
    {
        new PureWater(),
    };

    // 攻击建筑师的动画
    public override IEnumerable<string> GetArchitectAttackVfx()
    {
        return new[] { "scenes/vfx_attack_slash.tscn" };
    }

    // 音效回退
    public override string AttackSfxPath => "event:/sfx/characters/watcher/watcher_attack";
    public override string CastSfxPath => "event:/sfx/characters/watcher/watcher_cast";
    public override string DeathSfxPath => "event:/sfx/characters/watcher/watcher_die";
}
```

### 自定义卡池

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

public class WatcherCardPool : CardPoolModel
{
    public override string Title => "Watcher";                    // 影响卡面图路径
    public override string EnergyColorName => "watcher";          // 能量图标颜色
    public override string CardFrameMaterialPath => "card_frame_purple";
    public override Color DeckEntryCardColor => Colors.Purple;
    public override Color EnergyOutlineColor => Colors.White;
    public override bool IsColorless => false;

    public override IEnumerable<CardModel> GenerateAllCards()
    {
        return new CardModel[] {
            new StrikeCard(), new DefendCard(), new EruptionCard(), // ...
        };
    }
}
```

### 自定义遗物池

```csharp
public class WatcherRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "watcher";
    public override Color LabOutlineColor => Colors.Purple;
    public override IEnumerable<RelicModel> GenerateAllRelics() => new RelicModel[] { new PureWater() };
}
```

### 角色必需资源

| 资源 | 路径 |
|------|------|
| 待机动画场景 | `scenes/creature_visuals/<角色ID小写>.tscn` |
| 头像图标场景 | `scenes/ui/character_icons/<角色ID小写>_icon.tscn` |
| 能量计数器场景 | `scenes/combat/energy_counters/<角色ID小写>_energy_counter.tscn` |
| 选择界面底图 | `images/packed/character_select/char_select_<角色ID小写>.png` |
| 锁定底图 | `images/packed/character_select/char_select_<角色ID小写>_locked.png` |

### 注册角色

```csharp
[HarmonyPatch(typeof(ModelDb), "AllCharacters", MethodType.Getter)]
[HarmonyPostfix]
static void Postfix(ref IEnumerable<CharacterModel> __result)
{
    __result = __result.Append(new WatcherCharacter());
}
```

### 场景脚本映射（重要）

如果在场景中使用了自定义 C# 脚本，必须：

```csharp
// 在 ModEntry.Initialize() 中调用
ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
```

否则 Godot 在加载场景时会找不到脚本类。

---

## 模式十：自定义怪物 + 遭遇

### 怪物类

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves;

namespace MyMod.Monsters;

public class GoblinMonster : MonsterModel
{
    protected override IEnumerable<MoveState> GenerateMoveStateMachine()
    {
        var attack = new MoveState(
            "attack",
            new[] { MonsterIntent.Attack },
            async move => { /* 执行攻击逻辑 */ },
            followUpStateId: "defend"
        );

        var defend = new MoveState(
            "defend",
            new[] { MonsterIntent.Defend },
            async move => { /* 执行防御逻辑 */ },
            followUpStateId: "attack"
        );

        return new[] { attack, defend };
    }
}
```

### MonsterIntent 常用值

`Attack` / `Defend` / `Buff` / `Debuff` / `StrongAttack` / `Charge` / `Summon` / `Sleep`

### 随机分支 AI

```csharp
protected override IEnumerable<MoveState> GenerateMoveStateMachine()
{
    var randomBranch = new RandomBranchState("branch");
    var attack = new MoveState("attack", ..., "branch");  // 执行完回到分支
    var buff = new MoveState("buff", ..., "branch");
    var defend = new MoveState("defend", ..., "branch");
    randomBranch.AddBranch(attack, cooldown: 2, repeatType: None, weight: 3);
    randomBranch.AddBranch(buff, cooldown: 1, repeatType: None, weight: 1);
    randomBranch.AddBranch(defend, cooldown: 2, repeatType: None, weight: 2);
    return new MoveState[] { randomBranch };
}
```

### 条件分支

```csharp
var condBranch = new ConditionalBranchState("conditional");
condBranch.AddBranch(
    state => state.GetPlayerHp() <= 10,
    bigAttack,  // 玩家血量低时用大招
    normalAttack // 否则普通攻击
);
```

### 怪物资源

| 资源 | 路径 |
|------|------|
| 场景 | `scenes/creature_visuals/<怪物ID小写>.tscn` |

场景结构规则：根节点挂载 NCreatureVisuals 脚本 → 包含名为 `%Bounds` 的节点（定义选择框和血条位置）

### 遭遇类

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace MyMod.Encounters;

public class GoblinEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;
    public override IEnumerable<MonsterModel> AllPossibleMonsters => new[] { new GoblinMonster() };

    public override IEnumerable<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.GetById<MonsterModel>(ModelDb.GetId<GoblinMonster>()).ToMutable(), null),
        };
    }
}
```

### 注册遭遇

```csharp
[HarmonyPatch(typeof(Overgrowth), nameof(Overgrowth.GenerateAllEncounters))]
[HarmonyPostfix]
static void Postfix(ref IEnumerable<EncounterModel> __result)
{
    __result = __result.Append(new GoblinEncounter());
}
```

---

## 模式十一：自定义属性序列化

### SavedProperty 属性

```csharp
using MegaCrit.Sts2.Core.Saves.Runs;

public class CustomCard : CardModel
{
    [SavedProperty]
    public int KillCount { get; set; }

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public bool HasTriggered { get; set; }
}
```

### 初始化时注入类型

```csharp
// 在 ModEntry.Initialize() 中，AddModelToPool 之前
SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(CustomCard));
// 或
SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(CustomRelic));
```

**注意**：任何有自定义 `[SavedProperty]` 属性的类，都必须在初始化时调用这行，否则存档读取时会丢失自定义属性。

### 批量注册（反射扫描）

```csharp
// 扫描程序集中所有带特定属性或继承特定基类的类型
foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
{
    if (!type.IsAbstract && typeof(RelicModel).IsAssignableFrom(type))
    {
        SavedPropertiesTypeCache.InjectTypeIntoCache(type);
    }
}
```