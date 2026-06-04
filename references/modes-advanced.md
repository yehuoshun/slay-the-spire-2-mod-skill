## 🧩 模式七：Harmony 补丁

### 固定目标 Patch

```csharp
[HarmonyPatch(typeof(目标类), nameof(目标类.方法名))]
static class MyPatch
{
    static void Postfix(目标类 __instance, ref int __result)
    {
        __result = 99;  // 改返回值
    }
}

[HarmonyPatch(typeof(目标类), "方法名", typeof(参数1), typeof(参数2))]
static class MyPrefixPatch
{
    static bool Prefix(ref int __result) { __result = 42; return false; } // 跳过原方法
}
```

### 动态目标 Patch（遍历所有子类 override）

```csharp
[HarmonyPatch]
static class MyMultiPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        var baseGetter = AccessTools.PropertyGetter(typeof(基类), "属性名");
        if (baseGetter != null) yield return baseGetter;

        foreach (var t in typeof(基类).Assembly.GetTypes())
        {
            if (t.IsAbstract || !typeof(基类).IsAssignableFrom(t)) continue;
            var getter = AccessTools.PropertyGetter(t, "属性名");
            if (getter != null && getter.DeclaringType == t) yield return getter;
        }
    }

    static void Postfix(基类 __instance, ref int __result) { ... }
}
```

---

## 👤 模式八：自定义角色（最复杂）

角色需要 4 个模型类 + 大量场景/贴图资源 + 注册 Patch。

### 最小骨架

```csharp
// 1. 卡池
public class MyCardPool : CardPoolModel
{
    public override string Title => "myrole";
    public override Color DeckEntryCardColor => Colors.Purple;
    public override Color EnergyOutlineColor => Colors.Purple;
    public override IReadOnlyList<CardModel> GenerateAllCards() => [/* 卡牌列表 */];
}

// 2. 遗物池
public class MyRelicPool : RelicPoolModel
{
    public override IReadOnlyList<RelicModel> GenerateAllRelics() => [/* 遗物列表 */];
}

// 3. 药水池
public class MyPotionPool : PotionPoolModel
{
    public override IReadOnlyList<PotionModel> GenerateAllPotions() => [/* 药水列表 */];
}

// 4. 角色类
public class MyChar : CharacterModel
{
    public override CharacterGender Gender => CharacterGender.Female;
    public override CardPoolModel CardPool => ModelDb.GetByIdOrNull<CardPoolModel>(ModelDb.GetId(typeof(MyCardPool)));
    public override RelicPoolModel RelicPool => ModelDb.GetByIdOrNull<RelicPoolModel>(ModelDb.GetId(typeof(MyRelicPool)));
    public override PotionPoolModel PotionPool => ModelDb.GetByIdOrNull<PotionPoolModel>(ModelDb.GetId(typeof(MyPotionPool)));
    public override IReadOnlyList<CardModel> StartingDeck => [/* 初始卡组 */];
    public override IReadOnlyList<RelicModel> StartingRelics => [/* 初始遗物 */];
}

// 5. 注册 Patch（必须！）
[HarmonyPatch(typeof(ModelDb), "AllCharacters", MethodType.Getter)]
static void Postfix(ref IReadOnlyList<CharacterModel> __result)
{
    var list = __result.ToList();
    list.Add(ModelDb.GetByIdOrNull<CharacterModel>(ModelDb.GetId(typeof(MyChar))));
    __result = list;
}
```

### 角色资源清单（每个都需要手动创建）

| 资源 | 路径 |
|------|------|
| 待机动画 | `scenes/creature_visuals/<小写ID>.tscn`（根 `NCreatureVisuals`） |
| 头像场景 | `scenes/ui/character_icons/<小写ID>_icon.tscn` |
| 能量计数器 | `scenes/combat/energy_counters/<小写ID>_energy_counter.tscn` |
| 选择界面背景 | `scenes/screens/char_select/char_select_bg_<小写ID>.tscn` |
| 卡牌拖尾 | `scenes/vfx/card_trail_<小写ID>.tscn` |
| 头像图标 | `images/ui/top_panel/character_icon_<小写ID>.png` |

> **Godot 脚本修复（必须）：** 模组场景中的 C# 脚本不会被游戏自动识别，初始化中加：
> ```csharp
> Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
> ```

---

## 👹 模式九：自定义怪物 + 遭遇

