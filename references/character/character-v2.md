# 自定义角色

> 参考：[杀戮尖塔2模组开发教程08 - 自定义角色 - 哔哩哔哩](https://www.bilibili.com/opus/1182961747166756931)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomCharacterModel.cs`

---

> v2：新增「纯原生一键注册」（从 BaseLib 提炼，零第三方依赖）
## 概述

自定义角色除了继承 `CharacterModel` 抽象类，还需要定义卡池、遗物池、药水池，以及大量场景/纹理/材质资源。

---

## 一、角色卡池

```csharp
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;

public class MyCardPool : CardPoolModel
{
    public override string Title => "MyCharacter";
    public override string EnergyColorName => "purple";
    public override string CardFrameMaterialPath => "card_frame_purple";
    public override Color DeckEntryCardColor => Colors.Purple;
    public override Color EnergyOutlineColor => Colors.White;
    public override bool IsColorless => false;

    public override CardModel[] GenerateAllCards() => [];
}
```

### 卡池属性

| 属性 | 说明 |
|------|------|
| `Title` | 卡池名称，影响卡面图资源路径 |
| `EnergyColorName` | 能量图标名称 |
| `CardFrameMaterialPath` | 卡牌边框着色器材质 |
| `DeckEntryCardColor` | 缩略卡底色 |
| `EnergyOutlineColor` | 能量数字描边颜色 |
| `IsColorless` | 是否无色卡池 |
| `GenerateAllCards()` | 返回卡池中所有卡牌 |

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
    public override Color LabOutlineColor => Colors.Purple;
    public override RelicModel[] GenerateAllRelics() => [];
}
```

---

## 三、角色药水池

```csharp
public class MyPotionPool : PotionPoolModel
{
    public override PotionModel[] GenerateAllPotions() => [];
}
```

---

## 四、角色类

```csharp
public class MyCharacter : CharacterModel
{
    public override string Name => "MyCharacter";
    public override CharacterGender Gender => CharacterGender.Male;

    // 原生默认值（新增）
    public override int StartingGold => 99;           // 默认 99
    public override float AttackAnimDelay => 0.15f;   // 攻击动画延迟
    public override float CastAnimDelay => 0.25f;     // 施法动画延迟
    public override CharacterModel UnlocksAfterRunAs => null; // 前置角色

    public override CardPoolModel CardPool => new MyCardPool();
    public override PotionPoolModel PotionPool => new MyPotionPool();
    public override RelicPoolModel RelicPool => new MyRelicPool();

    public override CardModel[] StartingDeck => [];
    public override RelicModel[] StartingRelics => [];

    public override string[] GetArchitectAttackVfx() => [];
}
```

### CharacterGender

| 值 | 说明 |
|-----|------|
| `CharacterGender.Male` | 男性 |
| `CharacterGender.Female` | 女性 |
| `CharacterGender.Other` | 其他 |

### 角色原生属性

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `StartingGold` | 99 | 初始金币 |
| `AttackAnimDelay` | 0.15f | 攻击动画延迟 |
| `CastAnimDelay` | 0.25f | 施法动画延迟 |
| `UnlocksAfterRunAs` | null | 前置角色（null=默认解锁） |
| `CardPool` | - | 关联卡池 |
| `PotionPool` | - | 关联药水池 |
| `RelicPool` | - | 关联遗物池 |
| `StartingDeck` | - | 初始卡组 |
| `StartingRelics` | - | 初始遗物 |
| `GetArchitectAttackVfx()` | - | 攻击建筑师动画序列 |

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
[HarmonyPatch(typeof(ModelDb), "AllCharacters", MethodType.Getter)]
public static class AllCharactersPatch
{
    public static void Postfix(ref List<CharacterModel> __result)
    {
        __result.Add(new MyCharacter());
    }
}
```

---

## 九、程序集脚本检索修复

```csharp
ScriptManagerBridge.LookupScriptsInAssembly(GetType().Assembly);
```

---

## 十、本地化

路径：`res://<模组ID>/localization/<语言代码>/characters.json`

```json
{
  "MyCharacter": {
    "name": "自定义角色",
    "description": "一个来自异世界的战士。"
  }
}
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 角色不在选择界面 | 检查 `ModelDb.AllCharacters` Patch 是否生效 |
| 卡牌样式不对 | 检查 `CardFrameMaterialPath` 材质路径 + 着色器 |
| 能量图标不显示 | 检查 `EnergyColorName` 对应资源 |
| 角色场景报错 | 根节点必须继承 `NCreatureVisuals` |
| 能量计数器报错 | Label 必须使用 MegaLabel |
| 卡牌拖尾无效 | 根节点必须使用 `NCardTrailVfx` |
| 找不到程序集 | 调 `ScriptManagerBridge.LookupScriptsInAssembly` |
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
    private static void Postfix(ref List<CharacterModel> __result)
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (type.GetCustomAttribute<RegisteredCharacterAttribute>() != null)
                __result.Add((CharacterModel)Activator.CreateInstance(type)!);
        }
    }
}
```

```csharp
[RegisteredCharacter]
public class MyCharacter : CharacterModel { ... }   // 自动注册
```

> 角色的卡池/遗物池/药水池由角色类自身的 `CardPool` / `RelicPool` / `PotionPool` 属性关联，无需额外注册。

## 演进路线

- 当前：手动注册（保留为基准写法）
- v2 更优：**纯原生自动注册**（见上方「进阶」章节）——Attribute 标记 + ContentRegistry / 反射注册辅助，免手动注册
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