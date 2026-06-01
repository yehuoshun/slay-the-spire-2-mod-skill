---
name: slay-the-spire-2-mod-skill
description: 杀戮尖塔2（Slay the Spire 2 / STS2）模组开发实用指南。当用户提到杀戮尖塔2、杀戮尖塔、STS2、STS Mod、写模组、模组开发、卡牌、遗物、药水、能力、事件、角色、怪物、补丁、Harmony、Godot模组、Mod开发时使用。
---

# 杀戮尖塔 2 Mod 开发 — AI 工作流

> 你不是在查字典，你是在干活。按流程走，别跳步。

---

## 🚦 总工作流（收到请求后强制执行）

```
用户说"帮我做 X"
    │
    ├─ 1. 确定类型：卡牌/遗物/药水/能力/附魔/事件/怪物/角色/Patch？
    │
    ├─ 2. 查真实 API（强制）
    │      ls ~/.openclaw/workspace/code/sts2-res/src/ 2>/dev/null ||
    │        git clone --depth 1 https://github.com/yehuoshun/sts2-res ~/.openclaw/workspace/code/sts2-res
    │      grep -rn "目标类名\|目标方法" ~/.openclaw/workspace/code/sts2-res/src/
    │
    ├─ 3. 参考已有代码（多渠道参考）
    │      # ShunMod 自有实现
    │      grep -rn "关键词" ~/.openclaw/workspace/code/STS2-ShunMod/STS2-ShunModCode/
    │      # STS2-Mods 参考合集（Natsuki，12 个独立模组）
    │      curl -sL "https://raw.githubusercontent.com/s1f102500012/sts2mod/main/<模组目录>/src/<文件>.cs"
    │      # STS2Plus 参考（Stephen）
    │      curl -sL "https://raw.githubusercontent.com/StephenSHorton/STS2Plus/master/<路径>"
    │      # YuWanCard 参考（生产级角色 Mod，Builder/SavedProperty）
    │      curl -sL "https://raw.githubusercontent.com/YuWan886/Sts2-YuWanCard/master/<路径>"
    │
    ├─ 4. 写代码（遵循下方对应类型的模式）
    │
    └─ 5. 提交推送（git add -A && git commit -m "中文提交信息" && git push）
```

---

---

## 🏗️ 模式零：新建模组脚手架（第一步）

写任何代码之前先搭好目录和配置文件。

### 目录结构

```
MyMod/
├── assets/
│   ├── MyMod.json          ← 模组清单
│   ├── images/
│   │   ├── cards/
│   │   ├── relics/
│   │   ├── powers/
│   │   └── events/
│   └── localization/
│       ├── zhs/            ← 简体中文
│       │   ├── cards.json
│       │   ├── relics.json
│       │   ├── powers.json
│       │   └── events.json
│       └── eng/            ← 英文
│           └── ...
├── src/
│   ├── MyMod.csproj
│   ├── ModEntry.cs         ← 入口：[ModInitializer]
│   ├── ModInfo.cs          ← 常量（ModId/版本/资源路径）
│   ├── Cards/
│   ├── Relics/
│   ├── Powers/
│   ├── Events/
│   └── Patches/
└── tools/                  ← 构建脚本
    ├── build_and_deploy.sh
    ├── pack_mod.gd
    └── project.godot
```

### ModInfo.cs 模板

```csharp
namespace MyMod;

internal static class ModInfo
{
    public const string Id = "MyMod";
    public const string DisplayName = "我的模组";
    public const string TargetGameVersion = "0.103.2";
    public const string LogPrefix = $"[{Id}]";

    // 资源路径（供 AssetHooks 和卡牌/遗物用）
    public const string CardPortraitPath = $"res://{Id}/images/cards/{{0}}.png";
    public const string RelicIconPath = $"res://{Id}/images/relics/{{0}}.png";
    public const string PowerIconPath = $"res://{Id}/images/powers/{{0}}.png";
}
```

### ModEntry.cs 模板

```csharp
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MyMod;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    private const string HarmonyId = "Author.MyMod";
    private static Harmony? _harmony;

    public static void Initialize()
    {
        // 1. 注入自定义模型的序列化缓存（有自定义属性时必做）
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MyRelic));

        // 2. 注册模型到游戏池
        ModHelper.AddModelToPool<SharedRelicPool, MyRelic>();

        // 3. 安装 Harmony 补丁
        _harmony = new Harmony(HarmonyId);
        _harmony.PatchAll();

        // 4. 安装资源钩子（有自定义贴图时）
        // AssetHooks.Install(_harmony);

        Log.Info($"[{ModInfo.Id}] Loaded for STS2 {ModInfo.TargetGameVersion}.");
    }
}
```

