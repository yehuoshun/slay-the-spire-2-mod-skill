---
name: slay-the-spire-2-mod-skill
description: 杀戮尖塔2（Slay the Spire 2 / STS2）模组开发实用指南。当用户提到杀戮尖塔2、杀戮尖塔、STS2、STS Mod、写模组、模组开发、卡牌、补丁、Harmony、Godot模组、Mod开发时使用。
---

# 杀戮尖塔 2 Mod 开发技能

> 参考实现：[STS2Plus](https://github.com/StephenSHorton/STS2Plus)

---

## 1. 环境与项目

### 关键依赖

- **.NET 9.0 SDK** — 与游戏 global.json 一致
- **[Megadot](https://megadot.megacrit.com)** — STS2 自研 Godot 分支，不可用官方 Godot
- `0Harmony.dll` — 游戏自带，运行时 IL 补丁框架

### .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <OutputType>Library</OutputType>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="sts2"><HintPath>libs\sts2.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="0Harmony"><HintPath>libs\0Harmony.dll</HintPath><Private>false</Private></Reference>
  </ItemGroup>
</Project>
```

`sts2.dll` / `0Harmony.dll` 从游戏 `data_sts2_windows_x86_64/` 复制到 `libs/`。

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

| 字段 | 说明 |
|------|------|
| `affects_gameplay` | 加卡牌/修正器必须 true，联机会校验；UI 美化可 false |
| `has_dll` | 有 C# 代码设 true |
| `has_pck` | 有 Godot 资源（图片/本地化 json）设 true |

### 入口

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    static readonly Harmony harmony = new("mymod");
    public static void Initialize() => harmony.PatchAll();
}
```

### 调试

Steam 版无控制台日志，用 Megadot 打开项目运行，`mods/` 放编辑器同级目录。

---

## 2. Harmony 补丁

### 固定目标

```csharp
[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.Method))]
static void Postfix(ref int __result) => __result = 99;
```

### 动态目标（Patch 属性的所有子类重写）

```csharp
[HarmonyPatch]
static IEnumerable<MethodBase> TargetMethods()
{
    foreach (var t in typeof(BaseClass).Assembly.GetTypes())
    {
        if (t.IsAbstract || !typeof(BaseClass).IsAssignableFrom(t)) continue;
        var g = AccessTools.PropertyGetter(t, "Prop");
        if (g?.DeclaringType == t) yield return g;
    }
}
static void Postfix(ref int __result) => __result = 99;
```

### 替换方法体

```csharp
[HarmonyPatch(typeof(T), "Method")]
static bool Prefix(ref int __result)
{
    __result = 42;
    return false; // 跳过原方法
}
```

---

## 3. 自定义卡牌

### 基类

```csharp
public abstract class MyCardBase : CardModel
{
    readonly List<CardKeyword> _keywords = [];
    int? _costUpgrade;
    protected MyCardBase(int cost, CardType type, CardRarity rarity, TargetType tgt)
        : base(cost, type, rarity, tgt) {}
    protected sealed override IEnumerable<DynamicVar> CanonicalVars => [];
    public sealed override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;
    protected sealed override HashSet<CardTag> CanonicalTags => [];
    protected override void OnUpgrade() { if (_costUpgrade.HasValue) EnergyCost.UpgradeBy(_costUpgrade.Value); }
    protected void WithKeywords(params CardKeyword[] k) => _keywords.AddRange(k);
    protected void WithCostUpgradeBy(int a) => _costUpgrade = a;
}
```

### 实现 + 自动注册

```csharp
[Pool(typeof(ColorlessCardPool))]  // 标记即注册，不碰入口
public class MyCard : MyCardBase
{
    public MyCard() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { WithKeywords(CardKeyword.Exhaust); WithCostUpgradeBy(-1); }
    public override string PortraitPath => "res://MyMod/cards/my_card.png";
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { }
}

// PoolAttribute
[AttributeUsage(AttributeTargets.Class)]
public class PoolAttribute(Type poolType) : Attribute { public Type PoolType => poolType; }

// 注册器
public static class ContentRegistry
{
    public static void RegisterAll(Assembly a)
    {
        foreach (var t in GetTypes(a))
        {
            if (t.IsAbstract) continue;
            var p = t.GetCustomAttribute<PoolAttribute>();
            if (p != null) ModHelper.AddModelToPool(p.PoolType, t);
        }
    }
    static Type[] GetTypes(Assembly a)
    {
        try { return a.GetTypes(); }
        catch (ReflectionTypeLoadException e) => e.Types.Where(t => t != null).Cast<Type>().ToArray();
    }
}
```

### 常用 API

```csharp
CardCmd.Upgrade(card);
Owner.PlayerCombatState.AllCards          // 战斗中所有卡
PileType.Deck.GetPile(Owner).Cards        // 牌组
card.IsUpgradable
card.EnergyCost
```

---

## 4. 自定义遗物

### 遗物类

继承 `RelicModel`，类名自动转 `UPPER_SNAKE_CASE` 作为遗物 ID（如 `MyRelic` → `MY_RELIC`）。

```csharp
internal sealed class MyRelic : RelicModel
{
    // 稀有度：Starter(初始)/Common/Uncommon/Rare/Shop/Event/Ancient
    // Starter/Event/Ancient 不进常规宝箱/精英池
    protected override Rarity Rarity => Rarity.Starter;

    // 动态变量：{Energy:amount} 等显示用
    protected sealed override IEnumerable<DynamicVar> CanonicalVars => [
        DynamicVars.Energy.WithBase(1)
    ];

    // 回合开始事件钩子
    public override async Task AfterSideTurnStart(Side side, CombatState combatState)
    {
        if (side != Owner.Creature.Side) return;
        await Flash();  // 遗物闪烁动画
        await PlayerCmd.GainEnergy(Owner, DynamicVars.Energy.BaseValue);
    }
}
```

### 注册到遗物池

```csharp
// 初始化中
ModHelper.AddModelToPool(typeof(IroncladRelicPool), typeof(MyRelic));

// 或用自动注册（同卡牌模式）
[Pool(typeof(SharedRelicPool))]
internal sealed class MyRelic : RelicModel { }
```

| 遗物池 | 获取方式 |
|--------|---------|
| `IroncladRelicPool` 等角色池 | 精英/商店 |
| `SharedRelicPool` | 精英/商店/宝箱 |
| `EventRelicPool` | 仅问号事件 |
| `Shop` 稀有度 | 仅商店 |
| `FallbackRelicPool` | 兜底 |

### 修改初始遗物

```csharp
[HarmonyPatch(typeof(Ironclad), "StartingRelics", MethodType.Getter)]
static void Postfix(ref IReadOnlyList<RelicModel> __result)
{
    var list = __result.ToList();
    list.Add(ModelDb.GetByIdOrNull<RelicModel>(ModelDb.GetId(typeof(MyRelic))));
    __result = list;
}
```

### 遗物图标

| 类型 | 路径 | 格式 |
|------|------|------|
| 大图 | `res://images/relics/<小写ID>.png` | 256×256 PNG |
| 剪影 | `res://images/relics/<小写ID>_outline.png` | 未发现时显示 |
| 图标裁切 | `res://images/atlases/relic_atlas.sprites/<小写ID>.tres` | AtlasTexture 资源 |
| 剪影裁切 | `res://images/atlases/relic_outline_atlas.sprites/<小写ID>.tres` | AtlasTexture 资源 |

图标裁切 `.tres` 通过 Godot 编辑器创建 AtlasTexture 资源，指定 PNG 作为 Atlas，裁剪区域 256×256。

### 遗物本地化

`res://<modid>/localization/<lang>/relics.json`：

```json
{
  "MY_RELIC.title": "我的遗物",
  "MY_RELIC.description": "首回合获得 {Energy:amount} 点能量",
  "MY_RELIC.flavor": "引言文本"
}
```

### Harmony 版本兼容

Godot 4.5.1 的 GodotSharp 可能使用更高 .NET 版本，导致 Harmony 报错。编辑 `GodotSharp/Api/Debug/GodotPlugins.runtimeconfig.json`：

```json
{
  "runtimeOptions": {
    "framework": { "name": "Microsoft.NETCore.App", "version": "9.0.0" }
  }
}
```

---

## 5. 自定义修正器

```csharp
// 空类体即可，逻辑在 Patch 里
internal sealed class MyRule : SyncedModifierModel { }

// 状态管理 — 双路检查
public static bool IsRuleActive => RuleSelected || HasActiveModifier("MY_RULE");
```

---

## 6. UI 开发

```csharp
// Overlay 挂载
var layer = new CanvasLayer { Name = "MyLayer" };
((SceneTree)Engine.GetMainLoop()).Root.AddChild(layer);

// 检测 UI 阻塞
var stack = NOverlayStack.Instance;
bool blocked = tree.Paused || (stack?.ScreenCount > 0);
```

---

## 7. 联机适配

```csharp
var net = RunManager.Instance?.NetService;
bool isClient = net?.Type == NetGameType.Client;

// Client 端锁 UI
control.MouseFilter = isClient ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
control.Modulate = isClient ? new Color(1,1,1,0.65f) : Colors.White;
```

---

## 8. 反射与工具模式

```csharp
// 反射
AccessTools.Property(typeof(T), "P").GetValue(instance);
typeof(CardModel).Assembly.GetType("Namespace.Class");

// 防重复处理
static readonly HashSet<int> seen = [];
if (!seen.Add(RuntimeHelpers.GetHashCode(instance))) return;

// 弱引用额外数据
static readonly ConditionalWeakTable<CardModel, Data> extras = new();
extras.GetOrCreateValue(card);

// 反射删数据后手动触发事件
field.GetValue(player).Remove(relic);
foreach (var h in eventDelegate.GetInvocationList())
    h.Method.Invoke(h.Target, [relic]);
```

---

## 9. 本地化

```json
// eng/cards.json
{ "MY_CARD.title": "My Card", "MY_CARD.description": "Deal {Dmg:amount}." }
```

```csharp
new LocString("cards", "MY_CARD.title");
LocManager.Instance.GetTable("modifiers").MergeWith(dict);
```

---

## 10. 常见坑

| 坑 | 原因 | 对策 |
|----|------|------|
| 图标不显示 | 只能用内置图标 | `packed/modifiers/*.png` |
| 联机状态丢失 | 只查了本地状态 | 双路：本地 + `HasActiveModifier` |
| IL2CPP 崩溃 | `GetTypes()` 抛异常 | 捕获 `ReflectionTypeLoadException` |
| 节点销毁后崩 | `QueueFree()` 异步 | `_ExitTree` 取消订阅 |
| Steam 无日志 | Steam 不连控制台 | Megadot 项目模式运行 |
| Harmony 报错 | GodotSharp .NET 版本过高 | 编辑 `GodotPlugins.runtimeconfig.json` 强制 9.0 |

---

## 11. API 速查

| 类型 | 命名空间 | 用途 |
|------|---------|------|
| `ModInitializer` | `Core.Modding` | 入口 |
| `ModHelper.AddModelToPool` | `Core.Modding` | 入池 |
| `CardModel` | `Core.Models` | 卡牌基类 |
| `RelicModel` | `Core.Models` | 遗物基类 |
| `AbstractModel` | `Core.Models` | 所有内容基类 |
| `DynamicVars` | `Core.Localization.DynamicVars` | 动态变量集合 |
| `PlayerCmd.GainEnergy` | `Core.Commands` | 玩家操作（能量/金币等） |
| `RunManager.Instance` | `Core.Runs` | 运行状态 |
| `CardCmd.Upgrade` | `Core.Commands` | 升级卡牌 |
| `LocString` / `LocManager` | `Core.Localization` | 本地化 |
| `CombatManager` | `Core.Combat` | 战斗 |
| `NCombatRoom` | `Core.Nodes.Rooms` | 战斗场景 |
| `NOverlayStack` | `Core.Nodes.Screens.Overlays` | UI 栈 |
| `0Harmony.dll` | 游戏自带 | IL 补丁 |
| `Megadot` | megadot.megacrit.com | Godot 编辑器 |
