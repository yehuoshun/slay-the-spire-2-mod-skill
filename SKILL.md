---
name: slay-the-spire-2-mod-skill
description: 杀戮尖塔2（Slay the Spire 2 / STS2）模组开发实用指南。当用户提到杀戮尖塔2、杀戮尖塔、STS2、STS Mod、写模组、模组开发、卡牌、补丁、Harmony、Godot模组、Mod开发时使用。提供项目搭建、Harmony补丁、自定义卡牌、修正器、Godot UI、联机适配的完整参考。
---

# 杀戮尖塔 2 Mod 开发技能

> 本技能教你**怎么写** STS2 Mod。参考实现：[STS2Plus](https://github.com/StephenSHorton/STS2Plus)

---

## 1. 项目搭建

### 环境准备

- **.NET SDK 9.0** — 版本必须与游戏一致（查看游戏解包目录的 `global.json`）
- **Megadot 编辑器** — STS2 使用自研 Godot 分支 [Megadot](https://megadot.megacrit.com)，**不要用官方 Godot**，API 可能不兼容。下载最新版本解压即可
- **游戏源码** — 推荐保留一份解包后的游戏源码，方便随时查看 API（关键文件如 `res://src/Core/Modding/ModManifest.cs`）

### 创建 Godot 项目

1. 在 Megadot 可执行文件目录下创建 `._sc_` 空文件（项目隔离，防止混淆）
2. 打开 Megadot，新建项目，项目名用英文，与模组 ID 一致
3. 选择兼容渲染器（速度快，不影响主程序）
4. 编辑器菜单：项目 → 工具 → C# → 创建 C# 解决方案
5. 在 VS/Rider 中打开生成的 `.sln`

### 项目文件结构

```
MyMod/
├── MyMod.csproj            # C# 项目
├── mod_manifest.json       # 模组清单
├── MainFile.cs             # 入口脚本
├── libs/                   # 游戏依赖 DLL
│   ├── sts2.dll
│   └── 0Harmony.dll
├── build/                  # 构建输出目录
└── res://MyMod/            # Godot 资源
    └── mod_image.png       # 模组图标（可选）
```

### 模组清单 (mod_manifest.json)

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
| `id` | 唯一标识，推荐 `作者_模组名` 避免冲突 |
| `has_dll` | 有 C# 代码设 true，纯美化包设 false |
| `has_pck` | 有 Godot 资源（图片/音效/本地化）设 true |
| `affects_gameplay` | 新增卡牌/角色/修正器必须 true，联机会校验；UI 美化可 false |

### MyMod.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <OutputType>Library</OutputType>
    <Nullable>enable</Nullable>
    <LangVersion>13.0</LangVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

  <Target Name="CopyToBuild" AfterTargets="Build">
    <Copy SourceFiles="$(OutputPath)$(AssemblyName).dll"
          DestinationFolder="build\" />
  </Target>

  <ItemGroup>
    <Reference Include="sts2">
      <HintPath>libs\sts2.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="0Harmony">
      <HintPath>libs\0Harmony.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

从游戏 `data_sts2_<platform>/` 复制 `sts2.dll` 和 `0Harmony.dll` 到 `libs/` 统一管理。

构建：`dotnet build -c Release`，DLL 自动输出到 `build/`

### PCK 资源包导出

有卡牌贴图/本地化 JSON/音效等资源时：
1. 模组图标：`res://<modid>/mod_image.png`
2. 编辑器：项目 → 导出 → 添加 → Windows
3. 导出 PCK/ZIP，文件名 `<模组ID>.pck`
4. 取消勾选「使用调试导出」和「导出为补丁」
5. 保存到 `build/`

**资源替换**：PCK 内文件路径与游戏本体一致时自动覆盖，无需代码即可做材质包。

### 安装

复制到 `<游戏>/Mods/<模组ID>/`：

```
<游戏>/Mods/MyMod/
├── MyMod.dll
├── MyMod.pck       （可选）
└── MyMod.json      （mod_manifest.json 副本）
```

### 调试

- Steam 版不显示日志，推荐用 Megadot 打开项目运行
- `mods/` 放 Megadot 可执行文件同级目录
- 用 `GD.Print()` 或 `Log.Info()` 输出
- 游戏右下角显示模组加载数量

### 入口文件

```csharp
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    private static readonly Harmony harmony = new("mymod");

    public static void Initialize()
    {
        harmony.PatchAll();
        Log.Info("MyMod loaded!");
    }
}
```

---

## 2. Harmony 补丁

### 固定目标

```csharp
[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.Method))]
public static class MyPatch
{
    static void Postfix(ref int __result) => __result = 99;
}
```

### 动态目标

```csharp
[HarmonyPatch]
public static class MyDynamicPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        var baseGetter = AccessTools.PropertyGetter(typeof(BaseClass), "Prop");
        if (baseGetter != null) yield return baseGetter;
        foreach (var t in typeof(BaseClass).Assembly.GetTypes())
        {
            if (t.IsAbstract || !typeof(BaseClass).IsAssignableFrom(t)) continue;
            var g = AccessTools.PropertyGetter(t, "Prop");
            if (g != null && g.DeclaringType == t) yield return g;
        }
    }
    static void Postfix(ref int __result) => __result = 99;
}
```

### 替换方法体

```csharp
[HarmonyPatch(typeof(T), "Method")]
static class MyReplace
{
    static bool Prefix(ref int __result)
    {
        __result = 42;
        return false; // 跳过原方法
    }
}
```

---

## 3. 自定义卡牌

### 基类

```csharp
public abstract class MyCardBase : CardModel
{
    private readonly List<CardKeyword> _keywords = [];
    private int? _costUpgrade;
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

### 实现

```csharp
[Pool(typeof(ColorlessCardPool))]
public class MyCard : MyCardBase
{
    public MyCard() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
        WithCostUpgradeBy(-1);
    }
    public override string PortraitPath => "res://MyMod/cards/my_card.png";
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { }
}
```

### 自动注册

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class PoolAttribute : Attribute
{
    public Type PoolType { get; }
    public PoolAttribute(Type poolType) => PoolType = poolType;
}

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
    static Type[] GetTypes(Assembly a) {
        try { return a.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).Cast<Type>().ToArray(); }
    }
}
```

### 常用 API

```csharp
CardCmd.Upgrade(card);
CardCmd.Upgrade(card, CardPreviewStyle.None);
Owner.PlayerCombatState.AllCards
PileType.Deck.GetPile(Owner).Cards
card.IsUpgradable
card.EnergyCost
```

---

## 4. 自定义修正器

```csharp
internal sealed class MyRule : SyncedModifierModel { }
```

### 状态管理

```csharp
internal static class MyModState
{
    public static bool RuleSelected { get; private set; }
    public static bool IsRuleActive() => RuleSelected || HasActiveModifier("MY_RULE");
    public static void SetRule(bool e) => RuleSelected = e;
    public static void Sync(IEnumerable<ModifierModel> m) => RuleSelected = m.Any(x => x.Id.Entry == "MY_RULE");
}
```

---

## 5. UI 开发

```csharp
// 挂载
var root = ((SceneTree)Engine.GetMainLoop()).Root;
var layer = new CanvasLayer { Name = "MyLayer" };
root.AddChild(layer);

// 位置
((Control)this).Position = new Vector2(vpSize.X - Width - 16, 16);

// 检测阻塞
var stack = NOverlayStack.Instance;
bool blocked = (tree?.Paused == true) || (stack != null && stack.ScreenCount > 0);
```

---

## 6. 联机

```csharp
var net = RunManager.Instance?.NetService;
bool isClient = net?.Type == NetGameType.Client;

// 交互锁定
control.MouseFilter = isClient ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
control.Modulate = isClient ? new Color(1,1,1,0.65f) : Colors.White;
```

---

## 7. 反射与实用模式

```csharp
AccessTools.Property(typeof(T), "P").GetValue(i);
AccessTools.Field(typeof(T), "_f").GetValue(i);
typeof(CardModel).Assembly.GetType("Ns.Class");

// 防重复
static readonly HashSet<int> seen = [];
static bool Mark(object o) => seen.Add(RuntimeHelpers.GetHashCode(o));

// 弱引用
static readonly ConditionalWeakTable<CardModel, MyData> extras = new();
extras.GetOrCreateValue(card);

// 操作私有字段 + 触发事件
var f = typeof(Player).GetField("_relics", BindingFlags.NonPublic | BindingFlags.Instance);
if (f.GetValue(player) is List<RelicModel> list) list.Remove(relic);
var evt = typeof(Player).GetField("RelicRemoved", BindingFlags.Public | BindingFlags.Instance);
if (evt.GetValue(player) is Delegate d)
    foreach (var h in d.GetInvocationList()) h.Method.Invoke(h.Target, [relic]);
```

---

## 8. 本地化

```json
// eng/cards.json
{ "MY_CARD.title": "My Card", "MY_CARD.description": "Deal {Dmg:amount} dmg." }
// zhs/cards.json
{ "MY_CARD.title": "我的卡牌", "MY_CARD.description": "造成 {Dmg:amount} 点伤害。" }
```

```csharp
new LocString("cards", "MY_CARD.title");
LocManager.Instance.GetTable("modifiers").MergeWith(new Dictionary<string, string> { ["X.title"] = "X" });
```

---

## 9. 常见坑

| 坑 | 原因 | 对策 |
|----|------|------|
| 图标不显示 | 只能用内置图标 | `packed/modifiers/*.png` |
| 联机状态丢失 | 只查本地 | 双路：本地 + `HasActiveModifier` |
| IL2CPP 崩溃 | `GetTypes()` 抛异常 | 捕获 `ReflectionTypeLoadException` |
| 回调崩 | `QueueFree()` 异步 | `_ExitTree` 取消事件订阅 |
| Steam 无日志 | Steam 不显示控制台 | 用 Megadot 运行 |

---

## 10. API 速查

| 类型 | 命名空间 | 用途 |
|------|---------|------|
| `ModInitializer` | `Core.Modding` | 入口标记 |
| `ModHelper` | `Core.Modding` | 添加到卡池 |
| `CardModel` | `Core.Models` | 卡牌基类 |
| `RunManager` | `Core.Runs` | 运行管理 |
| `CardCmd` | `Core.Commands` | 卡牌操作 |
| `LocString/LocManager` | `Core.Localization` | 本地化 |
| `CombatManager` | `Core.Combat` | 战斗管理 |
| `NCombatRoom` | `Core.Nodes.Rooms` | 战斗场景 |
| `NOverlayStack` | `Core.Nodes.Screens.Overlays` | UI 层栈 |
