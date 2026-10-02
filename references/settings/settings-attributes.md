# 设置界面：UI 生成与主菜单注入

> 纯原生方案。灵感来源：BaseLib `NModConfigSubmenu`。

## 1. 自研子菜单（继承游戏原生 NSubmenu）

反射遍历配置属性，按 [ConfigSection] 分组动态生成控件（bool→CheckButton，double→HSlider+数值）：

```csharp
using Godot;
using System.Reflection;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

public partial class NModConfigSubmenu : NSubmenu
{
    private VBoxContainer _content = null!;
    protected override Control? InitialFocusedControl => _content;

    public override void _Ready()
    {
        base._Ready();
        BuildUI();
    }

    private void BuildUI()
    {
        _content = new VBoxContainer { CustomMinimumSize = new Vector2(600, 400) };
        var scroll = new ScrollContainer();
        scroll.AddChild(_content);
        AddChild(scroll);

        string? currentSection = null;
        foreach (var prop in typeof(MyModConfig).GetProperties())
        {
            var sectionAttr = prop.GetCustomAttribute<ConfigSectionAttribute>();
            if (sectionAttr != null && sectionAttr.Name != currentSection)
            {
                currentSection = sectionAttr.Name;
                _content.AddChild(new Label { Text = $"[b]{currentSection}[/b]" });
            }

            if (prop.PropertyType == typeof(bool))
            {
                var check = new CheckButton
                {
                    Text = prop.Name,
                    ButtonPressed = (bool)prop.GetValue(null)!,
                };
                check.Toggled += on => prop.SetValue(null, on);
                _content.AddChild(check);
            }
            else if (prop.PropertyType == typeof(double))
            {
                var attr = prop.GetCustomAttribute<ConfigSliderAttribute>() ?? new();
                var slider = new HSlider
                {
                    MinValue = attr.Min, MaxValue = attr.Max, Step = attr.Step,
                    Value = (double)prop.GetValue(null)!,
                };
                slider.ValueChanged += v => prop.SetValue(null, v);
                _content.AddChild(slider);
            }
        }

        var save = new Button { Text = "Save & Close" };
        save.Pressed += () => { ModConfigStorage.Save(); Visible = false; };
        _content.AddChild(save);
    }
}
```

## 2. 注册进主菜单（两个 Harmony Patch）

```csharp
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;      // NClickableControl
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

[HarmonyPatch(typeof(NMainMenuSubmenuStack), nameof(NMainMenuSubmenuStack.GetSubmenuType), typeof(Type))]
public static class InjectModConfigSubmenuPatch
{
    public static bool Prefix(NMainMenuSubmenuStack __instance, Type type, ref NSubmenu __result)
    {
        if (type != typeof(NModConfigSubmenu)) return true;
        var menu = new NModConfigSubmenu { Visible = false };

        __instance.AddChild(menu);
        __result = menu;
        return false;   // 跳过原生查找
    }
}

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
public static class InjectMainMenuButtonPatch
{
    public static void Postfix(NMainMenu __instance)
    {
        var settingsButton = __instance.GetNodeOrNull<NMainMenuTextButton>(
            "MainMenuTextButtons/SettingsButton");
        if (settingsButton == null) return;

        var modButton = (NMainMenuTextButton)settingsButton.Duplicate();
        modButton.Name = "ModConfigButton";
        modButton.Connect(NClickableControl.SignalName.Released, Callable.From(
            new Action<NButton>(_ => __instance.SubmenuStack.PushSubmenuType<NModConfigSubmenu>())));
        settingsButton.AddSibling(modButton);
        modButton.SetLocalization("MYMOD-MOD_CONFIGURATION");
    }
}
```

## 3. 本地化

按钮文字：`SetLocalization(key)` 查 **main_menu_ui** 表（`LocString("main_menu_ui", key)`），mod 本地化文件加：

```json
{ "MYMOD-MOD_CONFIGURATION": "模组设置" }
```

## 要点

- `NSubmenu` 必须实现 `InitialFocusedControl`；按钮路径 `MainMenuTextButtons/SettingsButton`
- 复制按钮继承 Settings 样式，只改 Name + 信号 + 本地化键；`GetNodeOrNull` 判重防重复注入
