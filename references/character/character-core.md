# 自定义角色：角色类与资源路径

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

