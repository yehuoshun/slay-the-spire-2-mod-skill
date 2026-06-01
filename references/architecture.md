# STS2 Mod 参考架构

> 查看其他模组的源码学模式。按需阅读。

---

## 📐 STS2-Plus 参考架构（值得学的模式）

从 [STS2Plus](https://github.com/StephenSHorton/STS2Plus) 学到的最佳实践：

| 模式 | 说明 | 示例 |
|------|------|------|
| **TargetMethods 遍历子类** | 不只用 `typeof(Base)`，遍历 Assembly 所有子类 override | `UnlimitedGrowthMaxUpgradePatch` |
| **安全检测** | 写局外补丁前先检查卡牌是否安全 | `UnlimitedGrowthSafety.CanUseUnlimitedGrowth` |
| **ThreadStatic 上下文** | 反序列化时传参数给 getter | `SerializationContext.Push/Pop/Peek` |
| **Finalizer 清理** | Harmony Finalizer 保证异常时也清理 | `UnlimitedGrowthDeserializePatch.Finalizer` |
| **Patch 分文件** | 一个功能拆多个 Patch 类，各管各的 | `UnlimitedGrowthMaxUpgradePatch` + `DeserializePatch` |
| **Catagory 分组** | `[HarmonyPatchCategory("CatName")]` 便于开关 | STS2Plus 的 `MoreRules` 类目 |

---

## 📐 STS2-Mods 参考架构（Natsuki 合集）

从 [s1f102500012/sts2mod](https://github.com/s1f102500012/sts2mod) 学到的模组管理+代码模式：

### 仓库包含的 12 个独立模组

| 模组 | Mod ID | 版本 | 类型 |
|------|--------|------|------|
| **俄洛伊** | Illaoi | 0.1.2 | 新角色（生长/触手/灵魂） |
| **AI 队友** | sts2AITeammate | 0.1.1 | AI 多人模式 |
| **海克斯符文** | HextechRunes | 0.6.3 | 符文选择+战斗变化 |
| **无尽模式** | EndlessMode | 0.2.6 | 无尽挑战流程扩展 |
| **自定义难度** | CustomDifficulty | 0.1.0 | 怪物血/攻滑条 |
| **第四幕** | StS1Act4 | 0.1.0 | 一代第四幕还原 |
| **奖励附魔** | RewardEnchants | 0.2.1 | 奖励卡牌随机附魔 |
| **更多进阶挑战** | MoreAscensionChallenge | 0.1.1 | 11-15 进阶 |
| **手牌上限解除** | RemoveHandLimit | 0.2.1 | 手牌上限→20 |
| **初始牌组调整** | StartingDeckTweaks | 1.0.0 | 修改起始牌组 |
| **基石符文** | KeystoneRunes | 0.3.3 | 基石符文机制 |
| **心之钢** | Heartsteel | 0.1.0 | 心之钢主题遗物 |

### 值得学的模式

| 模式 | 说明 | 示例来源 |
|------|------|----------|
| **RequireField/RequireMethod 反射** | 静态缓存反射访问，启动时校验存在性 | `EndlessMode`, `RemoveHandLimit` |
| **Transpiler 常量替换** | IL 级替换 int 常量（如手牌上限 10→20） | `RemoveHandLimit` 的 `ReplaceIntLoads` |
| **GetAsyncStateMachineTarget** | 对 async 方法打补丁必须拿到状态机 | `RemoveHandLimit` 的 Draw/Add 补丁 |
| **SavedPropertiesTypeCache** | 自定义模型必须注入缓存才能序列化 | `Heartsteel`, `EndlessMode` |
| **ModHelper.AddModelToPool<T>** | 比 `[Pool]` 更简洁的注册方式 | `Heartsteel` 初始化 |
| **AssetHooks 分离** | 资源/贴图 Hook 独立文件管理 | `Heartsteel` 的 `AssetHooks.cs` |
| **CoreHook（游戏钩子）** | 用游戏提供的 `Hook` 类代替 Harmony | `RewardEnchants` 的 `TryModifyCardRewardOptions` |
| **多模组项目结构** | 每个模组独立 csproj + assets/src/tools | 全部 12 个模组 |
| **build_and_deploy.sh** | 一键编译→打包 PCK→部署到 mods/ | 每个模组的 `tools/` |
| **pack_mod.gd** | Godot 脚本打包非代码资源为 .pck | 每个模组的 `tools/` |
| **StartingDeck 泛型 Patch** | 泛型复用角色牌组修改逻辑 | `StartingDeckTweaks` |
| **SavedProperty** | 自定义序列化属性的正确写法 | `Heartsteel` 的 `SavedMaxHpGained` |

---

## 📐 YuWanCard 参考架构（生产级角色 Mod）

从 [YuWan886/Sts2-YuWanCard](https://github.com/YuWan886/Sts2-YuWanCard) 源码提取的核心模式：

### 初始化架构（MainFile.cs）

```csharp
// 四阶段初始化：Patches → Platform → Content → Config
public static void Initialize()
{
    ModLifecycle.Publish(ModLifecyclePhase.Initializing);
    var patcher = new ModPatcher(ModId);

    // Phase 1: 批量 Harmony 补丁（PatchAllSafe — 每个类独立 try/catch，兼容 Android/Mono AOT）
    patcher.PatchAllSafe(Assembly.GetExecutingAssembly(), manualPatches);
    ModLifecycle.Publish(ModLifecyclePhase.PatchesApplied);

    // Phase 2: 平台条件补丁（桌面/移动端分别处理）
    patcher.ApplySingle(h => CustomEnergyIconPatches.Apply(h), "CustomEnergyIcons");
    patcher.ApplySingle(h => ModInteropProcessor.Process(h, assembly), "ModInterop");

    // Phase 3: 内容发现 — 扫描 [Pool] 和注册属性
    ModLifecycle.Publish(ModLifecyclePhase.ContentRegistering);
    ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly());
    SavedPropertyRegistration.RegisterAssembly(Assembly.GetExecutingAssembly());
    ModLifecycle.Publish(ModLifecyclePhase.ContentRegistered);

    // Phase 4: 配置、场景转换、多人游戏、资源
    Config = new YuWanCardConfig();
    ConfigRegistrar.TryDeferredRegister();
    NodeFactory.Init();
    AssetPreloader.Preload();
    ModLifecycle.Publish(ModLifecyclePhase.Initialized);
}
```

### 值得学的模式

| 模式 | 说明 | 源码位置 |
|------|------|----------|
| **ModPatcher.PatchAllSafe** | 每个 [HarmonyPatch] 类独立 try/catch，一个失败不影响其他。Android/Mono AOT 必备 | `Core/Patching/ModPatcher.cs` |
| **ModLifecycle 事件总线** | 7 阶段有序初始化（Initializing→PatchesApplied→ContentRegistering→...→Initialized），订阅者按序执行，晚订阅立即回调 | `Core/Lifecycle/ModLifecycle.cs` |
| **ContentRegistry 多属性注册** | `[Pool]`/`[RegisterAncient]`/`[RegisterOrb]`/`[RegisterMonster]`/`[RegisterEnchantment]`/`[RegisterCharacter]`/`[RegisterEvent]`/`[RegisterSingleton]`，Freeze 后禁止注册 | `Core/Registration/ContentRegistry.cs` |
| **SavedPropertyRegistration** | 扫描 `[SavedProperty]` 标注的属性，自动注入序列化缓存 | `Core/Registration/SavedPropertyRegistration.cs` |
| **ModInterop 跨模组兼容** | `[ModInterop]` 属性 + Transpiler 替换 stub 为真实调用，目标模组未加载时用 fallback | `Core/Interop/ModInteropProcessor.cs` |
| **YuWanXxx 抽象基类** | `YuWanCardModel`/`YuWanRelicModel`/`YuWanPowerModel`/`YuWanEventModel` 等 15+ 个基类，封装自动注册 + 通用逻辑 | `Core/Abstracts/` |
| **AssemblyScanner** | 安全类型扫描，捕获 `ReflectionTypeLoadException`，兼容 Android/Mono | `Core/Registration/AssemblyScanner.cs` |
| **CustomTargetType** | 自定义目标类型注册表，支持非标准目标选择 | `Core/CustomTargetType.cs` |
| **CardHandGlow** | 手牌高亮系统（条件/规则/合并），可注册自定义发光条件 | `Core/HandGlow/` |
| **CustomKeywordRegistry** | `[CustomKeyword]` + `[CustomEnum]` 自动注册自定义关键词 | `Core/Patches/Content/CustomKeywordRegistry.cs` |
| **ConfigRegistrar 延迟注册** | 配置在 NMainMenu._Ready 时延迟注册，避免初始化顺序问题 | `MainFile.cs` 的 `NMainMenu_ConfigRegisterPatch` |
| **平台检测** | `IsMobilePlatform()` 检查 OS 名称，桌面专用补丁跳过 Android/iOS | `MainFile.cs` |
| **Hextech 集成** | 跨模组符文系统兼容层，`HextechRuntimeCompat.TryInstall` | `Integrations/Hextech/` |

---

## 📐 BaseLib 参考架构（社区框架）

从 [Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) 源码提取的核心模式：

### CustomCardModel 核心能力

```csharp
public abstract class CustomCardModel : CardModel, ICustomModel, ILocalizationProvider
{
    // 自动注册（构造时 autoAdd=true）
    public CustomCardModel(...) { if (autoAdd) CustomContentDictionary.AddModel(GetType()); }

    // 自定义卡框/横幅材质
    public virtual Texture2D? CustomFrame => null;
    public virtual Material? CreateCustomFrameMaterial => null;
    public virtual Material? CreateCustomBannerMaterial => null;
    public virtual string? CustomBannerMaterialPath => null;
    public virtual string? CustomPortraitPath => null;
    public virtual Texture2D? CustomPortrait => null;

    // 计算型变量工厂方法
    public static IEnumerable<DynamicVar> MakeCalculatedDamage(int baseVal, Func<CardModel, Creature?, decimal> bonus, int mult = 1);
    public static IEnumerable<DynamicVar> MakeCalculatedBlock(int baseVal, Func<CardModel, Creature?, decimal> bonus, int mult = 1);
    public static IEnumerable<DynamicVar> MakeCalculatedVar(string name, int baseVal, Func<CardModel, Creature?, decimal> bonus, int mult = 1);

    // 内联本地化
    public virtual List<(string, string)>? Localization => null;
}
```

### 值得学的模式

| 模式 | 说明 | 源码位置 |
|------|------|----------|
| **CustomCardModel** | 封装自定义卡框/横幅材质/立绘 + `MakeCalculatedDamage/Block/Var` 工厂方法 | `Abstracts/CustomCardModel.cs` |
| **CustomRelicModel** | 遗物基类，构造自动注册 + `ILocalizationProvider` 内联本地化 | `Abstracts/CustomRelicModel.cs` |
| **CustomPowerModel** | 能力基类，`CustomPackedIconPath`/`CustomBigIconPath` + `IHealthBarForecastSource` 血条预测 | `Abstracts/CustomPowerModel.cs` |
| **ModConfig 设置面板** | JSON 持久化 + 防抖保存 + `ConfigChanged` 事件 + 游戏内 UI 面板 | `Config/ModConfig.cs` |
| **ModConfigRegistry** | 管理所有模组配置实例，统一保存/加载 | `Config/ModConfigRegistry.cs` |
| **CustomContentDictionary** | 全局模型字典，构造时自动注册，配合 Patch 注入游戏池 | `Patches/Content/` |
| **ILocalizationProvider** | 接口：返回 `List<(string, string)>`，类内直接定义本地化文本 | `Abstracts/ILocalizationProvider` |
| **Hooks 系统** | `IHealthBarForecastSource`（血条预测）、`IMaxHandSizeModifier`（手牌上限）、`IHealAmountModifier`（治疗量） | `Hooks/` |
| **Extensions 工具集** | 30+ 扩展类：`CardExtensions`/`DynamicVarExtensions`/`HarmonyExtensions`/`NodeExtensions`/`PlayerExtensions` 等 | `Extensions/` |
| **NodeFactory** | Godot 节点工厂，统一创建/缓存 UI 节点 | `Utils/NodeFactories/` |
| **ShaderUtils** | HSV 着色器材质生成工具 | `Utils/ShaderUtils.cs` |
| **SpireField** | 类似 STS1 的 SpireField 模式，给任意对象附加自定义数据 | `Utils/SpireField.cs` |
| **CommonActions** | 可复用卡牌效果动作库 | `Utils/CommonActions.cs` |
| **CardModifier** | 卡牌修改器抽象，运行时修改卡牌属性 | `Abstracts/CardModifier.cs` |
| **ConstructedCardModel** | 动态构造卡牌（程序化生成卡牌内容） | `Abstracts/ConstructedCardModel.cs` |

> ⚡ **建议**：BaseLib 适合快速原型，但生产级 Mod 建议理解底层 API 后再决定是否依赖框架。

---

## 📐 ModTemplate 参考（官方模板）

从 [Alchyr/ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) 提供的三种模板：

| 模板 | 内容 | 适用场景 |
|------|------|----------|
| **Empty Mod** | 空项目 + BaseLib 依赖 | 纯补丁/工具类模组 |
| **Content Mod** | MainFile + Card + Relic + Power + Extensions | 添加卡牌/遗物/能力 |
| **Character Mod** | 完整角色：CardPool + RelicPool + PotionPool + Character + Card + Relic + Potion + Power | 新角色 |

安装：`dotnet new install Alchyr.Sts2.Templates`，然后 `dotnet new sts2mod` / `sts2content` / `sts2character`。

> 创建解决方案时勾选「Put solution and project in same directory」，否则 Godot 无法识别。