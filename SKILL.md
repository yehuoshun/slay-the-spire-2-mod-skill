# 杀戮尖塔 2 Mod 开发技能

> **参考实现**：[STS2Plus](https://github.com/StephenSHorton/STS2Plus) v0.2.0（修正器/UI/联机）、[STS2-ShunMod](https://github.com/yehuoshun/STS2-ShunMod) v0.0.8（卡牌/注册/补丁）

---

## 🎯 你要做什么

这个技能告诉你**怎么写 STS2 Mod**。不是介绍别人写了什么，而是给你可复用的模式、API、坑点。

---

## 🚀 从零搭建 Mod 项目

### 最小项目结构

```
MyMod/
├── MyMod.csproj          # .NET 9 Library
├── mod_manifest.json     # 游戏识别 Mod 的清单
├── MainFile.cs           # 入口点
└── localization/         # 可选：JSON 本地化
    ├── eng/
    └── zhs/
```

### .csproj 模板

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <OutputType>Library</OutputType>
    <Nullable>enable</Nullable>
    <LangVersion>12.0</LangVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <STS2GameDir Condition="'$(STS2GameDir)' == ''">
      C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2
    </STS2GameDir>
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

构建：`dotnet build -p:STS2GameDir="你的游戏路径" -c Release -o out/`

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

---

## 🔌 入口点

### 最小入口

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    private static readonly Harmony harmony = new("mymod");

    public static void Initialize()
    {
        harmony.PatchAll();  // 自动应用所有 [HarmonyPatch]
    }
}
```

### 带内容注册的入口（推荐）

```csharp
public static void Initialize()
{
    harmony.PatchAll();
    ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly());
}
```

---

## 🎯 Harmony 补丁三大模式

### 模式 1：固定目标 Patch

```csharp
// Postfix 注入——最常用，在方法执行后修改结果
[HarmonyPatch(typeof(目标类), nameof(目标类.方法名))]
public static class MyPatch
{
    static void Postfix(ref int __result)  // ref 可以覆盖返回值
    {
        __result = 99;
    }
}
```

### 模式 2：动态目标 Patch

```csharp
// 运行时匹配多个目标
[HarmonyPatch]  // 不指定类型
public static class MyDynamicPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        // 匹配所有子类的重写方法
        foreach (var type in typeof(BaseClass).Assembly.GetTypes())
        {
            if (!typeof(BaseClass).IsAssignableFrom(type)) continue;
            var getter = AccessTools.PropertyGetter(type, "PropertyName");
            if (getter != null && getter.DeclaringType == type)
                yield return getter;
        }
    }

    static void Postfix(ref int __result) { ... }
}
```

### 模式 3：替换方法体（Prefix return false）

```csharp
[HarmonyPatch(typeof(TargetClass), "MethodName")]
public static class MyReplacement
{
    static bool Prefix(/* 参数与目标方法一致 */, ref ReturnType __result)
    {
        // 你的实现
        __result = newValue;
        return false;  // 跳过原始方法
    }
}
```

---

## 🏷️ 自动内容注册系统

### 三步添加新卡牌/遗物，无需改入口

**第 1 步：定义 [Pool] 属性**

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class PoolAttribute : Attribute
{
    public Type PoolType { get; }
    public PoolAttribute(Type poolType) => PoolType = poolType;
}
```

**第 2 步：扫描并注册**

```csharp
public static class ContentRegistry
{
    public static void RegisterAll(Assembly assembly)
    {
        foreach (var type in GetLoadableTypes(assembly))
        {
            if (type.IsAbstract) continue;
            var pool = type.GetCustomAttribute<PoolAttribute>();
            if (pool == null) continue;
            ModHelper.AddModelToPool(pool.PoolType, type);
        }
    }

    static IReadOnlyList<Type> GetLoadableTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex)
            => ex.Types.Where(t => t != null).Cast<Type>().ToArray();
    }
}
```

**第 3 步：入口一行调用**

```csharp
public static void Initialize()
{
    harmony.PatchAll();
    ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly());
}
```

### 常用卡池类型

| 类型 | 说明 |
|------|------|
| `ColorlessCardPool` | 无色牌 |
| `IroncladCardPool` | 铁卫牌 |
| `SilentCardPool` | 静默猎人 |
| `DefectCardPool` | 程序员 |
| `WatcherCardPool` | 观者 |

---

## 🃏 自定义卡牌

