# 组件化卡牌架构与设计原则

> AutoAnthony 0.3.147 精读笔记（COMPONENT_API.md + 源码核对）。这套架构的价值不在 API 本身，而在**把「卡牌语义」从「文本/代码」里彻底解放出来**的设计决策。

---

## 一、分层模型（组件 → 操作 → 运行时规格）

```text
ComponentAtom（创作期原子）
  ├─ Template      — ASCII 语义 ID（如 "N:B" 加 Block、"T:Poison" 上毒），非本地化文本
  ├─ Scope         — 目标/触发/修正角色，组装合法性用
  ├─ ChineseText   — 中文投影（创作辅助/旧存档迁移，永不当执行源）
  ├─ LocalizedText — 中英模板，占位符命名 RuntimeSpec 数值槽（[[damage]]/[[hits]]/[[amount:cardinal]]）
  ├─ RequiresSingleTarget / CardReference — 目标约束
  ├─ RuntimeSpec   — 权威执行规格（opcode/variant/target/zones/flags/命名数值槽/条件/触发）
  ├─ Category      — 编辑器分类（可选，默认从 RuntimeSpec 推导）
  └─ Multiplicity  — 出现作用域
        ↓ 组装
GeneratorOperation（生成后的操作，含数值实例）
        ↓ 编译/校验
OperationRuntimeSpec（版本化运行时规格，快照/多人传输的权威载体）
```

**核心规则：执行永远走 RuntimeSpec，绝不 recompile 本地化句子。** 游戏侧 `OperationRuntimeSpecCompiler.RequireStructured` 强制结构化；`CompileLegacy` 只用于旧存档水合/启动探测。

## 二、OperationRuntimeSpec 要点（源码核对）

- `sealed record OperationRuntimeSpec`，`CurrentSchemaVersion = 1`，带 `Validate()`
- **签名缓存**：`StableSignature()` / `RoutingSignature()`（opcode+variant+target+sourceZone）/ `ShapeSignature()` / `XShapeSignature()`——用签名做组件等价共享（SchemaKey）和迁移判断
- 数值槽：`RuntimeValueSlot(Id, BaseValue, Source="fixed", Offset, ...)`；`ScaledSummonX` 类槽支持 X 系数渲染（系数 3 + X+2 → 3X+6）
- 条件/触发：`RuntimeConditionSpec(Kind, Subject, ValueSlot)`、`RuntimeTriggerSpec(Kind, Lifetime, ThresholdSlot)`
- **升级走结构化**：`CardUpgradeEffect.Kind + OperationIndex + ValueSlotId + Delta`，新快照不写双语标签；`AddedKeywords` 等旧数组仅作 v2/schema 5-9 迁移输入，schema 10 以结构效果为权威（投影时 union 两者）

## 三、ComponentSemanticFlags（语义标记，组装器跨切面规则）

| Flag | 对组装器的含义 |
|---|---|
| `Beneficial` | 可满足卡的正面收益需求（注册了非负估值也隐含此标记） |
| `Negative` | 负面效果，参与补偿管线 |
| `Restricted` | 必须是 Power 上的效果；非 Power 上强制 Exhaust |
| `EnemyDamage` | 敌方伤害，可使无门槛非 Power 卡归类为 Attack |
| `PowerFoundation` | 足以支撑一张 Power 卡的持久状态 |
| `ScalableReward` | 数值可随整卡预算缩放 |
| `SelfCardMovement` | 移动/重放生成卡自身，Power 上拒绝 |

> ⚠️ 只用于跨切面合法性事实；目标/区域/数值/条件/触发放 RuntimeSpec 专属字段；执行与估值走注册路由。外部 Template 名**永不**被检查来推断玩法。

## 四、组件共享与归属

- 归属属于**原生出现（source occurrence）**，不属于执行副本：每个原版卡上的操作有角色前缀 SemanticId（`ironclad/anger/0`）
- 等价共享按 SchemaKey：相同结构化模式共享一条执行路由，但各自保留 `AtomCounts` 频率/数值证据
- 字符前缀旧别名只在**水合旧快照**时接受；新 recipe 出现即被构建期不变量拒绝
- 原子性边界：拆分会导致运行时 payload 丢失/状态捕获时机变化的必须保持原子（Nightmare 选牌、Prolong 快照当前格挡、临时减益规则 Power 等）

## 五、Multiplicity 三作用域

| 作用域 | 语义 |
|---|---|
| `Repeatable` | 普通可叠加 |
| `SinglePerCard` | 同一语义键一张卡最多一次（原「不可叠加」） |
| `UniquePerPool` | 整池唯一，**卡通过校验+命名+查重后才消耗**，失败组装不消耗 |

## 六、快照与 schema 迁移规则（可复用铁律）

- Schema 5 是最老可原地续档版本；1-4 历史可读但活跃 run 重新生成；schema 10 为当前结构化格式
- 快照序列化存 **runtime specs + 操作 ID**，不是本地化文本的解释
- 删除/改动组件必须有当前 run 快照的迁移路由
- **加枚举必须 append，序列化数值枚举禁止重排**
- 生成侧不假设「采到组件 = 卡被接受」，失败组装可重试且不影响频率
- 本地化模板是运行时缓存不序列化：快照已含卡文本 + 权威 RuntimeSpec，load 时从字段重建命名模板

## 七、单工程双用（生成器 + 游戏运行时共享源码）

```xml
<!-- ChaosMode.csproj：游戏工程用 <Compile Include> 链接生成器源码，同一份代码双场景编译 -->
<Compile Include="..\ChaosCardGenerator\RandomCardGenerator.cs" Link="Generator\RandomCardGenerator.cs" />
<EmbeddedResource Include="..\ChaosCardGenerator\Data\catalog_runtime_specs.json" Link="Data\catalog_runtime_specs.json" />
```

> 生成器是独立控制台程序（`--self-test` 自检），游戏侧链接同一批文件。数据用 EmbeddedResource 双端共享，避免双份维护。适合「工具链 + 运行时」一体的 mod 工程。

## 八、审计文化（构建即校验）

- 构建时跑：结构化目录验证、RuntimeSpec 注册表验证、执行路由覆盖率、本地化边界（`Verify-LocalizedTextBoundaries.ps1`）
- 生成器 `--self-test` + `tests/` 回归（多人开局准备、星费缓存、转化池、统一 X 升级）
- `ApiContractSmoke/`：以外部消费者身份独立编译的 API 契约测试——**公开 API 有编译级护栏**
- 历史池漂移语料 `audits/structured_operation_refactor/0.3.5_pre_component_api.json.gz` 作诊断基线
