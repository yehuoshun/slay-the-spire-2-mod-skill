# 巨型 Mod 工程模式（9 万行级：分层/裁决/Interop/文档）

> 实战验证（sts2mod 海克斯符文 HextechRunes，独立仓库 9.2 万行 C#，2026-10-07）。超大 mod 的「怎么组织」——不是单一技巧，是整套工程文化。配套：补丁框架 v3 见 [harmony-patch-framework-v3.md](../harmony/harmony-patch-framework-v3.md)。

## ① 单向依赖分层

```text
Platform/Hooks/Config/Localization/Telemetry
  -> Mayhem        （战斗共享状态：Tracking/ProcTracker/序列化分部）
  -> Selection     （选择流程）
  -> Core/Catalog/Runes/EnemyHexes
```

- 多人同步和随机数是横切能力：**所有影响联机一致性的选择结果必须通过明确 payload 或稳定随机输入表达**，不允许依赖「各端本地池子刚好一样」。
- Selection 内部再拆：`Coordinator/`（编排）、`Pool/`（池生成）、`Reroll/`（重随）、`Sync/`（多人编解码）、`EnemyAdjust/`（敌方同步）、`AI/`（代选）、`UI/`（渲染）。

## ② 设计裁决文档（防重复评估 + 记录坑）

- `docs/design-decisions.md`：只收录**代码和 CHANGELOG 读不出来、但改这块代码的人必须知道**的裁决——一句结论 + 理由 + 涉及类名。
- 典型条目：「瞄准镜返还只读 `CardPlay.Resources` 不读本地记账栈」（六次分叉只有房主能量不同——联机分叉的根因记录）；「一呼百应不会递归启动另一批」（防无限重入）；「玩家侧活力火花不能把原版敌方增益直接施加到玩家」（原版 BeforeCombatStart 会覆盖）。
- 裁决可量化：「接二连三计数」条目里写清了原版 `GeneratePlayCount` 一次性算总次数、`Hook.ModifyCardPlayCount` 按监听者顺序累加、0.111.0 没有 Late 版本——**把原版机制证据写进裁决**。

## ③ Interop 双轨（软依赖 / 硬依赖）

| | 软依赖 `HextechRunesInterop` | 硬依赖 `HextechRunesApi` |
|---|---|---|
| 引用 DLL | 不需要（反射 / RitsuLib `[ModInterop]`） | 需要 |
| 没装海克斯 | 照常运行 | 加载失败 |
| 契约 | 已发布签名不再改动，新能力走新方法 + `ApiVersion` 递增 | 跟随版本，升级需重编译 |

- `ApiVersion`（public static int）：反射取不到 = 版本太旧，直接跳过对接。
- 注册时机：按**程序集名**检测（不是 manifest id）；先查已加载程序集，没有就订阅 `AppDomain.CurrentDomain.AssemblyLoad`；注册必须在模型池首次枚举前 + 存档序列化缓存初始化前，窗口关闭后调用抛 `InvalidOperationException` 且不留半登记状态。
- manifest **不要**写 `dependencies`（没装海克斯的玩家也能加载你的 mod）。
- 外部符文 `Rarity` 必须是 `Starter`（原版有别的路径按稀有度抽遗物，只有 Starter 保证抽不到）。

## ④ 开发工具链（脚本驱动的内容管理）

| 工具 | 用途 |
|------|------|
| `hextech_dev.py find '中文名'` | 内容定位（中文名/品级/注册元数据/两种描述/源码引用） |
| `hextech_dev.py loc-copy A.description b.enemyDescription --apply` | 同语义文案复制（默认只展示 diff） |
| `sync_content_txt.py` | 生成 TXT 说明（读 CanonicalVars 静态解析变量，剥离 BBCode；人工描述只经 `--accept-json` 更新） |
| `validate_hextech_content.py` | 九语占位符集合 + BBCode 配平 + 疑似漏译检测（≥4 拉丁字母同 eng 值报 error） |

- 多语言：九种（zhs/eng/jpn/kor/esp/spa/ptb/rus/tha）；**中文批准文案是翻译基准**。
- 版本化 DLL 引用：`versioned-dll-backups/<游戏版本>/game-refs/`（不提交 Git，目录缺失构建直接报错，不回退本机安装）。

## ⑤ 测试文化（三方保障）

- 单元/集成测试：`DeterminismAuditGuardTests`（确定性审计）、`EndlessLoopRewindTests`（无尽回卷）、`Compatibility*Tests`（反射/注册/Interop 兼容）。
- 护栏测试（补丁清单冻结，见 harmony-patch-framework-v3.md ④）。
- 定向回归：文档改动查差异和链接；公式/Hook 用能复现旧错误的定向回归；身份/保存/补丁变化才查快照——**不每次都跑整套**。

## 相关 API 速查

| API | 说明 |
|-----|------|
| `AppDomain.CurrentDomain.AssemblyLoad` | 等待依赖 mod 加载（软依赖注册） |
| `ApiVersion`（Interop） | 版本协商 |
| `IHextechHealingMultiplierProvider` | 符文自报系数（同类只乘一次：`IsFirstOwnedInstance`） |
| `HextechStableRandom` | 稳定输入确定性工具（相同输入相同结果，重复触发要 ordinal/回合/玩家区分） |