### 快速脚手架命令

```bash
# 一键创建完整目录
mkdir -p MyMod/{assets/{images/{cards,relics,powers,events},localization/{zhs,eng}},src/{Cards,Relics,Powers,Events,Patches},tools}

# 创建 assets/MyMod.json（参考附录 D）
# 创建 src/MyMod.csproj（参考附录 E）
# 写 ModInfo.cs + ModEntry.cs（参考上面模板）
# 写卡牌类 → Cards/
# 写本地化 → assets/localization/zhs/cards.json（参考附录 C）
```

---

## 📁 本项目结构（ShunMod 实战模板）

```
STS2-ShunMod/
├── STS2-ShunMod.csproj          ← 项目文件
├── STS2-ShunModCode/
│   ├── MainFile.cs              ← 模组入口：[ModInitializer]
│   ├── Core/
│   │   ├── ShunLogger.cs        ← 统一日志
│   │   └── Registration/
│   │       └── ContentRegistry.cs ← 自动注册 [Pool] 标注的类
│   ├── Cards/
│   │   └── *.cs                 ← 卡牌（每文件一个类）
│   ├── Events/
│   │   └── *.cs                 ← 事件
│   ├── Patches/
│   │   └── *.cs                 ← Harmony 补丁（每文件一个或多个 Patch 类）
│   └── images/                  ← 图片资源（按类型分子目录）
└── localization/                 ← 本地化 JSON
```

**写新东西放哪：** Cards/ 放卡牌、Events/ 放事件、Patches/ 放 Harmony 补丁、Core/ 放工具类。

---

## 📦 模组入口（MainFile 模板）

```csharp
using System.Reflection;
using HarmonyLib;
using STS2_ShunMod.Core;

namespace STS2_ShunMod;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "STS2-ShunMod";
    private static readonly Harmony _harmony = new(ModId);

    public static void Initialize()
    {
        ShunLogger.Summary(ModId);
        try { _harmony.PatchAll(); ShunLogger.Info(ModId, "补丁加载完成"); }
        catch (Exception e) { ShunLogger.Error(ModId, $"补丁失败: {e.Message}"); }
        try { ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly()); }
        catch (Exception e) { ShunLogger.Error(ModId, $"注册失败: {e.Message}"); }
    }
}
```

**关键：** `Harmony.PatchAll()` 自动发现所有 `[HarmonyPatch]` 类。`ContentRegistry.RegisterAll` 自动注册所有 `[Pool]` 标注的卡牌/遗物/药水。

---

## 🃏 模式一：自定义卡牌（最常见）

### 最小可用模板

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;

namespace STS2_ShunMod.Cards;

public class MyCard : CardModel
{
    // ══════ 构造函数 ══════
    // CardModel(int cost, CardType type, CardRarity rarity, TargetType target)
    public MyCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.Opponent) {}

    // ══════ 必须实现 ══════
    public override string PortraitPath =>
        $"res://{MainFile.ModId}/images/cards/my_card.png";

    // ══════ 打出效果 ══════
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // ctx.Targets 是选中的目标（target 为 Opponent 时是敌人）
        await DamageCmd.Attack(8)           // 8 点伤害
            .FromCard(this)                  // 来源
            .Targeting(ctx.Targets.First())  // 目标
            .Execute();
    }

    // ══════ 升级回调 ══════
    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        // 修改伤害值：在 DynamicVars 上操作
        DynamicVars["Damage"].UpgradeValueBy(3); // 伤害 8→11
    }
}
```

### 卡牌构造参数速查

| 参数 | 值 | 说明 |
|------|-----|------|
| `cost` | int | 能耗 X |
| `type` | Attack / Skill / Power / Status / Curse | 卡牌类型 |
| `rarity` | Common / Uncommon / Rare / Event / Token | 稀有度 |
| `target` | Opponent / Ally / Self / AllOpponents / Everyone | 目标类型 |
| `showInLibrary` | true/false | 图鉴可见 |

### 常用卡牌功能速查

```csharp
// ═══ 伤害 ═══
await DamageCmd.Attack(6).FromCard(this).Targeting(target).Execute();                    // 单体
await DamageCmd.Attack(6).FromCard(this).TargetingAllOpponents(combatState).Execute();   // AOE
await DamageCmd.Attack(6).FromCard(this).TargetingRandomOpponents(combatState).Execute(); // 随机
await DamageCmd.Attack(6).FromCard(this).WithHitCount(3).Targeting(target).Execute();    // 三连击