### 基类封装模式（ShunCard）

继承 `CardModel`，用 sealed override 锁住抽象成员，子类只需填构造函数和 `OnPlay`：

```csharp
public abstract class ShunCard : CardModel
{
    private readonly List<CardKeyword> _keywords = [];
    private int? _costUpgrade;

    protected ShunCard(int baseCost, CardType type, CardRarity rarity, TargetType target)
        : base(baseCost, type, rarity, target) { }

    protected sealed override IEnumerable<DynamicVar> CanonicalVars => [];
    public sealed override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;
    protected sealed override HashSet<CardTag> CanonicalTags => [];

    protected override void OnUpgrade()
    {
        if (_costUpgrade.HasValue) EnergyCost.UpgradeBy(_costUpgrade.Value);
    }

    // 链式配置
    protected void WithKeywords(params CardKeyword[] k) => _keywords.AddRange(k);
    protected void WithCostUpgradeBy(int amount) => _costUpgrade = amount;
}
```

### 卡牌实现

```csharp
[Pool(typeof(ColorlessCardPool))]  // 自动注册，无需改入口
public class MyCard : ShunCard
{
    public MyCard()
        : base(baseCost: 2, type: CardType.Skill, rarity: CardRarity.Rare, target: TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
        WithCostUpgradeBy(-1); // 升级后费用减 1
    }

    public override string PortraitPath => "res://MyMod/cards/my_card.png";

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // 你的卡牌效果
    }
}
```

### 常用卡牌 API

```csharp
// 升级卡牌
CardCmd.Upgrade(card);
CardCmd.Upgrade(card, CardPreviewStyle.None);  // 无动画

// 获取卡牌
Owner.PlayerCombatState.AllCards      // 战斗中所有卡牌（手牌+抽牌+弃牌+消耗堆）
PileType.Deck.GetPile(Owner).Cards    // 牌组
PileType.Hand.GetPile(Owner).Cards    // 手牌
PileType.Discard.GetPile(Owner).Cards // 弃牌堆
PileType.Exhaust.GetPile(Owner).Cards // 消耗堆

// 卡牌属性
card.IsUpgradable    // bool — 是否可升级
card.EnergyCost      // DynamicVar — 费用
card.Enchantment     // EnchantmentModel — 当前附魔
```

---

## 🧰 反射工具速查

### AccessTools（Harmony 提供）

```csharp
// 获取属性/字段/方法
var prop = AccessTools.Property(typeof(T), "Name");
prop.GetValue(instance);
prop.SetValue(instance, value);

var field = AccessTools.Field(typeof(T), "_fieldName");
field.GetValue(instance);

var method = AccessTools.Method(typeof(T), "MethodName");
method.Invoke(instance, args);

// PropertyGetter 用于 TargetMethods
var getter = AccessTools.PropertyGetter(typeof(T), "PropertyName");
```

### 运行时类型查找

```csharp
// 从全限定名查找
var type = Type.GetType("Namespace.ClassName");

// 从 STS2 程序集查找
var type = typeof(CardModel).Assembly.GetType("Namespace.ClassName");

// 遍历查找（兼容 IL2CPP）
static Type FindType(string simpleName) =>
    typeof(CardModel).Assembly.GetTypes()
        .FirstOrDefault(t => t.Name == simpleName);
```

### 常见反射路径

```csharp
// 获取运行状态
RunManager.Instance.DebugOnlyGetState()

// 获取地图
typeof(CardModel).Assembly.GetType("...NMapScreen")
AccessTools.Property(mapScreenType, "Instance")?.GetValue(null)

// 获取当前房间
NCombatRoom.Instance

// 获取 Overlay 栈
NOverlayStack.Instance
```

---

## 🎭 自定义修正器

### SyncedModifierModel 模式

```csharp
// 第 1 步：定义修正器类（只需空类体）
internal sealed class MyRule : SyncedModifierModel { }

// SyncedModifierModel 已处理：
// - Title（从本地化表读取）
// - Description
// - Icon（内置图标映射）
```

### 注册修正器

```csharp
// 在 ModelDb 中注册
ModelDb.Inject(typeof(MyRule));

// 创建实例
var modifier = ModelDb.GetByIdOrNull<ModifierModel>(ModelDb.GetId(typeof(MyRule)));
```

### 修正器状态管理

