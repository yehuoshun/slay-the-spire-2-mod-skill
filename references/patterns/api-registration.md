# API 附录：注册点与文件索引

## 注册点

| 目标 | 方法 | 文件 |
|------|------|------|
| 卡牌到池 | `ModHelper.AddModelToPool<TPoolType, TModelType>()` 或 `(Type, Type)`；或 `[CardPool(typeof(池类))]` | serialization |
| 遗物到池 | `ModHelper.AddModelToPool<IroncladRelicPool, MyRelic>()`；或 `[RelicPool]` | serialization |
| 药水到池 | `ModHelper.AddModelToPool<SharedPotionPool, MyPotion>()`；或 `[PotionPool]` | serialization |
| 能力/球体/章节到 ModelDb | `ModelDb.Inject(typeof(X))` | serialization |
| 角色 | Patch `ModelDb.AllCharacters` getter（ref IEnumerable） | character |
| 事件 | Patch act 子类 `Overgrowth.AllEvents` getter（ref IEnumerable） | event |
| 先古之民 | Patch act 子类 `Hive.AllAncients` getter（ref IEnumerable） | ancient |
| 遭遇 | Patch act 子类 `Overgrowth.GenerateAllEncounters()`（实例方法） | monster |
| Modifier | Patch `NCustomRunModifiersList.GetModifiersTickedOn` | modifier |
| 序列化 | `SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(X))` | serialization |

> ⚠️ `ActModel.AllEvents`/`ActModel.AllAncients` 是抽象基类 getter，**Patch 基类不拦截子类实现**——要 Patch 具体 act 子类（`Overgrowth`/`Hive`/`Glory`/`Underdocks`）。池的注册由角色类 `CardPool`/`RelicPool`/`PotionPool` 属性关联，**无需** Patch `ModelDb.All*Pools` getter。

---

## 文件索引

| 文件 | 内容 |
|------|------|
| `references/card/card.md` | 自定义卡牌（v3 当前） |
| `references/relic/relic.md` | 自定义遗物（v3） |
| `references/power/power.md` | 自定义能力（v3） |
| `references/potion/potion.md` | 自定义药水（v3） |
| `references/enchantment/enchantment.md` | 自定义附魔（v3） |
| `references/event/event.md` | 自定义事件（v3） |
| `references/event/ancient.md` | 先古之民事件（v3） |
| `references/character/character.md` | 自定义角色（v3） |
| `references/monster/monster.md` | 自定义敌怪 & 遭遇（v3） |
| `references/modifier/modifier.md` | 自定义 Modifier（v3） |
| `references/orb/orb.md` | 自定义球体（v3） |
| `references/act/act.md` | 自定义章节（v3） |
| `references/pet/pet.md` | 自定义宠物（v3） |
| `references/harmony/harmony.md` | Harmony 补丁模式 |
| `references/serialization/serialization.md` | 序列化与注册（v3） |
| `references/settings/settings.md` | 设置界面（BaseLib，需转纯原生） |
| `references/baselib/design-patterns.md` | 纯原生设计模式总纲 |
| `references/setup/environment-setup.md` | 环境搭建 |
| `references/setup/rider.md` | Rider 开发环境配置 |
| `references/patterns/code-patterns.md` | 实战写法模式（v3 待校） |

