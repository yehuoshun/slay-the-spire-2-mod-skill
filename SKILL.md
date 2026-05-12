---
name: slay-the-spire-2-mod-skill
description: 杀戮尖塔2（Slay the Spire 2 / STS2）模组开发实用指南。当用户提到杀戮尖塔2、杀戮尖塔、STS2、STS Mod、写模组、模组开发、卡牌、遗物、药水、能力、事件、角色、怪物、补丁、Harmony、Godot模组、Mod开发时使用。
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

### Godot 项目

1. Megadot 新建项目，项目名 = 模组 ID
2. 菜单：项目 → 工具 → C# → 创建 C# 解决方案
3. 在 VS/Rider 中打开 `.sln`

### mod_manifest.json

```json
{ "id": "MyMod", "name": "My Mod", "author": "you", "version": "0.1.0",
  "has_dll": true, "has_pck": false, "dependencies": [], "affects_gameplay": true }
```

`affects_gameplay`：加卡牌/遗物/角色等必须 true，联机会校验。

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

Steam 版无控制台，用 Megadot 运行；`mods/` 放编辑器同级目录。

---

## 2. Harmony 补丁

### 固定目标

```csharp
[HarmonyPatch(typeof(T), nameof(T.Method))]
static void Postfix(ref int __result) => __result = 99;
```

### 动态目标

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
```

### 替换方法体

```csharp
[HarmonyPatch(typeof(T), "Method")]
static bool Prefix(ref int __result) { __result = 42; return false; }
```

---

## 3. 自定义卡牌

### 基类模式

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

### 卡牌实现

```csharp
[Pool(typeof(ColorlessCardPool))]
public class MyCard : MyCardBase
{
    public MyCard() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { WithKeywords(CardKeyword.Exhaust); WithCostUpgradeBy(-1); }
    public override string PortraitPath => "res://MyMod/cards/my_card.png";
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // ctx 获取选择目标，play 获取打出状态
    }
}
```

### 伤害链

```csharp
DamageCmd.Attack(damage)
    .FromCard(this)                      // 伤害来源
    .Targeting(target)                   // 目标 Creature
    .WithHitCount(count)                 // 连击次数
    .Execute();                          // 执行（必须 await）
```

### 抽牌与选牌

```csharp
CardPileCmd.Draw(owner, 2);                    // 抽 2 张
CardSelectCmd.FromHand(ctx, owner, prefs, filter, source);     // 从手牌选
CardSelectCmd.FromHandForUpgrade(ctx, owner, prefs, filter);   // 选牌升级
CardSelectCmd.FromHandForDiscard(ctx, owner, prefs, filter);   // 选牌丢弃
```

### 打出条件

```csharp
public override bool IsPlayable => Owner?.Gold >= 100;
public override bool ShouldGlowGoldInternal => Owner?.Gold >= 100;
```

### 回调钩子

```csharp
public override async Task OnTurnEndInHand(PlayerChoiceContext ctx)
{
    await PlayerCmd.Damage(Owner, 3);  // 回合结束还在手，扣血
}
```

### 自动注册

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class PoolAttribute(Type poolType) : Attribute { public Type PoolType => poolType; }

public static class ContentRegistry
{
    public static void RegisterAll(Assembly a)
    {
        foreach (var t in GetTypes(a))
        { if (t.IsAbstract) continue; var p = t.GetCustomAttribute<PoolAttribute>();
          if (p != null) ModHelper.AddModelToPool(p.PoolType, t); }
    }
    static Type[] GetTypes(Assembly a) {
        try { return a.GetTypes(); } catch (ReflectionTypeLoadException e) => e.Types.Where(t => t != null).Cast<Type>().ToArray(); }
}
```

### 图标与本地化

| 类型 | 路径 |
|------|------|
| 裁切纹理 | `images/atlases/card_atlas.sprites/<卡池>/<小写ID>.tres` |
| 大图 | `images/packed/card_portraits/<卡池>/<小写ID>.png` |
| 本地化 | `<modid>/localization/<lang>/cards.json` |

---

## 4. 自定义遗物

### 遗物类

```csharp
internal sealed class MyRelic : RelicModel
{
    protected override Rarity Rarity => Rarity.Starter;
    protected sealed override IEnumerable<DynamicVar> CanonicalVars => [
        DynamicVars.Energy.WithBase(1)
    ];
    public override async Task AfterSideTurnStart(Side side, CombatState combatState)
    {
        if (side != Owner.Creature.Side) return;
        await Flash();
        await PlayerCmd.GainEnergy(Owner, DynamicVars.Energy.BaseValue);
    }
}
```

