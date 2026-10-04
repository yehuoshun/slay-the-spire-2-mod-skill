# 设置界面：配置声明与持久化

> 纯原生方案。灵感来源：BaseLib `SimpleModConfig`（属性驱动），实现零第三方依赖。

## 静态配置声明

用静态类 + 自研 Attribute 描述配置项（类型决定 UI 控件：bool→开关，double→滑条）：

```csharp
using Godot;
using System.Reflection;

namespace MyMod.Settings;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigSectionAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigSliderAttribute(double min = 0, double max = 100, double step = 1) : Attribute
{
    public double Min { get; } = min;
    public double Max { get; } = max;
    public double Step { get; } = step;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigIgnoreAttribute : Attribute;

public static class MyModConfig
{
    [ConfigSection("通用")]
    public static bool EnableDeathEffect { get; set; } = true;

    [ConfigSection("通用")]
    [ConfigSlider(0.5, 4.0, 0.5)]
    public static double CursorScale { get; set; } = 2.0;
}
```

## 持久化（Godot ConfigFile）

`user://mod_configs/<ModId>/config.cfg`（对齐游戏 mod_configs 目录；首次运行加载失败则用默认值）：

```csharp
public static class ModConfigStorage
{
    private const string ConfigPath = "user://mod_configs/MyMod/config.cfg";
    private const string SectionName = "mod_config";

    public static void Save()
    {
        var cf = new ConfigFile();
        foreach (var prop in typeof(MyModConfig).GetProperties())
        {
            if (prop.GetCustomAttribute<ConfigIgnoreAttribute>() != null) continue;
            cf.SetValue(SectionName, prop.Name, Variant.From(prop.GetValue(null)));
        }
        cf.Save(ConfigPath);
    }

    public static void Load()
    {
        var cf = new ConfigFile();
        if (cf.Load(ConfigPath) != Error.Ok) return;   // 首次运行用默认值

        foreach (var prop in typeof(MyModConfig).GetProperties())
        {
            if (prop.GetCustomAttribute<ConfigIgnoreAttribute>() != null) continue;
            var value = cf.GetValue(SectionName, prop.Name, Variant.From(prop.GetValue(null)));
            prop.SetValue(null, Convert.ChangeType(value.Obj, prop.PropertyType));
        }
    }
}
```

> ⚠️ `ConfigFile.SetValue/GetValue` 第三参是 **Godot `Variant`**（不是 object）：写用 `Variant.From(...)`，读用 `value.Obj` 取回 boxed 值再 `Convert.ChangeType`。

## 加载时机

`ModEntry.Initialize` 里调用一次 `ModConfigStorage.Load()`；UI 修改后 `Save()`（也可在改值回调里即时保存）。

> ⚠️ **延迟/异步注册配置**（等初始化之后、游戏 UI 就绪时再挂）必须同时确认本地化管理器已就绪：`if (MainFile.Config == null || LocManager.Instance == null) return;`。`LocManager.Instance` 初值为 `null`，早于它就绪注册会拿不到本地化串，配置项显示为 key。`LocManager` 在 `MegaCrit.Sts2.Core.Localization`。

> Attribute 定义与 UI 生成 → [settings-attributes.md](settings-attributes.md)
