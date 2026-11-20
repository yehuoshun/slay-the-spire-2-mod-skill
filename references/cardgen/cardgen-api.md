# 组件 API 速查与代码模板

> 源码核对（AutoAnthony 0.3.165 / HEAD 705dd61，STS2 0.113.x）：生成器侧 API 在 `ChaosCardGenerator/`（游戏工程 Link 为 Generator/），游戏侧 API 在 `AutoAnthony` 命名空间。所有注册 API 遵循**同一套纪律**：ASCII ID 校验、重复注册拒绝、原子包提交、首次使用后冻结。

---

## 一、公共纪律（所有 API 一致）

- ID 必须非空 ASCII（`value.Any(c => c > 0x7f)` 即抛）
- `ApiVersion` 常量独立于 mod 版本（如 ComponentRuntimeApi v1 / CardTinkeringApi v6 / ComponentPackageApi v3）
- 注册冻结：`RegistrationsFrozen` 属性 + 冻结后注册抛 `InvalidOperationException`
- `RegisterPackage` 整体校验+原子提交，绝不留下半激活集成
- 执行顺序：内置 common → 内置 damage → 外部 handler → 旧模板兼容；外部 handler 返回 `false` 可继续走旧路由

### API 版本矩阵（0.3.165 实测，逐一核对 `const int ApiVersion`）

| 版本 | 面 | 位置 |
|---|---|---|
| v3 | `ComponentApi`（包注册/冻结） | `ChaosCardGenerator/ComponentApi.cs:372` |
| v3 | `ComponentPackageApi` | `ComponentPackageApi.cs:44` |
| v1 | `ComponentKeywordApi` | `ComponentApi.cs:223` |
| v2 | `ComponentLocalizationApi` | `OperationLocalizedText.cs:365` |
| v1 | `ComponentValuationApi` | `ComponentValuationApi.cs:34` |
| v1 | `NativeCardDecompositionApi` | `NativeCardDecompositionApi.cs:150` |
| v6 | `CardTinkeringApi` | `CardTinkeringApi.cs:92` |
| v4 | `ExternalComponentCharacterApi` | `ChaosMode/ExternalComponentCharacterApi.cs:52` |
| v1 | `ComponentRuntimeApi` | `ChaosMode/ComponentRuntimeApi.cs:92` |
| v2 | `ComponentPresentationApi` | `ChaosMode/ComponentPresentationApi.cs:29` |
| v1 | `ComponentKeywordRuntimeApi` | `ChaosMode/ComponentKeywordRuntimeApi.cs:28` |
| v2 / v1 / v1 / v1 | `ComponentRunSettingsApi` / `ComponentGenerationProgressApi` / `ComponentTriggerApi` / `ComponentSurpriseApi` | `ChaosMode/ExternalComponentIntegrationApi.cs:39 / :103 / :123 / :181` |
| v5 | `AutoAnthonySettingsApi` | `ChaosMode/AutoAnthonySettingsApi.cs:45` |
| v2 / v2 / v3 / v1 | `AutoAnthonyEditorApi` / `AutoAnthonyFreeformCardApi` / `AutoAnthonyNativeCardApi` / `AutoAnthonyBalanceAdjustmentApi` | `ChaosMode/AutoAnthonyEditorApi.cs:44 / AutoAnthonyFreeformCardApi.cs:13 / AutoAnthonyNativeCardApi.cs:28 / AutoAnthonyBalanceAdjustmentApi.cs:12` |
| 无版本号 | `CardNameGenerator.RegisterExternalParts`（0.3.32 起可用） | — |

- **版本号与 mod 版本无关**：`ApiVersion` 是接口面版本，只有签名/语义变化才涨。唯一没有 `ApiVersion` 的是老接口 `CardNameGenerator.RegisterExternalParts`（自 0.3.32 起可用）—— 它没有版本常量可查，接入时按 mod 版本判断
- 守卫别只写顶层一条：核对你实际调用的**每一个**面，最小守卫示例：

```csharp
if (ComponentApi.ApiVersion != 3 || ComponentPackageApi.ApiVersion != 3
    || ComponentRuntimeApi.ApiVersion != 1 || ExternalComponentCharacterApi.ApiVersion != 4)
    throw new NotSupportedException("Unsupported AutoAnthony Component API version.");
// 卡片编辑器另需 CardTinkeringApi.ApiVersion == 6；只读设置快照需 AutoAnthonySettingsApi.ApiVersion == 5；
// 改身份的编辑器还需 AutoAnthonyEditorApi.ApiVersion == 2
```

