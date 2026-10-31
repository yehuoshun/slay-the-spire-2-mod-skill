# 卡牌编辑器 API（CardTinkering / 身份编辑）

> 源码核对（CardTinkeringApi v6 / AutoAnthonyEditorApi v2 / AutoAnthonySettingsApi v5 / AutoAnthonyBalanceAdjustmentApi v1）。编辑器生态的完整模式：**本地化无关、预算校验先行、结构化序列化**。

---

## 一、定位

- `CardTinkeringApi`（v6）：本地化无关的卡牌编辑集成面——序列化完整结构化卡 payload、评估整卡预算、校验替换操作列表、重建描述/升级，**全程不解析渲染文本**
- 配套衍生 mod CardTinkering：从战斗外（含奖励页）卡组页面进入工匠台，可编辑全部 481 张常规原版牌，不替换原版池、不转换未修改牌
- 数值货币：`100` 单位 = 1 点普通单目标伤害（与生成器同一货币）

## 二、核心 API 签名

```csharp
CardTinkeringApi.SerializeCard(GeneratedCard card) → string      // 存 RuntimeSpec+命名本地化+升级槽身份
CardTinkeringApi.DeserializeCard(string payload) → GeneratedCard // 适合编辑器自己的持久化/多人 payload

CardTinkeringApi.GetComponentPrototypes(ComponentProfileRequest) // 枚举已解析 profile（含外部注册）
CardTinkeringApi.GetKeywordPrototypes(...)                       // profile 本地的基础/增/删关键词权限
CardTinkeringApi.GetEditableValues(GeneratorOperation op)        // 可编辑数值槽
CardTinkeringApi.TrySetEditableValue(op, slotId, value, ...)     // 改槽值（受 schema 校验）
CardTinkeringApi.Analyze(GeneratedCard card, ...)                // 完整分析
CardTinkeringApi.AnalyzeAtX(card, resolvedX, ...)                // 特定 X 下的分析
CardTinkeringApi.IsVariableX(card) / GetEffectiveCost(card, resolvedX)
CardTinkeringApi.GetKeywordValue(CardTag tag) → double           // 关键词价值（平衡模型）
CardTinkeringApi.Validate(GeneratedCard shell, IReadOnlyList<GeneratorOperation> ops) → CardTinkeringValidationResult
CardTinkeringApi.Rebuild(GeneratedCard shell, IReadOnlyList<GeneratorOperation> ops) → GeneratedCard
```

**流程铁律**：`Validate` 通过才 `Rebuild`；失败结果是用户可见的非法组合，不是该安装的运行时操作。

## 三、预算评估（编辑器要精确不等式用它）

- `EvaluateBudget(...)`：正值 + 线性负面补偿 + 乘法负面容量 + 净值 + 普通 shell 上限
- `EvaluateComponent(...)`：**刻意无上下文**——移动组件不继承旧触发器的折扣
- 平衡调整监听：`AutoAnthonyBalanceAdjustmentApi` v1 注册定义变更回调，池级调整跳过独立编辑的单卡定义

## 四、单卡身份编辑（AutoAnthonyEditorApi v2）

> 重命名一张生成卡 / 换卡图**唯一**受支持路径；`ApplyTinkeredDefinition` 直连路径拒绝改名、不读卡图覆写。

```csharp
AutoAnthonyEditorIdentity identity =
    AutoAnthonyEditorApi.RerollEditorIdentity(card.Generated, seed);   // 确定性、无副作用
GeneratedCard rebuilt = CardTinkeringApi.Rebuild(card.Generated, proposedOperations);
AutoAnthonyEditorApi.ApplyEditorDefinition(card, rebuilt, identity);
```

- `RerollEditorIdentity` 复用生产组件相关性、费用/类型/颜色匹配、同角色名字部件、Strike/Form 后缀规则、随机卡图设置
- `ApplyEditorDefinition` 以 `identity.Name` 为权威，但**拒绝**改：费用、星费、类型、目标、稀有度、角色、标签、shell 拥有的升级行为
- 编辑后定义 + 卡图源/变体存卡实例；缺装饰 provider 的机器渲染稳定原生 fallback
- 外部角色：`IExternalEditorIdentityProvider` + ProfileId 重载；编辑能力**独立于生成能力**注册（`AutoAnthonyEditorApi.RegisterExternalCapabilities`），消费者先查 `Supports`

## 五、原生卡拆解（只读目录 + 可编辑水合）

- `NativeCardDecompositionApi`（只读，Card Tinkering 工具用）：567 条 v111 非多人参考记录 / 449 组件契约 / 7 个原生关键词；全部 `ReferenceOnly=true`、`GenerationEligible=false`
- `FindByNativeId` 返回 ModelId 全部变体；`Resolve` 歧义时需显式模板绑定
- `TryCreateBaseDefinition` 物化 481 张已审角色/无色 recipe（可执行结构化操作）；`TryCreateDefinition` 含精确原生升级动作（repeat/打出添加/全选/升级前打出）
- 86 个生命周期敏感引用扩展保持结构化但**不可执行**（等专用适配器）
- 游戏侧 `AutoAnthonyNativeCardApi.TryResolve(CardModel, ...)` 处理原生 ModelId + Mad Science 的 saved type/rider 状态；preview **无副作用**；**不得为了检视而替换原版卡**——只有用户确认的编辑才物化 freeform `ChaosCardModel`，未触碰的原版卡与池保持不变

## 六、设置 API（v5/v2）

- `AutoAnthonySettingsApi` v5：一个不可变快照暴露全部用户设置（含可选平衡调整规则、独立 `AddGeneratedCards` 池开关、即时 `DecomposeOriginalCards` 展示开关——后者保持原生执行、仅用结构化目录投影未触碰原版卡描述）
- `ComponentRunSettingsApi` v2：池开关与主 `Enabled` 分离（关随机池插入 ≠ 关组件执行/原生卡编辑）；不暴露 mutation，生效的 run/房主权威多人设置归它管
