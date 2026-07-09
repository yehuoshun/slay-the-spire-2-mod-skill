# 附录

---

## A. 完整 API 速查表

| API | 签名 | 说明 |
|-----|------|------|
| `CardModel` | `CardModel(cost, type, rarity, target)` | 卡牌基类 |
| `RelicModel` | `RelicModel()` | 遗物基类 |
| `PotionModel` | `PotionModel()` | 药水基类 |
| `PowerModel` | `PowerModel()` | 能力基类 |
| `EnchantmentModel` | `EnchantmentModel()` | 附魔基类 |
| `EventModel` | `EventModel()` | 事件基类 |
| `AncientEventModel` | `AncientEventModel()` | 先古之民基类 |
| `CharacterModel` | `CharacterModel()` | 角色基类 |
| `MonsterModel` | `MonsterModel()` | 怪物基类 |
| `EncounterModel` | `EncounterModel()` | 遭遇基类 |
| `CardPoolModel` | `CardPoolModel()` | 卡池基类 |
| `RelicPoolModel` | `RelicPoolModel()` | 遗物池基类 |
| `PotionPoolModel` | `PotionPoolModel()` | 药水池基类 |

### BaseLib 扩展（3.3.3+）

| API | 说明 |
|-----|------|
| `CustomTemporaryPowerModel` | 临时能力基类，支持 `ITemporaryPower` + `IBetaCompatTempPower` |
| `IBetaCompatTempPower.IgnoreNextInstance()` | 忽略下一次能力实例化（核分支兼容） |
| `CardRewardSerializationCompatibility.SupportsLegacyCustomCardPool` | 检查当前 STS2 版本是否支持旧版 `CustomCardPool` |
| `CardRewardSerializationCompatibility.GetCustomCardPool(options)` | 获取 `CardCreationOptions` 的自定义卡池 |
| `CardRewardSerializationCompatibility.CreateCustomPoolOptions(cards, source, rarityOdds)` | 创建兼容旧版的自定义卡池 `CardCreationOptions` |

### 命令（Cmd）

| API | 说明 |
|-----|------|
| `CreatureCmd.Damage(ctx, target, amount, source, card)` | 造成伤害 |
| `CreatureCmd.GainBlock(target, amount, source)` | 获得格挡 |
| `PlayerCmd.GainEnergy(ctx, amount)` | 获得能量 |
| `PlayerCmd.GainGold(ctx, amount)` | 获得金币 |
| `PlayerCmd.Heal(ctx, amount)` | 治疗 |
| `PlayerCmd.Damage(ctx, amount)` | 玩家受伤 |
| `PowerCmd.Apply<T>(target, amount, source, card)` | 应用能力 |
| `PowerCmd.Remove<T>(target, amount)` | 移除能力 |
| `CardCmd.Discard(ctx, cards)` | 弃牌 |
| `CardCmd.Upgrade(ctx, card)` | 升级卡牌 |
| `CardCmd.Enchant(ctx, card, enchantment)` | 附魔 |
| `CardPileCmd.Draw(ctx, count, player)` | 抽牌 |
| `CardSelectCmd.FromHandGeneric(ctx, ...)` | 从手牌选择 |
| `CardSelectCmd.FromDeckGeneric(ctx, ...)` | 从牌库选择 |
| `OrbCmd.EvokeNext(ctx, player)` | 激发下一个充能球 |
| `OrbCmd.AddSlots(player, count)` | 增加充能球槽位 |
| `CardCmd.Downgrade(ctx, card)` | 降级卡牌 |
| `CardPileCmd.Shuffle(ctx, player)` | 洗牌 |
| `CardSelectCmd.MultiPileSelect(ctx, ...)` | 多牌堆选牌（BaseLib 3.3.0+） |

### 模型数据库

| API | 说明 |
|-----|------|
| `ModelDb.GetId(type)` | 获取模型 ID |
| `ModelDb.GetByIdOrNull<T>(id)` | 按 ID 获取模型 |
| `ModelDb.Card<T>()` | 获取卡牌实例 |
| `ModelDb.Relic<T>()` | 获取遗物实例 |
| `ModelDb.Potion<T>()` | 获取药水实例 |
| `ModelDb.CardPool<T>()` | 获取卡池实例 |
| `ModelDb.RelicPool<T>()` | 获取遗物池实例 |
| `ModelDb.PotionPool<T>()` | 获取药水池实例 |
| `ModelDb.Monster<T>()` | 获取怪物实例 |
| `ModelDb.Inject(type)` | 运行时注入模型 |
| `ModelDb.Contains(type)` | 检查模型是否已注册 |

### 注册

| API | 说明 |
|-----|------|
| `ModHelper.AddModelToPool<T>(poolType, modelType)` | 注册模型到池 |
| `SavedPropertiesTypeCache.InjectTypeIntoCache(type)` | 注入序列化缓存 |
| `ScriptManagerBridge.LookupScriptsInAssembly(assembly)` | 场景脚本映射 |
| `Harmony.PatchAll(assembly)` | 批量 Harmony 补丁 |
| `Harmony.PatchCategory(assembly, category)` | 按分组打补丁 |

---

## B. 枚举速查

### CardType

| 值 | 说明 |
|----|------|
| `Attack` | 攻击牌 |
| `Skill` | 技能牌 |
| `Power` | 能力牌 |
| `Curse` | 诅咒牌 |
| `Status` | 状态牌 |