// ═══ 格挡 ═══
await PlayerCmd.GainBlock(Owner, 5);

// ═══ 抽牌 ═══
await CardPileCmd.Draw(Owner, 2);

// ═══ 能力 ═══
await PlayerCmd.ApplyPower(Owner, PowerDb.Get<StrengthPower>(), 2);  // 2 层力量
await PlayerCmd.ApplyPower(target, PowerDb.Get<VulnerablePower>(), 1);

// ═══ 选牌（从手牌选一张消耗）═══
var picked = await CardSelectCmd.FromHandGeneric(Owner,
    new CardSelectorPrefs(ChooseCardPrompt.Exhaust, 1), filter: null);

// ═══ 关键词/标签 ═══
public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
public override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
```

### 升级差异显示

```csharp
// 伤害值升级 → 卡牌描述自动显示 8→11
DynamicVars["Damage"].SetBase(8);          // OnUpgrade: UpgradeValueBy(3)
DynamicVars["Block"].SetBase(5);           // OnUpgrade: UpgradeValueBy(3)
```

### 卡牌注册（`[Pool]` 自动注册）

```csharp
[Pool(typeof(ColorlessCardPool))]  // ← 标注就自动注册到对应卡池
public class MyCard : CardModel { ... }
```

卡池类型：`IroncladCardPool` / `SilentCardPool` / `ColorlessCardPool` / `SharedCardPool` 等。

---

## 💎 模式二：自定义遗物

### 最小可用模板

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Commands;

namespace STS2_ShunMod.Relics;

public class MyRelic : RelicModel
{
    // ══════ 稀有度（决定获取途径）══════
    protected override RelicRarity Rarity => RelicRarity.Common;
    // Starter=初始 Event=事件 Ancient=先古 Common/Uncommon/Rare=奖励/商店 Shop=仅商店

    // ══════ 回合开始效果 ══════
    public override async Task AfterSideTurnStart(Side side, CombatState combatState)
    {
        if (side == Owner!.Creature.Side)  // 玩家回合
        {
            await Flash();                 // 遗物闪烁动画
            await PlayerCmd.GainEnergy(Owner, 1);
        }
    }
}
```

### 常用遗物钩子

```csharp
// 战斗开始时
public override async Task OnCombatStart(CombatState combatState) { ... }
// 回合开始前
public override async Task BeforeSideTurnStart(Side side, CombatState state) { ... }
// 获得遗物
public override async Task OnObtained() { ... }
// 失去遗物
public override async Task OnRemoved() { ... }
// 受到伤害
public override async Task OnDamageReceived(DamageInfo info) { ... }
// 卡牌打出后
public override async Task AfterCardPlayed(CardModel card) { ... }
```

### 遗物图标路径

```
images/relics/<小写ID>.png              ← 256×256 大图
images/relics/<小写ID>_outline.png       ← 剪影
images/atlases/relic_atlas.sprites/<小写ID>.tres      ← 裁切纹理
images/atlases/relic_outline_atlas.sprites/<小写ID>.tres ← 剪影裁切
```

---

## 🧪 模式三：自定义药水

```csharp
public class MyPotion : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    // CombatOnly=仅战斗 AnyTime=任意场景 ProgrammaticOnly=代码触发

    public override TargetType TargetType => TargetType.Opponent;

    public override async Task OnUse(PlayerChoiceContext ctx, PotionTarget target)
    {
        await DamageCmd.Attack(30).FromCard(this).Targeting(target.TargetCreature!).Execute();
    }
}
```

---

## ⚡ 模式四：自定义能力（Buff/Debuff）

```csharp
public class MyPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;          // Buff/Debuff/Neutral
    public override PowerStackType StackType => PowerStackType.Intensity;  // Intensity=层数 Duration=存在/不存在
    public override bool AllowNegative => false;

    // 每回合开始
    public override async Task AfterSideTurnStart(Side side, CombatState state)
    {
        if (side == Owner!.Side) await PlayerCmd.GainBlock(Owner, Amount);  // Amount=层数
    }

    // 受伤时修正
    public override decimal ModifyDamage(DamageInfo info, decimal damage)
        => damage + Amount;  // 每层 +1 伤害
}
```

---

## ✨ 模式五：自定义附魔

```csharp
public class MyEnchant : EnchantmentModel
{
    public override decimal EnchantDamageAdditive => Amount * 3;   // 每层 +3 伤害
    public override decimal EnchantBlockAdditive => Amount * 2;     // 每层 +2 格挡
    public override void ModifyCard() { }
}

// 附魔卡牌
await CardCmd.Enchant(mutableEnch, card, 1);
```

