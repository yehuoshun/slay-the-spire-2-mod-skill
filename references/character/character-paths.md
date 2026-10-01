# 自定义角色：资源路径

> 从 character-core.md 拆出。角色 mod 需要全套 UI/场景资源，路径全部以 `<ID小写>` 拼资源名。

## 场景资源

| 资源 | 路径 |
|------|------|
| 默认待机动画场景 | `res://scenes/creature_visuals/<ID小写>.tscn` |
| 头像缩略图图标场景 | `res://scenes/ui/character_icons/<ID小写>_icon.tscn` |
| 能量计数器场景 | `res://scenes/combat/energy_counters/<ID小写>_energy_counter.tscn` |
| 商店待机动画场景 | `res://scenes/merchant/characters/<ID小写>_merchant.tscn` |
| 火堆休息动画场景 | `res://scenes/rest_site/characters/<ID小写>_rest_site.tscn` |
| 卡牌拖尾特效场景 | `res://scenes/vfx/card_trail_<ID小写>.tscn` |
| 角色选择界面背景 | `res://scenes/screens/char_select/char_select_bg_<ID小写>.tscn` |

## 纹理资源

| 资源 | 路径 |
|------|------|
| 头像缩略图纹理 | `res://images/ui/top_panel/character_icon_<ID小写>.png` |
| 头像缩略图描边 | `res://images/ui/top_panel/character_icon_<ID小写>_outline.png` |
| 选择界面底部图 | `res://images/packed/character_select/char_select_<ID小写>.png` |
| 未解锁底部图 | `res://images/packed/character_select/char_select_<ID小写>_locked.png` |
| 地图标记箭头 | `res://images/packed/map/icons/map_marker_<ID小写>.png` |
| 联机手臂-手指 | `res://images/ui/hands/multiplayer_hand_<ID小写>_point.png` |
| 联机手臂-石头 | `res://images/ui/hands/multiplayer_hand_<ID小写>_rock.png` |
| 联机手臂-布 | `res://images/ui/hands/multiplayer_hand_<ID小写>_paper.png` |
| 联机手臂-剪刀 | `res://images/ui/hands/multiplayer_hand_<ID小写>_scissors.png` |
| 过场动画着色器材质 | `res://materials/transitions/<ID小写>_transition_mat.tres` |

## FMOD 音效路径

```
event:/sfx/characters/<ID小写>/<ID小写>_attack
event:/sfx/characters/<ID小写>/<ID小写>_cast
event:/sfx/characters/<ID小写>/<ID小写>_die
```

## charui 通用约定（官方模板）

| 资源 | 默认路径约定 | 对应属性 |
|------|-------------|---------|
| 角色选择图标 | `images/charui/character_icon_<id>.png` | `CustomIconTexturePath` |
| 角色名（锁定）图标 | `images/charui/char_select_char_name.png` | `CustomCharacterSelectLockedIconPath` |
| 大地图标记 | `images/charui/map_marker_<id>.png` | `CustomMapMarkerPath` |
| 大能量图标 | `images/charui/big_energy.png` | `CustomEnergyCounterPath` |
| 文字能量图标 | `images/charui/text_energy.png` | — |

> 明细以 [character.md](character.md) 为准，此处给出通用目录约定。
