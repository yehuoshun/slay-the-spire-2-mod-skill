# 自定义遗物：资源、注册与自动注册

## 本地化

路径：`res://<ModId>/localization/<语言代码>/relics.json`

```json
{
  "MY_CUSTOM_RELIC": {
    "title": "自定义遗物",
    "description": "每场战斗开始时，获得 {0} 点能量。",
    "flavor": "一段引文。"
  }
}
```

**遗物 ID 规则**：大驼峰类名 → 大写字母加下划线分割。如 `MyCustomRelic` → `MY_CUSTOM_RELIC`。

---

## 图标资源

### 文件结构

```
res://MyCustomMod/images/
├── relics/
│   ├── my_custom_relic.png          ← 大图标（256x256）
│   └── my_custom_relic_outline.png  ← 描边图标
├── atlases/
│   ├── relic_atlas.sprites/
│   │   └── my_custom_relic.tres     ← AtlasTexture 资源
│   └── relic_outline_atlas.sprites/
│       └── my_custom_relic.tres     ← 描边 AtlasTexture 资源
```

### 创建步骤

1. 在对应路径右键 → 新建 → 资源 → 搜索 `AtlasTexture` → 创建
2. 命名：`<小写遗物ID>.tres`
3. 双击资源 → 属性检查器 → 拖入 PNG 到 `Atlas`
4. 设置 `Region` 的 `w`/`h` 为图标大小（256x256）
5. 描边同样操作，路径在 `relic_outline_atlas.sprites/`

**回退**：若找不到 atlas 资源，游戏会尝试用大图标替换。

---

## 注册（ModEntry）

```csharp
[ModInitializer(nameof(Initialize))]
public static class MyCustomModInitializer
{
    private const string HarmonyId = "Author.MyCustomMod";
    private static Harmony? _harmony;

    public static void Initialize()
    {
        try
        {
            _harmony = new Harmony(HarmonyId);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            ModHelper.AddModelToPool<IroncladRelicPool, MyCustomRelic>();
        }
        catch (Exception e)
        {
            Log.Error("[MyCustomMod] 加载失败");
            Log.Error(e.ToString());
        }
    }
}
```

---

## 进阶：纯原生自动注册

> 从 BaseLib 提炼，零第三方依赖。用 `[RelicPool]` 标记 + ContentRegistry 反射扫描，免去每个遗物手动 `AddModelToPool`。
> 框架完整代码见 [serialization.md](../serialization/serialization.md)「进阶：纯原生自动注册框架」。

```csharp
[RelicPool(typeof(SharedRelicPool))]   // 标记进哪个遗物池
public class MyCustomRelic : RelicModel { ... }

[RelicPool(typeof(IroncladRelicPool))]
public class MyIroncladRelic : RelicModel { ... }
```

- Starter / Event / Ancient 稀有度不走随机池，仍需 Patch 或事件发放（见上方「修改角色初始遗物」）
- 自定义 Attribute 定义 + ContentRegistry 扫描代码见 serialization.md，本模块只需加 `[RelicPool]` 标记

