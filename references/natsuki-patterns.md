## 🧬 实战模式：从 Natsuki 12 模组提炼的必知写法

> 以下模式来自 [s1f102500012/sts2mod](https://github.com/s1f102500012/sts2mod) 真实代码，agent 写 Mod 时直接套用。

### 1. ModEntry 初始化标准流程

```csharp
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    private static Harmony? _harmony;
    private static bool _hooksInstalled;

    public static void Initialize()
    {
        // 步骤 1：注入自定义模型的序列化缓存（有 [SavedProperty] 必做）
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MyRelic));

        // 步骤 2：注册到游戏池
        ModHelper.AddModelToPool<SharedRelicPool, MyRelic>();

        // 步骤 3：安装 Harmony 补丁
        Harmony harmony = _harmony ??= new Harmony("Author.ModId");
        InstallHooks(harmony);

        // 步骤 4：安装资源钩子（有自定义贴图时）
        AssetHooks.Install(harmony);

        Log.Info($"[MyMod] Loaded for STS2 {TargetGameVersion}.");
    }

    private static void InstallHooks(Harmony harmony)
    {
        if (_hooksInstalled) return;
        // 逐个 Patch，不用 PatchAll（更可控）
        harmony.Patch(
            RequireGetter(typeof(ModelDb), nameof(ModelDb.AllCharacters)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(AllCharactersPostfix)));
        _hooksInstalled = true;
    }

    // 反射辅助 — 找不到就抛异常，不在运行时静默失败
    private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags, params Type[] params) { ... }
    private static MethodInfo RequireGetter(Type type, string name) { ... }
    private static FieldInfo RequireField(Type type, string name) { ... }
}
```

### 2. 反射安全访问（RequireXxx 模式）

不用 `AccessTools`，直接用反射 + 启动时校验：

```csharp
private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags, params Type[] parameterTypes)
{
    return type.GetMethod(name, flags, null, parameterTypes, null)
        ?? throw new InvalidOperationException($"找不到方法 {type.FullName}.{name}");
}

private static MethodInfo RequireGetter(Type type, string propertyName)
{
    return type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetMethod
        ?? throw new InvalidOperationException($"找不到属性 {type.FullName}.{propertyName}");
}

private static FieldInfo RequireField(Type type, string name)
{
    return type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException($"找不到字段 {type.FullName}.{name}");
}
```

### 3. AssetHooks — 自定义贴图的标准写法

独立文件 `AssetHooks.cs`，Hook `RelicModel.Icon` / `BigIcon` getter：

```csharp
internal static class AssetHooks
{
    private static readonly Dictionary<string, Texture2D> TextureCache = new();

    public static void Install(Harmony harmony)
    {
        // Hook 遗物图标 getter
        harmony.Patch(
            RequireGetter(typeof(RelicModel), nameof(RelicModel.Icon)),
            prefix: new HarmonyMethod(typeof(AssetHooks), nameof(RelicIconPrefix)));
        harmony.Patch(
            RequireGetter(typeof(RelicModel), nameof(RelicModel.BigIcon)),
            prefix: new HarmonyMethod(typeof(AssetHooks), nameof(RelicBigIconPrefix)));
    }

    private static bool RelicIconPrefix(RelicModel __instance, ref Texture2D __result)
    {
        if (__instance.Id == ModelDb.GetId<MyRelic>())
        {
            __result = LoadTexture("res://MyMod/images/relics/my_relic.png")!;
            return false; // 跳过原方法
        }
        return true;
    }

    private static Texture2D? LoadTexture(string path)
    {
        if (TextureCache.TryGetValue(path, out var cached) && IsUsable(cached))
            return cached;
        // 优先文件系统，fallback 嵌入式资源
        if (Godot.FileAccess.GetFileAsBytes(path) is { Length: > 0 } bytes)
        {
            var img = new Image();
            img.LoadPngFromBuffer(bytes);
            var tex = new PortableCompressedTexture2D();
            tex.CreateFromImage(img, PortableCompressedTexture2D.CompressionMode.Lossless);
            TextureCache[path] = tex;
            return tex;
        }
        return null;
    }
}
```

### 4. Transpiler — IL 级常量替换

手牌上限 10→20 就是靠这个：

```csharp
// 把方法里所有 ldc.i4 10 替换成 ldc.i4 20
private static IEnumerable<CodeInstruction> ReplaceIntLoads(
    IEnumerable<CodeInstruction> instructions, int expectedCount, MethodBase originalMethod)
{
    var list = instructions.ToList();
    int patched = 0;
    foreach (var inst in list)
    {
        if (inst.opcode == OpCodes.Ldc_I4_S && inst.operand is sbyte sb && sb == 10)
            { inst.operand = (sbyte)20; patched++; }
        else if (inst.opcode == OpCodes.Ldc_I4 && inst.operand is int i && i == 10)
            { inst.operand = 20; patched++; }
    }
    if (patched != expectedCount)
        throw new InvalidOperationException($"预期替换 {expectedCount} 处，实际 {patched}");
    return list;
}
```

### 5. async 方法 Patch — GetAsyncStateMachineTarget

async 方法编译后变成状态机，Patch 必须打 `MoveNext`：

```csharp
private static MethodBase GetAsyncStateMachineTarget(MethodInfo method)
{
    var attr = method.GetCustomAttribute<AsyncStateMachineAttribute>();
    if (attr?.StateMachineType == null) return method;
    return attr.StateMachineType.GetMethod("MoveNext",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        ?? throw new InvalidOperationException($"找不到 {attr.StateMachineType.Name}.MoveNext");
}

// 使用
var drawMethod = RequireMethod(typeof(CardPileCmd), nameof(CardPileCmd.Draw), ...);
harmony.Patch(GetAsyncStateMachineTarget(drawMethod),
    transpiler: new HarmonyMethod(typeof(ModEntry), nameof(PatchDrawTranspiler)));
```

### 6. CoreHook — 游戏内置钩子（不用 Harmony）

某些功能游戏提供了 `CoreHook` 类，比 Harmony 更稳定：

```csharp
// 修改卡牌奖励（给奖励卡牌随机附魔）
harmony.Patch(
    RequireMethod(typeof(CoreHook), nameof(CoreHook.TryModifyCardRewardOptions), ...),
    postfix: new HarmonyMethod(typeof(ModEntry), nameof(TryModifyCardRewardOptionsPostfix)));
```

### 7. 事件选项的高级写法

```csharp
// 带遗物预览的选项
new EventOption(this, onChosen, textKey, relic.HoverTips).WithRelic(relic)

// 扣血选项
new EventOption(this, onChosen, textKey).ThatDoesDamage(28)

// 不保存到选择历史（无尽模式用）
option.ThatWontSaveToChoiceHistory();
```

### 8. 初始牌组修改 — 泛型 Patch

```csharp
private static void PatchStartingDeck<T>(Harmony harmony, string prefixName)
    where T : CharacterModel
{
    var getter = typeof(T).GetProperty(nameof(CharacterModel.StartingDeck))!.GetMethod!;
    harmony.Patch(getter, prefix: new HarmonyMethod(typeof(ModEntry), prefixName));
}

// 使用
PatchStartingDeck<Ironclad>(harmony, nameof(IroncladStartingDeckPrefix));

static bool IroncladStartingDeckPrefix(ref IEnumerable<CardModel> __result)
{
    __result = [ModelDb.Card<StrikeIronclad>(), ...];
    return false; // 跳过原方法
}
```

### 9. 自定义角色完整注册

```csharp
// 1. 模型类型列表（用于 ModelDb.Inject）
internal static class MyContent
{
    public static readonly Type[] ModelTypes =
    [
        typeof(MyCharacter), typeof(MyCardPool), typeof(MyRelicPool),
        typeof(MyStrike), typeof(MyDefend), // ... 所有卡牌/遗物/能力
    ];
}

// 2. 如果 ModelDb 已初始化，手动注入
if (ModelDb.Contains(typeof(Ironclad)))
{
    foreach (var type in MyContent.ModelTypes)
    {
        if (!ModelDb.Contains(type))
        {
            ModelDb.Inject(type);
            ModelDb.GetById<AbstractModel>(ModelDb.GetId(type)).InitId(ModelDb.GetId(type));
        }
    }
}

// 3. Patch AllCharacters getter 追加角色
static void AllCharactersPostfix(ref IEnumerable<CharacterModel> __result)
{
    __result = __result.Append(ModelDb.Character<MyCharacter>());
}

// 4. 清除 ModelDb 缓存（强制重新扫描）
typeof(ModelDb).GetField("_allCards", BindingFlags.Static | BindingFlags.NonPublic)
    ?.SetValue(null, null);
```

### 10. SavedProperty 序列化

```csharp
public class MyRelic : RelicModel
{
    private int _counter;

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int MyCounter
    {
        get => _counter;
        set { _counter = Math.Max(0, value); InvokeDisplayAmountChanged(); }
    }

    public override bool ShowCounter => true;
    public override int DisplayAmount => !IsCanonical ? _counter : 0;
}
```

### 11. DeepClone — 重置追踪状态

```csharp
protected override void DeepCloneFields()
{
    base.DeepCloneFields();
    _trackedEnemies = new Dictionary<Creature, int>();
    _markedEnemies = new HashSet<Creature>();
}
```

### 12. 多人联机安全

```csharp
// 共享遗物只在主实例上生效
internal static bool IsPrimarySharedRelic(RelicModel relic, IRunState? runState)
{
    var primary = runState?.Players
        .SelectMany(p => p.Relics.OfType<MyRelic>())
        .OrderByDescending(r => r.StackCount)
        .FirstOrDefault();
    return primary == null || ReferenceEquals(primary, relic);
}
