# 先古之民事件：对话、纹理与注册

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

