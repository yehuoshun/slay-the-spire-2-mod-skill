# 设置界面

> 两种方案：依赖 ModConfig 框架（推荐）或纯自研零 Harmony（学 ShunMod）。

---

## 方案一：依赖 ModConfig 框架（推荐）

ModConfig 是社区标准设置框架，零 Harmony + 双向绑定 + 持久化 debounce。

### 支持的控件

| 控件 | ConfigType | 说明 |
|------|-----------|------|
| Toggle | `ConfigType.Toggle` | 开关 |
| Slider | `ConfigType.Slider` | 滑块 |
| Dropdown | `ConfigType.Dropdown` | 下拉选择 |
| KeyBind | `ConfigType.KeyBind` | 按键绑定 |
| TextInput | `ConfigType.TextInput` | 文本输入 |
| ColorPicker | `ConfigType.ColorPicker` | 颜色选择器 |
| Button | `ConfigType.Button` | 按钮 |
| Header | `ConfigType.Header` | 标题分隔 |
| Separator | `ConfigType.Separator` | 分隔线 |

### 注册配置

```csharp
// 在 ModEntry.Initialize 中
ModConfigApi.Register(
    modId: "MyMod",
    displayName: "我的模组",
    entries: new ConfigEntry[]
    {
        new ConfigEntry
        {
            Key = "enable_feature",
            Label = "启用功能",
            Type = ConfigType.Toggle,
            DefaultValue = true,
            OnChanged = value => MyConfig.EnableFeature = (bool)value
        },
        new ConfigEntry
        {
            Key = "damage_multiplier",
            Label = "伤害倍率",
            Type = ConfigType.Slider,
            DefaultValue = 1.0f,
            Min = 0.5f,
            Max = 3.0f,
            Step = 0.1f,
            Format = "F1",
            OnChanged = value => MyConfig.DamageMultiplier = (float)value
        },
        new ConfigEntry
        {
            Key = "difficulty",
            Label = "难度",
            Type = ConfigType.Dropdown,
            DefaultValue = "普通",
            Options = new[] { "简单", "普通", "困难", "地狱" },
            OnChanged = value => MyConfig.Difficulty = (string)value
        }
    }
);
```

### 多语言支持

```csharp
new ConfigEntry
{
    Key = "enable_feature",
    Label = "Enable Feature",
    Labels = new Dictionary<string, string>
    {
        ["zhs"] = "启用功能",
        ["eng"] = "Enable Feature"
    },
    Description = "Toggle this feature on/off",
    Descriptions = new Dictionary<string, string>
    {
        ["zhs"] = "开启或关闭此功能",
        ["eng"] = "Toggle this feature on/off"
    },
    Type = ConfigType.Toggle,
    DefaultValue = true
}
```

---

## 方案二：自研零 Harmony 方案（学 ShunMod）

如果不想依赖 ModConfig，可以自己写。核心思路：

1. **零 Harmony**：用 Godot 的 `SceneTree.NodeAdded` 事件检测 `NSettingsTabManager` 创建
2. **Duplicate 模板**：复制游戏的第一个设置标签页，清空后填充自己的 UI
3. **双向绑定**：`SettingsManager.ValueChanged` 事件 + WeakReference 绑定
4. **持久化 debounce**：`ProcessFrame` 延迟批量写 JSON

### 架构

```
SettingsManager    ← 持久化（JSON 文件，按 key 分文件）
    ↓ ValueChanged 事件
SettingsUI         ← UI 注入（Duplicate + 填充 Godot 控件）
    ↓ 双向绑定
LiveBinding        ← WeakReference 绑定控件 ↔ 配置值
```

### 核心代码骨架

```csharp
// SettingsManager: 持久化
internal static class SettingsManager
{
    private const string ConfigDir = "user://MyMod/";
    private static readonly Dictionary<string, object> _values = new();
    internal static event Action<string, object>? ValueChanged;

    internal static T GetValue<T>(string key, T fallback) { ... }
    internal static void SetValue(string key, object value)
    {
        _values[key] = value;
        ValueChanged?.Invoke(key, value);
        ScheduleSave(key); // debounce
    }
}

// SettingsUI: UI 注入
internal static class SettingsUI
{
    internal static void Initialize(ConfigEntry[] entries)
    {
        SettingsManager.ValueChanged += OnSettingChanged;
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.NodeAdded += OnNodeAdded; // 检测 NSettingsTabManager
    }

    private static void OnNodeAdded(Node node)
    {
        if (node is not NSettingsTabManager tabManager) return;
        InjectTab(tabManager); // Duplicate + 填充
    }
}
```

---

## 配置持久化格式

每个 key 一个 JSON 文件，放在 `user://ModName/` 下：

```
user://MyMod/
├── enable_feature.json    → true
├── damage_multiplier.json → 1.5
└── difficulty.json        → "困难"
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 设置不保存 | 检查文件路径和写入权限 |
| UI 不显示 | 确认 `NSettingsTabManager` 检测正确 |
| 控件值不更新 | 检查双向绑定是否注册 |
| 多个设置面板冲突 | 用 `WeakReference` 追踪所有实例 |