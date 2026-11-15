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

### 注册序列化类型（仅 ≤0.112 需要）

**0.113 起不用做了**：游戏自己扫描 mod 程序集加载类型，`SavedPropertiesTypeCache` 这个类在 0.113 里已整个删除（缓存并入 `ModelIdSerializationCache` 并转私有，mod 侧没有公开入口）。按 0.113 编译时写这行直接 CS0103。

**≤0.112 必须做，漏了自定义属性不会保存/加载，读档后数据丢失：**

```csharp
// 在 ModEntry.Initialize 中调用（仅在目标游戏版本 ≤ 0.112 时编译/执行）
SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MyRelic));
```

兼容两种版本的标准写法是给这行套版本条件（自定义 `#if` 符号由 csproj 按目标 dll 定义，或运行时用 `AccessTools.TypeByName("…SavedPropertiesTypeCache")` 判空后再反射调用）：类型找不到 = 新版游戏，跳过即可。

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

---

## 跨战斗持久化（卡牌多次打出）

> 实战项目验证（YuWanCard）。「永久升级/累计次数」类卡牌的标准模式：`[SavedProperty]` 存数据 + `BaseReplayCount` 允许一回合多次打出 + `DeckVersion` 同步回牌组规范实例。

```csharp
public class MyCard : CardModel
{
    [SavedProperty]
    public int MyPrefix_ReplayCount { get; set; }   // 属性名加模组前缀，避免冲突警告

    static MyCard() { SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MyCard)); }

    protected override void AfterDeserialized()
    {
        base.AfterDeserialized();
        BaseReplayCount = MyPrefix_ReplayCount;   // 读档恢复
    }

    // OnPlay 里：效果后同步回牌组
    if (DeckVersion is MyCard deckCard)
    {
        deckCard.MyPrefix_ReplayCount += 1;
        deckCard.BaseReplayCount = deckCard.MyPrefix_ReplayCount;
    }
}
```

**要点**：`DeckVersion` 是牌组中的规范实例，改它才能跨战斗持久化（战斗内实例是副本）；`BaseReplayCount` 表示可额外打出次数。


### 读缺值：[SavedProperty] 里 struct 的 `FirstOrDefault` 判不出「没找到」

存档读回时按名字在 `props.ints` / `props.bools` / `props.strings` 等列表里查找。对 **struct 元素**用 `FirstOrDefault(p => p.name == name)`，找不到时返回的是 `default`（各字段为空/null），于是「键不存在」被当成「存在且值为 0/false」——玩家层数、开关被静默清空，且没有任何报错。

正确写法是给 struct 一个「找不到就返回 null」的重载：

```csharp
static TSource? FirstOrNullStruct<TSource>(this IEnumerable<TSource> src, Func<TSource, bool> pred)
    where TSource : struct
{
    foreach (var item in src) if (pred(item)) return item;
    return null;
}
// 调用处
var found = props.ints?.FirstOrNullStruct(p => p.name == name);
if (found == null) return false;
```

数组类字段还要多判一层元素本身是否为 null（`found == null || found.Value.value == null`）。

写侧提醒：`[SavedProperty]` 的类型被删/改后，老存档里那个键会留在原处，读的时候按**名字**找不到就跳过即可，不要按位置索引（位置会随类型增删漂移）。