```csharp
// 怪物
public class MyMonster : MonsterModel
{
    public override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var attack = new MoveState("ATTACK",
            async ctx => await DamageCmd.Attack(6).FromMonster(this).Targeting(ctx.GetPlayer()).Execute(),
            [new AttackIntent(6)]);
        attack.FollowUpState = attack;  // 循环攻击
        return new MonsterMoveStateMachine([attack], "ATTACK");
    }
}

// 遭遇
public class MyEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;
    public override IEnumerable<(MonsterModel, string?)> GenerateMonsters()
    {
        return [(ModelDb.GetByIdOrNull<MonsterModel>(ModelDb.GetId(typeof(MyMonster)))!.ToMutable(), null)];
    }
}

// 注册到地图：
[HarmonyPatch(typeof(Overgrowth), "GenerateAllEncounters")]
static void Postfix(ref IEnumerable<EncounterModel> __result)
{
    __result = __result.Append(ModelDb.GetByIdOrNull<EncounterModel>(ModelDb.GetId(typeof(MyEncounter))));
}
```

---

## 🧩 模式十：高级 Harmony 技巧

### Transpiler — IL 级常量替换

当需要修改方法体内硬编码的常量时（如手牌上限），用 Transpiler 在 IL 码层面替换：

```csharp
private static IEnumerable<CodeInstruction> ReplaceIntLoads(
    IEnumerable<CodeInstruction> instructions, int expectedCount, MethodBase method)
{
    int replaced = 0;
    foreach (var inst in instructions)
    {
        if (inst.opcode == System.Reflection.Emit.OpCodes.Ldc_I4_S
            && inst.operand is sbyte val && val == 10)
        {
            inst.operand = (sbyte)HandLimit;  // 替换常量 10→20
            replaced++;
        }
        yield return inst;
    }
}

// 用法：
[HarmonyPatch]
static IEnumerable<CodeInstruction> Transpiler(
    IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    => ReplaceIntLoads(instructions, expectedCount: 3, __originalMethod);
```

### 对 async 方法打补丁

async 方法编译后变成状态机类，不能直接 Harmony Patch 原方法。必须拿到状态机的 `MoveNext`：

```csharp
// ❌ 错误：直接 Patch async 方法
harmony.Patch(typeof(CardPileCmd).GetMethod("Draw"), ...);

// ✅ 正确：拿到状态机 MoveNext
var drawerMethod = typeof(CardPileCmd).GetMethod("Draw", /* 参数签名 */);
var target = GetAsyncStateMachineTarget(drawerMethod);
harmony.Patch(target, transpiler: ...);

// Helper：
static MethodBase GetAsyncStateMachineTarget(MethodInfo method)
{
    var attr = method.GetCustomAttribute<AsyncStateMachineAttribute>();
    if (attr == null) throw new InvalidOperationException($"{method.Name} is not async");
    return AccessTools.Method(attr.StateMachineType, "MoveNext");
}
```

### RequireField/RequireMethod — 启动时反射缓存

一次性反射并缓存，找不到直接抛异常（fail-fast 原则）：

```csharp
// 一次性静态字段缓存
private static readonly FieldInfo MapPointHistoryField =
    RequireField(typeof(RunState), "_mapPointHistory");

private static readonly MethodInfo RunStateRngSetter =
    RequirePropertySetter(typeof(RunState), nameof(RunState.Rng),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

// 这些 Helper 需要自己实现，参考 Natsuki 仓库：
private static FieldInfo RequireField(Type type, string name,
    BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic)
{
    var field = type.GetField(name, flags);
    if (field == null) throw new MissingFieldException(type.Name, name);
    return field;
}

// 使用：
var history = (IReadOnlyList<MapPointState>)
    MapPointHistoryField.GetValue(runState);
```

---

## 📐 模式十一：模组注册与序列化

### ModHelper.AddModelToPool — 最简注册

两种注册方式可选：

```csharp
// 方式一：ModHelper（Natsuki 风格，简洁）
public static void Initialize()
{
    ModHelper.AddModelToPool<SharedRelicPool, MyRelic>();
}

// 方式二：[Pool] Attribute（ShunMod 风格，需 ContentRegistry.RegisterAll）
[Pool(typeof(SharedRelicPool))]
public class MyRelic : RelicModel { ... }
```

### SavedPropertiesTypeCache — 自定义模型必须注入

如果你的遗物/能力/卡牌有自定义属性且需要序列化保存，**必须**注入类型缓存：

```csharp
public static void Initialize()
{
    // 在 Harmony 打补丁之前调用
    SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MyRelic));
    SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MyPower));
}
```

### SavedProperty — 自定义可序列化属性

```csharp
public class MyRelic : RelicModel
{
    private int _myValue;

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int SavedMyValue
    {
        get => _myValue;
        set
        {
            _myValue = Math.Max(0, value);  // 业务校验
            InvokeDisplayAmountChanged();     // 触发 UI 更新
        }
    }

    public override bool ShowCounter => true;
    public override int DisplayAmount => !IsCanonical ? _myValue : 0;
}
```

### CoreHook — 游戏提供的钩子（优于打补丁）