- **二进制签名会独立于版本号变化**：`GeneratedCardName` 加第 4 成员 `MelodyTags` 时版本常量没动，但 record 的构造/解构签名变了 —— 跨版本必须对当前 `AutoAnthony.dll` 重编，别拿「版本对上」当「可直接复用旧 DLL」的理由

## 二、组件包注册（生成器侧，v3）

```csharp
ComponentPackageApi.Register(new ComponentPackageRegistration(
    "my_mod:watcher_components",              // 包 ID（ASCII，跨存档稳定）
    new ComponentProfileRequest("my_mod:watcher", GeneratedCharacter.Regent, false),
    profile,                                  // ComponentGenerationProfile：ShellCatalog+ComponentCatalog+
                                              //   NameCatalog+IComponentOccurrencePolicy+IComponentValuePolicy
    localizedTexts, keywordUpgrades, multiplicities));
```

- `ComponentProfileRequest.ProfileId` 是外部身份；`Character` 只是最近的内置平衡/合法性原型（任意数量外部 profile 可复用同原型不冲突）
- Provider 按注册逆序查询（后注册可覆盖内置）；首次 `Resolve` 后冻结
- `IncludeInUltimateChaos` 默认 true：外部包的完整原生 recipe 数据集并入终极混沌加权，结构原子去重
- `ComponentApi.ComposeCatalog` 暴露保 recipe 的 union，可任意组合内置目录子集
- 组件键按 `opcode/variant/target/slot` 结构 schema 匹配，别抄角色前缀别名

## 三、运行时执行路由（游戏侧，v1）

```csharp
public interface IComponentRuntimeHandler
{
    /// <returns>true=完成；false=继续走兼容回退</returns>
    Task<bool> ExecuteAsync(ComponentRuntimeContext context);
}

public sealed record ComponentRuntimeRoute(string Opcode, string Variant, IComponentRuntimeHandler Handler);

// 单条注册 or 原子包注册（推荐包方式）
ComponentRuntimeApi.Register("my_mod:op", "v1", handler);
ComponentRuntimeApi.RegisterPackage("my_mod:routes", [new ComponentRuntimeRoute("my_mod:op", "", handler)]);
// 精确 opcode+variant 优先于 opcode 通配（variant 为空）；首条生成操作执行后冻结
```

**ComponentRuntimeContext**（handler 拿到的上下文，绝不传本地化文本）：

```csharp
Card / OperationIndex / Operation / RuntimeSpec / Amount（X/依赖缩放后）/
ChoiceContext / CardPlay / Target / EventCard / EventAmount / IsTriggered /
LastDamageDealt / CardSlots（只读字典）
context.RecordDamageDealt(n);   // 记录实际伤害（后续操作依赖）
context.ReplaceDrawnCards(cards);
context.SetCardSlot(slot, card); // 槽 ID 必须 ASCII
context.RequestEndTurn();        // 延迟到出牌阶段安全路径的结束回合
```

## 四、自定义关键词（v3，闭源枚举的扩法）

> `CardKeyword`/`CardTag` 是封闭枚举 → 关键词身份用 **ASCII 字符串**，投影走适配器，**绝不制造枚举成员**。关键词不是隐藏效果容器：可执行行为/平衡值必须由同 recipe 里的结构化操作提供。

```csharp
// 1) 生成器侧：注册关键词元数据 + 升级规则
var keyword = new ComponentKeywordDefinition("my_mod:stance_locked", new StanceKeywordRule());
var upgrade = new ComponentKeywordUpgrade(
    "my_mod:enter_stance", [], [], AddedCustomKeywords: [keyword.KeywordId]);
ComponentPackageApi.Register(new ComponentPackageRegistration(
    "my_mod:components", request, profile,
    KeywordUpgrades: [upgrade], Keywords: [keyword]));

// 2) 游戏侧：注册投影适配器（冻结在首次卡查询）
ComponentKeywordRuntimeApi.RegisterPackage("my_mod:keyword_runtime",
[
    new ComponentKeywordRuntimeRegistration(keyword.KeywordId, new StanceKeywordAdapter())
]);

public interface IComponentKeywordRuntimeAdapter
{
    IEnumerable<CardKeyword> CanonicalKeywords(ChaosCardModel card) => [];
    IEnumerable<GameCardTag> SemanticTags(ChaosCardModel card) => [];
    IEnumerable<IHoverTip> BuildHoverTips(ChaosCardModel card) => [];
    void OnUpgrade(ChaosCardModel card, bool added) { }
}
```

