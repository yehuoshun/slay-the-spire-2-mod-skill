# 杀戮尖塔 2 Mod 开发技能

> 本技能教你**怎么写** STS2 Mod。参考实现：[STS2Plus](https://github.com/StephenSHorton/STS2Plus)、[STS2-ShunMod](https://github.com/yehuoshun/STS2-ShunMod)

---

## 1. 项目搭建

### 最小项目文件

```
MyMod/
├── MyMod.csproj
├── mod_manifest.json
└── MainFile.cs
```

### MyMod.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <OutputType>Library</OutputType>
    <Nullable>enable</Nullable>
    <LangVersion>12.0</LangVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <STS2GameDir>C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2</STS2GameDir>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="sts2">
      <HintPath>$(STS2GameDir)\data_sts2_windows_x86_64\sts2.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="GodotSharp">
      <HintPath>$(STS2GameDir)\data_sts2_windows_x86_64\GodotSharp.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="0Harmony">
      <HintPath>$(STS2GameDir)\data_sts2_windows_x86_64\0Harmony.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

构建：`dotnet build -c Release -o out/`

### mod_manifest.json

```json
{
  "id": "MyMod",
  "name": "My Mod",
  "author": "you",
  "version": "0.1.0",
  "has_dll": true,
  "has_pck": false,
  "dependencies": [],
  "affects_gameplay": true
}
```

安装：复制 DLL + manifest 到 `<游戏>/Mods/MyMod/`

### 入口 MainFile.cs

```csharp
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    private static readonly Harmony harmony = new("mymod");

    public static void Initialize()
    {
        harmony.PatchAll();
    }
}
```

`harmony.PatchAll()` 自动应用程序集中所有带 `[HarmonyPatch]` 的类。

---

## 2. Harmony 补丁

STS2 Mod 通过 Harmony 在运行时注入 IL 代码，无需修改游戏文件。

### 模式 A：固定目标 — 修改已知类的方法

```csharp
using HarmonyLib;

[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
public static class MyPatch
{
    // Postfix：在方法执行后修改结果（最常用）
    static void Postfix(ref int __result)
    {
        __result = 99;  // ref 可以覆盖非 void 方法的返回值
    }

    // Prefix：在方法执行前拦截
    static bool Prefix()
    {
        // 返回 false = 跳过原始方法
        return true;
    }
}
```

### 模式 B：动态目标 — 运行时匹配一批方法

当需要 Patch 一个属性的所有子类重写时使用：

```csharp
[HarmonyPatch]  // 不指定目标类型
public static class MyDynamicPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        var baseGetter = AccessTools.PropertyGetter(typeof(BaseClass), "PropertyName");
        if (baseGetter != null) yield return baseGetter;

        foreach (var type in typeof(BaseClass).Assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(BaseClass).IsAssignableFrom(type))
                continue;
            var getter = AccessTools.PropertyGetter(type, "PropertyName");
            if (getter != null && getter.DeclaringType == type)
                yield return getter;
        }
    }

    static void Postfix(BaseClass __instance, ref int __result)
    {
        __result = 99;
    }
}
```

### 模式 C：完全替换 — Prefix 返回 false

```csharp
[HarmonyPatch(typeof(TargetClass), "TargetMethod")]
public static class MyReplacement
{
    static bool Prefix(/* 参数与目标方法一致 */, ref ReturnType __result)
    {
        __result = /* 新返回值 */;
        return false;  // 跳过原始方法体
    }
}
```

---

## 3. 自定义卡牌

### 卡牌基类

继承 `CardModel`，用 `sealed override` 锁住抽象成员，子类只填构造函数和 `OnPlay`：

```csharp
public abstract class MyCardBase : CardModel
{
    private readonly List<CardKeyword> _keywords = [];
    private int? _costUpgrade;

    protected MyCardBase(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target) { }

    protected sealed override IEnumerable<DynamicVar> CanonicalVars => [];
    public sealed override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;
    protected sealed override HashSet<CardTag> CanonicalTags => [];

    protected override void OnUpgrade()
    {
        if (_costUpgrade.HasValue)
            EnergyCost.UpgradeBy(_costUpgrade.Value);
    }

    protected void WithKeywords(params CardKeyword[] k) => _keywords.AddRange(k);
    protected void WithCostUpgradeBy(int amount) => _costUpgrade = amount;
}
```

### 卡牌实现

```csharp
[Pool(typeof(ColorlessCardPool))]  // 声明卡池，自动注册
public class MyCard : MyCardBase
{
    public MyCard()
        : base(cost: 2, type: CardType.Skill, rarity: CardRarity.Rare, target: TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
        WithCostUpgradeBy(-1);
    }

    public override string PortraitPath => "res://MyMod/cards/my_card.png";

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // 打出效果
    }
}
```

### 自动注册

```csharp
// PoolAttribute
[AttributeUsage(AttributeTargets.Class)]
public class PoolAttribute : Attribute
{
    public Type PoolType { get; }
    public PoolAttribute(Type poolType) => PoolType = poolType;
}

// ContentRegistry
public static class ContentRegistry
{
    public static void RegisterAll(Assembly assembly)
    {
        foreach (var type in GetTypes(assembly))
        {
            if (type.IsAbstract) continue;
            var pool = type.GetCustomAttribute<PoolAttribute>();
            if (pool != null)
                ModHelper.AddModelToPool(pool.PoolType, type);
        }
    }

    static Type[] GetTypes(Assembly a)
    {
        try { return a.GetTypes(); }
        catch (ReflectionTypeLoadException e)
            => e.Types.Where(t => t != null).Cast<Type>().ToArray();
    }
}

// 入口调用一次
public static void Initialize()
{
    harmony.PatchAll();
    ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly());
}
```

### 常用卡牌 API

```csharp
// 升级
CardCmd.Upgrade(card);
CardCmd.Upgrade(card, CardPreviewStyle.None);

// 获取卡牌
Owner.PlayerCombatState.AllCards      // 战斗中所有卡牌
PileType.Deck.GetPile(Owner).Cards    // 牌组
PileType.Hand.GetPile(Owner).Cards    // 手牌

// 属性
card.IsUpgradable       // 是否可升级
card.EnergyCost         // 费用（DynamicVar）
```

---

## 4. 自定义修正器

### 修正器类

```csharp
// 继承 SyncedModifierModel 即可，逻辑通过 Harmony Patch 实现
internal sealed class MyRule : SyncedModifierModel { }
```

### SyncedModifierModel 基类

```csharp
internal abstract class SyncedModifierModel : ModifierModel
{
    public override LocString Title => new LocString("modifiers", Id.Entry + ".title");
    public override LocString Description => new LocString("modifiers", Id.Entry + ".description");
    // Icon 自动映射到游戏内置图标
}
```

### 状态管理

```csharp
internal static class MyModState
{
    public static bool MyRuleSelected { get; private set; }

    // 检查修正器是否活跃 — 双路：本地选择 + 运行状态
    public static bool IsMyRuleActive()
        => MyRuleSelected || GameReflection.HasActiveModifier("MY_RULE");

    public static void SetMyRule(bool enabled)
        => MyRuleSelected = enabled;

    // 从运行状态恢复（存档加载/联机同步时调用）
    public static void SyncFromModifiers(IEnumerable<ModifierModel> modifiers)
        => MyRuleSelected = modifiers.Any(m => m.Id.Entry == "MY_RULE");
}
```

### 在 Patch 中应用修正器效果

```csharp
[HarmonyPatch(typeof(SomeClass), "SomeMethod")]
static void Postfix(ref int __result)
{
    if (MyModState.IsMyRuleActive())
        __result += bonus;
}
```

---

## 5. UI 开发

### 独立 Overlay 挂载

```csharp
// 挂载到场景根节点，独立于游戏 UI
var root = ((SceneTree)Engine.GetMainLoop()).Root;
var layer = new CanvasLayer { Name = "MyLayer" };
root.AddChild(layer);

// 在 _Process 中控制可见性
layer.Visible = ShouldShow();
```

### 位置控制 — 右上角浮动

```csharp
var vpSize = DisplayServer.WindowGetSize();
var margin = 16f;
((Control)this).Position = new Vector2(vpSize.X - Width - margin, margin);
```

### 自定义绘制

```csharp
public override void _Draw()
{
    DrawColoredPolygon(vertices, fillColor, null, null);
    DrawCircle(center, radius, color);
}
```

### 检测 UI 阻塞 — Overlay 打开时隐藏

```csharp
static bool IsBlocked()
{
    var tree = (SceneTree)Engine.GetMainLoop();
    if (tree?.Paused == true) return true;

    var stack = NOverlayStack.Instance;
    if (stack != null && stack.ScreenCount > 0) return true;

    return false;
}
```

### 从游戏捕获字体

```csharp
static Font? FindGameFont(Node parent)
{
    foreach (var child in parent.GetChildren())
    {
        var label = child.GetNodeOrNull<Label>("%Value");
        if (label != null)
            return label.GetThemeFont("font", null);
    }
    return null;
}
```

---

## 6. 联机适配

### Host/Client 检测

```csharp
// 从 RunManager 检测
var net = RunManager.Instance?.NetService;
bool isHost = net?.Type == NetGameType.Host;
bool isClient = net?.Type == NetGameType.Client;
bool isMultiplayer = isHost || isClient;
```

### 交互锁定

Client 不能修改规则，UI 控件需锁住：

```csharp
public static bool IsInteractionLocked()
{
    return RunManager.Instance?.NetService?.Type == NetGameType.Client;
}

// 应用到 UI
control.MouseFilter = IsInteractionLocked() ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
control.Modulate = IsInteractionLocked() ? new Color(1, 1, 1, 0.65f) : Colors.White;
```

### 修正器同步

Host 选择修正器 → 发送消息 → Client 同步：

```csharp
// Host 端：选择后发送
var message = new RuleSelectionSyncMessage { /* entries */ };
netService.Send(message);

// Client 端：接收后同步状态
MyModState.SyncFromModifiers(receivedModifiers);
```

---

## 7. 反射工具

```csharp
// Harmony AccessTools（推荐）
AccessTools.Property(typeof(T), "PropName").GetValue(instance);
AccessTools.Field(typeof(T), "_fieldName").GetValue(instance);
AccessTools.Method(typeof(T), "MethodName").Invoke(instance, args);

// 运行时类型查找
var type = typeof(CardModel).Assembly.GetType("Full.Namespace.TypeName");

// 检查节点存活
GodotObject.IsInstanceValid(node);
```

---

## 8. 实用模式

### 防重复处理

```csharp
static readonly HashSet<int> processed = [];

static bool Mark(object instance)
    => processed.Add(RuntimeHelpers.GetHashCode(instance));

// 使用：
if (!Mark(entity)) return;  // 已处理过，跳过
// 执行处理...
```

### 弱引用存储额外数据

```csharp
static readonly ConditionalWeakTable<CardModel, MyData> extras = new();

var data = extras.GetOrCreateValue(card);  // card 被 GC 时自动清理
```

### 操作私有数据后触发事件

```csharp
// 反射修改私有字段
var field = typeof(Player).GetField("_relics", BindingFlags.NonPublic | BindingFlags.Instance);
if (field.GetValue(player) is List<RelicModel> list)
    list.Remove(relic);

// 手动触发公共事件
var evt = typeof(Player).GetField("RelicRemoved", BindingFlags.Public | BindingFlags.Instance);
if (evt.GetValue(player) is Delegate del)
    foreach (var handler in del.GetInvocationList())
        handler.Method.Invoke(handler.Target, args);
```

---

## 9. 本地化

### JSON 文件结构

```json
// localization/eng/cards.json
{
  "MY_CARD.title": "My Card",
  "MY_CARD.description": "Deal {Damage:damageAmount} damage."
}
// localization/zhs/cards.json
{
  "MY_CARD.title": "我的卡牌",
  "MY_CARD.description": "造成 {Damage:damageAmount} 点伤害。"
}
```

### 代码中使用

```csharp
// CardModel/ModifierModel 自动读 localization 表
new LocString("cards", "MY_CARD.title")       // → cards.json
new LocString("modifiers", "MY_RULE.title")   // → modifiers.json
new LocString("relics", "MY_RELIC.name")      // → relics.json

// 运行时动态合并
LocManager.Instance.GetTable("modifiers")
    .MergeWith(new Dictionary<string, string>
    {
        ["MY_RULE.title"] = "My Rule",
        ["MY_RULE.description"] = "Does something."
    });
```

---

## 10. 常见坑

| 坑 | 原因 | 对策 |
|----|------|------|
| 修正器图标不显示 | 只能用游戏内置图标路径 | 用 `packed/modifiers/*.png` 不能自创 |
| 联机修正器状态丢失 | 只查了本地状态 | 双路检查：本地 + `HasActiveModifier` |
| IL2CPP/Mono 崩溃 | `Assembly.GetTypes()` 不兼容 | 捕获 `ReflectionTypeLoadException` |
| 节点销毁后回调崩 | `QueueFree()` 异步 | 事件订阅在 `_ExitTree` 取消 |
| `PatchAll()` 漏补丁 | 只扫描当前程序集 | 跨程序集用 `PatchCategory` |

---

## 11. API 速查

| 类型 | 命名空间 | 用途 |
|------|---------|------|
| `ModInitializer` | `MegaCrit.Sts2.Core.Modding` | 入口标记 |
| `ModHelper` | `MegaCrit.Sts2.Core.Modding` | 添加内容到卡池 |
| `CardModel` | `MegaCrit.Sts2.Core.Models` | 卡牌基类 |
| `ModifierModel` | `MegaCrit.Sts2.Core.Models` | 修正器基类 |
| `RunManager` | `MegaCrit.Sts2.Core.Runs` | 运行管理 |
| `CardCmd` | `MegaCrit.Sts2.Core.Commands` | 卡牌操作命令 |
| `LocString` / `LocManager` | `MegaCrit.Sts2.Core.Localization` | 本地化 |
| `CombatManager` | `MegaCrit.Sts2.Core.Combat` | 战斗管理 |
| `NCombatRoom` | `MegaCrit.Sts2.Core.Nodes.Rooms` | 战斗场景节点 |
| `NOverlayStack` | `MegaCrit.Sts2.Core.Nodes.Screens.Overlays` | Overlay 层栈 |
| `PlayerChoiceContext` | `MegaCrit.Sts2.Core.GameActions.PlayerInput` | 玩家选择上下文 |
