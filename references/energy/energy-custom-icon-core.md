# 自定义能量图标：接口、辅助类与使用

> 纯原生实现（零第三方依赖）。

## 定义接口

放在 Mod 公共命名空间，池模型实现这个接口即可获得自定义图标。

```csharp
public interface ICustomEnergyIcon
{
    /// <summary>大图标路径（卡牌左上角），返回 null 走原生</summary>
    string? BigIconPath { get; }

    /// <summary>文本内联图标路径（描述里的 [icon] 标签），返回 null 走原生</summary>
    string? TextIconPath { get; }
}
```

## 分隔符与辅助方法

分隔符把 ModelId 编码进 `EnergyColorName`，Patch 端据此判断是否是自定义池。

```csharp
public static class EnergyIconHelper
{
    /// <summary>模型 ID 编码分隔符</summary>
    public const char Delimiter = '∴';

    /// <summary>获取编码后的 EnergyColorName</summary>
    public static string EncodePoolId(ModelId id)
        => $"{id.Category}{Delimiter}{id.Entry}";

    /// <summary>通过编码解析出自定义池模型</summary>
    public static T? DecodePool<T>(string prefix) where T : AbstractModel
    {
        int idx = prefix.IndexOf(Delimiter);
        if (idx < 0) return null;
        return ModelDb.GetById<T>(new ModelId(
            prefix[..idx], prefix[(idx + 1)..]));
    }
}
```

分隔符选 `∴` 是因为它几乎不可能出现在普通 `EnergyColorName` 里，确保不会误判。

## 在卡池模型中使用

```csharp
public class MyCardPool : CardPoolModel, ICustomEnergyIcon
{
    // EnergyColorName 必须是唯一值，不能和其他池撞
    // 用 EncodePoolId 确保唯一
    public override string EnergyColorName =>
        EnergyIconHelper.EncodePoolId(Id);

    public string? BigIconPath =>
        "res://mymod/images/energy/my_energy_icon.png";

    public string? TextIconPath =>
        "res://mymod/images/energy/my_energy_text.png";
}
```

也适用于药水池、遗物池（只要实现了 `EnergyColorName` 虚属性的池模型都可以）。

## 在 ModEntry 注册 Patch

```csharp
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        var harmony = new Harmony("mymod");
        harmony.PatchAll(); // 自动注册两个 Patch
    }
}
```

## 参见

- [energy-custom-icon-patches.md](energy-custom-icon-patches.md) — 两个 Patch 的完整实现