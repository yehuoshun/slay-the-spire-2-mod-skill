# 自定义章节（Act）

> 参考：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomActModel.cs`

---

> v3：API 全量校正（对照真实 ActModel/Overgrowth：补 IsDefault/BossDiscoveryOrder 必填、地图/休息背景命名约定不可 override、本地化 title 键）。v2 为存档，勿直接使用。
## 概述

章节（Act）控制游戏每章的地图背景、音乐、宝箱、房间数量等。继承 `ActModel` 抽象类。

原版章节：Act 1（密林 Overgrowth）、Act 2（蜂巢 Hive）、Act 3（荣耀 Glory）等。

---

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

---

## 注册

```csharp
// 手动注册到 ModelDb
ModelDb.Inject(typeof(MyAct));
```

BaseLib 方式：继承 `CustomActModel` 自动注册。

---

## 本地化

路径：`res://<模组ID>/localization/<语言代码>/acts.json`（locTable = `acts`）

```json
{
  "MY_ACT": {
    "title": "自定义章节"
  }
}
```

> 键用 `title`（旧版写 `name` 是错的）。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 自定义章节不生效 | 检查 `ModelDb.Inject` 是否调用 |
| 地图背景不显示 | 资源放 `packed/map/map_bgs/<id小写>/`（命名约定，非 override） |
| 音乐不播放 | 检查 `BgMusicOptions` / `MusicBankPaths` 路径 |
| 宝箱不显示 | 检查 `ChestSpineSkinName*` 与 `ChestSpineResourcePath` |
| 房间数量不对 | 重写 `BaseNumberOfRooms` |
| 先古之民不出现 | 在 `AllAncients` 中添加 |
| 缺抽象成员报错 | 必填：`Index`/`IsDefault`/`Map*Color`/`BgMusicOptions`/`ChestSpine*`/`BossDiscoveryOrder`/`AllAncients`/`GenerateAllEncounters` |

---

## 进阶：纯原生自动注册（v2）

> 从 BaseLib 提炼，零第三方依赖。用 `[ActModel]` 标记 + ContentRegistry 统一 `ModelDb.Inject`。
> 框架完整代码见 [serialization.md](../serialization/serialization.md)「进阶：纯原生自动注册框架」。

```csharp
[ActModel]
public class MyAct : ActModel { ... }
```

## 演进路线

- 当前：手动注册（保留为基准写法）
- v2：纯原生自动注册（Attribute 标记 + ContentRegistry）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`MapTopBgPath` 等非 virtual override、缺 `IsDefault`/`BossDiscoveryOrder`/`GenerateAllEncounters` 必填、本地化 `name`）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