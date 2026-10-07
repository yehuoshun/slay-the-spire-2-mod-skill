# ModConfig 内部机制（注入 / 渲染 / 持久化 / KeyBind / i18n）

> 来源：ModConfig-STS2 `Scripts/`（v0.2.2）逐行精读。**本文件 = 纯原生转译素材**：完整可编译转译示例见 sts2-mod-examples `Settings/ModsTabInjector.cs`（设置页 Tab 注入核心，CI 真编译验证）。纯接入方只需看 modconfig-integration.md。

## 1. Settings「Mods」Tab 注入（核心 hack）

```csharp
// 监听场景树：任何 NSettingsTabManager 进树 → ready(OneShot) → 注入
var tree = (SceneTree)Engine.GetMainLoop();
tree.NodeAdded += OnNodeAdded;   // node is NSettingsTabManager 且无 "Mods" 子节点才动手

// 注入流程（InjectModsTab）：
// ① 反射读私有字段 _tabs（IDictionary: NSettingsTab → NSettingsPanel）
var tabsField = typeof(NSettingsTabManager).GetField("_tabs", BindingFlags.NonPublic | BindingFlags.Instance);
// ② 克隆第一个 tab → Name="Mods" → SetLabel(双语) → Deselect() → AddChild
var modsTab = (NSettingsTab)firstTab.Duplicate();
// ③ 克隆第一个 panel → 清理子节点（只留 Content 容器）
//    ⚠️ 必须在 AddChild 前清理：游戏内部节点（NDropdownPositioner 等）_Ready 时
//       引用原 panel 控件 → AddChild 即抛 ObjectDisposedException
// ④ 注册进 _tabs: tabs.Add(modsTab, modsPanel)
// ⑤ 点击切换: modsTab.Connect(NClickableControl.SignalName.Released,
//        → tabManager.Call("SwitchTabTo", modsTab))
// ⑥ 高度封顶：NSettingsPanel.RefreshSize 会撑爆面板（ScrollContainer 失效）→
//       固定 Size.Y = firstPanel.Size.Y + 重写 viewport resize 时的 RefreshSize
// ⑦ 字体：CacheGameFont(firstPanel) → ApplyGameFont(label) 复制游戏字体
// ⑧ PopulateInto(容器) + RebuildFocusTargets(收集可聚焦控件，手柄导航)
```

位置处理 `PositionNewTab`：按间距 `tabs[1].X - tabs[0].X` 放到末尾；超出 TabManager 宽度时按 `Size.X / totalTabs` 等分重排所有 tab。

## 2. 控件渲染（PopulateInto）

ScrollContainer（禁横向滚动）→ 每 mod 一个分组：`AddModHeaderWithReset`（标题 + 折叠 + Reset 按钮）+ 行分隔线，再逐个渲染条目：

| 控件 | Godot 控件 | 信号 | 备注 |
|------|-----------|------|------|
| Toggle | `CheckButton` | `Toggled` | `ButtonPressed` 回填 |
| Slider | `HSlider` + 数值 `Label` | `ValueChanged` | `Format` 格式化（P0 百分比） |
| Dropdown | `OptionButton` | `ItemSelected` | 选项 `AddItem(ResolveDropdownOption)`；双语走 OptionsKeys |
| KeyBind | `Button` | `Pressed` | 见 §4 |
| TextInput | `LineEdit` | 文本变化 | `MaxLength`/`Placeholder`；Validator false → 红边框 |
| Button | `Button` | `Pressed` | 动作按钮，不存值 |
| ColorPicker | `ColorPickerButton` + hex `Label` | `ColorChanged` | `EditAlpha=false`；hex `#RRGGBB` 大写显示 |
| Header | Label 分组标题 | — | 视觉分组 |
| Separator | 分隔线 | — | 视觉分隔 |

- **LiveBinding 机制**：`(modId, key)` → `List<Func<object,bool>>` 应用回调；`SetValue` 时只刷绑定控件，不整树重建；回调返回 false（控件已销毁）即移除
- **UiUpdateGuard**：ColorPicker/Slider 等「UI 改值 → SetValue → LiveBinding 回写 UI」防回环（guard.Suppress 置位跳过）

## 3. 持久化（ModConfigManager）

- 路径：`user://ModConfig/<modId>.json`（System.Text.Json，缩进写入）；UI 折叠状态 `user://ModConfig/_ui_state.json`
- **防抖保存**：改值 → 进 `_dirtyMods` → 挂 `tree.ProcessFrame += FlushSaves` 下帧统一 flush（滑条拖动只写一次）
- 加载按类型反序列化：Toggle→`GetBoolean()`、Slider→`GetDouble()`→float、KeyBind→`GetInt64()`、Dropdown/TextInput/ColorPicker→`GetString()`；损坏字段回退默认值
- `GetValue<T>`：值表优先 → 注册表 DefaultValue → `default!` 三级回退，`Convert.ChangeType` 兜底
- `ResetToDefaults`：遍历非 Header/Separator/Button 条目重置默认值 + 触发 OnChanged + 防抖保存；返回是否真有变更
- ColorPicker 默认值归一化：`Color.FromHtml` → `#RRGGBB` 大写（`NormalizeColorHexOrDefault`）

## 4. KeyBind 捕获（KeyCaptureNode）

- 临时 Node 挂 `Root`，`_UnhandledKeyInput` 捕获单键；`_Ready` 里 `SetProcessUnhandledKeyInput(true)`
- Esc → `OnKeyCaptured(0)`（=Unbound 显示「Unbound」）；单独修饰键（Ctrl/Shift/Alt/Meta）忽略
- 组合键：`keyEvent.GetKeycodeWithModifiers()` 返回 `long`（含修饰位），`OS.GetKeycodeString((Key)keyCode)` 解码显示
- 鼠标取消：`ProcessFrame` 后武装（避免吞掉打开瞬间的点击），任意鼠标键点击取消（滚轮除外）；自毁（QueueFree）
- 捕获中按钮显示「Press any key...」金色；切换捕获前先恢复旧值

## 5. i18n（I18n.cs）

- **双通道加载**：DLL 内嵌资源流（`ModConfig.localization.{lang}.json`）→ 回退 PCK（`res://ModConfig/localization/{lang}.json`）；语言候选链：精确 → 去 region 后缀 → `en`
- **语言检测**：`LocManager.Instance.Language` 优先 → `TranslationServer.GetLocale()` 回退
- **变化订阅**：`LocManager.Instance.SubscribeToLocaleChange(OnLocaleChanged)` → 重载翻译 + `Changed` 事件（Mods tab 标签 + 全 UI 刷新）
- 代码归一化：`zh*→zhs`、`en*→en`、`ja→jpn`、`ko→kor`，其余三字母码直通
- 模组显示名双语：`DisplayNames` dict，`GetLocalizedName` 精确 → 前缀 → 回退英文
