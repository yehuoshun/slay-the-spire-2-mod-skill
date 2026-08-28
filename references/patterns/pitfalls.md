# 常见坑速览（纯原生）

> SKILL.md 的「常见坑速览」全文，独立成文件便于按需加载。

| 问题 | 解决 |
|------|------|
| 图标不显示 | PNG 未打包进 PCK；检查路径与代码一致 |
| 本地化不生效 | 必须 **Publish**（非 Build），本地化是资源文件 |
| Harmony 报 .NET 版本 | `GodotPlugins.runtimeconfig.json` → `"version": "9.0.0"` |
| Mod 不加载 | `assets/MyMod.json` 的 `id` 和文件名一致 |
| 自定义属性不保存 | `[SavedProperty]` + `SavedPropertiesTypeCache.InjectTypeIntoCache()` |
| `AddModelToPool` 泛型报错 | 用反射重载 `ModHelper.AddModelToPool(poolType, modelType)` |
| 遗物 `Rarity=Starter` 但池里不出现 | Starter 不走随机池，需 Patch 或用事件给 |
| Harmony PatchAll 异常 | 单类 try-catch 包裹，防止一个类炸了全挂 |
| ModelDb 已初始化后注册模型 | 用 `ModelDb.Inject(type)` 补救 |
| 角色卡池缓存不刷新 | 反射重置 `CardPoolModel._allCards` / `ModelDb._allCards` |
| 先古之民对话不显示 | Patch `AncientDialogueSet.GetValidDialogues` + `DefineDialogues` |
| ModelId 序列化缓存重复 | Patch `ModelId.ToTypeNameMap` 注入时去重 |

> BaseLib / 第三方生产级 Mod 的常见坑见 [design-patterns.md](../baselib/design-patterns.md)「常见坑（灵感参考）」——纯原生定位下仅作参考，需自行转译实现。