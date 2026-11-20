# 组件 API 跟随 v0.113 的集成变更（0.3.147 → 0.3.165）

> 来源：[mewcodex/AutoAnthony](https://github.com/mewcodex/AutoAnthony) 上游 `COMPONENT_API.md`（0.3.165 版，新增「v113 integration notes (0.3.158–0.3.165)」小节）+ `docs/game-v0.113-*.md`。
> 适用：**只有**在开发 AutoAnthony 生态集成（组件包 / 随机卡池 / 卡牌编辑器 / 外部角色）时才需要读；写普通卡走 `card/`。

## 一、注册接口没变，但必须对着 v113 程序集重编

`ComponentPackageApi` / `ComponentRuntimeApi` / 关键词注册的**签名一个都没动**。变的是它们背后依赖的原生类型与枚举：

- 必须对 **STS2 v113 的 `sts2.dll` + 当前 `AutoAnthony.dll`** 一起重编，旧的已编译二进制不再兼容。
- `GeneratedCharacter.Fool` **追加在 `Colorless` 之后**（不重排旧值，枚举全文见 `ChaosCardGenerator/RandomCardGenerator.cs:157`）；角色 profile 与池子从「5 角色 6 池」变成「**6 角色 7 池**」（旧六池快照仍要能恢复：`docs/fool-integration-plan.md:19`「Preserve old six-pool snapshots」）。
- `GeneratedCardName` 新增第 4 个**可选位置成员** `IReadOnlyList<CardTag>? MelodyTags = null`：3 个参数的调用仍编译，但 record 的构造/解构签名变了 —— 所以旧二进制必须重编（这也是「注册接口没变」却不能直接复用的原因）。
- `CardTag` 追加 `Wah` / `Cho` / `Ki` / `Shoot`（`RandomCardGenerator.cs:161`：`… OstyAttack, Wah, Cho, Ki, Shoot }`）。**一律 append**，永不插值重排。

## 二、旋律标签：结构化，不是从名字解析

- 旋律是结构化标签（`Wah`/`Cho`/`Ki`），运行时**绝不从本地化卡名反推**；`FoolMelodySubset.WithMask(atom, mask)` 用 bit 1/2/4（`Wah=1`/`Cho=2`/`Ki=4`）选非空子集 —— 非空子集只有 **7 个（mask 1–7）**，越界（`<1` 或 `>7`）直接抛 `ArgumentOutOfRangeException`（`ChaosCardGenerator/FoolComponentRules.cs:134`）。
- `WithMask` 一次同步三处：隐藏值槽 `melody_mask`（`BaseValue=mask`、`Upgradable=false`、`Explicit=false`）、`CardFilter = "melody_{mask}"`、`melodies` 双语文本槽；atom 不带 `melody_subset_slot` flag 会被拒（`FoolComponentRules.cs:139`–`:153`）。
- 隐藏字段 `melody_mask` 是**语义选择器**，不是随机强度、也不是升级候选。
- 复制卡身份时要一并复制 `MelodyTags`：用 `GeneratedCardName copy = originalName with { };`（record 的 `with` 会把第 4 个成员一起带过去），再 `FoolMelodyNames.ApplyNameTags(definition with { Name = copy })`；`ApplyNameTags` 只在 `Name.MelodyTags` 存在时把 `Wah/Cho/Ki` 同步进 `card.Tags`，**其它标签原样保留**（`ChaosCardGenerator/FoolMelodyNames.cs:80`–`:82`；调用范例 `ApiContractSmoke/ExternalConsumer.cs:19`–`:23`）。`AutoAnthonyEditorApi.ApplyEditorDefinition` 也经同一个 `ApplyNameTags`（`:135` 与 `:156` 两处），保证编辑产物与生成产物同路径打标。
- `FoolComponentRules.HasValidShootAssembly(operations, allowIncomplete = false)` 校验箭矢装配：**最多 1 个 `Shoot` 修饰 + 1 个未链接的选中敌人伤害操作**，不能有额外效果 / 触发 / 修饰（普通关键词允许）。`allowIncomplete: true` 只用于生成期「装配还没收尾」的追加判定（`FoolComponentRules.cs:110`，由 `CanAppend` 传入），成卡时仍用默认 `false` 复查，并另加 `HasValidExhaustDependencies(operations, ultimateChaos)`（`:88`）。

## 三、自降费组件的计价规则（新）

- 印刷自减**不能超过固定基础能量**；含重复触发的总期望节省被基础费**封顶**。当前口径 **700 / 每点未来能量**（`ChaosCardGenerator/SelfCostReductionValuation.cs:7` `ValuePerEnergy = 700d`）：2 费卡自减 2、触发频率 2 → 最多 1400，而不是 2800。
- 多组件同时自减时，按比例**分享**这个上限：`ownSavings * Math.Min(1d, cost / totalSavings) * ValuePerEnergy`（`SelfCostReductionValuation.cs:19`），所以逐组件估值之和与整卡估值对得上。
- 星（Star）与「即时返还能量」是**另一回事**，不参与上述封顶。
- 独立的原子估值没有 shell 上下文，所以只能拿到「无封顶」的裸值 —— 封顶逻辑只在整个卡壳装完后的整卡估值里做。

## 四、持久化与模板复用

- 持久化定义存 **RuntimeSpec / 值槽 ID / 卡名 / 美术身份 / 小本地化 ID**，重做（rework）之后**组件身份必须稳定**，否则旧存档指向错的效果。
- loader 在复用旧模板前要**校验 opcode / variant**，并恢复 one-card transform 的显式计数；**保留旧存档的效果**，而不是借用改写后组件的描述（0.3.165 的 Expertise 就是范例：原生改成「抽 2 + 整手 Retain」，旧存档仍保留「抽 2 + 这 2 张 Retain」的文本与可执行规格）。

## 五、原生卡拆解目录的规模变化

| 指标 | v111（0.3.147） | v113（0.3.165） |
|---|---|---|
| 原生卡记录 | 567 | **659** |
| 覆盖原生类 | — | 651 |
| 组件契约 | 449 | **517** |
| 原生关键词 | 7 | 7 |
| `TryCreateBaseDefinition` 可物化 recipe | 481 | **566** |
| 生命周期敏感、结构化但不可执行 | 86 | 93 |

- 上表口径出处：v113 列 = `COMPONENT_API.md:56`–`:57`（659 记录 / 651 原生类 / 517 契约 / 7 关键词）与 `:63`–`:66`（566 可精确物化、93 lifecycle-sensitive 仅结构化）；v111 列 = `NATIVE_CARD_TINKERING_IMPLEMENTATION.md:43`–`:45`、`:51`（567 / 449 / 481 / 86）。别把两列混着抄。
- v113 的投影用原生 **`HitCount` / `CalculatedHits`** 身份，**别拿数字字面量**（伤害 × 次数）去匹配卡（`COMPONENT_API.md:68`）。
- `UpgradeDelta = 0` 合法（Speedster 3 点伤害只因 Innate 关键词升级，`COMPONENT_API.md:70`；该卡的 2→3 修正在 `docs/game-v0.113-follow-up-audit.md:37`）。
- `NameCatalog` 的 normal / ultimate 语义已改：**Ultimate Chaos 用「六角色 + 无色」合并后的 ultimate 语料**取名（`CardIdentityAndUpgradeGenerator.cs:1698` 取 `ComponentProfileRequest(character, true).NameCatalog`、`:1702` 断言 id 集合相等、`:1717` 用 ultimate 目录生成），**普通 profile 只用自己的语料**（`ComponentAssemblyGenerator.cs:844` `_unlockComponentRoles ? "ultimate" : _character.ToString()`），**外部 profile 保留自己注册的命名权威**（`CardIdentityAndUpgradeGenerator.cs:1321` `RegisterExternalParts(packageId, entries)`）。

## 六、结论 / 迁移清单

1. 换 v113 dll + 最新 AutoAnthony.dll，整包重编（别指望二进制兼容）。
2. 检查自己注册的角色枚举值是否 append、池子是否按「6 角色 7 池」来的。
3. 如果用了旋律标签：改成结构化 `CardTag`，复制身份时带上 `MelodyTags`。
4. 如果做了自降费组件：确认总期望节省被基础能量封顶，多组件按比例分享。
5. 如果持久化了自己的定义：确认组件身份稳定、loader 复用前校验 opcode/variant。
6. 跑一遍原生拆解审计（`--native-decomposition-audit`）与库激活兼容测试，确认 recipe 数与你预期的 659 / 566 一致。
7. 如果你复用或新写组件：身份按 **RuntimeSpec schema**（opcode / variant / 槽结构）对齐而不是按文本，语义相同的即时效果共用一条契约；需要捕获「选中的牌 / 目标 / 当前值 / 未来选择」的操作保持原子，普通延迟子句则「触发器（如 `C:NextTurnStart` / `C:NextTurnsStart`）+ 同一个即时 payoff」，只有节奏影响估值（wiki `Component-API-Overview.md:75`–`:88`）。

## 相关

- 运行时执行层的通用技巧（OwnsOperation 掩码、`LiveCombatSourceCard`、增量成长状态、升级单入口）：[cardgen-runtime.md](cardgen-runtime.md)
- 愚者（Fool）词条系统：[cardgen-fool.md](cardgen-fool.md)
- 数值口径变化（自减 2800、首飞刀 200、回合内抽牌 3.2、金币阈值 200）：[cardgen-balance.md](cardgen-balance.md) §六
