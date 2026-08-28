# 序列化与注册：注册时机与方式

## 注册时机

`ModEntry.Initialize` 三阶段顺序：

```
Harmony Patch → 模型注册 → 设置/配置
```

所有注册必须在 ModelDb 初始化完成前进行。如果错过了时机，用 `ModelDb.Inject(type)` 补救。

---

## 注册方式

**推荐（硬规则4-①）：attribute + ContentRegistry 自动注册**

见 [design-patterns.md](../baselib/design-patterns.md)「模式 1」：用 `[CardPool(typeof(ColorlessCardPool))]` / `[RelicPool(typeof(SharedRelicPool))]` 标记模型类，在 `ModEntry.Initialize` 里调一次 `ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly())` 批量扫描注册，无需逐个 `AddModelToPool`。

以下是手动 / 补救方式（硬规则4-②）：

### 方式1：ModHelper.AddModelToPool

```csharp
// 泛型（编译期确定池与模型类型）
ModHelper.AddModelToPool<IroncladRelicPool, MyRelic>();
ModHelper.AddModelToPool<ColorlessCardPool, MyCard>();
ModHelper.AddModelToPool<SharedRelicPool, MyRelic>();

// 反射重载（池类型运行时确定）
ModHelper.AddModelToPool(poolType, modelType);
```

### 方式2：ModelDb.Inject（错过时机后补救）

```csharp
// ModelDb 已初始化后注册
ModelDb.Inject(typeof(MyPower));
```

`ModelDb.Inject` 只注册模型 ID，不关联池。池关联用 `AddModelToPool`；角色类自身的 `CardPool`/`RelicPool`/`PotionPool` 属性会自动带出三池，无需额外处理。

### 方式3：HarmonyPatch 注入

需要 Patch 注入的模型（角色/事件/遭遇/Modifier）：

| 目标 | Patch 方法 | 注意 |
|------|-----------|------|
| 角色 | `ModelDb.AllCharacters` getter | Postfix 用 `ref IEnumerable<CharacterModel>` + `Append` |
| 事件 | `Overgrowth.AllEvents` getter | 具体 act 子类（不要 Patch 抽象基类 `ActModel.AllEvents`）；`ref IEnumerable<EventModel>` |
| 先古之民 | `Hive.AllAncients` getter | act 子类；`ref IEnumerable<AncientEventModel>` |
| 遭遇 | `Overgrowth.GenerateAllEncounters()` | **实例方法非 getter**，不写 `MethodType.Getter`；`ref IEnumerable<EncounterModel>` |
| Modifier | `NCustomRunModifiersList.GetModifiersTickedOn` | `ref List<ModifierModel>` |

> ⚠️ 卡池/遗物池/药水池**无需** Patch `ModelDb.All*Pools` getter——它们是由角色类 `CardPool`/`RelicPool`/`PotionPool` 属性派生的只读组合。

