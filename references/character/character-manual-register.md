# 角色手动注册 + 初始化时序（ModelDb 已初始化场景）

> 实战验证（sts2mod 俄洛伊 Illaoi v0.2.0，2026-10-07）。BaseLib 依赖角色 mod 的手动注册模式，解决「加载时序不确定（ModelDb 可能已初始化）」+「注册后缓存不失效」两个问题。对照：[character-advanced.md](character-advanced.md)（纯原生一键注册）。

## 核心问题

mod 初始化时 ModelDb 可能已经建好（BaseLib 先行等）。此时：模型不注册 → 角色不出现；注册后缓存不失效 → 卡/遗物查不到。

## 模式：ModelTypes 清单 + 兜底注入 + 双缓存重置

```csharp
public static void Initialize()
{
    InjectSavedPropertyCaches();   // ① [SavedProperty] 类型注入（硬规则 5）
    EnsureModelsRegistered();      // ② 兜底注入
    RegisterPoolEntries();         // ③ ModHelper.AddModelToPool（如 EventRelicPool 里塞自定义遗物）
    Harmony harmony = InstallHooks();
    AssetHooks.Install(harmony);   // ④ 资源注入总闸（见 character-asset-hooks.md）
    ResetModelDbCaches();          // ⑤ 清 ModelDb 静态缓存
    ResetOwnCardPoolCache();       // ⑥ 清自定义卡池实例缓存
}

private static void EnsureModelsRegistered()
{
    if (!ModelDb.Contains(typeof(Ironclad))) return;   // ModelDb 未初始化 → 留给自动注册流程
    foreach (Type type in MyContent.ModelTypes)        // 全部模型集中清单
    {
        if (ModelDb.Contains(type)) continue;
        ModelDb.Inject(type);
        ModelDb.GetById<AbstractModel>(ModelDb.GetId(type)).InitId(ModelDb.GetId(type));
    }
}
```

- `ModelTypes` 数组集中声明全部模型（角色/池/卡/遗物/怪物/能力），注册/兜底/测试三处共用。

## 缓存重置（⑤⑥）

```csharp
// ⑤ ModelDb 8 个静态缓存字段，反射清空，下次访问重建
string[] cacheFields = ["_allCards", "_allCardPools", "_allCharacterCardPools",
    "_allPotions", "_allPotionPools", "_allCharacterPotionPools",
    "_allRelics", "_allCharacterRelicPools"];
foreach (string f in cacheFields)
    typeof(ModelDb).GetField(f, BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);

// ⑥ 自定义卡池实例缓存
CardPoolModel pool = ModelDb.CardPool<MyCardPool>();
foreach (string f in new[] { "_allCards", "_allCardIds" })
    typeof(CardPoolModel).GetField(f, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(pool, null);
```

## 解锁进度跳过（自定义角色专用）

原版 `ProgressSaveManager` 的 epoch 检查（15 精英/15 Boss/角色解锁）遇自定义角色会误判，prefix 返回 false 跳过：

```csharp
harmony.Patch(RequireMethod(typeof(ProgressSaveManager), "CheckFifteenElitesDefeatedEpoch",
    BindingFlags.Instance | BindingFlags.NonPublic, typeof(Player)),
    prefix: new HarmonyMethod(RequirePatchMethod(nameof(ProgressCharacterEpochCheckPrefix))));

private static bool ProgressCharacterEpochCheckPrefix(Player __0)
    => __0.Character is MyCharacter ? false : true;   // 自定义角色跳过
```

实测命中：`CheckFifteenElitesDefeatedEpoch(Player)`、`CheckFifteenBossesDefeatedEpoch(Player)`、`ObtainCharUnlockEpoch(Player, int)`（打全 `Instance|Public|NonPublic`）。

## 先古对话注入（9 先古全量）

```csharp
// 每先古一个 postfix：__result.CharacterDialogues[entry] = [ new AncientDialogue([sfx...]){ VisitIndex = 0 }, ... ]
PatchAncientDialogues<Neow>(harmony, nameof(NeowPostfix));   // Neow/Darv/Nonupeipe/Tanx/... 9 个
// DefineDialogues 用 AccessTools.Method 找，找不到只 Warn（低版本兼容）
```

- `CharacterDialogues` 是 `required Dictionary<string, IReadOnlyList<AncientDialogue>>`，键 = 角色 ModelId 的 `.Entry`；音频 = FMOD event 路径（`event:/sfx/npcs/neow/neow_welcome`）。
- 另需 prefix 接管 `AncientDialogueSet.GetValidDialogues(ModelId, int, int, bool)`：只处理自定义角色（`characterId.Entry == entry`），按 `VisitIndex` 精确匹配 → 无则 `IsRepeating` 池 → 命中时 `ref __result` 赋值并返回 false；其余放行。

## 周边联动

| 场景 | Patch 方式 |
|------|-----------|
| 初始遗物升级链 | `TouchOfOrobas.GetUpgradedStarterRelic(RelicModel)` postfix：starterRelic 是自定义初始遗物 → `__result = 升级版` |
| 超越遗物升级卡 | `ArchaicTooth.get_TranscendenceUpgrades`（static getter）postfix：`__result[卡A.Id] = 卡B()` |
| 新开局语音 | `RunState.CreateForNewRun` postfix：玩家含自定义角色 → 播语音 |