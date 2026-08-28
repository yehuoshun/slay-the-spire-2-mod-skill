# 自定义章节：模板与可覆盖成员

## 基础模板

```csharp
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

public class MyAct : ActModel
{
    public override int Index => 1;        // 0=Act1, 1=Act2, 2=Act3
    public override bool IsDefault => false; // 是否为默认章节

    // 音乐（abstract 必填）
    public override string[] BgMusicOptions => [
        "event:/music/act1_a1_v1",
        "event:/music/act1_a2_v1"
    ];
    public override string[] MusicBankPaths => [
        "res://banks/desktop/act1_a1.bank",
        "res://banks/desktop/act1_a2.bank"
    ];
    public override string AmbientSfx => "";

    // 地图颜色（abstract 必填）
    public override Color MapTraveledColor => new("27221C");
    public override Color MapUntraveledColor => new("6E7750");
    public override Color MapBgColor => new("9B9562");

    // 宝箱（abstract 必填）
    public override string ChestSpineSkinNameNormal => "";
    public override string ChestSpineSkinNameStroke => "";
    public override string ChestOpenSfx => "";

    // Boss 首次出现顺序（abstract 必填）
    public override IEnumerable<EncounterModel> BossDiscoveryOrder => [];

    // 房间数量（protected abstract）
    protected override int BaseNumberOfRooms => 15;

    // 先古之民列表（abstract 必填）
    public override IEnumerable<AncientEventModel> AllAncients => [];

    // 遭遇（abstract 必填，见 monster 模块）
    public override IEnumerable<EncounterModel> GenerateAllEncounters() => [];
}
```

> 地图背景/休息背景路径（`MapTopBgPath` 等）由原生**命名约定**自动生成（`packed/map/map_bgs/<id小写>/...`），**非 virtual 不可 override**。

---

## 可覆盖成员（真实存在）

| 成员 | 类型 | 说明 |
|------|------|------|
| `Index` | `public abstract int` | 章节索引（0=Act1） |
| `IsDefault` | `public abstract bool` | 是否为默认章节（**必填**） |
| `MapTraveledColor` / `MapUntraveledColor` / `MapBgColor` | `public abstract Color` | 地图颜色 |
| `BgMusicOptions` / `MusicBankPaths` / `AmbientSfx` | `public abstract string[] / string` | 音乐与环境音效 |
| `ChestSpineSkinNameNormal` / `ChestSpineSkinNameStroke` / `ChestOpenSfx` | `public abstract string` | 宝箱皮肤/音效 |
| `BossDiscoveryOrder` | `public abstract IEnumerable<EncounterModel>` | Boss 首次出现顺序 |
| `BaseNumberOfRooms` | `protected abstract int` | 基础房间数量 |
| `AllAncients` | `public abstract IEnumerable<AncientEventModel>` | 先古之民 |
| `GenerateAllEncounters()` | `public abstract IEnumerable<EncounterModel>` | 遭遇 |
| `ChestSpineResourcePath` | `public virtual string` | 宝箱 Spine 资源（有默认） |

> 地图背景/休息处背景（`MapTopBgPath`/`MapMidBgPath`/`MapBotBgPath`/`RestSiteBackgroundPath`）是**命名约定自动生成**（非 virtual），把资源放到 `packed/map/map_bgs/<id小写>/` 即可，无需写代码。