```csharp
// 本地状态
static bool myRuleSelected;

// 活跃性检查（双重保障：本地 + 运行状态）
public static bool IsMyRuleActive()
    => myRuleSelected || GameReflection.HasActiveModifier("MY_RULE");

// 设置状态
public static void SetMyRule(bool enabled)
{
    myRuleSelected = enabled;
}

// 从运行状态同步
public static void SyncFromRunState()
{
    myRuleSelected = GameReflection.HasActiveModifier("MY_RULE");
}
```

### 修正器影响数值

```csharp
// 在 Patch 中检查并应用：
[HarmonyPatch(typeof(CardModel), "method")]
static void Postfix(CardModel __instance, ref int __result)
{
    if (PlusState.IsMyRuleActive())
        __result += 3;  // 应用修正器加成
}
```

---

## ✨ UI 组件开发模式

### 独立 Overlay 挂载

```csharp
// 挂载到场景根节点
var root = ((SceneTree)Engine.GetMainLoop()).Root;
var canvasLayer = new CanvasLayer { Name = "MyLayer" };
root.AddChild(canvasLayer);

// 按需控制可见性
canvasLayer.Visible = ShouldShow();
```

### 尺寸控制——右键上方浮动

```csharp
// 固定大小 + 固定位置
((Control)this).Size = new Vector2(228f, 42f);
((Control)this).Position = new Vector2(
    viewportSize.X - 228f - 148f,  // 右边缘偏移
    86f                             // 顶部偏移
);
```

### 自定义绘制控件

```csharp
// 重写 _Draw 用 CanvasItem 绘制
public override void _Draw()
{
    DrawColoredPolygon(vertices, fillColor, null, null);
    DrawCircle(center, radius, color);
}
```

### 检测 UI 栈阻塞

```csharp
// Overlay 打开时暂停
var overlayStack = NOverlayStack.Instance;
if (overlayStack != null && overlayStack.ScreenCount > 0)
    return; // 有 Overlay 打开，隐藏自己

// 游戏暂停时
var tree = (SceneTree)Engine.GetMainLoop();
if (tree.Paused) return;
```

### 从游戏捕获字体

```csharp
// 遍历生物节点找到 RichTextLabel/→Label 的 theme font
foreach (var child in parent.GetChildren())
{
    var label = child.GetNodeOrNull<Label>("%Value");
    if (label != null)
        return label.GetThemeFont("font", null);
}
```

---

## 👥 联机适配

### Host/Client 检测

```csharp
// 从 RunManager.NetService 检测
var net = RunManager.Instance?.NetService;
bool isMultiplayer = net != null && (net.Type == NetGameType.Host || net.Type == NetGameType.Client);

// 判断当前是否 Client（交互锁定）
bool isClient = net?.Type == NetGameType.Client;
```

### 交互锁定

联机中 Client 不能修改规则，UI 需要锁住：

```csharp
// 锁住控件
control.MouseFilter = isClient ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
control.Modulate = isClient ? new Color(1,1,1,0.65f) : Colors.White;
```

---

## 🌍 本地化

### JSON 文件结构

```json
// localization/eng/cards.json
{
  "MY_CARD.title": "My Card",
  "MY_CARD.description": "Deal {Damage:damageAmount} damage. {Exhaust:showExhaustTooltip}"
}

// localization/zhs/cards.json
{
  "MY_CARD.title": "我的卡牌",
  "MY_CARD.description": "造成 {Damage:damageAmount} 点伤害。{Exhaust:showExhaustTooltip}"
}
```

### 在代码中使用

```csharp
// CardModel 自动从 localization 表读取 Title/Description
// 只需确保 LocString 指向正确的表和键：
new LocString("cards", "MY_CARD.title")     // → 读取 cards.json 的 MY_CARD.title
new LocString("modifiers", "ID.entry")      // → 读取 modifiers.json
new LocString("relics", "RELIC_ID.name")    // → 读取 relics.json
```

### 动态合并（运行时）

```csharp
LocManager.Instance.GetTable("modifiers").MergeWith(new Dictionary<string, string>
{
    ["MY_RULE.title"] = "My Rule",
    ["MY_RULE.description"] = "Does something."
});
```

---

## 🔧 实用工具模式

### 防止重复处理（AppliedTracker）

