# ModConfig 接入：零依赖反射桥（ModConfigBridge）

> 来源：ModConfig-STS2 `examples/ModConfigBridge.cs`（v0.2.2）。**复制模板 → 改命名空间/modId → 编辑 `BuildEntries()` → `Initialize()` 里调 `DeferredRegister()`**，完事。

## 三步接入

```csharp
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace YourMod;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Godot.Node
{
    internal const string ModId = "your.mod.id";

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        ModConfigBridge.DeferredRegister(); // ← 唯一新增一行
    }
}
```

## 反射桥核心逻辑

```csharp
// 1. 延迟注册：等 1 帧（mod 可能比 ModConfig 先加载，字母序）
internal static void DeferredRegister()
{
    var tree = (SceneTree)Engine.GetMainLoop();
    tree.ProcessFrame += OnNextFrame;
}

// 2. 检测：AppDomain 全量扫类型，找 3 个类型全命中才算可用
_apiType       = allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ModConfigApi");
_entryType     = allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ConfigEntry");
_configTypeEnum= allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ConfigType");
_available = _apiType != null && _entryType != null && _configTypeEnum != null;

// 3. 注册：4 参重载优先（带双语显示名），找不到再退 3 参
var registerMethod = _apiType.GetMethods(BindingFlags.Public | BindingFlags.Static)
    .Where(m => m.Name == "Register")
    .OrderByDescending(m => m.GetParameters().Length).First();
registerMethod.Invoke(null, new object[] { "your.mod.id", displayNames["en"], displayNames, entries });
```

## 运行时读写

```csharp
// 读：ModConfig 没装时返回 fallback
bool  enabled = ModConfigBridge.GetValue("featureEnabled", true);
float speed   = ModConfigBridge.GetValue("speedMultiplier", 1.0f);

// 写：mod 在 UI 之外改了设置（热键/自建菜单）时同步回去保证持久化
ModConfigBridge.SetValue("featureEnabled", false);
```

```csharp
// GetValue 内部：MakeGenericMethod 反射调用
_apiType.GetMethod("GetValue", BindingFlags.Public | BindingFlags.Static)
    ?.MakeGenericMethod(typeof(T))
    ?.Invoke(null, new object[] { "your.mod.id", key });
```

## 配置项声明（BuildEntries 内）

```csharp
list.Add(Entry(cfg =>
{
    Set(cfg, "Key", "myToggle");
    Set(cfg, "Label", "My Feature");
    Set(cfg, "Labels", L("My Feature", "我的功能"));        // 双语标签
    Set(cfg, "Type", EnumVal("Toggle"));                     // 枚举名，反射解析
    Set(cfg, "DefaultValue", (object)true);
    Set(cfg, "Description", "Enable this feature");
    Set(cfg, "Descriptions", L("Enable this feature", "启用此功能"));
    Set(cfg, "OnChanged", new Action<object>(v => { /* 应用设置 */ }));
}));
```

## ConfigEntry 属性速查

| 属性 | 类型 | 用于 |
|------|------|------|
| `Key` | string | 持久化唯一键（Header/Separator 不需要） |
| `Label` / `Labels` | string / Dict | 显示文本 / 双语 `{"en","zhs"}`，渲染时解析，回退 Label |
| `Description` / `Descriptions` | string / Dict | 控件下方说明 / 双语 |
| `Type` | ConfigType | 控件类型 |
| `DefaultValue` | object | Toggle=bool, Slider=float, Dropdown=string, KeyBind=long, TextInput=string, ColorPicker=`"#RRGGBB"` |
| `Min`/`Max`/`Step`/`Format` | float | Slider：范围/步长/显示格式（`F0`/`F1`/`P0`） |
| `Options` / `OptionsKeys` | string[] | Dropdown 选项 / 选项双语键（等长） |
| `MaxLength` / `Placeholder` | int / string | TextInput：最大长度（默认 64）/ 占位符 |
| `ButtonText` / `ButtonTexts` | string / Dict | Button：按钮文字（与 Label 分离） |
| `Validator` | Func\<object,bool\> | TextInput 校验，false 红框 |
| `OnChanged` | Action\<object\> | 值变化回调（Toggle/Slider/Dropdown/KeyBind/TextInput/Button/ColorPicker） |

## ConfigType 枚举（值序，v0.2.2）

`Toggle=0, Slider=1, Dropdown=2, KeyBind=3, TextInput=4, Header=5, Separator=6, Button=7, ColorPicker=8`

- Header / Separator / Button **不持久化**（Reset 也跳过它们）
- 反射侧取枚举：`Enum.Parse(_configTypeEnum, "Toggle")`

## 反射辅助（模板自带，勿改）

`Entry(Action<object>)`=Activator.CreateInstance+configure；`Set(obj,name,value)`=反射写属性；`L(en,zhs)`=双语 dict；打包用 `Array.CreateInstance(_entryType!, count)` 转强类型数组再 Invoke。
