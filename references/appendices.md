# STS2 Mod 附录

> API 速查、资源路径、本地化、生命周期钩子、常见坑。

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
