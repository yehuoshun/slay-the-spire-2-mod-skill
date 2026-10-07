# 内容事件 Mod 组织模式（facade 拆分 + 一事件一文件）

> 实战验证（sts2mod 集成战略事件 IntegratedStrategyEvents v0.5.6，2026-10-07，28.6k 行压轴）。大型「事件内容包」的组织方式：让几百个事件可维护、可校验、可并行创作。

## ① 目录契约（README 明写，结构即规范）

```text
src/Events/<Name>Event.cs            一个事件一个文件——只描述选项/分支/翻页
src/Events/<Name>Event.*.cs          partial 辅助（分支表等私有机制）
src/Events/Definitions/<Name>Event.Definition.cs   定义 partial：肖像文件名/布局/本地化
src/Events/IntegratedStrategyEventEffects.cs(+.*)  共享效果 facade（HP/金币/奖励/发卡/牌组操作）
src/Events/IntegratedStrategyEventRewards.cs(+.*)  随机奖励 facade（遗物/药水/卡牌奖励屏/卡池）
src/Events/IntegratedStrategyEventSpawnRules.cs(+.*) 生成规则 facade（幕限制/条件门）
src/Relics/*Relic.cs                 一个事件遗物一文件
src/Encounters/*Encounter.cs         一个自定义遭遇一文件
```

- `tools/validate_event_structure.sh`：CI 校验「事件流/定义/资源保持分离」。
- 事件模型继承 RitsuLib `ModEventTemplate`，本模组再包一层：

```csharp
public abstract partial class IntegratedStrategyEventModel : ModEventTemplate
{
    protected abstract IntegratedStrategyEventDefinition Definition { get; }   // 肖像/布局/对齐契约
    public override string? CustomInitialPortraitPath => Definition.PortraitPath;
    public override bool IsShared => false;
    protected override Task BeforeEventStarted(bool isPreFinished) { ... }
}
```

## ② 共享 facade（效果/奖励/生成规则单点）

- 事件只描述「流程」，效果实现全部走 facade（`IntegratedStrategyEventEffects.GainGold(...)` 等）——同一动作全 mod 一致。
- 奖励/生成规则同理：`Rewards.RollRelic(...)`、`SpawnRules.IsAllowedInAct(...)`（按幕的事件池限制）+ `Gates`（HP/金币/牌组/卡池条件门）。
- 好处：文案/数值口径统一、测试可单测 facade、新事件只拼装。

## ③ 创作标准（EVENT_AUTHORING_STANDARDS.md）

- 图片：1280×720 16:9 PNG、8-bit RGBA/sRGB、Lanczos 缩放、Godot 无损导入（compress/mode=0、无 mipmap、不二次缩放）。
- 文案：用户原文视为定稿（不删改压缩）；固定字号（正文 28px、选项说明 22px）不缩字号硬凑排版；富文本按语义少量用（`[red]` 伤害/[green] 恢复/[gold] 奖励/[purple] 异常…），嵌套反向闭合。
- 选项：标题写「玩家要做什么」，说明写代价/奖励；数值与逻辑逐项核对；锁定选项同标题改说明为真实门槛；离开选项放最后；hover 预览用 helper 不用纯文本。

## ④ 终局树洞系统（TreeHoles——临时 ActMap 体系）

- 多套 `FinaleActMap`（深渊丛林/自在天/欲望厅/无尽终局），实现 `IIntegratedStrategyTemporaryActMap`——事件生成**临时章节地图**（替换当前幕，事件结束恢复原图）。
- `FinaleTransitionPatch` 系列：进下一幕/架构师选项显示与点击/创建房间 4 个 patch 全接管终局过渡。
- `TreeHoleSessionManager`：会话状态机 + `AwaitNextProcessFrame()`（`NGame.Instance.AwaitProcessFrame()` 等帧）——切图/结算的帧同步。
- 音乐控制器：终局 BGM 切换。

## ⑤ 两面对决遭遇（TwoSidedEncounter）

```csharp
public abstract class IntegratedStrategyTwoSidedEncounter<TMonster> : ModEncounterTemplate
{
    public const string LeftSlot = "crusher";
    public const string RightSlot = "rocket";
    public override bool FullyCenterPlayers => true;                       // 玩家居中
    public override IReadOnlyList<string> Slots => [LeftSlot, RightSlot];  // 左右两槽
    public override string? CustomEncounterScenePath => SceneHelper.GetScenePath("encounters/kaiser_crab_boss");
    public override IEnumerable<MonsterModel> AllPossibleMonsters => [Monster<TMonster>()];
}
```

- 复用原版 kaiser_crab_boss 场景的槽位/镜头/生成逻辑（侧翼夹击）；精英/首领两个变体基类（RoomType）。

## ⑥ 测试三件套（tests/）

- `DesignGuardrails`：设计护栏（模型生命周期回调使用检查，防用错回调）。
- `MapCompatibilityProbe`：地图兼容探针（自定义地图节点与事件共存验证）。
- `snapshots/<游戏版本>/`：补丁/模型/存档属性回归快照（embedded resource 进 DLL）。

## ⚠️ 待实测疑云（源生成器坑第二案例）

- `src/UI/IntegratedStrategyEventPortraitDriver.cs` 覆写 `_Process` 做肖像逐帧适配——本 mod 也是 **Microsoft.NET.Sdk** 单 DLL 编译（PRTS 光标实测该环境下自定义 Node 覆写回调不被调用）。
- 无 ProcessFrame 替代路径（仅 TreeHoles 有 AwaitProcessFrame，是另一机制）→ 肖像适配可能只在初始 `CallDeferred` 执行一次、缩放不跟随。
- 与「mod 成熟运营」矛盾，**需游戏内实测定案**；修复路径现成：PRTS 的 SceneTree.ProcessFrame 信号方案（见 overlay/overlay-cursor.md）。