```csharp
static class AppliedTracker
{
    static readonly HashSet<int> processed = [];

    public static bool Mark(object instance)
        => processed.Add(RuntimeHelpers.GetHashCode(instance));

    public static void Reset() => processed.Clear();
}

// 使用：
if (!AppliedTracker.Mark(creature))
    return; // 已处理过
// 否则：应用效果
```

### ConditionalWeakTable 弱引用存储

```csharp
// 存储额外数据但不阻止 GC
static readonly ConditionalWeakTable<CardModel, Dictionary<string, object>> extras = new();

var dict = extras.GetOrCreateValue(card);
dict["myData"] = value;

// 当 card 被 GC 时，关联数据自动清理
```

### 反射操作私有字段 + 手动触发事件

```csharp
// 反射移除私有列表中的元素
var field = typeof(Player).GetField("_relics", BindingFlags.NonPublic | BindingFlags.Instance);
if (field.GetValue(player) is List<RelicModel> list)
    list.Remove(relic);

// 必须手动触发公共事件！
var evt = typeof(Player).GetField("RelicRemoved", BindingFlags.Public | BindingFlags.Instance);
if (evt.GetValue(player) is Delegate del)
    foreach (var handler in del.GetInvocationList())
        handler.Method.Invoke(handler.Target, [relic]);
```

---

## ⚠️ 常见坑点

### 1. 修正器图标必须用游戏内置的

自定义修正器只能映射到已存在的内置图标路径：
```csharp
"packed/modifiers/all_star.png"     // ✓
"res://MyMod/icons/custom.png"      // ✗
```

### 2. 联机时序

修正器状态在 Host 选择 → 发送消息 → Client 接收这个链条中可能丢。必须**双路检查**：本地选择状态 + 运行状态中的修正器列表。

### 3. Harmony PatchAll 只扫描当前程序集

子模块/子命名空间中的 Patch 需要显式指定：
```csharp
harmony.PatchCategory(assembly, "MyCategory");
```

### 4. Unity IL2CPP / Mono 兼容

`Assembly.GetTypes()` 在非标准运行时可能抛 `ReflectionTypeLoadException`，必须捕获。

### 5. Godot 节点生命周期

- 从 `_ExitTree` 移除事件订阅，否则节点销毁后回调崩
- `QueueFree()` 不是同步的，下一帧才生效
- 使用 `GodotObject.IsInstanceValid(obj)` 检查节点是否存活

---

## 📦 发布

### 手动发布

```bash
dotnet build -c Release -o out/MyMod
cp mod_manifest.json out/MyMod/MyMod.json
# 打包 out/MyMod/ 下所有内容为 MyMod.zip
# 用户解压到 <Game>/Mods/MyMod/
```

### CI 自动发布（GitHub Actions）

- 上传 `sts2.dll` 和 `0Harmony.dll` 到 Release 作为构建依赖
- tag push 触发 CI 自动构建打包发布
- 构建时引用游戏 DLL（不打包进 Mod）

---

## 🔗 API 速查

| 命名空间 | 关键类型 | 用途 |
|---------|---------|------|
| `MegaCrit.Sts2.Core.Modding` | `ModInitializer`, `ModHelper` | 入口/注册 |
| `MegaCrit.Sts2.Core.Models` | `CardModel`, `ModifierModel`, `RelicModel` | 数据模型 |
| `MegaCrit.Sts2.Core.Runs` | `RunManager`, `RunState` | 运行状态 |
| `MegaCrit.Sts2.Core.Commands` | `CardCmd` | 卡牌操作命令 |
| `MegaCrit.Sts2.Core.Models.CardPools` | `ColorlessCardPool` 等 | 卡池类型 |
| `MegaCrit.Sts2.Core.Entities.Cards` | `CardKeyword`, `CardType`, `CardRarity`, `TargetType` | 卡牌枚举 |
| `MegaCrit.Sts2.Core.Localization` | `LocString`, `LocManager` | 本地化 |
| `MegaCrit.Sts2.Core.Combat` | `CombatManager`, `CombatState` | 战斗状态 |
| `MegaCrit.Sts2.Core.Nodes.Rooms` | `NCombatRoom` | 战斗场景 |
| `MegaCrit.Sts2.Core.Nodes.Screens.Overlays` | `NOverlayStack` | UI 层栈 |
| `MegaCrit.Sts2.Core.Nodes.Screens.Map` | `NMapScreen` | 地图屏幕 |
| `Godot` | `Node`, `Control`, `CanvasLayer`, `Label` 等 | Godot UI |
