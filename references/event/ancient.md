# 先古之民事件（Ancient）

> 参考：[杀戮尖塔2模组开发教程06 - 自定义事件 - 哔哩哔哩](https://www.bilibili.com/opus/1180714323922649110)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomAncientModel.cs`

---

## 概述

先古之民出现在每一章开头，继承 `AncientEventModel`。相比普通事件，额外要求实现对话系统（`DefineDialogues`）和选项列表（`AllPossibleOptions`）。

---

## 基础模板

```csharp
public class MyAncient : AncientEventModel
{
    // 对话组：对象初始化器（不是多参构造）
    protected override AncientDialogueSet DefineDialogues()
    {
        return new AncientDialogueSet
        {
            FirstVisitEverDialogue = new AncientDialogue("event:/sfx/npcs/my_ancient/intro"),
            CharacterDialogues = new Dictionary<string, IReadOnlyList<AncientDialogue>>
            {
                [AncientEventModel.CharKey<Ironclad>()] = new List<AncientDialogue>
                {
                    new AncientDialogue("event:/sfx/npcs/my_ancient/talk_0") { VisitIndex = 0 },
                },
            },
            AgnosticDialogues = new List<AncientDialogue>
            {
                new AncientDialogue(""),  // 无音效的单行
            },
        };
    }

    // 所有可能选项（抽象属性）
    public override IEnumerable<EventOption> AllPossibleOptions =>
        new List<EventOption>
        {
            new EventOption(this, OnBless, "MY_ANCIENT.options.BLESS"),
        };

    private async Task OnBless()
    {
        SetEventFinished(L10NLookup("MY_ANCIENT.options.BLESS.description"));
    }
}
```

> `AncientDialogue(params string[] sfxPaths)`：每行一个 SFX 路径，无音效行传 `""`。`VisitIndex` 控制第几次拜访出现，`IsRepeating` 由本地化键 `r` 后缀自动推断。

---

## 可覆盖成员（真实存在）

| 成员 | 类型 | 说明 |
|------|------|------|
| `DefineDialogues()` | `protected abstract AncientDialogueSet` | 定义对话组 |
| `AllPossibleOptions` | `public abstract IEnumerable<EventOption>` | 所有可能出现的选项 |
| `AnyCharacterDialogueBlacklist` | `public virtual IEnumerable<CharacterModel>` | 不显示特定角色对话 |
| `DialogueColor` | `public virtual Color` | 对话颜色 |
| `AmbientBgm` | `public virtual string` | 环境 BGM |

> ⚠️ 旧版写的 `OptionPools`/`IsValidForAct`/`ShouldForceSpawn` 是 **BaseLib `CustomAncientModel`** 的，原生 `AncientEventModel` 不存在。章节有效性由章节配置决定，非本类控制。

---

## 对话本地化

路径：`res://<模组ID>/localization/<语言代码>/ancients.json`

```json
{
  "MyAncient.title": "远古守护者",
  "MyAncient.epithet": "时间之墟的守望者",
  "MyAncient.talk.firstVisitEver.0-0": "又一个旅人来到了这里...",
  "MyAncient.talk.IRONCLAD.0-0": "铁甲战士，我见过你的同类。",
  "MyAncient.talk.ANY.0-0r": "凡人的命运总是如此相似。"
}
```

### 键格式（PopulateLines 自动生成）

本地化键由 `AncientDialogue.PopulateLines(ancientEntry, charEntry, dialogueIndex)` 自动生成：`<ID>.talk.<角色>. <对话序号>-<行号>` + 说话人后缀。

| 键格式 | 说明 |
|--------|------|
| `<ID>.talk.firstVisitEver.<组>-<行>.ancient` | 首次全局访问对话 |
| `<ID>.talk.<角色ID>.<组>-<行>.ancient` | 对应角色对话 |
| `<ID>.talk.ANY.<组>-<行>.ancient` | 通用对话（回退） |

### 说话人后缀

| 后缀 | 说明 |
|------|------|
| `.ancient` | 先古之民发出 |
| `.char` | 玩家角色发出 |

---

## 纹理资源

| 资源 | 路径 |
|------|------|
| 背景图 | `images/events/<ID小写>.png` |
| 地图图标 | `res://images/packed/map/ancients/ancient_node_<ID小写>.png` |
| 地图描边 | `res://images/packed/map/ancients/ancient_node_<ID小写>_outline.png` |
| 历史对话图标 | `res://images/ui/run_history/<ID小写>.png` |
| 历史对话描边 | `res://images/ui/run_history/<ID小写>_outline.png` |
| 背景场景 | `res://scenes/events/background_scenes/<ID小写>.tscn` |

5 种素材缺一不可。

---

## 注册

```csharp
[HarmonyPatch(typeof(Hive), nameof(Hive.AllAncients), MethodType.Getter)]
public static class HiveAllAncientsPatch
{
    public static void Postfix(ref IEnumerable<AncientEventModel> __result)
    {
        __result = __result.Append(new MyAncient());
    }
}
```

> `AllAncients` 真实返回 `IEnumerable<AncientEventModel>`，Postfix 需 `ref IEnumerable`。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 先古之民不出现 | 检查 `AllAncients` Patch（ref IEnumerable）+ 5 种纹理资源 |
| 对话不显示 | `DefineDialogues` 对象初始化器正确 + `AncientDialogue` 每行 sfx 路径 |
| 选项不出现 | 检查 `AllPossibleOptions` 返回 |
| 音效不播放 | 检查 SFX 路径（每行一个，空行用 `""`） |

---

## 进阶：纯原生注册辅助

> 从 BaseLib 提炼，零第三方依赖。先古之民走 Patch 注入，用 Attribute 标记 + 反射统一收集。

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RegisteredAncientAttribute : Attribute { }

[HarmonyPatch(typeof(Hive), nameof(Hive.AllAncients), MethodType.Getter)]
public static class AllAncientsPatch
{
    private static void Postfix(ref IEnumerable<AncientEventModel> __result)
    {
        var registered = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<RegisteredAncientAttribute>() != null)
            .Select(t => (AncientEventModel)Activator.CreateInstance(t)!);
        __result = __result.Concat(registered);
    }
}
```

```csharp
[RegisteredAncient]
public class MyAncient : AncientEventModel { ... }   // 自动注册
```

## 演进路线

- 当前：手动 Patch 注入（保留为基准写法）
- 纯原生自动注册（Attribute 标记 + 反射统一注入）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`new AncientDialogueSet(3参)`、`OptionPools/IsValidForAct/ShouldForceSpawn`、`ref List` Patch）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