---

## 📜 模式六：自定义事件

```csharp
internal sealed class MyEvent : EventModel
{
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var player = Owner;
        return new List<EventOption>
        {
            // 选项一：给卡牌
            Opt(async () =>
            {
                await GiveCard(typeof(MyCard));
                SetEventFinished(L10NLookup("pages.DONE.description"));
            }, "OPT_1"),

            // 选项二：给金币
            Opt(async () =>
            {
                await PlayerCmd.GainGold(player!, 100);
                SetEventFinished(L10NLookup("pages.GOLD.description"));
            }, "OPT_2"),

            // 离开
            new EventOption(this, async () => SetEventFinished(L10NLookup("pages.CLOSE.description")),
                $"{Id.Entry}.pages.INITIAL.options.OPT_3")
        };
    }

    private EventOption Opt(Func<Task> cb, string key, IEnumerable<IHoverTip>? tips = null) =>
        new(this, cb, $"{Id.Entry}.pages.INITIAL.options.{key}", tips ?? []);
}
```

**多页选项**：用 `SetEventState(description, newOptions)` 切换页面；用 `SetEventFinished(text)` 结束事件。

**添加到地图：**

```csharp
[HarmonyPatch(typeof(Overgrowth), "AllEvents", MethodType.Getter)]
static void Postfix(ref IReadOnlyList<EventModel> __result)
{
    var list = __result.ToList();
    list.Add(ModelDb.GetByIdOrNull<EventModel>(ModelDb.GetId(typeof(MyEvent))));
    __result = list;
}
```

---

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

## 🔧 开发工具链

