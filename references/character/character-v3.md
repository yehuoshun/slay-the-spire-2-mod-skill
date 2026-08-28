# 自定义角色

> 参考：[杀戮尖塔2模组开发教程08 - 自定义角色 - 哔哩哔哩](https://www.bilibili.com/opus/1182961747166756931)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomCharacterModel.cs`

---

> v3：API 全量校正（对照真实 CharacterModel/Deprived：删编造 Name、Gender 真实枚举、protected GenerateAllCards/Relics/Potions、ModelDb 池引用、ref IEnumerable Patch）。v2 为存档，勿直接使用。
## 概述

自定义角色除了继承 `CharacterModel` 抽象类，还需要定义卡池、遗物池、药水池，以及大量场景/纹理/材质资源。

> 角色**没有 `Name` 属性**——名字走本地化（`characters` 表 `<ID>.title`）。

---

## 一、角色卡池

```csharp
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;

public class MyCardPool : CardPoolModel
{
    public override string Title => "MyCharacter";
    public override string EnergyColorName => "purple";
    public override string CardFrameMaterialPath => "card_frame_purple";
    public override Color DeckEntryCardColor => Colors.Purple;
    public override bool IsColorless => false;

    // protected，不是 public
    protected override CardModel[] GenerateAllCards() => [];
}
```

### 卡池属性

| 属性 | 说明 |
|------|------|
| `Title` | 卡池名称，影响卡面图资源路径 |
| `EnergyColorName` | 能量图标名称 |
| `CardFrameMaterialPath` | 卡牌边框着色器材质 |
| `DeckEntryCardColor` | 缩略卡底色 |
| `EnergyOutlineColor` | 能量数字描边颜色（virtual，有默认） |
| `IsColorless` | 是否无色卡池 |
| `GenerateAllCards()` | **protected**，返回所有卡牌 |

### 卡池资源

```
能量图标大图：    res://images/ui/energys/<EnergyColorName>_energy_icon.png
能量图标裁切：    res://images/atlases/ui_atlas.sprites/card/energy_<EnergyColorName>.tres
富文本缩略图标：  res://images/packed/sprite_fonts/<EnergyColorName>_energy_icon.png
卡牌边框材质：    res://materials/cards/frames/<CardFrameMaterialPath>_mat.tres
```

---

## 二、角色遗物池

```csharp
public class MyRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "purple";

    // protected，返回 IEnumerable
    protected override IEnumerable<RelicModel> GenerateAllRelics() => [];
}
```

> `LabOutlineColor` 是 virtual（有默认值 `halfTransparentBlack`），可不必 override。

---

## 三、角色药水池

```csharp
public class MyPotionPool : PotionPoolModel
{
    public override string EnergyColorName => "purple";

    // protected，返回 IEnumerable
    protected override IEnumerable<PotionModel> GenerateAllPotions() => [];
}
```

---

## 四、角色类

```csharp
public class MyCharacter : CharacterModel
{
    // 抽象必填：名字颜色 / 性别 / 血量 / 金币
    public override Color NameColor => Colors.Purple;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 70;
    public override int StartingGold => 99;

    public override float AttackAnimDelay => 0.15f;   // 攻击动画延迟
    public override float CastAnimDelay => 0.25f;     // 施法动画延迟
    public override int MaxEnergy => 3;               // 能量上限（virtual，默认 3）

    // protected，前置角色（null = 默认解锁）
    protected override CharacterModel? UnlocksAfterRunAs => null;

    // 池：用 ModelDb 引用，不是 new
    public override CardPoolModel CardPool => ModelDb.CardPool<MyCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<MyRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<MyPotionPool>();

    public override IEnumerable<CardModel> StartingDeck => [];
    public override IReadOnlyList<RelicModel> StartingRelics => [];
    public override IReadOnlyList<PotionModel> StartingPotions => [];

    public override List<string> GetArchitectAttackVfx() => [];
}
```

### CharacterGender（真实枚举）

| 值 | 说明 |
|-----|------|
| `CharacterGender.Neutral` | 中性 |
| `CharacterGender.Feminine` | 阴性/女性语法 |
| `CharacterGender.Masculine` | 阳性/男性语法 |

> ⚠️ 旧版写的 `Male/Female/Other` 不存在，且 `Name` 属性不存在（名字走本地化 `Title`）。

### 角色抽象属性（必填）

| 属性 | 类型 | 说明 |
|------|------|------|
| `NameColor` | `Color` | 名字颜色 |
| `Gender` | `CharacterGender` | 语法性别 |
| `StartingHp` | `int` | 初始生命 |
| `StartingGold` | `int` | 初始金币 |
| `AttackAnimDelay` / `CastAnimDelay` | `float` | 攻击/施法动画延迟 |
| `UnlocksAfterRunAs` | `CharacterModel?`（protected） | 前置角色 |
| `CardPool` / `RelicPool` / `PotionPool` | 池模型 | 关联三池 |
| `StartingDeck` | `IEnumerable<CardModel>` | 初始卡组 |
| `StartingRelics` | `IReadOnlyList<RelicModel>` | 初始遗物 |
| `GetArchitectAttackVfx()` | `List<string>` | 攻击建筑师动画序列 |

---

## 五、角色资源路径

| 资源 | 路径 |
|------|------|
| 默认待机动画场景 | `res://scenes/creature_visuals/<ID小写>.tscn` |
| 头像缩略图图标场景 | `res://scenes/ui/character_icons/<ID小写>_icon.tscn` |
| 能量计数器场景 | `res://scenes/combat/energy_counters/<ID小写>_energy_counter.tscn` |
| 商店待机动画场景 | `res://scenes/merchant/characters/<ID小写>_merchant.tscn` |
| 火堆休息动画场景 | `res://scenes/rest_site/characters/<ID小写>_rest_site.tscn` |
| 卡牌拖尾特效场景 | `res://scenes/vfx/card_trail_<ID小写>.tscn` |
| 头像缩略图纹理 | `res://images/ui/top_panel/character_icon_<ID小写>.png` |
| 头像缩略图描边 | `res://images/ui/top_panel/character_icon_<ID小写>_outline.png` |
| 角色选择界面背景 | `res://scenes/screens/char_select/char_select_bg_<ID小写>.tscn` |
| 选择界面底部图 | `res://images/packed/character_select/char_select_<ID小写>.png` |
| 未解锁底部图 | `res://images/packed/character_select/char_select_<ID小写>_locked.png` |
| 地图标记箭头 | `res://images/packed/map/icons/map_marker_<ID小写>.png` |
| 联机手臂-手指 | `res://images/ui/hands/multiplayer_hand_<ID小写>_point.png` |
| 联机手臂-石头 | `res://images/ui/hands/multiplayer_hand_<ID小写>_rock.png` |
| 联机手臂-布 | `res://images/ui/hands/multiplayer_hand_<ID小写>_paper.png` |
| 联机手臂-剪刀 | `res://images/ui/hands/multiplayer_hand_<ID小写>_scissors.png` |
| 过场动画着色器材质 | `res://materials/transitions/<ID小写>_transition_mat.tres` |

### FMOD 音效路径

```
event:/sfx/characters/<ID小写>/<ID小写>_attack
event:/sfx/characters/<ID小写>/<ID小写>_cast
event:/sfx/characters/<ID小写>/<ID小写>_die
```

---

## 六、场景搭建注意事项

### 角色场景（NCreatureVisuals）

根节点必须挂载 `NCreatureVisuals` 脚本：

```csharp
public class CustomCreatureVisuals : NCreatureVisuals { }
```

场景结构：
```
NCreatureVisuals (根节点)
  └─ Visuals (Node2D，唯一名称访问 %)
```

### 能量计数器场景（NEnergyCounter + MegaLabel）

```csharp
public class CustomMegaLabel : MegaLabel { }
```

```
NEnergyCounter (根节点)
  └─ Label (MegaLabel, MinFontSize=32, MaxFontSize=36)
```

### 卡牌拖尾场景（NCardTrailVfx）

```csharp
public class CustomCardTrailVfx : NCardTrailVfx { }
```

```
NCardTrailVfx (根节点)
  └─ NCardTrail (Line2D) × 2
```

---

## 七、Spine 动画导入

1. Mod 根目录创建 `bin/` 文件夹
2. 下载 Godot-Spine 插件 GDExtension 版放入 `bin/`
3. 重启编辑器加载插件
4. Spine JSON 后缀改为 `.spine-json`

---

## 八、注册角色

```csharp
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)]
public static class AllCharactersPatch
{
    public static void Postfix(ref IEnumerable<CharacterModel> __result)
    {
        __result = __result.Append(new MyCharacter());
    }
}
```

> 注意：`AllCharacters` 真实返回 `IEnumerable<CharacterModel>`（不是 `List`），Postfix 需 `ref IEnumerable<CharacterModel>`。
---

## 九、本地化

路径：`res://<模组ID>/localization/<语言代码>/characters.json`

```json
{
  "MY_CHARACTER": {
    "title": "自定义角色",
    "description": "一个来自异世界的战士。"
  }
}
```

> 键与类名 ID 一致（大驼峰 → 大写加下划线），`title` 用于角色名（旧版写 `name` 是错的）。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 角色不在选择界面 | 检查 `ModelDb.AllCharacters` Patch（ref IEnumerable） |
| 卡牌样式不对 | 检查 `CardFrameMaterialPath` 材质路径 + 着色器 |
| 能量图标不显示 | 检查 `EnergyColorName` 对应资源 |
| 角色场景报错 | 根节点必须继承 `NCreatureVisuals` |
| 能量计数器报错 | Label 必须使用 MegaLabel |
| 卡牌拖尾无效 | 根节点必须使用 `NCardTrailVfx` |
| Spine 动画不识别 | 后缀改为 `.spine-json` |
| 导出时报错 | 忽略 `addons/*` 防止插件重复定义 |
| 音效缺失 | 指定复用其他角色音效路径 |

---

## 进阶：纯原生一键注册（v2）

> 从 BaseLib 提炼，零第三方依赖。用 `[RegisteredCharacter]` 标记 + 反射在 Patch 里统一注册角色及其关联池，替代逐个手写。

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RegisteredCharacterAttribute : Attribute { }

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)]
public static class AllCharactersPatch
{
    private static void Postfix(ref IEnumerable<CharacterModel> __result)
    {
        var registered = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<RegisteredCharacterAttribute>() != null)
            .Select(t => (CharacterModel)Activator.CreateInstance(t)!);
        __result = __result.Concat(registered);
    }
}
```

```csharp
[RegisteredCharacter]
public class MyCharacter : CharacterModel { ... }   // 自动注册
```

> 角色的卡池/遗物池/药水池由角色类自身的 `CardPool` / `RelicPool` / `PotionPool` 属性关联，无需额外注册。

## 演进路线

- 当前：手动 Patch 注入（保留为基准写法）
- v2：纯原生一键注册（Attribute 标记 + 反射）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`Name`、`Gender.Male/Female/Other`、`public GenerateAllCards/Relics/Potions`、`new MyCardPool()`、`ref List` Patch、`ScriptManagerBridge`）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