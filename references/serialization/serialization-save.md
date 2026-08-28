# 序列化与注册：序列化与兼容

## 序列化

### [SavedProperty] — 标记可保存属性

```csharp
public class MyRelic : RelicModel
{
    [SavedProperty]
    public int TimesUsed { get; set; }

    [SavedProperty]
    public string? LastTarget { get; set; }

    [SavedProperty]
    public bool IsEnhanced { get; set; }
}
```

### InjectTypeIntoCache — 注册序列化类型

```csharp
// 在 ModEntry.Initialize 中调用
SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MyRelic));
```

**漏了这步，自定义属性不会保存/加载，读档后数据丢失。**

### 支持的属性类型

`int` / `string` / `bool` / `decimal` / `float` / 枚举 / `ModelId` / `List<T>` / `Dictionary<K,V>`

---

## 序列化兼容

### 存档兼容

- 增删属性：旧数据自动忽略，新属性用默认值
- 类型变更：可能导致反序列化异常，尽量不改类型

### 缓存刷新

运行时注册新模型后，需刷新缓存：

```csharp
// 卡池缓存
var field = typeof(CardPoolModel).GetField("_allCards",
    BindingFlags.NonPublic | BindingFlags.Instance);
field?.SetValue(cardPool, null);

// ModelDb 缓存
var field2 = typeof(ModelDb).GetField("_allCards",
    BindingFlags.NonPublic | BindingFlags.Static);
field2?.SetValue(null, null);
```

