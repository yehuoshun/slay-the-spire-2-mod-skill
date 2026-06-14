# 自定义角色

> 学 ModTemplate + YuWanCard 的角色模型。

---

## 最简角色

```csharp
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;

namespace MyMod.Characters;

public sealed class MyCharacter : CharacterModel
{
    public override string CharacterId => "MyCharacter";
    public override string DisplayName => "我的角色";
    public override Color NameColor => new("ff4444");
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 75;

    // 初始卡组
    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>()
    ];

    // 初始遗物
    public override IReadOnlyList<RelicModel> StartingRelics => [
        ModelDb.Relic<BurningBlood>()
    ];

    // 卡池
    public override CardPoolModel CardPool => ModelDb.CardPool<MyCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<MyRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<MyPotionPool>();
}
```

---

## 自定义卡池

```csharp
public sealed class MyCardPool : CardPoolModel
{
    public override string Title => "MyCharacter";

    public override IEnumerable<CardModel> GenerateAllCards()
    {
        // 基础牌
        yield return ModelDb.Card<StrikeIronclad>();
        yield return ModelDb.Card<DefendIronclad>();

        // 自定义牌
        yield return ModelDb.Card<MyCustomCard>();
        yield return ModelDb.Card<MyRareCard>();

        // 无色牌
        yield return ModelDb.Card<Panacea>();
    }
}
```

---

## 自定义遗物池

```csharp
public sealed class MyRelicPool : RelicPoolModel
{
    public override string Title => "MyCharacter";

    public override IEnumerable<RelicModel> GenerateAllRelics()
    {
        // 通用遗物
        foreach (var relic in ModelDb.AllRelics)
            yield return relic;

        // 角色专属遗物
        yield return ModelDb.Relic<MyStarterRelic>();
    }
}
```

---

## 角色视觉资源

```csharp
public override Control CustomIcon { ... }
public override string CustomIconTexturePath => "res://MyMod/images/characters/icon.png";
public override string CustomCharacterSelectIconPath => "res://MyMod/images/characters/select.png";
public override string CustomCharacterSelectLockedIconPath => "res://MyMod/images/characters/select_locked.png";
public override string CustomMapMarkerPath => "res://MyMod/images/characters/map_marker.png";
```

---

## 角色注册

```csharp
// 方案一：直接注册
ModHelper.AddModelToPool(typeof(CharacterPool), typeof(MyCharacter));

// 方案二：属性扫描
[RegisterCharacter]
public sealed class MyCharacter : CharacterModel { ... }
```

---

## 角色场景（休息点、商人等）

```csharp
// ModEntry 中注册场景
NodeFactory.RegisterSceneType<NCreatureVisuals>("res://MyMod/scenes/characters/my_char.tscn");
NodeFactory.RegisterSceneType<NRestSiteCharacter>("res://MyMod/scenes/rest_site/my_char_rest.tscn");
NodeFactory.RegisterSceneType<NMerchantCharacter>("res://MyMod/scenes/merchant/my_char_merchant.tscn");
```

---

## 本地化

`assets/localization/zhs/characters.json`：

```json
{
  "MyCharacter": {
    "NAME": "我的角色",
    "DESCRIPTION": "一个自定义角色。"
  }
}
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 角色不显示在选择界面 | 确认 `CharacterPool` 注册正确 |
| 卡池不刷新 | 反射重置 `CardPoolModel._allCards` 缓存 |
| 自定义场景不加载 | 调 `ScriptManagerBridge.LookupScriptsInAssembly` |