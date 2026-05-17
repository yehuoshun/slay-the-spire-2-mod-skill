# 杀戮尖塔 2 Mod 开发技能

> 🦞 自包含完整指南——从零搭建环境到发布成品 Mod，只看这一份就够了。

## 这是什么

一个 OpenClaw AI 技能，让 AI 助手帮你写 Slay the Spire 2 模组。配备了这份 Skill 后，AI 能直接给出**可编译、可运行的完整代码**，覆盖 STS2 Mod 开发的全部内容类型。

不需要翻 Wiki、不需要看源码、不需要踩坑——把需求说清楚，AI 就能落地。

## 覆盖范围

### 🏗 从零到运行
- .NET 9 SDK + Megadot 编辑器环境配置
- Rider / Visual Studio 项目搭建
- `dotnet new` 模板安装（空白/内容/角色三种）
- Build vs Publish 的区别与使用场景
- `mod_manifest.json` 完整字段说明
- 模组文件结构（`.dll` / `.pck` / `.json`）

### 🧩 Harmony 补丁
- 固定目标 Patch、动态目标 Patch、方法体替换
- 错误隔离：`PatchAllSafe` 每类独立 try-catch
- 平台条件补丁（桌面/Android 分支）

### 🃏 自定义卡牌
- `CardModel` 构造函数 5 参数详解
- 基类封装 → `[Pool]` 自动注册
- 伤害链 API：`DamageCmd.Attack().FromCard().Targeting().Execute()`
- 抽牌 / 选牌 / 升级 / 丢弃
- 打出条件（`IsPlayable`）与金光提示（`ShouldGlowGoldInternal`）
- 卡牌关键词（消耗/虚无/保留/固有）与标签（打击/技能等）
- `OnUpgrade` / `OnTurnEndInHand` 回调
- 肖像图路径规范（裁切纹理 + 大图）

### 💎 自定义遗物
- `RelicModel` 完整实现，稀有度与获取途径对照
- DynamicVar 动态变量系统（能量/伤害/格挡等）
- 回合钩子：`AfterSideTurnStart` / `BeforeSideTurnStart`
- 遗物池分类（角色池/共享池/事件池/兜底池）
- 修改角色初始遗物、`Flash()` 闪烁提示
- 遗物图标三步流程（大图 → 剪影 → AtlasTexture）

### 🧪 自定义药水
- 三种模式：伤害药水 / 效果药水 / 自动消耗（复活）
- `PotionRarity` / `PotionUsage` 枚举详解
- `CanBeGeneratedInCombat` 随机生成控制
- `ShouldDie` / `AfterPreventingDeath` 复活逻辑

### ⚡ 自定义能力（Buff/Debuff）
- `PowerType`（Buff/Debuff/Neutral）
- `PowerStackType`（Intensity 层数 vs Duration 状态）
- `IsInstanced` 多实例控制
- `AllowNegative` 负层数支持
- `smartDescription` 动态变量描述

### ✨ 自定义附魔
- `EnchantmentModel` 伤害/格挡加成
- `ModifyCard()` 附魔时修改卡牌属性
- `CardCmd.Enchant()` 附魔 API

### 📜 自定义事件
- `EventModel` 基础事件 + 多页选项
- `EventOption` / `SetEventState` / `SetEventFinished`
- `AncientEventModel` 先古之民完整对话系统
- `AncientDialogueSet` 对话组配置
- 对话键格式（初见/角色特殊/通用对话）
- 先古之民 6 种必备资源路径

### 👹 自定义怪物
- `MonsterModel` + `MonsterMoveStateMachine`
- `MoveState` 构造与意图显示
- 多状态循环 / `RandomBranchState` 随机分支 / `ConditionalBranchState` 条件分支
- `EncounterModel` 遭遇定义与站位场景
- 怪物场景结构规则（`NCreatureVisuals` / `%Bounds`）