### 遗物池

| 池 | 获取方式 |
|----|---------|
| `IroncladRelicPool` 等角色池 | 精英/商店 |
| `SharedRelicPool` | 精英/商店/宝箱 |
| `EventRelicPool` | 仅问号事件 |
| `Shop` 稀有度 | 仅商店 |

### 修改初始遗物

```csharp
[HarmonyPatch(typeof(Ironclad), "StartingRelics", MethodType.Getter)]
static void Postfix(ref IReadOnlyList<RelicModel> __result)
{ var list = __result.ToList(); list.Add(ModelDb.GetByIdOrNull<RelicModel>(ModelDb.GetId(typeof(MyRelic)))); __result = list; }
```

### 图标与本地化

| 类型 | 路径 |
|------|------|
| 大图 | `images/relics/<小写ID>.png` (256×256) |
| 剪影 | `images/relics/<小写ID>_outline.png` |
| 裁切 | `images/atlases/relic_atlas.sprites/<小写ID>.tres` |
| 剪影裁切 | `images/atlases/relic_outline_atlas.sprites/<小写ID>.tres` |
| 本地化 | `<modid>/localization/<lang>/relics.json` |

---

## 5. 自定义药水

### 三种模式

```csharp
// 伤害药水
public override async Task OnUse(PlayerChoiceContext ctx, PotionTarget target)
{ await DamageCmd.Attack(30).FromCard(this).TargetingAllOpponents(combatState).Execute(); }

// 效果药水（自损换 Buff）
public override async Task OnUse(PlayerChoiceContext ctx, PotionTarget target)
{ await PlayerCmd.Damage(Owner, 10); await PlayerCmd.ApplyPower(Owner, PowerDb.Get<IntangiblePower>(), 2); }

// 自动消耗药水（复活）
public override PotionUsage Usage => PotionUsage.ProgrammaticOnly;
public override bool CanBeGeneratedInCombat => false;  // 不随机生成
public override async Task<bool> ShouldDie(Creature creature)
{ return creature != Owner.Creature || !IsUsable; }  // 阻止死亡
public override async Task AfterPreventingDeath(Creature creature)
{ await Consume(); await PlayerCmd.Heal(Owner, 10); }  // 自动消耗
```

### 药水池

```csharp
ModHelper.AddModelToPool(typeof(SharedPotionPool), typeof(MyPotion));
```

### 图标与本地化

| 类型 | 路径 |
|------|------|
| 大图 | `images/potions/<小写ID>.png` |
| 裁切 | `images/atlases/potion_atlas.sprites/<小写ID>.tres` |
| 本地化 | `<modid>/localization/<lang>/potions.json` |

---

## 6. 自定义能力 (Buff/Debuff)

```csharp
internal sealed class MyPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;           // Buff / Debuff
    public override PowerStackType StackType => PowerStackType.Intensity;  // 层数可叠
    public override bool AllowNegative => false;                 // 层数≤0 时移除

    // 监听事件
    public override async Task AfterCardPlayed(CardModel card)
    { await PlayerCmd.GainBlock(Owner, Amount); }
    public override async Task OnSideTurnEnd(Side side)
    { if (side == Owner.Creature.Side) Amount--; }
}
```

### 图标与本地化

| 类型 | 路径 |
|------|------|
| 大图 | `images/powers/<小写ID>.png` |
| 裁切 | `images/atlases/power_atlas.sprites/<小写ID>.tres` |
| 本地化 | `<modid>/localization/<lang>/powers.json` (smartDescription 用于动态变量) |

---

## 7. 自定义附魔

```csharp
internal sealed class MyEnchant : EnchantmentModel
{
    // 伤害加成
    public override decimal EnchantDamageAdditive => Amount * 3;
    // 格挡加成
    public override decimal EnchantBlockAdditive => Amount * 2;

    // 附魔时修改卡牌
    public override void ModifyCard()
    { Card.WithKeywords(); Card.Keywords.Remove(CardKeyword.Exhaust); }
}

// 给卡牌附魔
CardCmd.Enchant(card, new MyEnchant { Amount = 2 });
```

