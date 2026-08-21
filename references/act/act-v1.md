# 自定义章节（Act）

> 参考：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomActModel.cs`

---

## 概述

章节（Act）控制游戏每章的地图背景、音乐、宝箱、房间数量等。继承 `ActModel` 抽象类。

原版章节：Act 1（密林）、Act 2（蜂巢）、Act 3。

---

## 基础模板

```csharp
using MegaCrit.Sts2.Core.Models;

public class MyAct : ActModel
{
    public override int Index => 1;  // 0=Act1, 1=Act2, 2=Act3

    public override string[] BgMusicOptions => [
        "event:/music/act1_a1_v1",
        "event:/music/act1_a2_v1"
    ];

    public override string[] MusicBankPaths => [
        "res://banks/desktop/act1_a1.bank",
        "res://banks/desktop/act1_a2.bank"
    ];
}
```

---

## 可覆盖属性

```csharp
public class MyAct : ActModel
{
    // 章节索引（0=Act1, 1=Act2, 2=Act3）
    public override int Index => 0;

    // 地图背景（顶部/中部/底部）
    // 默认路径：res://images/packed/map/map_bgs/<id>/map_<位置>_<id>.png
    public string MapTopBgPath => "";
    public string MapMidBgPath => "";
    public string MapBotBgPath => "";

    // 背景场景路径
    // 默认：res://scenes/backgrounds/<id>/<id>_background.tscn
    public string BackgroundScenePath => "";

    // 休息处背景
    public string RestSiteBackgroundPath => "";

    // 音乐
    public override string[] BgMusicOptions => [];
    public override string[] MusicBankPaths => [];

    // 环境音效
    public override string AmbientSfx => "";

    // 地图颜色
    public override Color MapTraveledColor => new("27221C");
    public override Color MapUntraveledColor => new("6E7750");
    public override Color MapBgColor => new("9B9562");

    // 宝箱 Spine 资源
    public override string ChestSpineResourcePath => "";
    public override string ChestSpineSkinNameNormal => "";
    public override string ChestSpineSkinNameStroke => "";
    public override string ChestOpenSfx => "";

    // 房间数量
    protected override int BaseNumberOfRooms => 15;

    // 先古之民列表
    public override IEnumerable<AncientEventModel> AllAncients => [];
}
```

### 属性说明

| 属性 | 类型 | 说明 |
|------|------|------|
| `Index` | `int` | 章节索引（0=Act1, 1=Act2, 2=Act3） |
| `MapTopBgPath` | `string` | 地图顶部背景 |
| `MapMidBgPath` | `string` | 地图中部背景 |
| `MapBotBgPath` | `string` | 地图底部背景 |
| `BackgroundScenePath` | `string` | 背景场景 |
| `RestSiteBackgroundPath` | `string` | 休息处背景 |
| `BgMusicOptions` | `string[]` | 背景音乐列表 |
| `MusicBankPaths` | `string[]` | 音乐音效包路径 |
| `AmbientSfx` | `string` | 环境音效 |
| `MapTraveledColor` | `Color` | 已走过路径颜色 |
| `MapUntraveledColor` | `Color` | 未走过路径颜色 |
| `MapBgColor` | `Color` | 地图背景颜色 |
| `ChestSpineResourcePath` | `string` | 宝箱 Spine 资源路径 |
| `ChestSpineSkinNameNormal` | `string` | 宝箱普通皮肤名 |
| `ChestSpineSkinNameStroke` | `string` | 宝箱描边皮肤名 |
| `ChestOpenSfx` | `string` | 宝箱开启音效 |
| `BaseNumberOfRooms` | `int` | 基础房间数量 |
| `AllAncients` | `IEnumerable<AncientEventModel>` | 可用先古之民列表 |
| `BossDiscoveryOrder` | `IEnumerable<EncounterModel>` | Boss 首次出现顺序 |

---

## 注册

```csharp
// 手动注册到 ModelDb
ModelDb.Inject(typeof(MyAct));
```

BaseLib 方式：继承 `CustomActModel` 自动注册。

---

## 本地化

路径：`res://<模组ID>/localization/<语言代码>/acts.json`

```json
{
  "MyAct": {
    "name": "自定义章节"
  }
}
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 自定义章节不生效 | 检查 `ModelDb.Inject` 是否调用 |
| 地图背景不显示 | 检查 `MapTopBgPath` / `MapMidBgPath` / `MapBotBgPath` |
| 音乐不播放 | 检查 `BgMusicOptions` / `MusicBankPaths` 路径 |
| 宝箱不显示 | 检查 `ChestSpineResourcePath` |
| 房间数量不对 | 重写 `BaseNumberOfRooms` |
| 先古之民不出现 | 在 `AllAncients` 中添加 |

---

## 演进路线

- 当前：手动创建 + 注册
- BaseLib：`CustomActModel` 带自动注册 + 场景转换