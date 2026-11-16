# 愚者（Fool）词条系统：把原生新角色灌进组件化随机卡

> 来源：AutoAnthony v0.3.165 / commit `705dd61` / STS2 v0.113.x（Fool 是 v113 原生新增的**第七个角色**）。
> 定位：这是「**生态参考**」，不是写普通卡的路线 —— 但它示范了一件通用的事：**一个新角色怎么零侵入地接进既有卡池体系**。

## 一、这个系统在做什么

把 Fool 的 **84 张原生卡逐张手工拆解成 60 个 `F:` 语义组件契约**（每个契约只给「命名槽」，不给整卡数值），再由这些组件生成 **91 个新槽位**（10 Basic / 20 Common / 35 Uncommon / 24 Rare / 2 Ancient），七池合计 **606 槽**。

- 拆解是**手工**的，脚本（`tools/author_fool_catalog.py`）只负责把散文式的组件定义整理成 JSON，不做自动反编译。
- 组件之间可复用：同一条「打出一张旋律牌时抽 1 张」被多张卡共享，数值走 RuntimeSpec 的值槽，而不是复制一份卡。
- 原卡的**中文模板**留在 JSON 里（模板里的数字 token 在离线编译时被替换成值槽），所以本地化仍然只是输出。

## 二、旋律（Melody）是原生机制，不是命名约定

| 要点 | 具体 |
|---|---|
| 三种旋律 | `Wah` / `Cho` / `Ki`（哇 / 啾 / 叽），对应 `CardTag.Wah/Cho/Ki` |
| 怎么「获得」 | 打出一张带该标签的牌，即在该回合获得该旋律；**每回合每种旋律只算首次**（`MelodyCmd.IsFirstPlayThisTurn`） |
| 效果怎么写 | 卡的效果按「拥有几种 / 哪几种旋律」缩放，而不是按打出顺序 |
| 标签来源 | **显式元数据**（`CardTag`），战斗期绝不从本地化牌名反推 |
| 掩码 | `FoolMelodySubset.WithMask(atom, mask)` 的 bit 1/2/4 选非空子集；隐藏字段 `melody_mask` 是**语义选择器**，不是随机强度、也不是升级候选 |

配套还有 `Shoot`（射击）与箭矢状态：`AimPower` / `StretchPower`、`ArrowCmd.GainArrowSlot`、`AfterArrowImpaled` / `AfterArrowDislodged` 等原生事件。

## 三、四层分层（照这个切，别混）

| 层 | 职责 | 位置 |
|---|---|---|
| JSON 契约 | 内嵌资源。逐条 `F:` 模板 → Opcode / Variant / Target / Flags / Values / Condition / Trigger / TextSlots | `ChaosCardGenerator/Data/fool_components.json`，记录形状见 `ChaosCardGenerator/FoolComponentDefinitions.cs:18`–`:22` |
| Definitions | 加载、把中文模板里的数字编译成 `OperationRuntimeSpec`、给组件定价 | `ChaosCardGenerator/FoolComponentDefinitions.cs:11`（`Load:27`、`TryCompile:38`、`Localized:65`、`TryEstimate:69`） |
| Rules | 生成期合法性（射击装配、锚点、消耗依赖、能否追加）与旋律位掩码 | `ChaosCardGenerator/FoolComponentRules.cs:4`；`FoolMelodySubset:130` |
| Executor | 运行期把模板名分派到原生 `*Cmd` / `*Power`，并编排触发 | `ChaosMode/ChaosFoolExecutor.cs:18`（`ExecuteFoolOperation:71`） |

## 四、API 速查（逐字签名）

```csharp
// ChaosCardGenerator/FoolComponentRules.cs:30
public static bool HasValidAnchors(IReadOnlyList<GeneratorOperation> operations)
// FoolComponentRules.cs:88
public static bool HasValidExhaustDependencies(IReadOnlyList<GeneratorOperation> operations, bool ultimateChaos)
// FoolComponentRules.cs:104
internal static bool CanAppend(IReadOnlyList<GeneratorOperation> previous, ComponentAtom atom, bool ultimateChaos)
// FoolComponentRules.cs:132
public static IReadOnlyList<CardTag> Tags(int mask)
// FoolComponentDefinitions.cs:38
internal static bool TryCompile(GeneratorOperation operation, out OperationRuntimeSpec? result)
// ChaosCardGenerator/FoolMelodyNames.cs:78
public static GeneratedCard ApplyNameTags(GeneratedCard card)
// ChaosMode/ChaosFoolCardState.cs:50
public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
```

- `TryCompile` 是**唯一**的「模板 → RuntimeSpec」入口，编译器在 `OperationRuntimeSpec` 的对账流程里**优先**调它（Fool 原子以 `SemanticId` 前缀 `fool/` 识别，被排除在旧文本解析器的对账之外，只由 `ValidateLocalizationIndependence` 覆盖）。
- `ApplyNameTags` 在 `AutoAnthonyEditorApi.ApplyEditorDefinition` 里被调用，把编辑后的名字标签归一 —— 编辑产物和生成产物走同一条旋律打标路径。
- `HasValidShootAssembly`：一张牌最多 1 个 `Shoot` 修饰 + 1 个**未链接**的选中敌人伤害操作，不能再有额外效果 / 触发 / 修饰（普通关键词允许）。

## 五、章节导航

| 章节 | 文件 | 内容 |
|---|---|---|
| 运行期与生命周期 | [cardgen-fool-runtime.md](cardgen-fool-runtime.md) | 一张牌从 JSON 到入池到战斗的完整链路、卡钩子 vs 复合 Power 的边界、`[SavedProperty]` 状态、卡面提示、坑与可复用经验 |

## 常见问题

**Q：为什么不直接从牌名解析 `Wah/Cho/Ki`？**
A：牌名是本地化产物，玩家改语言、编辑器重命名都会让标签漂移。标签必须是卡上的显式元数据（`CardTag`）；命名语法（`FoolMelodyNames.Compose`）只负责**展示**。复制卡身份时要一并复制 `MelodyTags`，否则两个语言下行为不同。

**Q：91 个空壳卡是什么？**
A：`ChaosFoolCards.cs` 里 91 个只覆盖 `Slot` 的壳，纯粹给 91 个池槽位编址；真实行为全由组件生成的 `RuntimeSpec` 挂上去。壳存在的意义是让池子有稳定身份（旧存档不漂移），而不是承载逻辑。

**Q：组件契约只有 60 条，怎么覆盖 84 张原卡？**
A：一张原卡会被拆成多个原子（伤害 / 条件 / 触发 / 修饰），多个原卡共用的原子只留一条契约。契约数少于卡数是设计目标 —— 复制整卡等于放弃复用。

**Q：为什么 Fool 的枚举值是追加在 `Colorless` 之后？**
A：`GeneratedCharacter` 是持久化身份的一部分，插值重排会让旧存档指向别的角色。新角色一律 append；同理 `CardTag.Wah/Cho/Ki/Shoot` 也是追加（顺序见 [cardgen-architecture.md](cardgen-architecture.md)）。