| 路径 | 说明 |
|------|------|
| `images/enchantments/<小写ID>.png` | 图标 |
| `<modid>/localization/<lang>/enchantments.json` | 本地化 |

---

## 8. 自定义事件

### 基础事件

```csharp
internal sealed class MyEvent : EventModel
{
    public override IEnumerable<EventOption> GenerateInitialOptions(PlayerChoiceContext ctx)
    {
        return [
            new EventOption(
                ctx,
                InitialOptionKey("OPT_1"),                    // 本地化键：<事件ID>.pages.INITIAL.options.OPT_1
                async () => { await GiveCard(typeof(MyCard)); SetEventFinished(L10NLookup("pages.RECHARGE.description")); }
            )
        ];
    }
}
```

### 添加到地图

```csharp
[HarmonyPatch(typeof(Overgrowth), "AllEvents", MethodType.Getter)]
static void Postfix(ref IReadOnlyList<EventModel> __result)
{ var list = __result.ToList(); list.Add(ModelDb.GetByIdOrNull<EventModel>(ModelDb.GetId(typeof(MyEvent)))); __result = list; }
```

| 路径 | 说明 |
|------|------|
| `images/events/<小写ID>.png` | 背景图 (3440×1613) |
| `<modid>/localization/<lang>/events.json` | 本地化 |

---

## 9. 自定义怪物

```csharp
internal sealed class MyMonster : MonsterModel
{
    public override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var attackState = new MoveState("ATTACK",              // 状态 ID
            async (ctx) => { await DamageCmd.Attack(6).FromMonster(this).Targeting(ctx.GetPlayer()).Execute(); },
            [new AttackIntent(6)]);                            // 显示攻击意图
        attackState.FollowUpState = attackState;               // 循环自身
        return new MonsterMoveStateMachine([attackState], "ATTACK");
    }
}
```

### 遭遇类与添加

```csharp
// 遭遇定义
internal sealed class MyEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;
    public override IEnumerable<(MonsterModel, string?)> GenerateMonsters()
    { return [(ModelDb.GetByIdOrNull<MonsterModel>(ModelDb.GetId(typeof(MyMonster))).ToMutable(), null)]; }
}

// 添加到地图
[HarmonyPatch(typeof(Overgrowth), "GenerateAllEncounters")]
static void Postfix(ref IEnumerable<EncounterModel> __result)
{ __result = __result.Append(ModelDb.GetByIdOrNull<EncounterModel>(ModelDb.GetId(typeof(MyEncounter)))); }
```

| 路径 | 说明 |
|------|------|
| `scenes/creature_visuals/<小写ID>.tscn` | 怪物动画场景 |
| `scenes/encounters/<小写ID>.tscn` | 遭遇站位场景（Marker2D） |
| `<modid>/localization/<lang>/monsters.json` | 名称/意图标题 |
| `<modid>/localization/<lang>/encounters.json` | 遭遇文本 |

---

## 10. 自定义角色（要点）

创建角色 = `CharacterModel` 子类 + `CardPoolModel` + `RelicPoolModel` + `PotionPoolModel`。

### 角色类骨架

```csharp
internal sealed class MyChar : CharacterModel
{
    public override CharacterGender Gender => CharacterGender.Female;
    public override CharacterModel? UnlocksAfterRunAs => null;

    // 关联池
    public override CardPoolModel CardPool => ModelDb.GetByIdOrNull<CardPoolModel>(ModelDb.GetId(typeof(MyCardPool)));
    public override RelicPoolModel RelicPool => ModelDb.GetByIdOrNull<RelicPoolModel>(ModelDb.GetId(typeof(MyRelicPool)));
    public override PotionPoolModel PotionPool => ModelDb.GetByIdOrNull<PotionPoolModel>(ModelDb.GetId(typeof(MyPotionPool)));

    public override IReadOnlyList<CardModel> StartingDeck => [/* 初始卡组 */];
    public override IReadOnlyList<RelicModel> StartingRelics => [/* 初始遗物 */];
}
```

### 注册角色

```csharp
[HarmonyPatch(typeof(ModelDb), "AllCharacters", MethodType.Getter)]
static void Postfix(ref IReadOnlyList<CharacterModel> __result)
{ var list = __result.ToList(); list.Add(ModelDb.GetByIdOrNull<CharacterModel>(ModelDb.GetId(typeof(MyChar)))); __result = list; }
```

### 关键资源路径

