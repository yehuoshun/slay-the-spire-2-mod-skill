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
    ├─ 3. 参考已有代码（优先找 ShunMod 已有模式）
    │      grep -rn "关键词" ~/.openclaw/workspace/code/STS2-ShunMod/STS2-ShunModCode/
    │
    ├─ 4. 写代码（遵循下方对应类型的模式）
    │
    └─ 5. 提交推送（git add -A && git commit -m "中文提交信息" && git push）
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

## 🔧 开发工具链

| 需求 | 工具/命令 |
|------|----------|
| 查游戏源码 | `grep -rn "关键词" ~/.openclaw/workspace/code/sts2-res/src/` |
| 查 ShunMod 已有实现 | `grep -rn "关键词" ~/.openclaw/workspace/code/STS2-ShunMod/STS2-ShunModCode/` |
| 查 STS2Plus 参考 | `curl -sL https://raw.githubusercontent.com/StephenSHorton/STS2Plus/master/路径` |
| 构建 | 用户的 Rider/VS：dotnet build |
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

## 📋 附录 C：本地化路径速查

```
<modid>/localization/<lang>/cards.json           → 卡牌
<modid>/localization/<lang>/relics.json          → 遗物
<modid>/localization/<lang>/potions.json         → 药水
<modid>/localization/<lang>/powers.json          → 能力
<modid>/localization/<lang>/enchantments.json    → 附魔
<modid>/localization/<lang>/events.json          → 事件
<modid>/localization/<lang>/ancients.json        → 先古之民
<modid>/localization/<lang>/monsters.json        → 怪物
<modid>/localization/<lang>/characters.json      → 角色
```

格式：`{ "MY_CARD.title": "标题", "MY_CARD.description": "{Dmg:amount} 点伤害。" }`

## 📋 附录 D：常见坑速查

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