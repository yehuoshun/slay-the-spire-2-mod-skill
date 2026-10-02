# 自定义敌怪：资源、遭遇与添加

## 怪物资源

```
待机动画场景：res://scenes/creature_visuals/<怪物ID小写>.tscn
```

场景结构：
```
NCreatureVisuals (根节点)
  ├─ Visuals (Node2D, 唯一名称 %)
  └─ Bounds (Node2D, 唯一名称 %) — 选择框/血条/意图位置
```

### 音效

```
攻击音效：event:/sfx/enemy/enemy_attacks/<ID小写>/<ID小写>_attack
施法音效：event:/sfx/enemy/enemy_attacks/<ID小写>/<ID小写>_cast
死亡音效：event:/sfx/enemy/enemy_attacks/<ID小写>/<ID小写>_die
```

---

## 本地化

路径：`res://<模组ID>/localization/<语言代码>/monsters.json`（**扁平键**，实测格式）

```json
{
  "MY_MONSTER.name": "自定义怪物",
  "MY_MONSTER.moves.ATTACK.title": "准备攻击",
  "MY_MONSTER.moves.BUFF.title": "正在蓄力"
}
```

---

## 遭遇类

```csharp
public class MyEncounter : EncounterModel
{
    public override bool HasScene => false;
    public override RoomType RoomType => RoomType.Monster;
    public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel> { ModelDb.Monster<MyMonster>() };

    // protected，返回 (怪物, 站位ID) 列表——站位 ID 在元组里，无独立 Slots 属性
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<MyMonster>().ToMutable(), "front"),
        };
    }
}
```

> ⚠️ **原生 EncounterModel 没有 `Slots` 属性**（旧版编造，已删）——站位 ID 就是 `GenerateMonsters()` 元组的第二元素。`MonsterModel.ToMutable()` 真实存在（生成战斗实例）。

### RoomType（真实枚举）

`Monster` / `Elite` / `Boss` / `Treasure` / `Shop` / `Event` / `RestSite` / `Map`（外加 `Unassigned`）

> ⚠️ 旧版写的 `BossChest`/`Ancient` 不存在。

### 自定义站位

- 站位 ID 写在 `GenerateMonsters()` 元组第二元素（无独立 `Slots` 属性）
- 自定义场景时 `HasScene = true`，场景资源按命名约定 `res://scenes/encounters/<遭遇ID小写>.tscn`，节点名 = 站位 ID

---

## 添加遭遇

```csharp
[HarmonyPatch(typeof(Overgrowth), nameof(Overgrowth.GenerateAllEncounters))]
public static class OvergrowthEncountersPatch
{
    public static void Postfix(ref IEnumerable<EncounterModel> __result)
    {
        __result = __result.Append(new MyEncounter());
    }
}
```

> 注意：`GenerateAllEncounters()` 真实返回 `IEnumerable<EncounterModel>`（不是 `List`），且是**实例方法**不是 getter，Patch 不要写 `MethodType.Getter`。