| 需求 | 工具/命令 |
|------|----------|
| 查游戏源码 | `grep -rn "关键词" ~/.openclaw/workspace/code/sts2-res/src/` |
| 查 ShunMod 已有实现 | `grep -rn "关键词" ~/.openclaw/workspace/code/STS2-ShunMod/STS2-ShunModCode/` |
| 查 Natsuki 参考合集 | `curl -sL https://raw.githubusercontent.com/s1f102500012/sts2mod/main/<目录>/src/<文件>.cs` |
| 查 YuWanCard 参考 | `curl -sL https://raw.githubusercontent.com/YuWan886/Sts2-YuWanCard/master/<路径>` |
| 查 STS2Plus 参考 | `curl -sL https://raw.githubusercontent.com/StephenSHorton/STS2Plus/master/<路径>` |
| 构建 | `dotnet build` + Godot 打包 PCK |
| 部署 | `tools/build_and_deploy.sh`（Mac）/ 手动 cp DLL+PCK+JSON 到 mods/ |
| 调试图标 | icons 路径规范 → 附录 B |
| 控制台测试 | Megadot 运行，按 `` ` `` 打开控制台 |

---

## 📐 STS2-Plus 参考架构（值得学的模式）

从 [STS2Plus](https://github.com/StephenSHorton/STS2Plus) 学到的最佳实践：

| 模式 | 说明 | 示例 |
|------|------|------|
| **TargetMethods 遍历子类** | 不只用 `typeof(Base)`，遍历 Assembly 所有子类 override | `UnlimitedGrowthMaxUpgradePatch` |
| **安全检测** | 写局外补丁前先检查卡牌是否安全 | `UnlimitedGrowthSafety.CanUseUnlimitedGrowth` |
| **ThreadStatic 上下文** | 反序列化时传参数给 getter | `SerializationContext.Push/Pop/Peek` |
| **Finalizer 清理** | Harmony Finalizer 保证异常时也清理 | `UnlimitedGrowthDeserializePatch.Finalizer` |
| **Patch 分文件** | 一个功能拆多个 Patch 类，各管各的 | `UnlimitedGrowthMaxUpgradePatch` + `DeserializePatch` |
| **Catagory 分组** | `[HarmonyPatchCategory("CatName")]` 便于开关 | STS2Plus 的 `MoreRules` 类目 |

---

## 📐 STS2-Mods 参考架构（Natsuki 合集）

从 [s1f102500012/sts2mod](https://github.com/s1f102500012/sts2mod) 学到的模组管理+代码模式：

### 仓库包含的 12 个独立模组

| 模组 | Mod ID | 版本 | 类型 |
|------|--------|------|------|
| **俄洛伊** | Illaoi | 0.1.2 | 新角色（生长/触手/灵魂） |
| **AI 队友** | sts2AITeammate | 0.1.1 | AI 多人模式 |
| **海克斯符文** | HextechRunes | 0.6.3 | 符文选择+战斗变化 |
| **无尽模式** | EndlessMode | 0.2.6 | 无尽挑战流程扩展 |
| **自定义难度** | CustomDifficulty | 0.1.0 | 怪物血/攻滑条 |
| **第四幕** | StS1Act4 | 0.1.0 | 一代第四幕还原 |
| **奖励附魔** | RewardEnchants | 0.2.1 | 奖励卡牌随机附魔 |
| **更多进阶挑战** | MoreAscensionChallenge | 0.1.1 | 11-15 进阶 |
| **手牌上限解除** | RemoveHandLimit | 0.2.1 | 手牌上限→20 |
| **初始牌组调整** | StartingDeckTweaks | 1.0.0 | 修改起始牌组 |
| **基石符文** | KeystoneRunes | 0.3.3 | 基石符文机制 |
| **心之钢** | Heartsteel | 0.1.0 | 心之钢主题遗物 |

### 值得学的模式

| 模式 | 说明 | 示例来源 |
|------|------|----------|
| **RequireField/RequireMethod 反射** | 静态缓存反射访问，启动时校验存在性 | `EndlessMode`, `RemoveHandLimit` |
| **Transpiler 常量替换** | IL 级替换 int 常量（如手牌上限 10→20） | `RemoveHandLimit` 的 `ReplaceIntLoads` |
| **GetAsyncStateMachineTarget** | 对 async 方法打补丁必须拿到状态机 | `RemoveHandLimit` 的 Draw/Add 补丁 |
| **SavedPropertiesTypeCache** | 自定义模型必须注入缓存才能序列化 | `Heartsteel`, `EndlessMode` |
| **ModHelper.AddModelToPool<T>** | 比 `[Pool]` 更简洁的注册方式 | `Heartsteel` 初始化 |
| **AssetHooks 分离** | 资源/贴图 Hook 独立文件管理 | `Heartsteel` 的 `AssetHooks.cs` |
| **CoreHook（游戏钩子）** | 用游戏提供的 `Hook` 类代替 Harmony | `RewardEnchants` 的 `TryModifyCardRewardOptions` |
| **多模组项目结构** | 每个模组独立 csproj + assets/src/tools | 全部 12 个模组 |
| **build_and_deploy.sh** | 一键编译→打包 PCK→部署到 mods/ | 每个模组的 `tools/` |
| **pack_mod.gd** | Godot 脚本打包非代码资源为 .pck | 每个模组的 `tools/` |
| **StartingDeck 泛型 Patch** | 泛型复用角色牌组修改逻辑 | `StartingDeckTweaks` |
| **SavedProperty** | 自定义序列化属性的正确写法 | `Heartsteel` 的 `SavedMaxHpGained` |

---

## 📐 YuWanCard 参考架构（生产级角色 Mod）

从 [YuWan886/Sts2-YuWanCard](https://github.com/YuWan886/Sts2-YuWanCard) 学到的最佳实践：

| 模式 | 说明 | 示例 |
|------|------|------|
| **Builder 链式构造** | 卡牌用 Fluent Builder 替代构造函数参数堆叠 | `CardBuilder.Create().WithCost(2).WithDamage(8)...` |
| **分阶段初始化** | 角色分 PreInit/Init/PostInit 三阶段，避免初始化顺序问题 | `CharacterInitPhase` |
| **SavedProperty 序列化** | 自定义属性用 `[SavedProperty]` 标注，自动持久化 | 角色解锁状态、自定义计数器 |
| **多语言分离** | 本地化按 `zh/` `en/` 分目录，每种内容类型独立 JSON | `localization/zh/cards.json` |
| **资源路径常量** | 所有资源路径集中在常量类，不散落在各文件中 | `ResourcePaths.CardPortrait` |

---

## 📐 BaseLib 参考架构（社区框架）

从 [Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) 学到的快捷封装：

| 模式 | 说明 | 示例 |
|------|------|------|
| **CustomModel 系列** | `CustomCard`/`CustomRelic`/`CustomPower` 基类，封装了常见样板代码 | 继承 `CustomCard` 只需写 `OnPlay` |
| **ModConfig 设置面板** | 游戏内设置 UI，玩家可调参数 | `ModConfig.AddSlider("难度", 1, 10)` |
| **自动注册** | 扫描 Assembly 自动注册所有模型，不用手动写注册代码 | `BaseMod.RegisterAll()` |
| **日志工具** | 统一的 Log 封装，带 Mod 前缀 | `Log.Info("消息")` |

> ⚡ **建议**：BaseLib 适合快速原型，但生产级 Mod 建议理解底层 API 后再决定是否依赖框架。

---

## 📋 附录 A：API 速查（最常用）

```
CardModel            → 卡牌基类。构造 (cost, type, rarity, target)
RelicModel           → 遗物基类。Rarity 决定获取途径
PotionModel          → 药水基类。Usage 决定使用场景
PowerModel           → 能力基类。Type/StackType 决定行为
EnchantmentModel     → 附魔基类。ModifyCard() 改卡牌属性
EventModel           → 事件基类。GenerateInitialOptions() 返回选项
AncientEventModel    → 先古之民。DefineDialogues() 返回对话
CharacterModel       → 角色基类。CardPool/RelicPool/StartingDeck
MonsterModel         → 怪物基类。GenerateMoveStateMachine()
EncounterModel       → 遭遇基类。GenerateMonsters()
CardPoolModel        → 卡池基类。GenerateAllCards()
DynamicVars          → 动态变量集合。["Name"].UpgradeValueBy(n)
PlayerCmd            → 玩家操作：GainEnergy/GainBlock/GainGold/Damage/Heal/ApplyPower
CardCmd              → 卡牌：Enchant/Discard/Upgrade
DamageCmd            → 伤害链：Attack().FromCard().Targeting().Execute()
CardPileCmd          → 牌堆：Draw
CardSelectCmd        → 选牌：FromHandGeneric/FromDeckGeneric
ModelDb              → 反射注册：GetId/GetByIdOrNull/AllCards/AllRelics
LocString/LocManager → 本地化
SavedPropertiesTypeCache → 序列化缓存注入
ModHelper             → 注册辅助：AddModelToPool<T>
CoreHook              → 游戏钩子：TryModifyCardRewardOptions
```

## 📋 附录 B：资源路径速查

| 内容类型 | 大图 | 裁切纹理 (.tres) |
|----------|------|------------------|
| 卡牌 | `images/packed/card_portraits/<池>/<id>.png` | `images/atlases/card_atlas.sprites/<池>/<id>.tres` |
| 遗物 | `images/relics/<id>.png` | `images/atlases/relic_atlas.sprites/<id>.tres` |
| 药水 | `images/potions/<id>.png` | `images/atlases/potion_atlas.sprites/<id>.tres` |
| 能力 | `images/powers/<id>.png` | `images/atlases/power_atlas.sprites/<id>.tres` |
| 附魔 | `images/enchantments/<id>.png` | — |
| 事件 | `images/events/<id>.png` | — |

## 📋 附录 C：本地化与 DynamicVars（卡牌描述核心）

### 文件结构

```
<ModId>/assets/localization/zhs/
├── cards.json       ← 卡牌标题+描述
├── relics.json      ← 遗物标题+描述+flavor
├── powers.json      ← 能力标题+描述+smartDescription
├── potions.json     ← 药水
├── events.json      ← 事件文本（多页、多选项）
├── ancients.json    ← 先古之民对话
├── monsters.json    ← 怪物名+意图
├── characters.json  ← 角色名
└── modifiers.json   ← 自定义 RunModifier
```

### cards.json 格式

```json
{
  "MY_CARD_ID.title": "奥术冲击",
  "MY_CARD_ID.description": "造成{Damage:diff()}点伤害{Times:diff()}次。\n抽{Cards:amount}张牌。",
  "MY_CARD_ID.upgradeDescription": "造成{Damage:diff()}点伤害{Times:diff()}次。\n抽{Cards:amount}张牌。"
}
```

### DynamicVars 占位符语法

卡牌描述中的 `{变量名:格式}` 会被 DynamicVars 自动替换：

| 占位符 | 含义 | 示例效果 |
|--------|------|----------|
| `{Damage:diff()}` | 带升级差异的伤害值 | 显示为 `8(11)` |
| `{Block:diff()}` | 带升级差异的格挡值 | 显示为 `5(8)` |
| `{Damage:amount}` | 纯数值（无差异） | 显示为 `8` |
| `{Cards:diff()}` | 抽牌数 | `1(2)` |
| `{Times:diff()}` | 次数 | `2(3)` |
| `{Energy:energyIcons()}` | 能量图标 | 显示能量图标 |
| `{Turns:diff()}` | 持续回合 | `3(5)` |
| `{Amount}` | 能力层数（Power 用） | `5` |

### powers.json 格式

```json
{
  "MY_POWER_ID.title": "燃烧",
  "MY_POWER_ID.description": "在你的回合开始时，受到{Amount}点伤害。",
  "MY_POWER_ID.smartDescription": "在你的回合开始时，受到[red]{Amount}[/red]点伤害。"
}
```

> `description` 是图鉴/提示用的静态文本；`smartDescription` 是实时显示的动态文本（带 `{Amount}` 占位）。

### relics.json 格式

```json
{
  "MY_RELIC_ID.title": "奥术之书",
  "MY_RELIC_ID.description": "每场战斗第一次打出技能牌时，抽[blue]2[/blue]张牌。",
  "MY_RELIC_ID.flavor": "古老的符文依然散发着微光…"
}
```

### C# 代码中定义 DynamicVars

卡牌类的 DynamicVars 必须在构造函数或 OnUpgrade 中设置：

```csharp
public class MyCard : CardModel
{
    public MyCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.Opponent)
    {
        // 基础值 → 描述中 {Damage:diff()} 显示为 "8"
        DynamicVars["Damage"].SetBase(8);
        // 多段 → 描述中 {Times:diff()} 显示为 "2"
        DynamicVars["Times"].SetBase(2);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        // 升级 → 显示 "8(11)"
        DynamicVars["Damage"].UpgradeValueBy(3);
        DynamicVars["Times"].UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int damage = DynamicVars["Damage"].GetValue(this);  // 基础8 升级后11
        int times = DynamicVars["Times"].GetValue(this);

        for (int i = 0; i < times; i++)
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(ctx.Targets.First())
                .Execute();
    }
}
```

### 颜色标签

描述文本中可用 BBCode 风格的颜色标签：

| 标签 | 颜色 |
|------|------|
| `[red]` | 红色 |
| `[green]` | 绿色 |
| `[blue]` | 蓝色 |
| `[gold]` | 金色（关键词） |
| `[purple]` | 紫色 |
| `[gray]`或`[darkgray]` | 灰色 |
| `\n` | 换行 |

## 📋 附录 D：模组清单（manifest.json）

每个模组根目录必须有的 `assets/<ModId>.json`：

```json
{
  "id": "MyMod",
  "name": "我的模组",
  "author": "作者名",
  "description": "一句话描述模组功能",
  "version": "0.1.0",
  "min_game_version": "0.103.2",
  "has_pck": true,
  "has_dll": true,
  "affects_gameplay": true
}
```

| 字段 | 必填 | 说明 |
|------|------|------|
| `id` | ✅ | 模组唯一 ID，与 csproj AssemblyName 一致 |
| `name` | ✅ | 显示名称 |
| `author` | ✅ | 作者名 |
| `description` | ✅ | 一句话描述 |
| `version` | ✅ | 语义化版本 |
| `min_game_version` | — | 最低游戏版本要求 |
| `has_pck` | ✅ | 有 PCK 资源包则为 true |
| `has_dll` | ✅ | 有 DLL 代码则为 true |
| `affects_gameplay` | ✅ | 影响玩法则为 true（联机同步用） |

## 📋 附录 E：.csproj 项目配置

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <AssemblyName>MyMod</AssemblyName>
    <RootNamespace>MyMod</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- 引用游戏自带的 DLL（路径按实际安装位置改） -->
    <Reference Include="sts2">
      <HintPath>游戏目录/data_sts2_xxx/sts2.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="GodotSharp">
      <HintPath>游戏目录/data_sts2_xxx/GodotSharp.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="0Harmony">
      <HintPath>游戏目录/data_sts2_xxx/0Harmony.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <ItemGroup>
    <!-- 内嵌资源（图片打进 DLL 作为 fallback） -->
    <EmbeddedResource Include="../assets/images/cards/*.png"
      LogicalName="MyMod.images.cards.%(Filename)%(Extension)" />
    <EmbeddedResource Include="../assets/images/relics/*.png"
      LogicalName="MyMod.images.relics.%(Filename)%(Extension)" />
    <EmbeddedResource Include="../assets/images/powers/*.png"
      LogicalName="MyMod.images.powers.%(Filename)%(Extension)" />
  </ItemGroup>
</Project>
```