- 活动 ID 集 = 基础卡 + `AddedCustomKeywords`/`RemovedCustomKeywords`（升级/存档/重连同一身份）
- `ComponentKeywordPolicy` 分 allow-list：基础自定义关键词 / 升级增删 / 全局升级候选；`IComponentKeywordRule` 可基于完整结构化卡上下文拒绝
- 原生关键词白名单（Exhaust/Innate/Retain/Sly/Ethereal/Eternal/Unplayable）与语义标签（Strike/Defend/OstyAttack）**分区**，后者不是可显示关键词

## 五、外部角色接入（七步注册法）

```csharp
// 角色 mod 自己的初始化器里，按此顺序注册（wiki Component-API-Overview「Lifecycle」）：
// 1. ComponentPackageApi.Register —— recipe/组件/本地化/关键词升级/multiplicity/估值
// 2. CardNameGenerator.RegisterExternalParts —— 可选：注册审过的中英双语名字形态（语料由 profile 提供）
// 3. ComponentRuntimeApi.RegisterPackage —— 自定义 opcode 执行
// 4. ComponentPresentationApi.RegisterPackage —— 可选：引用卡/命名机制提示
// 5. ComponentKeywordRuntimeApi.RegisterPackage —— 自定义关键词投影
// 6. new ExternalComponentCharacterRegistration(profileId, archetype, energyIconPrefix, 可选 ancientAdapter)
// 7. 可选 ExternalComponentCharacterRuntimeRegistration —— 固定槽类型/活跃 run 谓词/生成池
// 运行期：生成或反序列化完整 ChaosCardDefinition 列表后（⚠️ 两个方法都带 profileId，v4 实测 2 参 / 1 参）
ExternalComponentCharacterApi.InstallDefinitions(profileId, definitions); // 逐槽校验后整表替换
ExternalComponentCharacterApi.ClearDefinitions(profileId);               // run 结束清理
```

> 「外部角色注册」本身就足以得到 AutoAnthony 的生成与运行时支持；**编辑器集成是另一件事且要单独选择加入** —— 除非角色明确支持对应的编辑/freeform/改身份/原生分解能力，否则不要注册 `ExternalEditorCapabilities`。

**`InstallDefinitions` 的校验清单**（`ExternalComponentCharacterApi.cs:204`，任一条不满足直接抛，绝不半装）：

- 至少 1 条定义（空列表抛 `ArgumentException`）
- 槽位**从 0 连续**：`definition.Slot != slot`（已按 `Slot` 排序后逐条比对）
- 每条定义的 `Card.Character` **必须等于**注册时的 `BalanceArchetype`（外部 profile 不能借用别的角色壳）
- `RuntimeSpecs.Count == Card.Operations.Count`，且每个 spec 过 `spec.Validate()`
- 若已注册 runtime host：`materialized.Length == host.CardCount`（槽数与宿主声明的卡数必须一致）
- **安装即冻结**：写入时置 `_registrationsFrozen = true`；`GetDefinitions(profileId)` / 内部 `ForSlot` 首次访问同样会冻结 —— 所以「先跑一局再补注册」不可能

> 更早的 wiki 摘录写的是 `InstallDefinitions(definitions)` 无 profileId —— **那是错的**，源码实测带 `profileId`，且 `ValidateId` 强校验 ASCII。

- 外部 mod 声明固定槽卡类：继承 `ExternalChaosCardModel`，只覆写 `ComponentProfileId`/`Slot`/`Pool`
- AutoAnthony **不 patch** `ModelDb.AllCharacters`、不改写别人的池；角色模型/卡池/初始卡/存档/多人传输仍归外部 mod
- 外部 profile ID 会带进 `ChaosCompositePower` 保存属性和多人校验和，延迟效果跨存档/重连可解析
- 完整示例：`examples/WatcherComponentAdapter.cs.txt`（观者 0.9.25 适配，框架无关）

## 六、入场检查清单（外部集成完成的判据）

- [ ] 生成器包/运行路由/展示路由/自定义关键词适配器都在首次 profile/卡查询前注册
- [ ] 定义经外部 mod 存档往返 + 房主权威多人 payload
- [ ] 每个 shell 都有结构化、本地化、可执行的 fallback 组件（应急路径需要：每种卡型至少 1 个独立可升级正面组件；Special-X 配额还需可转换的固定数值 Attack/Skill 组件）
- [ ] 本地化文本客户端**永不** re-parse
