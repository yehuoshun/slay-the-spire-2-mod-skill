# 纯原生设计模式：便捷 override 与内联本地化

## 模式 3：便捷 override（默认实现）

### BaseLib 的做法

- `CustomCardModel.GainsBlock` 自动从 `DynamicVars` 推断是否给格挡
- 图标路径带 `ResourceLoader.Exists()` 回退检查

### 纯原生转译

```csharp
// 手动 override 原生虚属性（BaseLib 只是帮你自动算，纯原生手写即可）
public override bool GainsBlock => true;   // 该卡给格挡

// 图标路径回退辅助
public static class PathFallback
{
    public static string FirstExisting(params string[] paths)
        => paths.FirstOrDefault(ResourceLoader.Exists) ?? paths.Last();
}

// 用法
public override string BigIconPath =>
    PathFallback.FirstExisting(
        "res://MyMod/images/relics/my_relic.png",
        "res://MyMod/images/relics/fallback.png");
```

### 关键点

- BaseLib 的"自动推断"本质是**帮助计算一个原生 override**，纯原生手动写即可
- 回退逻辑用 `ResourceLoader.Exists()`（Godot 原生）+ LINQ 一行实现

---

## 模式 4：内联本地化（转译 CardLoc）

### BaseLib 的做法

`Localization => new CardLoc("MyCard").WithName(...).WithDescription(...)` 在代码里内联定义本地化。

### 纯原生转译

原生用 JSON 文件本地化（`res://<ModId>/localization/<语言>/cards.json`），这是标准做法，不需要内联。

如果想让 key 管理更稳，可加一个常量辅助：

```csharp
public static class LocKeys
{
    public const string MyCard = "MyCard";
    // 集中管理所有本地化 key，避免手写字符串拼错
}
```

**结论：本地化保持原生 JSON 方案即可，BaseLib 的内联写法没有纯原生优势，不转译。**


### v3.4.8 增量：通用取值 / 悬停标题 / 注册时机

- **任意模型取动态变量**：`GetDynamicVarSet(this AbstractModel)` 覆盖 Card/Relic/Power/**Potion/Enchantment/Event**，不支持时返回 `null`；`GetDynamicVar(model, key)` 改为 `GetDynamicVarSet()?[key] ?? throw`。写通用工具先判空——「不支持」的唯一信号是 null，不是异常。
- **悬停标题带实时数值**：`WithTooltip(locKey)` 现在优先查 `<key>.smartTitle`（可写 `"流血 {Bleed}"` 这种带格式化的标题），缺了才回落 `<key>.title`；描述同理优先 `.smartDescription`。缺键静默回落，不报错。
- **药水也吃动态变量提示**：`PotionModel.HoverTips` 的 getter Postfix 已把 `DynamicVars` 里带 tooltip 的变量挂上，模组侧不要再重复加。
- **球模型的悬停提示**：`TooltipSource` 支持 `OrbModel`，走 `DumbHoverTip`（静态描述，不含实例数值）。
- **自定义网络消息**：接收 handler 改在网络服务构造时注册，lobby 里发的消息也能收到。