**关键：**
- `TargetFramework` 必须是 `net9.0`
- 三个 Reference DLL 路径指向游戏安装目录，`Private=false` 表示不复制
- EmbeddedResource 把 PNG 打包进 DLL 作为备用（优先用 PCK 里的 res:// 路径）

## 📋 附录 F：生命周期钩子速查

### 卡牌钩子

```csharp
// 打出
protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
// 消耗
public override async Task OnExhausted(CardPile pile)
// 弃掉
public override async Task OnDiscarded(CardPile pile)
// 获得时
public override async Task OnObtained()
// 移除牌组时
public override async Task OnRemovedFromDeck()
```

### 遗物钩子

```csharp
public override async Task OnCombatStart(CombatState state)
public override async Task BeforeCombatStart()
public override async Task OnObtained()
public override async Task OnRemoved()
public override async Task BeforeSideTurnStart(Side side, CombatState state)
public override async Task AfterSideTurnStart(Side side, CombatState state)
public override async Task AfterTurnEnd(PlayerChoiceContext ctx, CombatSide side)
public override async Task OnDamageReceived(DamageInfo info)
public override async Task OnDamageTaken(DamageInfo info)
public override async Task AfterDamageGiven(PlayerChoiceContext ctx, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
public override async Task AfterCardPlayed(CardModel card)
public override async Task AfterPlayerGainedBlock(PlayerChoiceContext ctx, Creature creature, decimal amount)
public override async Task AfterPlayerGainedEnergy(PlayerChoiceContext ctx, decimal amount)
public override async Task AfterPlayerHealed(PlayerChoiceContext ctx, Creature creature, decimal amount)
public override async Task AfterCreatureKilled(Creature creature, Creature? killer)
public override async Task AfterCreatureAddedToCombat(Creature creature)
public override async Task BeforeCardDrawn(CardModel card)
public override async Task AfterRoomEntered(RoomType room)
```

