# 序列化与注册

> 参考：sts2-res 源码 + SKILL.md 硬规则

---

## 概述

STS2 的所有模型（卡牌/遗物/能力/药水等）必须注册到 `ModelDb` 才能被识别。序列化确保自定义属性在保存/加载/多人同步时数据不丢失。

---

## 注册时机

`ModEntry.Initialize` 三阶段顺序：

```
Harmony Patch → 模型注册 → 设置/配置
```

所有注册必须在 ModelDb 初始化完成前进行。如果错过了时机，用 `ModelDb.Inject(type)` 补救。

---

## 注册方式

### 方式1：ModHelper.AddModelToPool

```csharp
// 注册到指定池
ModHelper.AddModelToPool<IroncladRelicPool>(typeof(MyRelic));
ModHelper.AddModelToPool<ColorlessCardPool>(typeof(MyCard));
ModHelper.AddModelToPool<SharedRelicPool>(typeof(MyRelic));

// 池类型在运行时确定时
ModHelper.AddModelToPool(poolType, modelType);
```

### 方式2：ModelDb.Inject（错过时机后补救）

```csharp
// ModelDb 已初始化后注册
ModelDb.Inject(typeof(MyPower));
```

`ModelDb.Inject` 只注册模型 ID，不关联池。池关联需要额外的 Patch 或 `AddModelToPool`。

### 方式3：HarmonyPatch 注入

需要 Patch 特定 Getter 的模型（角色/事件/遭遇/Modifier）：

| 目标 | Patch 方法 |
|------|-----------|
| 角色 | `ModelDb.AllCharacters` getter |
| 卡池 | `ModelDb.AllCardPools` getter |
| 遗物池 | `ModelDb.AllRelicPools` getter |
| 药水池 | `ModelDb.AllPotionPools` getter |
| 事件 | `ActModel.AllEvents` getter |
| 遭遇 | `GenerateAllEncounters` |
| 先古之民 | `ActModel.AllAncients` getter |
| Modifier | `NCustomRunModifiersList.GetModifiersTickedOn` |

---

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

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 属性没保存 | 检查 `[SavedProperty]` + `InjectTypeIntoCache` |
| 模型不注册 | 检查注册时机 |
| `AddModelToPool` 泛型报错 | 用反射重载 `AddModelToPool(poolType, modelType)` |
| ModelDb 已初始化无法注册 | 用 `ModelDb.Inject(type)` |
| 卡池缓存不刷新 | 反射重置 `_allCards` 字段 |
| ModelId 冲突 | 类名全局唯一 |

---

## 演进路线

- 当前：手动注册 + 手动 `InjectTypeIntoCache`
- 更优：自定义 Attribute 扫描 + 反射自动注册
- BaseLib：`Custom*Model` 自带 `autoAdd` + `[Pool]` 属性自动关联池