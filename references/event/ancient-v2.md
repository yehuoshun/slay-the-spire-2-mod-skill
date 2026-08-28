# 先古之民事件（Ancient）
> ⚠️ **存档版（v2），勿直接使用**——含错误签名，当前版见同目录 `*-v3.md`。

> 参考：[杀戮尖塔2模组开发教程06 - 自定义事件 - 哔哩哔哩](https://www.bilibili.com/opus/1180714323922649110)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomAncientModel.cs`

---

> v2：新增「纯原生注册辅助」（从 BaseLib 提炼，零第三方依赖）
## 概述

先古之民出现在每一章开头，继承 `AncientEventModel`。相比普通事件，额外要求实现对话系统（`DefineDialogues`）和选项列表。

---

## 基础模板

```csharp
public class MyAncient : AncientEventModel
{
    public override AncientDialogueSet DefineDialogues()
    {
        return new AncientDialogueSet(
            new AncientDialogue("banks/desktop/sfx.bank", "event_sfx_path_1"),
            new AncientDialogue(null),
            new AncientDialogue("banks/desktop/sfx.bank", "event_sfx_path_3")
        );
    }

    public override List<EventOption> AllPossibleOptions { get; }
}
```

---

## 可覆盖属性（新增）

```csharp
public class MyAncient : AncientEventModel
{
    // 加权随机选项池系统
    public override AncientOptionPools OptionPools { get; }

    // 判断是否在指定章节有效
    public override bool IsValidForAct(int act) => true;

    // 是否强制生成（跳过随机池，必定出现）
    public override bool ShouldForceSpawn(int act) => false;
}
```

### 属性说明

| 属性 | 说明 |
|------|------|
| `DefineDialogues()` | 定义对话组 |
| `AllPossibleOptions` | 所有可能出现的选项列表 |
| `OptionPools` | 加权随机选项池系统 |
| `IsValidForAct(int)` | 是否在指定章节有效 |
| `ShouldForceSpawn(int)` | 是否强制生成 |

### 选项池模式

```csharp
// 创建加权随机选项池
var pools = new AncientOptionPools();
pools.AddPool("PoolA", weight: 3);  // 权重 3
pools.AddPool("PoolB", weight: 1);  // 权重 1
```

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

### 键格式

| 键格式 | 说明 |
|--------|------|
| `<ID>.title` | 先古之民名字 |
| `<ID>.epithet` | 描述/称号 |
| `<ID>.talk.firstVisitEver.<组>-<行>` | 第一次对话 |
| `<ID>.talk.<角色ID>.<组>-<行>` | 对应角色特殊对话 |
| `<ID>.talk.ANY.<组>-<行>r` | 通用对话（末尾 r） |

### 对话行末尾标记

| 标记 | 说明 |
|------|------|
| `ancient` | 先古之民发出 |
| `next` | 角色选项分支 |
| `char` | 玩家角色发出 |

---

## 纹理资源

| 资源 | 路径 |
|------|------|
| 背景图 | `images/events/<ID小写>.png` |
| 地图图标 | `res://images/packed/map/ancients/ancient_node_<ID小写>.png` |
| 地图描边 | `res://images/packed/map/ancients/ancient_node_<ID小写>_outline.png` |
| 历史对话图标 | `res://images/ui/run_history/<ID小写>.png` |
| 历史对话描边 | `res://images/ui/run_history/<ID小写>.png` |
| 背景场景 | `res://scenes/events/background_scenes/<ID小写>.tscn` |

5 种素材缺一不可。

---

## 注册

```csharp
[HarmonyPatch(typeof(Hive), "AllAncients", MethodType.Getter)]
public static class HiveAllAncientsPatch
{
    public static void Postfix(ref List<AncientEventModel> __result)
    {
        __result = new List<AncientEventModel> { new MyAncient() };
    }
}
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 先古之民不出现 | 检查 `AllAncients` Patch + 5 种纹理资源 |
| 对话不显示 | Patch `AncientDialogueSet.GetValidDialogues` + `DefineDialogues` |
| 选项不出现 | 检查 `OptionPools` 权重配置 |
| 音效不播放 | 检查 FMOD 路径 |

---

## 进阶：纯原生注册辅助（v2）

> 从 BaseLib 提炼，零第三方依赖。先古之民走 Patch 注入，用 Attribute 标记 + 反射统一收集。

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RegisteredAncientAttribute : Attribute { }

[HarmonyPatch(typeof(Hive), nameof(Hive.AllAncients), MethodType.Getter)]
public static class AllAncientsPatch
{
    private static void Postfix(ref List<AncientEventModel> __result)
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (type.GetCustomAttribute<RegisteredAncientAttribute>() != null)
                __result.Add((AncientEventModel)Activator.CreateInstance(type)!);
        }
    }
}
```

```csharp
[RegisteredAncient]
public class MyAncient : AncientEventModel { ... }   // 自动注册
```

## 演进路线

- 当前：手动注册（保留为基准写法）
- v2 更优：**纯原生自动注册**（见上方「进阶」章节）——Attribute 标记 + ContentRegistry / 反射注册辅助，免手动注册
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