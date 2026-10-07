# 占位角色双基类（自定义资产分步替换）

> 实战验证（sts2mod 俄洛伊 Illaoi v0.2.0，2026-10-07）。角色开发「先跑通玩法、后补美术」的骨架：两层抽象基类，全部 Custom* 路径默认 null/复用原版资源，子类只覆写自己有的资产。对照：[character-overrides.md](character-overrides.md)（BaseLib Placeholder 覆写点清单）。

## 两层结构

```
CharacterModel（原版抽象）
 └─ CustomCharacterModel（BaseLib.Abstracts）      ← 全部 Custom* 路径 virtual，默认 null
     └─ MyCustomCharacterModel（本 mod 层 1）      ← 统一默认值 + 资产汇总
         └─ MyPlaceholderCharacterModel（层 2）    ← PlaceholderId，复用原版 ironclad 资源
             └─ MyCharacter（实际角色）            ← 只覆写差异点
```

### 层 1：统一默认 + 汇总（MyCustomCharacterModel）

```csharp
public abstract class MyCustomCharacterModel : CustomCharacterModel
{
    // 全部 Custom* 路径默认 null（无自定义资源 = 用原版默认）
    public override string? CustomVisualPath => null;
    public override string? CustomTrailPath => null;
    // ...CustomIconTexturePath / CustomEnergyCounterPath / CustomRestSiteAnimPath /
    //    CustomMerchantAnimPath / CustomArm*TexturePath / CustomCharacterSelect*
    //    / CustomMapMarkerPath / CustomAttackSfx / CustomCastSfx / CustomDeathSfx 全部 null
    public virtual IEnumerable<string> ExtraCustomAssetPaths => [];
    public virtual IEnumerable<string> ExtraCustomCharacterSelectAssetPaths => [];
    protected override CharacterModel? UnlocksAfterRunAs => null;

    // 汇总：全部自定义路径去重，供注册/加载一次性遍历
    public virtual IEnumerable<string> AllCustomAssetPaths =>
        NonEmpty(CustomVisualPath, CustomIconTexturePath, ..., CustomDeathSfx)
            .Concat(ExtraCustomAssetPaths)
            .Where(p => !string.IsNullOrWhiteSpace(p)).Distinct();
}
```

### 层 2：占位复用原版资源（MyPlaceholderCharacterModel）

```csharp
public abstract class MyPlaceholderCharacterModel : MyCustomCharacterModel
{
    public virtual string PlaceholderId => "ironclad";   // 复用哪个原版角色的资源
    private string Key => PlaceholderId.ToLowerInvariant();

    public override string? CustomVisualPath => SceneHelper.GetScenePath("creature_visuals/" + Key);
    public override string? CustomIconTexturePath => ImageHelper.GetImagePath("ui/top_panel/character_icon_" + Key + ".png");
    public override string? CustomRestSiteAnimPath => SceneHelper.GetScenePath("rest_site/characters/" + Key + "_rest_site");
    public override string? CustomMerchantAnimPath => SceneHelper.GetScenePath("merchant/characters/" + Key + "_merchant");
    public override string? CustomArmPointingTexturePath => ImageHelper.GetImagePath("ui/hands/multiplayer_hand_" + Key + "_point.png");
    // ...CustomCharacterSelectBg / CustomMapMarkerPath / 攻击/施法/死亡音效 = event:/sfx/characters/{key}/...
}
```

- 实际角色只写差异：`public override string? CustomVisualPath => ModInfo.MyOwnScenePath;` 等，没写的自动用铁甲战士动画/音效/图标占位。

## 角色模型公共覆写速查（俄洛伊实战值）

| 属性 | 例子 |
|------|------|
| `NameColor` / `DialogueColor` / `MapDrawingColor` | 主题色（卡牌绿） |
| `StartingHp` / `StartingGold` | 75 / 99 |
| `AttackAnimDelay` / `CastAnimDelay` | 0.15f / 0.25f（攻击/施法动画延迟，节奏） |
| `EnergyLabelOutlineColor` / `SpeechBubbleColor` / `RemoteTargetingLineColor` | UI 配色 |
| `CharacterSelectSfx` | 随机语音（`Random.Shared.Next` 从数组抽） |
| `GetArchitectAttackVfx()` | 架构师战攻击特效列表 |
| `ExtraCustomAssetPaths` / `ExtraCustomCharacterSelectAssetPaths` | 追加资源（战斗图/休息站图/精灵字体能量图标） |

## 本地化双键（BaseLib 角色）

`characters.json` 里每个键写**两遍**：`ILLAOI_CHARACTER.title` 和 `ILLAOI-ILLAOI_CHARACTER.title`（`{池ID}-{模型ID}` 前缀变体）——BaseLib/原版两种查找路径都覆盖，共 11 键 ×2（title/titleObject/pronoun* 等）。卡/能力/遗物本地化用扁平键 `{ID}.title` / `{ID}.description`（见 [card.md](../card/card.md)）。

## 提示

- 音效路径占位复用 ironclad 时，后续替换只需改对应 `Custom*Sfx` 覆写。
- `AllCustomAssetPaths` 汇总贴合 `CharacterModel.AssetPaths` 的语义，加载资源/校验缺失路径时一次遍历即可。