### 能力钩子

```csharp
public override async Task AfterSideTurnStart(Side side, CombatState state)
public override async Task AfterTurnEnd(PlayerChoiceContext ctx, CombatSide side)
public override async Task OnCreated()
public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
public override decimal ModifyDamage(DamageInfo info, decimal damage) => damage;
public override decimal ModifyBlockGained(decimal block) => block;
public override decimal ModifyDamageReceived(DamageInfo info, decimal damage) => damage;
```

> **使用方式：** 拿到需求后，从上面钩子表匹配 → 去已有实现验证用法 → 写代码。不需要 grep 猜方法名。

## 📋 附录 G：常见坑速查

| 坑 | 对策 |
|----|------|
| 图标不显示 | `.tres` AtlasTexture 没创建、或 PNG 路径不对 |
| 本地化不生效 | 非代码资源必须 **Publish**（非 Build）生成 .pck |
| Harmony 报版本错 | 编辑 `GodotPlugins.runtimeconfig.json` 强制 .NET 9.0 |
| 场景找不到脚本 | `LookupScriptsInAssembly` 或 `ScanAssembly` 修复 |
| 卡牌没进池 | 确认 [Pool] Attribute 标注正确、ContentRegistry 已调用 |
| 联机标签不同步 | `affects_gameplay: true` 且在 manifest 声明 |
| 搜索源码无结果 | 先 `git pull` 更新 sts2-res |
| Patch 不生效 | 检查 TargetMethods 返回类型是否匹配、方法名大小写 |
| async 方法 Patch 失败 | 必须用 `GetAsyncStateMachineTarget` 拿状态机 MoveNext |
| 自定义模型序列化丢失 | 加 `SavedPropertiesTypeCache.InjectTypeIntoCache` |
| 遗物图标在无尽模式失效 | 需 Hook `RelicModel.Icon` getter（参考 EndlessMode） |
| PCK 资源不更新 | 非代码资源必须 Publish→PCK，Build 只管 DLL |
| dotnet 找不到 | `build_and_deploy.sh` 里按顺序 fallback 找 dotnet 路径 |

---

## 🙏 鸣谢

- **[StephenSHorton/STS2Plus](https://github.com/StephenSHorton/STS2Plus)** — STS2 社区最成熟的参考实现，多角色兼容、保存安全、修正器架构、联机同步等模式来源
- **[s1f102500012/sts2mod](https://github.com/s1f102500012/sts2mod)** — Natsuki 的 12 个独立模组合集，涵盖角色/符文/无尽/难度/UI/构建等全方位参考
- **[YuWan886/Sts2-YuWanCard](https://github.com/YuWan886/Sts2-YuWanCard)** — 生产级角色 Mod，Builder 链式构造、分阶段初始化、SavedProperty 序列化
- **[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2)** — 社区框架，CustomModel 快捷封装、ModConfig 设置面板
- **[Alchyr/ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2)** — 官方模板，dotnet new 一键创建（空/内容/角色三种）
- **[yehuoshun/sts2-res](https://github.com/yehuoshun/sts2-res)** — STS2 反编译源码参考（用于 API 查询）
- **[Megadot](https://megadot.megacrit.com)** — STS2 专用 Godot 引擎
- **B 站教程系列** — 从环境搭建到自定义怪物，9 篇完整 step-by-step