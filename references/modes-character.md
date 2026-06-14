# 模式八：自定义角色

## 最简角色类

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;

namespace MyMod.Characters;

public class WatcherCharacter : CharacterModel
{
    public override CharacterGender Gender => CharacterGender.Female;
    public override CharacterModel? UnlocksAfterRunAs => null; // 不需要解锁
    public override CardPoolModel CardPool => new WatcherCardPool();
    public override RelicPoolModel RelicPool => new WatcherRelicPool();

    public override IEnumerable<CardModel> StartingDeck => new CardModel[]
    {
        new StrikeCard(), new StrikeCard(), new StrikeCard(), new StrikeCard(),
        new DefendCard(), new DefendCard(), new DefendCard(), new DefendCard(),
        new EruptionCard(), new VigilanceCard(),
    };

    public override IEnumerable<RelicModel> StartingRelics => new RelicModel[]
    {
        new PureWater(),
    };

    // 攻击建筑师的动画
    public override IEnumerable<string> GetArchitectAttackVfx()
    {
        return new[] { "scenes/vfx_attack_slash.tscn" };
    }

    // 音效回退
    public override string AttackSfxPath => "event:/sfx/characters/watcher/watcher_attack";
    public override string CastSfxPath => "event:/sfx/characters/watcher/watcher_cast";
    public override string DeathSfxPath => "event:/sfx/characters/watcher/watcher_die";
}
```

## 自定义卡池

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

public class WatcherCardPool : CardPoolModel
{
    public override string Title => "Watcher";                    // 影响卡面图路径
    public override string EnergyColorName => "watcher";          // 能量图标颜色
    public override string CardFrameMaterialPath => "card_frame_purple";
    public override Color DeckEntryCardColor => Colors.Purple;
    public override Color EnergyOutlineColor => Colors.White;
    public override bool IsColorless => false;

    public override IEnumerable<CardModel> GenerateAllCards()
    {
        return new CardModel[] {
            new StrikeCard(), new DefendCard(), new EruptionCard(), // ...
        };
    }
}
```

### CardPoolModel 属性说明

| 属性 | 说明 |
|------|------|
| `Title` | 卡池名称，影响卡面图资源路径 `card_atlas.sprites/<Title>/` |
| `EnergyColorName` | 能量图标名，对应 `ui_atlas.sprites/card/energy_<Name>.tres` |
| `CardFrameMaterialPath` | 卡牌边框着色器材质，路径 `materials/cards/frames/<Path>_mat.tres` |
| `DeckEntryCardColor` | 缩略卡牌底色 |
| `EnergyOutlineColor` | 能量数字描边颜色 |
| `IsColorless` | 是否无色卡池 |

## 自定义遗物池

```csharp
public class WatcherRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "watcher";
    public override Color LabOutlineColor => Colors.Purple;
    public override IEnumerable<RelicModel> GenerateAllRelics() => new RelicModel[] { new PureWater() };
}
```

## 自定义药水池

```csharp
public class WatcherPotionPool : PotionPoolModel
{
    public override IEnumerable<PotionModel> GenerateAllPotions() => new PotionModel[] { };
}
```

## 角色必需资源

| 资源 | 路径 |
|------|------|
| 待机动画场景 | `scenes/creature_visuals/<角色ID小写>.tscn` |
| 头像图标场景 | `scenes/ui/character_icons/<角色ID小写>_icon.tscn` |
| 能量计数器场景 | `scenes/combat/energy_counters/<角色ID小写>_energy_counter.tscn` |
| 商店待机场景 | `scenes/merchant/characters/<角色ID小写>_merchant.tscn` |
| 火堆休息场景 | `scenes/rest_site/characters/<角色ID小写>_rest_site.tscn` |
| 卡牌拖尾特效 | `scenes/vfx/card_trail_<角色ID小写>.tscn` |
| 选择界面底图 | `images/packed/character_select/char_select_<角色ID小写>.png` |
| 锁定底图 | `images/packed/character_select/char_select_<角色ID小写>_locked.png` |
| 地图标记箭头 | `images/packed/map/icons/map_marker_<角色ID小写>.png` |
| 过场着色器 | `materials/transitions/<角色ID小写>_transition_mat.tres` |

## 场景搭建规则

### 角色动画场景

- 根节点必须挂载 `NCreatureVisuals` 脚本（或继承类）
- 包含 `%Visuals` 节点（2D 节点，设为唯一名称访问）
- 包含 `%Bounds` 节点（定义选择框/血条位置）

### 能量计数器场景

- 根节点挂载 `NEnergyCounter` 脚本
- 内部 Label 必须是 `MegaLabel`（可通过继承占位或导入 mega_text 插件）

### 卡牌拖尾场景

- 根节点挂载 `NCardTrailVfx` 脚本
- 包含两个 `Line2D` 节点

## 场景脚本映射（重要）

如果场景中使用了自定义 C# 脚本，必须在初始化时调用：

```csharp
ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
```

否则 Godot 加载场景时找不到脚本类。

## 注册角色

```csharp
[HarmonyPatch(typeof(ModelDb), "AllCharacters", MethodType.Getter)]
[HarmonyPostfix]
static void Postfix(ref IEnumerable<CharacterModel> __result)
{
    __result = __result.Append(new WatcherCharacter());
}
```

## 先古之民对话注入

每个先古之民（Neow/Darv/Orobas 等）对每个角色有独立对话。自定义角色需要 Patch 每个 NPC 的 `DefineDialogues()`：

```csharp
// 对每个 NPC 做 Postfix
private static void NeowDefineDialoguesPostfix(AncientDialogueSet __result)
{
    string entry = ModelDb.GetId<MyCharacter>().Entry;
    __result.CharacterDialogues[entry] = new []
    {
        new AncientDialogue(["sfx/npcs/neow/neow_welcome", "",
            "sfx/npcs/neow/neow_sleepy", ""])
        { VisitIndex = 0 },  // 第一次见
        new AncientDialogue(["sfx/npcs/neow/neow_welcome", "", "", ""])
        { VisitIndex = 1 },  // 第2-3次
        new AncientDialogue(["sfx/npcs/neow/neow_sleepy", "", "sfx/..."])
        { VisitIndex = 4 }   // 第5次及以上
    };
}
```

## 先古对话系统 Patch

除了对话内容，还需要 Patch `AncientDialogueSet.GetValidDialogues` 来处理角色对话索引：

```csharp
[HarmonyPrefix]
[HarmonyPatch(typeof(AncientDialogueSet), "GetValidDialogues")]
static bool Prefix(AncientDialogueSet __instance, ref List<AncientDialogue> __result,
    ModelId characterId, int visits)
{
    if (!__instance.CharacterDialogues.TryGetValue(characterId.Entry, out var dialogues))
        return true; // 非自定义角色，走原逻辑

    // 优先匹配 exact visit，其次 repeating
    var exact = dialogues.Where(d => d.VisitIndex == visits).ToList();
    if (exact.Count > 0) { __result = exact; return false; }

    var repeating = dialogues.Where(d => d.IsRepeating
        && (!d.VisitIndex.HasValue || visits >= d.VisitIndex.Value)).ToList();
    if (repeating.Count > 0) { __result = repeating; return false; }

    return true;
}
```

## 角色本地化

```json
// assets/localization/zhs/characters.json
{
  "WATCHER_CHARACTER": {
    "title": "观者",
    "description": "一个来自远方的神秘角色。"
  }
}
```