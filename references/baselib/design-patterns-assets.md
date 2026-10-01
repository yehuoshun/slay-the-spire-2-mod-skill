# 纯原生设计模式：资源路径工具与卡池卡背

> 从 design-patterns-extras.md 拆出。学自 ModTemplate-StS2 官方模板的 `StringExtensions` 与 `CustomCardPoolModel`。

## 资源路径工具（StringExtensions）

官方模板把图片路径收敛成一组扩展方法，每类资源一个，**找不到时回退到默认图 + Logger 提示**，纯原生可直接抄：

```csharp
public static class StringExtensions
{
    // 基础：res://<ModId>/images/<path>
    public static string ImagePath(this string path)
        => Path.Join(MainFile.ResPath, "images", path);

    public static string CardImagePath(this string path)
    {
        path = Path.Join(MainFile.ResPath, "images", "card_portraits", path);
        if (ResourceLoader.Exists(path)) return path;
        MainFile.Logger.Info("Could not find card image path: " + path);
        return Path.Join(MainFile.ResPath, "images", "card_portraits", "card.png");
    }

    public static string BigCardImagePath(this string path)   // 大图：big/ 子目录
    {
        path = Path.Join(MainFile.ResPath, "images", "card_portraits", "big", path);
        if (ResourceLoader.Exists(path)) return path;
        MainFile.Logger.Info("Could not find big card image path: " + path);
        return Path.Join(MainFile.ResPath, "images", "card_portraits", "big", "card.png");
    }

    public static string PowerImagePath(this string path)     // powers/
    public static string BigPowerImagePath(this string path)  // powers/big/
    public static string RelicImagePath(this string path)     // relics/（含 _outline.png 变体）
    public static string BigRelicImagePath(this string path)  // relics/big/
    public static string PotionImagePath(this string path)    // potions/
    public static string PotionOutlineImagePath(this string path) // potions/outline/
    public static string CharacterUiPath(this string path)    // charui/（无回退，直接拼接）
}
```

> 完整实现见 ModTemplate-StS2 `content/*/ContentModCode/Extensions/StringExtensions.cs`。
> 纯原生用 `MainFile.ResPath` + `ResourceLoader.Exists` 即可，不依赖 BaseLib。

## 卡池卡片背色（CustomCardPoolModel）

BaseLib 卡池用 HSV 控制卡背颜色（着色器作用在已有图片上），纯原生对应 `CardFrameMaterialPath` 材质方案：

```csharp
// BaseLib 版（灵感）：H/S/V ∈ [0,1]
public override float H => 1f;  // Hue 色相
public override float S => 1f;  // Saturation 饱和度
public override float V => 1f;  // Brightness 明度

// 也可自定义卡框图（注释版）：
/* public override Texture2D CustomFrame(CustomCardModel card)
   => PreloadManager.Cache.GetTexture2D("cards/frame.png".ImagePath()); */
```

> 纯原生走 `CardFrameMaterialPath => "card_frame_purple"` 材质路径（见 character-pools.md），等价效果。
