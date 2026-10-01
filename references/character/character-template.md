# 自定义角色：官方模板骨架（学自 ModTemplate-StS2 的 CharMod）

> 官方 Character 模板用 `PlaceholderCharacterModel` 做基类（BaseLib 抽象类），未覆盖的资源自动用占位图，适合起步。纯原生 = 继承 `CharacterModel` 手动实现同等属性。

## 完整骨架

```csharp
public class CharMod : PlaceholderCharacterModel   // BaseLib 版；纯原生改继承 CharacterModel
{
    public const string CharacterId = "CharMod";   // 资源路径前缀

    public static readonly Color Color = new("ffffff");
    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 70;

    // 初始卡组：直接用 ModelDb 引用原版卡（5 打击 + 5 防御）
    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<StrikeIronclad>(), ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(), ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<DefendIronclad>(), ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(), ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>()
    ];

    // 初始遗物：铁甲战士的燃烧之血
    public override IReadOnlyList<RelicModel> StartingRelics =>
        [ModelDb.Relic<BurningBlood>()];

    // 三池关联（用 ModelDb 引用，不是 new）
    public override CardPoolModel CardPool => ModelDb.CardPool<CharModCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<CharModRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<CharModPotionPool>();

    // 角色选择界面图标（NodeFactory 加载 + 铺满）
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }
    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();
}
```

## 要点

| 要点 | 说明 |
|------|------|
| `PlaceholderCharacterModel` | BaseLib 抽象基类，未覆盖资源用占位图；纯原生继承 `CharacterModel` |
| `ModelDb.Card<T>()` / `ModelDb.Relic<T>()` | 引用原版卡/遗物做初始卡组，无需自建 |
| `CustomIcon` | 角色选择界面的自定义图标，`NodeFactory<Control>.CreateFromResource` + 铺满全屏 |
| `CharacterUiPath()` | 图片路径工具，指向 `res://<ModId>/images/charui/`（见 design-patterns-assets.md） |
| 未覆盖资源 | 官方模板要求至少覆盖：`CustomIcon` + 选择图标 + 锁定图标 + 地图标记 |