### 👤 自定义角色（最全）
- `CardPoolModel` — 卡池标题/能量图标/边框材质/缩略卡颜色
- `RelicPoolModel` / `PotionPoolModel`
- `CharacterModel` 完整骨架
- 完整资源清单（20+ 种场景/贴图路径）
- 场景结构规则（`NCreatureVisuals` / `NEnergyCounter` / `NCardTrailVfx` / `MegaLabel`）
- 卡池边框 HSV 着色器
- Godot 脚本检索修复（`ScanAssembly`）
- Spine 动画插件安装

### 🔧 进阶工具
- **解包与逆向**：Rider 反编译 / ILSpy / dnSpy / GDRE Tools 资源提取
- BaseLib vs 纯自研对比（依赖/扩展性/更新风险六维度）
- BaseLib 进阶：`DynamicEnumValueMinter` / `SpireField` / `AddedNode` / `[SavedProperty]`
- UI 开发（CanvasLayer）/ 联机适配
- 本地化完整路径速查（11 种内容类型）
- YuWanCard Builder Pattern 链式构造

### ⚠️ 避坑手册
12 个常见坑及对策（图标不显示/联机状态丢失/IL2CPP 崩溃/节点销毁/Steam 无日志/Harmony 版本/场景脚本找不到/Publish vs Build 差异等）

## 快速开始

```bash
# 安装到本地
cd ~/.openclaw/workspace/skills
git clone https://github.com/yehuoshun/slay-the-spire-2-mod-skill.git
```

安装后，在 OpenClaw 对话中直接描述你的 Mod 需求即可，例如：

> "帮我做一个 2 费攻击卡，造成 8 点伤害，升级后造成 12 点，目标为单个敌人"

> "帮我做一个遗物，每回合开始时给 1 点能量，稀有度为 Starter"

> "帮我做一个自定义角色，卡池用紫色边框"

## 设计理念

- **自包含**：无需翻阅外部 Wiki、教程、源码，Skill 内部涵盖全部必要知识
- **两条路线**：BaseLib 快速封装 vs 纯自研零依赖，两种方式都给出完整代码
- **避坑优先**：先告诉你哪里会卡住，再教你怎么写
- **干就完了**：面向结果，复制即用

## 鸣谢

- [STS2Plus](https://github.com/StephenSHorton/STS2Plus) — 修正器架构、UI 组件、联机同步参考实现
- [ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) — 官方模组模板（空/内容/角色三种）
- [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) — 社区 STS2 Mod 框架（CustomModel 系列、ModConfig 设置面板、图像路径工具、扩展方法，可选）
- [Sts2-YuWanCard](https://github.com/YuWan886/Sts2-YuWanCard) — 生产级角色 Mod（分阶段初始化、自动路径发现、资源预加载、场景注册）
- [Megadot](https://megadot.megacrit.com) — 杀戮尖塔 2 专用 Godot 编辑器
- [B站开发教程 01](https://www.bilibili.com/opus/1179300682687053826) — 环境搭建、PCK 导出、资源替换
- [B站开发教程 02](https://www.bilibili.com/opus/1179604439936270359) — 自定义遗物、DynamicVar、遗物池
- [B站开发教程 03](https://www.bilibili.com/opus/1179979923167641608) — 自定义卡牌、伤害链、选牌抽牌
- [B站开发教程 04](https://www.bilibili.com/opus/1180032536494997541) — 自定义药水、三种使用模式
- [B站开发教程 05](https://www.bilibili.com/opus/1180713881530531843) — 自定义附魔、附魔加成
- [B站开发教程 06](https://www.bilibili.com/opus/1180714323922649110) — 自定义事件、先古之民
- [B站开发教程 07](https://www.bilibili.com/opus/1181126133981118470) — 自定义能力 Buff/Debuff
- [B站开发教程 08](https://www.bilibili.com/opus/1182961747166756931) — 自定义角色、Spine 动画
- [B站开发教程 09](https://www.bilibili.com/opus/1183380755590414377) — 自定义怪物、遭遇定义

## 作者

**yehuoshun** 🦞

---

*有人生在罗马，有人生来就是牛马。但牛马写的 Mod，不打折。*
