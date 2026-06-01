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
# 推荐：用官方模板一键创建（需先安装 dotnet new 模板）
dotnet new install Alchyr.Sts2.Templates
dotnet new sts2mod -n MyMod          # 空模组（BaseLib 依赖）
dotnet new sts2content -n MyMod      # 内容模组（卡牌/遗物/能力）
dotnet new sts2character -n MyMod    # 角色模组（完整角色）

# 手动创建完整目录（不用模板时）
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
| 创建新模组 | `dotnet new sts2mod -n MyMod` / `sts2content` / `sts2character`（需先 `dotnet new install Alchyr.Sts2.Templates`） |
| 构建 | `dotnet build` + Godot 打包 PCK |
| 部署 | `tools/build_and_deploy.sh`（Mac）/ 手动 cp DLL+PCK+JSON 到 mods/ |
| 控制台测试 | Megadot 运行，按 `` ` `` 打开控制台 |

> 📚 深度参考见 [references/architecture.md](references/architecture.md) 和 [references/appendices.md](references/appendices.md)

---

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
```
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


> 📚 其余附录见 [references/appendices.md](references/appendices.md)（附录 B~G：资源路径、本地化、清单、csproj、生命周期钩子、常见坑）

