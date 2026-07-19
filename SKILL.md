# 杀戮尖塔 2 纯原生 Mod 开发 — AI 工作流

> 🦞 零第三方依赖。只靠 `0Harmony.dll` + `sts2.dll` 和你的脑子。

---

## 🚫 硬规则（优先级高于一切，必须逐条遵守）

### 一、写前必读
1. 写代码前必须读 references 对应模式文件，完整读完，不准凭训练数据记忆写
2. 写代码前必须查 API 签名：`grep -rn "方法名" sts2-res/src/` 确认参数类型和顺序
3. 不准复制外部 mod 源码，只准用 references 模板 + 原生 `sts2.dll` API

### 二、代码规范
4. 所有模型类必须加 `[CardPool]`/`[RelicPool]` 属性（除非手动注册）
5. 所有 `[SavedProperty]` 必须调 `InjectTypeIntoCache`
6. 所有 Harmony Patch 必须用 try-catch 包裹
7. `OnPlay` 必须有 `if (cardPlay.Target == null) return;` 空值检查
8. `OnUpgrade` 必须调 `UpgradeValueBy()`，不能直接改字段值

### 三、结构完整
9. 必须包含 `ModEntry.cs`（三阶段初始化）+ `ModInfo.cs`（常量）+ 至少一个模型类 + 本地化 JSON + 清单 JSON
10. `ModEntry.Initialize` 必须三阶段：Harmony → 注册 → 设置，缺一不可

### 四、静态验证
11. 逐行对照 API 源码检查：方法名存在、命名空间正确、无外部 mod 依赖、`[SavedProperty]` 有对应 `InjectTypeIntoCache`、Harmony 有 try-catch
12. 文件清单完整：入口 + 模型 + 本地化 JSON + 清单 JSON，缺一不可输出

### 五、GitHub 工作流（如果项目托管在 GitHub 且配置了 CI）
13. 修改完成后必须 commit + push 到对应分支
14. 必须观察 Actions 运行结果
15. CI 报错必须回退修改，改完重新 commit+push，循环直到 CI 通过

---

## 🚦 总工作流

```
用户说"帮我做 X"
  │
  ├─ 1. 确定类型：卡牌 / 遗物 / 药水 / 能力 / 附魔 / 事件 / 先古之民 / 怪物 / 角色 / Patch / 设置界面？
  │     （→ 硬规则 1 + 2）
  │
  ├─ 2. 读 references/setup/rider.md（如果使用 Rider 开发）
  │     （→ 硬规则 1）
  │
  ├─ 3. 查 API 签名
  │     ls ~/.openclaw/workspace/code/sts2-res/src/ 2>/dev/null ||
  │       git clone --depth 1 https://github.com/yehuoshun/sts2-res ~/.openclaw/workspace/code/sts2-res
  │     grep -rn "ClassName\|MethodName" ~/.openclaw/workspace/code/sts2-res/src/
  │     （→ 硬规则 2）
  │
  ├─ 4. 写代码（C# + 本地化 JSON + 清单 JSON）
  │     （→ 硬规则 3-10）
  │
  ├─ 5. 静态验证（逐行对照 API 源码 + 文件清单完整性）
  │     （→ 硬规则 11-12）
  │
  ├─ 6. 如果有 Rider 环境：参考 references/rider.md 处理代码检查
  │
  ├─ 7. Commit + Push → 观察 CI
  │     （→ 硬规则 13-15）
  │
  └─ 8. CI 通过 → 输出结果。CI 报错 → 跳回 4 改代码
```

---

## 📂 参考资料

| 分类 | 文件 | 内容 |
|------|------|------|
| `setup/` | [environment-setup.md](references/setup/environment-setup.md) | 环境搭建、项目创建、PCK 打包、C# 入口 |
| `setup/` | [rider.md](references/setup/rider.md) | Rider 开发环境配置（代码检查、Harmony 抑制规则） |
| `content/` | [relic.md](references/content/relic.md) | 自定义遗物（代码模板、稀有度、池、图标、本地化） |

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
| 遗物 `Rarity=Starter` 但池里不出现 | Starter 不走随机池，需 Patch 或用事件给 |
| Harmony PatchAll 异常 | 单类 try-catch 包裹，防止一个类炸了全挂 |
| 设置 UI 自己写容易出 bug | 参考 ShunMod 的零 Harmony + Godot 纯信号方案 |
| 自定义属性序列化丢失 | `[SavedProperty]` + `SavedPropertiesTypeCache.InjectTypeIntoCache()` |
| ModelDb 已初始化后注册模型 | 用 `ModelDb.Inject(type)` 而非 `AddModelToPool` |
| 角色卡池缓存不刷新 | 反射重置 `CardPoolModel._allCards` / `ModelDb._allCards` 等缓存字段 |
| 先古之民对话不显示 | Patch `AncientDialogueSet.GetValidDialogues` + `DefineDialogues` |
| 先古遗物不在图鉴显示 | 实现 `AncientRelicCompendiumPatch` 自定义注册表 |
| 多人模式状态不同步 | 用**确定性随机**（`DeterministicRandomUtils`）替代 `System.Random` |
| ModelId 序列化缓存重复 | Patch `ModelId.ToTypeNameMap` 注入时去重 |
| 多人模式专属卡牌 | 添加 `IsMultiplayerOnly` 标记防止单人模式卡死 |
| 卡牌奖励序列化丢失自定义卡池 | `CardRewardSerializationCompatibility` 兼容层（BaseLib 3.3.4+） |
| 临时能力核分支兼容 | 继承 `CustomTemporaryPowerModel` 并实现 `IBetaCompatTempPower`（BaseLib 3.3.3+） |
| 外部 Mod 卡牌目标兼容 | 用 `ExternalCardTargetingCompat` 桥接层反射调用 |
| 角色皮肤系统 | 实现 `IYuWanCharacterSkinProvider` 接口 + `CharacterSkinSelectionManager` 持久化选择 |
| 配置系统只依赖 RitsuLib | YuWanCard v0.5.10 已移除 BaseLib 配置支持，仅保留 RitsuLib |