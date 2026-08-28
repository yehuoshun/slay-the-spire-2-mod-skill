# 设置界面：基础使用

## 基础使用

```csharp
using BaseLib.Config;

// 继承 SimpleModConfig，添加静态属性即可
public class MyConfig : SimpleModConfig
{
    public static bool MyToggle { get; set; } = true;

    [ConfigSlider(0, 100)]
    public static float MySlider { get; set; } = 50f;

    public static string MyText { get; set; } = "默认值";
}
```

### 注册

```csharp
// 在 ModEntry.Initialize 中注册
var config = new MyConfig();
ModConfigRegistry.Register("MyModId", config);
```