### CardRarity

| 值 | 说明 |
|----|------|
| `Basic` | 基础牌 |
| `Common` | 普通牌 |
| `Uncommon` | 罕见牌 |
| `Rare` | 稀有牌 |
| `Token` | 衍生物 |
| `Special` | 特殊牌 |

### TargetType

| 值 | 说明 |
|----|------|
| `Self` | 自己 |
| `AnyEnemy` | 任意敌人 |
| `AllEnemies` | 所有敌人 |
| `Any` | 任意目标 |
| `None` | 无目标 |

### CardKeyword

常用：`Exhaust` `Ethereal` `Retain` `Innate` `Heavy` `Strike` `Defend`

### RelicRarity

| 值 | 获取方式 |
|----|---------|
| `Common` | 普通池 |
| `Uncommon` | 稀有池 |
| `Rare` | BOSS 池 |
| `Starter` | 初始遗物（不走随机池） |
| `Shop` | 商店 |
| `Special` | 特殊获取 |
| `Event` | 事件专属 |

### PowerType

| 值 | 说明 |
|----|------|
| `Buff` | 增益 |
| `Debuff` | 减益 |

### StackType

| 值 | 说明 |
|----|------|
| `Intensity` | 数值叠加 |
| `Duration` | 回合叠加 |
| `None` | 不叠加 |

---

## C. 常见坑速览

| 问题 | 解决 |
|------|------|
| 图标不显示 | PNG 未打包进 PCK；检查路径与代码一致 |
| 本地化不生效 | 必须 **Publish**（非 Build），本地化是资源文件 |
| Harmony 报 .NET 版本 | `GodotPlugins.runtimeconfig.json` → `"version": "9.0.0"` |
| 场景脚本找不到 | `ScriptManagerBridge.LookupScriptsInAssembly` |
| Mod 不加载 | `assets/MyMod.json` 的 `id` 和文件名一致 |
| 自定义属性不保存 | 没调 `SavedPropertiesTypeCache.InjectTypeIntoCache()` |
| `AddModelToPool` 泛型报错 | 用 `ModHelper.AddModelToPool(poolType, modelType)` 反射重载 |
| 遗物 `Rarity=Starter` 但池里不出现 | Starter 稀有度不走随机池，需 Patch 或用事件给 |
| Harmony PatchAll 异常 | 单类 try-catch 包裹，防止一个类炸了全挂 |
| 设置 UI 自己写容易出 bug | 参考 modes-settings-ui.md 零 Harmony 方案 |
| 自定义属性序列化丢失 | `[SavedProperty]` + `SavedPropertiesTypeCache.InjectTypeIntoCache()` |
| ModelDb 已初始化后注册模型 | 用 `ModelDb.Inject(type)` 而非 `AddModelToPool` |
| 角色卡池缓存不刷新 | 反射重置 `CardPoolModel._allCards` / `ModelDb._allCards` 等缓存字段 |
| 先古之民对话不显示 | Patch `AncientDialogueSet.GetValidDialogues` + `DefineDialogues` |
| 卡牌 Pool 返回 null | 检查 `IsMutable && Owner != null` 条件判断 |
| 事件不触发 | 事件不走 `AddModelToPool`，需要特殊注册 |
| 多个 Mod 补丁同一方法 | 用 `HarmonyPriority` 控制执行顺序 |
| 多人模式状态不同步 | 用**确定性随机**（`DeterministicRandomUtils`）替代 `System.Random` |
| ModelId 序列化缓存重复 | Patch `ModelId.ToTypeNameMap` getter 注入时去重 |
| 多人模式专属卡牌 | 添加 `IsMultiplayerOnly` 标记防止单人模式卡死 |
| 先古遗物不在图鉴显示 | 实现 `CustomAncientRegistry` + `AncientRelicCompendiumPatch` |
| 卡牌降级 Hook | 实现 `IAfterCardDowngraded` 接口（BaseLib 3.3.0+） |
| 废品堆机制 | 实现 `ITrashHeapCard`/`ITrashHeapRelic` 接口（BaseLib 3.3.0+） |
| 自定义关卡 | 继承 `CustomActModel`（BaseLib 3.3.0+） |
| 角色初始化复杂 | 用 `CustomCharacterUtils` 简化（BaseLib 3.3.0+） |
| 宠物/宠物或自身目标 | 用 `CustomTargetType.Pet`/`PetOrSelf`（BaseLib 3.3.0+） |
| 卡牌奖励序列化丢失自定义卡池 | `CardRewardSerializationCompatibility` 兼容层（BaseLib 3.3.4+） |
| 临时能力核分支兼容 | 继承 `CustomTemporaryPowerModel` + `IBetaCompatTempPower`（BaseLib 3.3.3+） |
| 外部 Mod 卡牌目标兼容 | 反射桥接 `ExternalCardTargetingCompat`；`Assembly.GetTypes()` 需 try-catch `ReflectionTypeLoadException` |
| 角色皮肤持久化 | `CharacterSkinSelectionManager` JSON 文件存储 + `IYuWanCharacterSkinProvider` 接口 |
| 集成战略事件 0.5.0 前置变更 | 必须安装 RitsuLib 替代旧前置 |
| 卡牌奖励序列化（旧版 STS2） | 无 `CustomCardPool` 属性的版本，用 `BuildSpecificCardsExt` 存储具体卡牌 ID 列表 |