| 资源 | 路径 |
|------|------|
| 动画场景 | `scenes/creature_visuals/<小写ID>.tscn` (挂 `NCreatureVisuals`) |
| 能量计数器 | `scenes/combat/energy_counters/<小写ID>_energy_counter.tscn` (挂 `NEnergyCounter`) |
| 角色图标 | `images/ui/top_panel/character_icon_<小写ID>.png` |
| 选择界面 | `images/packed/character_select/char_select_<小写ID>.png` |
| 本地化 | `<modid>/localization/<lang>/characters.json` |

### Godot 脚本修复

模组中带脚本的场景无法被 Godot 检索到，初始化中加：

```csharp
typeof(Node).Assembly.GetType("GodotPlugins.Game")?.GetMethod("ScanAssembly", BindingFlags.NonPublic|BindingFlags.Static)
    ?.Invoke(null, [Assembly.GetExecutingAssembly()]);
```

---

## 11. UI 开发

```csharp
var layer = new CanvasLayer { Name = "MyLayer" };
((SceneTree)Engine.GetMainLoop()).Root.AddChild(layer);
bool blocked = tree.Paused || (NOverlayStack.Instance?.ScreenCount > 0);
```

---

## 12. 联机适配

```csharp
bool isClient = RunManager.Instance?.NetService?.Type == NetGameType.Client;
control.MouseFilter = isClient ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
```

---

## 13. 反射与工具模式

```csharp
AccessTools.Property(typeof(T), "P").GetValue(instance);
typeof(CardModel).Assembly.GetType("Namespace.Class");

// 防重复
static readonly HashSet<int> seen = [];
if (!seen.Add(RuntimeHelpers.GetHashCode(instance))) return;

// 弱引用
static readonly ConditionalWeakTable<CardModel, Data> extras = new();
extras.GetOrCreateValue(card);
```

---

## 14. 本地化

```json
{ "MY_CARD.title": "Title", "MY_CARD.description": "{Dmg:amount} damage." }
```

```csharp
new LocString("cards", "MY_CARD.title");
LocManager.Instance.GetTable("modifiers").MergeWith(dict);
```

---

## 15. 常见坑

| 坑 | 对策 |
|----|------|
| 图标不显示 | 只能用内置图标或规范路径 |
| 联机状态丢失 | 双路检查：本地 + `HasActiveModifier` |
| IL2CPP 崩溃 | 捕获 `ReflectionTypeLoadException` |
| 节点销毁后崩 | `_ExitTree` 取消事件订阅 |
| Steam 无日志 | Megadot 项目模式运行 |
| Harmony 报错 | 编辑 `GodotPlugins.runtimeconfig.json` 强制 .NET 9.0 |
| 模组场景找不到脚本 | `ScanAssembly` 修复 Godot 脚本映射 |

---

## 16. API 速查

| 类型 | 命名空间/路径 | 用途 |
|------|---------|------|
| `ModInitializer` | `Core.Modding` | 入口 |
| `ModHelper.AddModelToPool` | `Core.Modding` | 入池 |
| `CardModel` | `Core.Models` | 卡牌 |
| `RelicModel` | `Core.Models` | 遗物 |
| `PotionModel` | `Core.Models` | 药水 |
| `PowerModel` | `Core.Models` | 能力/Buff |
| `EnchantmentModel` | `Core.Models` | 附魔 |
| `EventModel` | `Core.Models` | 事件 |
| `CharacterModel` | `Core.Models` | 角色 |
| `MonsterModel` | `Core.Models` | 怪物 |
| `EncounterModel` | `Core.Models` | 遭遇 |
| `DynamicVars` | `Core.Localization.DynamicVars` | 动态变量 |
| `PlayerCmd` | `Core.Commands` | 玩家操作 |
| `CardCmd` | `Core.Commands` | 卡牌操作 |
| `DamageCmd` | `Core.Commands` | 伤害链 |
| `CardPileCmd.Draw` | `Core.Commands` | 抽牌 |
| `CardSelectCmd` | `Core.Commands` | 选牌 |
| `RunManager` | `Core.Runs` | 运行状态 |
| `ModelDb` | `Core.Models` | 模型数据库 |
| `LocString/LocManager` | `Core.Localization` | 本地化 |
| `NCombatRoom` | `Core.Nodes.Rooms` | 战斗场景 |
| `Megadot` | megadot.megacrit.com | 编辑器 |