```csharp
// 用法：Hook 卡牌奖励生成（参考 RewardEnchants）
harmony.Patch(
    RequireMethod(typeof(CoreHook), nameof(CoreHook.TryModifyCardRewardOptions),
        BindingFlags.Static | BindingFlags.Public,
        typeof(IRunState), typeof(Player), typeof(List<CardCreationResult>),
        typeof(CardCreationOptions), typeof(List<AbstractModel>).MakeByRefType()),
    postfix: new HarmonyMethod(typeof(ModEntry), nameof(RewardPostfix)));

static void RewardPostfix(
    Player player, List<CardCreationResult> cardRewardOptions,
    CardCreationOptions creationOptions, ref bool __result)
{
    // 在这里给奖励卡牌加附魔、修改卡牌等
}
```

---

## 🎴 模式十二：初始牌组修改

批量修改所有角色的起始牌组，用泛型复用逻辑（参考 StartingDeckTweaks）：

```csharp
private static void PatchStartingDeck<TCharacter>(Harmony harmony, string prefixName)
    where TCharacter : CharacterModel
{
    MethodInfo? getter = typeof(TCharacter)
        .GetProperty(nameof(CharacterModel.StartingDeck),
            BindingFlags.Instance | BindingFlags.Public)
        ?.GetMethod;
    if (getter == null)
        throw new InvalidOperationException(
            $"Could not find {typeof(TCharacter).FullName}.StartingDeck getter.");

    harmony.Patch(getter,
        prefix: new HarmonyMethod(typeof(ModEntry), prefixName));
}

// 调用：
PatchStartingDeck<Ironclad>(harmony, nameof(IroncladDeckPrefix));
PatchStartingDeck<Silent>(harmony, nameof(SilentDeckPrefix));
PatchStartingDeck<Defect>(harmony, nameof(DefectDeckPrefix));
// ...

// Prefix 实现（return false 跳过原方法）：
static bool IroncladDeckPrefix(ref IEnumerable<CardModel> __result)
{
    __result = [
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<Bash>(),
        ModelDb.Card<TwinStrike>()  // 加张牌
    ];
    return false;
}
```

---

## 🏗️ 模式十三：构建与部署脚本

每个模组建议提供 `tools/build_and_deploy.sh` + `tools/pack_mod.gd` + `tools/project.godot`：

### 项目目录结构（Natsuki 风格）

```
MyMod/
├── assets/           ← 清单 JSON、本地化、图片、音频
│   └── MyMod.json    ← 模组清单
├── src/              ← C# 源码
│   └── MyMod.csproj  ← 独立的 .csproj
├── tools/            ← 构建/打包/部署脚本
│   ├── build_and_deploy.sh
│   ├── pack_mod.gd
│   └── project.godot
└── dist/             ← 构建输出（不提交 git）
    ├── MyMod.json
    ├── MyMod.pck
    └── MyMod.dll
```

### build_and_deploy.sh 结构

```bash
#!/bin/zsh
set -euo pipefail

# 1. 变量定义
MOD_NAME="MyMod"
ROOT="/path/to/mods/$MOD_NAME"
GAME_APP="/path/to/Slay the Spire 2/SlayTheSpire2.app"
MOD_DIR="$GAME_APP/Contents/MacOS/mods/$MOD_NAME"

# 2. 清理 + 编译
dotnet build "$ROOT/src/$MOD_NAME.csproj" -c Release

# 3. 打包 PCK（非代码资源：图片/音频/本地化/场景）
"$GAME_BIN" --headless \
  --path "$ROOT/tools" \
  -s res://pack_mod.gd -- \
  "$MANIFEST_SRC" \
  "$ROOT/dist/$MOD_NAME.pck"

# 4. 拷贝 DLL（排除游戏自带的 sts2.dll / GodotSharp.dll / 0Harmony.dll）
for dll in "$BUILD_OUT"/*.dll; do
  case "$(basename "$dll")" in
    sts2.dll|GodotSharp.dll|0Harmony.dll) continue ;;
  esac
  cp "$dll" "$MOD_DIR/"
done

# 5. 部署到游戏 mods/
cp "$ROOT/dist/$MOD_NAME.json" "$MOD_DIR/"
cp "$ROOT/dist/$MOD_NAME.pck" "$MOD_DIR/"
```

### pack_mod.gd（Godot 脚本打包 PCK）

```gdscript
extends SceneTree

func _init():
    var args = OS.get_cmdline_args()
    var manifest_path = args[0]
    var output_path = args[1]
    var manifest = JSON.parse_string(FileAccess.get_file_as_string(manifest_path))

    var files = PackedStringArray()
    for asset in manifest.get("assets", []):
        var path = asset.replace("res://", "")
        if FileAccess.file_exists(path):
            files.append(path)

    var packed = PCKPacker.new()
    packed.pck_start(output_path)
    for f in files:
        packed.add_file("res://" + f, f)
    packed.flush()
    quit()
```

### project.godot（最小模板）

```ini
[application]
config/name="MyMod Pack"
config/description="Resource packer for MyMod"
```

---
