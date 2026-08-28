# 设置界面：Attribute、示例与本地化

## 支持的属性类型

| 类型 | 生成的 UI | 说明 |
|------|----------|------|
| `bool` | 开关（Toggle） | 勾选框 |
| `int` / `float` / `double` | 滑块（Slider） | 加 `[ConfigSlider]` 指定范围 |
| `string` | 输入框（LineEdit） | 加 `[ConfigTextInput]` 限制输入 |
| `Color` | 颜色选择器 | 点选颜色 |
| 枚举 | 下拉菜单（Dropdown） | 自动生成选项 |

---

## 属性 Attribute

### [ConfigSection] — 分组

```csharp
[ConfigSection("GeneralSettings")]
public static bool Option1 { get; set; } = true;

[ConfigSection("AdvancedSettings")]
public static bool Option2 { get; set; } = false;
```

### [ConfigSlider] — 滑块范围

```csharp
[ConfigSlider(1, 64)]           // 最小值 1，最大值 64，步长 1
public static int SfxPlayerLimit { get; set; } = 16;

[ConfigSlider(0.0, 1.0, 0.1)]  // 步长 0.1
public static double Volume { get; set; } = 0.8;
```

### [ConfigButton] — 按钮

```csharp
[ConfigButton("SaveNow")]
public static void OnSaveButton(ModConfig config)
{
    config.Save();
}
```

### [ConfigHideInUI] — 隐藏

```csharp
[ConfigHideInUI]
public static int InternalCounter { get; set; } = 0;
```

### [ConfigVisibleIf] — 条件显示

```csharp
public static bool EnableExtraOptions { get; set; } = false;

// 只有 EnableExtraOptions 为 true 时才显示
[ConfigVisibleIf(nameof(EnableExtraOptions))]
public static bool ExtraOption { get; set; } = true;

// 枚举条件
public enum Mode { Basic, Advanced }
public static Mode CurrentMode { get; set; } = Mode.Basic;

[ConfigVisibleIf(nameof(CurrentMode), Mode.Advanced)]
public static float AdvancedValue { get; set; } = 0f;
```

### [ConfigIgnore] — 完全忽略

```csharp
[ConfigIgnore]
public static int NotConfig { get; set; } = 0; // 不保存，不显示
```

### [ConfigIgnoreRestoreDefaults] — 忽略恢复默认

```csharp
[ConfigIgnoreRestoreDefaults]
public static int PersistentState { get; set; } = 0;
```

### [ConfigTextInput] — 输入限制

```csharp
[ConfigTextInput(MaxLength = 1024)]
public static string LongText { get; set; } = "";

[ConfigTextInput(TextInputPreset.Alphanumeric)]
public static string Code { get; set; } = "";
```

### [ConfigColorPicker] — 颜色选择器

```csharp
[ConfigColorPicker(EditAlpha = true)]
public static Color AccentColor { get; set; } = new(1f, 0.5f, 0.2f);
```

---

## 完整示例

```csharp
using BaseLib.Config;
using Godot;

[ConfigHoverTipsByDefault]
public class MyModConfig : SimpleModConfig
{
    [ConfigSection("Gameplay")]
    public static bool EnableExtraRelics { get; set; } = true;

    [ConfigSlider(1, 10)]
    public static int ExtraRelicCount { get; set; } = 3;

    [ConfigSection("UI")]
    public static bool ShowDamagePreview { get; set; } = true;

    [ConfigColorPicker]
    public static Color HighlightColor { get; set; } = new(1f, 0.8f, 0.2f);

    [ConfigHideInUI]
    public static int InternalVersion { get; set; } = 1;
}

// 注册
ModConfigRegistry.Register("MyMod", new MyModConfig());
```

---

## 本地化

路径：`res://<模组ID>/localization/<语言代码>/settings_ui.json`

```json
{
  "MYMOD-MY_TOGGLE.title": "我的开关",
  "MYMOD-MY_SLIDER.title": "滑块值",
  "MYMOD-GENERAL_SETTINGS.title": "通用设置",
  "MYMOD-ADVANCED_SETTINGS.title": "高级设置",
  "MYMOD-ENABLE_EXTRA_OPTIONS.title": "启用额外选项",
  "MYMOD-EXTRA_OPTION.title": "额外选项"
}
```

键格式：`<MODID大写>-<属性名大写>.title`

