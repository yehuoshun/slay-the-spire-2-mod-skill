# 杀戮尖塔 2 纯原生 Mod 开发 — AI 工作流

> 🦞 零第三方依赖。只靠 `0Harmony.dll` + `sts2.dll` 和你的脑子。

## 🚦 总工作流

```
用户说"帮我做 X"
  │
  ├─ 1. 确定类型：卡牌 / 遗物 / 药水 / 能力 / 附魔 / 事件 / 先古之民 / 怪物 / 角色 / Patch / 设置界面？
  │
  ├─ 2. 查 API（强制）
  │      ls ~/.openclaw/workspace/code/sts2-res/src/ 2>/dev/null ||
  │        git clone --depth 1 https://github.com/yehuoshun/sts2-res ~/.openclaw/workspace/code/sts2-res
  │      grep -rn "ClassName\|MethodName" ~/.openclaw/workspace/code/sts2-res/src/
  │
  ├─ 3. 查 references/<模式文件> 获取代码模板
  │
  ├─ 4. 写 C# 代码 + 本地化 JSON
  │
  ├─ 5. 构建 → 部署 → 测试
  │
  └─ 6. Commit + Push
```

---

## 📂 模式速查

| 需求 | 文件 |
|------|------|
| 项目搭建（脚手架、目录、csproj、构建部署） | [references/project-scaffold.md](references/project-scaffold.md) |
| 卡牌 | [references/modes-card.md](references/modes-card.md) |
| 遗物 | [references/modes-relic.md](references/modes-relic.md) |
| 药水 | [references/modes-potion.md](references/modes-potion.md) |
| 能力（Buff/Debuff） | [references/modes-power.md](references/modes-power.md) |
| 附魔 | [references/modes-enchantment.md](references/modes-enchantment.md) |
| 事件 & 先古之民 | [references/modes-event.md](references/modes-event.md) |
| Harmony 补丁 | [references/modes-harmony.md](references/modes-harmony.md) |
| 角色 | [references/modes-character.md](references/modes-character.md) |
| 怪物 & 遭遇 | [references/modes-monster.md](references/modes-monster.md) |
| 序列化 & 注册 | [references/modes-serialization.md](references/modes-serialization.md) |
| 设置界面 | [references/modes-settings-ui.md](references/modes-settings-ui.md) |
| 实战写法模式（从 STS2Plus / YuWanCard / 海克斯符文提炼） | [references/real-code-patterns.md](references/real-code-patterns.md) |
| 附录（API 速查、枚举、常见坑） | [references/appendices.md](references/appendices.md) |

---

## 📋 核心 API 速查

| API | 说明 |
|-----|------|
| `CardModel(cost, type, rarity, target)` | 卡牌基类构造 |
| `RelicModel` | 遗物基类。`Rarity` 决定获取途径 |
| `PotionModel` | 药水基类。`Usage` 决定使用场景 |
| `PowerModel` | 能力基类。`Type`/`StackType` 决定行为 |
| `EnchantmentModel` | 附魔基类。`ModifyCard()` 改卡牌属性 |
| `EventModel` | 事件基类。`GenerateInitialOptions()` 返回选项 |
| `AncientEventModel` | 先古之民。`DefineDialogues()` 返回对话 |
| `CharacterModel` | 角色基类。`CardPool`/`RelicPool`/`StartingDeck` |
| `MonsterModel` | 怪物基类。`GenerateMoveStateMachine()` |
| `EncounterModel` | 遭遇基类。`GenerateMonsters()` |
| `CardPoolModel` | 卡池基类。`GenerateAllCards()` |
| `DynamicVars` | 动态变量集合。`["Name"].UpgradeValueBy(n)` |
| `PlayerCmd` | 玩家操作：`GainEnergy`/`GainBlock`/`GainGold`/`Damage`/`Heal`/`ApplyPower` |
| `CardCmd` | 卡牌：`Enchant`/`Discard`/`Upgrade` |
| `DamageCmd` | 伤害链：`Attack().FromCard().Targeting().Execute()` |
| `CardPileCmd` | 牌堆：`Draw` |
| `CardSelectCmd` | 选牌：`FromHandGeneric`/`FromDeckGeneric` |
| `ModelDb` | 反射注册：`GetId`/`GetByIdOrNull`/`AllCards`/`AllRelics` |
| `ModHelper.AddModelToPool<T>()` | 注册模型到对应池 |
| `ModelDb.Inject(type)` | 绕过 Init 直接注入模型（ModelDb 已初始化后使用） |
| `SavedPropertiesTypeCache.InjectTypeIntoCache()` | 自定义属性序列化缓存注入 |
| `ScriptManagerBridge.LookupScriptsInAssembly` | 场景脚本映射（有自定义场景时必须） |
| `Harmony.PatchAll` | 批量 Harmony 补丁。用 try-catch 包裹防止一个类炸了全挂 |
| `SavedProperty` 特性 | 标记需要序列化保存的自定义属性 |

---

## ⚠️ 常见坑速览

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
| 设置 UI 自己写容易出 bug | 参考 modes-settings-ui.md 零 Harmony + Godot 纯信号方案 |
| 自定义属性序列化丢失 | `[SavedProperty]` + `SavedPropertiesTypeCache.InjectTypeIntoCache()` |
| ModelDb 已初始化后注册模型 | 用 `ModelDb.Inject(type)` 而非 `AddModelToPool` |
| 角色卡池缓存不刷新 | 反射重置 `CardPoolModel._allCards` / `ModelDb._allCards` 等缓存字段 |
| 先古之民对话不显示 | Patch `AncientDialogueSet.GetValidDialogues` + `DefineDialogues` |