# 模式十二：设置界面 — 学 ModConfig 架构自己实现

> 学习 [xhyrzldf/ModConfig-STS2](https://github.com/xhyrzldf/ModConfig-STS2) 的核心设计，自己造轮子。
> 关键设计：零 Harmony 注入、延迟注册、双向绑定、持久化 debounce。

---

## 核心架构

```
SceneTree.NodeAdded 信号
    │
    ▼
检测 NSettingsTabManager 创建
    │
    ▼
Duplicate 现有 Tab/Panel → 注入自定义标签页
    │
    ▼
Populate UI（从 ConfigEntry[] 渲染控件）
    │
    ▼
LiveBinding 双向绑定（SetValue → 自动更新 UI）
```

---

## 设计要点

### 1. 零 Harmony 注入标签页

不用 `[HarmonyPatch]` Patch 游戏代码，用 Godot 原生信号：

```csharp
internal static void Initialize()
{
    var tree = (SceneTree)Engine.GetMainLoop();
    tree.NodeAdded += OnNodeAdded;
}

private static void OnNodeAdded(Node node)
{
    if (node is not NSettingsTabManager) return;
    if (node.GetNodeOrNull("Mods") != null) return; // 已注入

    // 等 ready 后再操作（子节点已就绪）
    node.Connect("ready",
        Callable.From(() => InjectTab((NSettingsTabManager)node)),
        (uint)GodotObject.ConnectFlags.OneShot);
}
```

### 2. Duplicate 现有 Tab/Panel

```csharp
private static void InjectTab(NSettingsTabManager tabManager)
{
    // 获取现有 _tabs 字典
    var tabsField = typeof(NSettingsTabManager).GetField("_tabs",
        BindingFlags.NonPublic | BindingFlags.Instance);
    var tabs = tabsField.GetValue(tabManager) as IDictionary;

    // 取第一个 Tab/Panel 作为模板
    NSettingsTab firstTab = null;
    NSettingsPanel firstPanel = null;
    foreach (DictionaryEntry entry in tabs)
    {
        firstTab = entry.Key as NSettingsTab;
        firstPanel = entry.Value as NSettingsPanel;
        break;
    }

    // Duplicate Tab
    var myTab = (NSettingsTab)firstTab.Duplicate();
    myTab.Name = "MyMod";
    myTab.SetLabel("My Mod");
    tabManager.AddChild(myTab);

    // Duplicate Panel → 清空子控件 → 作为容器
    var myPanel = (NSettingsPanel)firstPanel.Duplicate();
    myPanel.Name = "MyModSettings";
    myPanel.Visible = false;

    // 找到 Content 容器，清空
    var content = myPanel.Content;
    foreach (var child in content.GetChildren().ToArray())
    {
        content.RemoveChild(child);
        child.Free();
    }

    firstPanel.GetParent().AddChild(myPanel);

    // 注册到 _tabs
    tabs.Add(myTab, myPanel);

    // 连接点击事件
    myTab.Connect(NClickableControl.SignalName.Released,
        Callable.From<NButton>(_ => tabManager.Call("SwitchTabTo", myTab)));

    // 填充 UI
    PopulateUI(content);
}
```

### 3. 延迟注册（处理加载时序）

模组按字母序加载，你的 mod 可能比依赖的其他 mod 先加载。延迟一帧：

```csharp
internal static void DeferredInit(Action onReady)
{
    var tree = (SceneTree)Engine.GetMainLoop();
    tree.ProcessFrame += OnFrame;

    void OnFrame()
    {
        tree.ProcessFrame -= OnFrame;
        onReady();
    }
}
```

### 4. 双向绑定（LiveBinding）

`SetValue` 后自动更新 UI，不用手动刷新：

```csharp
private sealed class LiveBinding
{
    public Func<object, bool> Apply { get; }
    public LiveBinding(Func<object, bool> apply) => Apply = apply;
}

private static readonly Dictionary<(string Key), List<LiveBinding>> _bindings = new();

internal static void NotifyValueChanged(string key, object value)
{
    if (!_bindings.TryGetValue(key, out var list)) return;
    for (int i = list.Count - 1; i >= 0; i--)
    {
        if (!list[i].Apply(value))
            list.RemoveAt(i); // 控件已销毁，清理绑定
    }
}

// 创建控件时注册绑定
private static void RegisterBinding(string key, LiveBinding binding)
{
    if (!_bindings.ContainsKey(key))
        _bindings[key] = new List<LiveBinding>();
    _bindings[key].Add(binding);
}
```

### 5. 持久化设计

```csharp
private const string ConfigDir = "user://MyModConfig/";
private static readonly HashSet<string> _dirtyKeys = new();
private static bool _saveScheduled;

// 按 key 分文件存 JSON
internal static T GetValue<T>(string key, T fallback)
{
    var path = ConfigDir + key + ".json";
    if (!FileAccess.FileExists(path)) return fallback;
    try
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        return JsonSerializer.Deserialize<T>(file.GetAsText());
    }
    catch { return fallback; }
}

internal static void SetValue(string key, object value)
{
    _values[key] = value;
    NotifyValueChanged(key, value);
    ScheduleSave(key);
}

// Debounce：合并到下一帧批量写
private static void ScheduleSave(string key)
{
    _dirtyKeys.Add(key);
    if (_saveScheduled) return;
    _saveScheduled = true;

    var tree = (SceneTree)Engine.GetMainLoop();
    tree.ProcessFrame += FlushSaves;
}

private static void FlushSaves()
{
    var tree = (SceneTree)Engine.GetMainLoop();
    tree.ProcessFrame -= FlushSaves;
    _saveScheduled = false;

    foreach (var key in _dirtyKeys)
    {
        var path = ConfigDir + key + ".json";
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        file.StoreString(JsonSerializer.Serialize(_values[key]));
    }
    _dirtyKeys.Clear();
}
```

---

## 控件渲染

### Toggle（开关）

```csharp
private static void AddToggle(VBoxContainer parent, ConfigEntry entry)
{
    var hbox = new HBoxContainer();
    var label = new Label { Text = entry.Label };
    var tickbox = new NTickbox
    {
        ButtonPressed = GetValue<bool>(entry.Key),
        FocusMode = Control.FocusModeEnum.All
    };

    var guard = new UiUpdateGuard(); // 防止循环更新
    var tickboxRef = new WeakReference<NTickbox>(tickbox);

    RegisterBinding(entry.Key, new LiveBinding(value =>
    {
        if (!tickboxRef.TryGetTarget(out var tb) || !GodotObject.IsInstanceValid(tb))
            return false;
        guard.Suppress = true;
        tb.ButtonPressed = Convert.ToBoolean(value);
        guard.Suppress = false;
        return true;
    }));

    tickbox.Toggled += pressed =>
    {
        if (guard.Suppress) return;
        SetValue(entry.Key, pressed);
    };

    hbox.AddChild(label);
    hbox.AddChild(tickbox);
    parent.AddChild(hbox);
}
```

### Slider（滑条）

```csharp
private static void AddSlider(VBoxContainer parent, ConfigEntry entry)
{
    var hbox = new HBoxContainer();
    var label = new Label { Text = entry.Label };
    var slider = new HSlider
    {
        MinValue = entry.Min,
        MaxValue = entry.Max,
        Step = entry.Step,
        Value = GetValue<float>(entry.Key),
        FocusMode = Control.FocusModeEnum.All
    };
    var valueLabel = new Label
    {
        Text = slider.Value.ToString(entry.Format ?? "F0")
    };

    var guard = new UiUpdateGuard();
    var sliderRef = new WeakReference<HSlider>(slider);
    var labelRef = new WeakReference<Label>(valueLabel);

    RegisterBinding(entry.Key, new LiveBinding(value =>
    {
        if (!sliderRef.TryGetTarget(out var s) || !GodotObject.IsInstanceValid(s))
            return false;
        guard.Suppress = true;
        s.Value = Convert.ToSingle(value);
        guard.Suppress = false;
        if (labelRef.TryGetTarget(out var l) && GodotObject.IsInstanceValid(l))
            l.Text = s.Value.ToString(entry.Format ?? "F0");
        return true;
    }));

    slider.ValueChanged += v =>
    {
        valueLabel.Text = v.ToString(entry.Format ?? "F0");
        if (guard.Suppress) return;
        SetValue(entry.Key, v);
    };

    hbox.AddChild(label);
    hbox.AddChild(slider);
    hbox.AddChild(valueLabel);
    parent.AddChild(hbox);
}
```

### Dropdown（下拉框）

```csharp
private static void AddDropdown(VBoxContainer parent, ConfigEntry entry)
{
    var hbox = new HBoxContainer();
    var label = new Label { Text = entry.Label };
    var dropdown = new NOptionButton { FocusMode = Control.FocusModeEnum.All };

    foreach (var opt in entry.Options)
        dropdown.AddItem(opt);

    var current = GetValue<string>(entry.Key);
    for (int i = 0; i < entry.Options.Length; i++)
    {
        if (entry.Options[i] == current)
        {
            dropdown.Select(i);
            break;
        }
    }

    dropdown.ItemSelected += index =>
    {
        SetValue(entry.Key, entry.Options[index]);
    };

    hbox.AddChild(label);
    hbox.AddChild(dropdown);
    parent.AddChild(hbox);
}
```

---

## 完整示例：补丁开关系统

```csharp
// ConfigEntry 定义
internal class ConfigEntry
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public ConfigEntryType Type { get; set; }
    public object DefaultValue { get; set; } = false;
    public float Min { get; set; }
    public float Max { get; set; } = 100f;
    public float Step { get; set; } = 1f;
    public string Format { get; set; } = "F0";
    public string[] Options { get; set; } = Array.Empty<string>();
    public Action<object>? OnChanged { get; set; }
}

internal enum ConfigEntryType { Toggle, Slider, Dropdown }

// 定义配置
internal static ConfigEntry[] GetConfigEntries() => new[]
{
    new ConfigEntry
    {
        Key = "infiniteUpgrade", Label = "无限升级",
        Type = ConfigEntryType.Toggle, DefaultValue = true,
        OnChanged = v => PatchManager.SetEnabled("InfiniteUpgrade", (bool)v)
    },
    new ConfigEntry
    {
        Key = "blockRetention", Label = "格挡保留",
        Type = ConfigEntryType.Toggle, DefaultValue = true,
        OnChanged = v => PatchManager.SetEnabled("BlockRetention", (bool)v)
    },
    new ConfigEntry
    {
        Key = "damageMultiplier", Label = "伤害倍率",
        Type = ConfigEntryType.Slider, DefaultValue = 1.0f,
        Min = 0.5f, Max = 3.0f, Step = 0.1f, Format = "F1",
        OnChanged = v => PatchManager.DamageMultiplier = (float)v
    }
};

// 在 Initialize() 中
SettingsUI.Initialize();       // 注册 NodeAdded 信号
SettingsUI.SetEntries(GetConfigEntries());
PatchManager.LoadFromSettings(); // 加载初始值
```

---

## 避坑指南

| 问题 | 原因 | 解决 |
|------|------|------|
| 标签页不显示 | 注入时机太早，子节点未就绪 | 用 `Connect("ready", ..., OneShot)` |
| 控件值不更新 | 没有双向绑定 | LiveBinding 机制 |
| 存档丢失 | 没有持久化 | JSON 文件存 `user://` |
| 滑块疯狂写盘 | 每次 ValueChanged 都写文件 | Debounce 合并到下一帧 |
| 重复注入 | NodeAdded 多次触发 | 检查 `GetNodeOrNull("MyMod")` |
| 循环更新 | SetValue → OnChanged → 更新 UI → ValueChanged → SetValue... | UiUpdateGuard 标记 |
| 控件销毁后绑定残留 | WeakReference 失效 | LiveBinding.Apply 返回 false 时清理 |
| 主菜单和暂停菜单各一个实例 | 两个 NSettingsTabManager | 用 `List<WeakReference<VBoxContainer>>` 追踪所有容器 |
