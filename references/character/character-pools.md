# 自定义角色：卡池/遗物池/药水池

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

