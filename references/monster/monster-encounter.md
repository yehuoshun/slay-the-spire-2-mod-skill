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

路径：`res://<模组ID>/localization/<语言代码>/monsters.json`

```json
{
  "MY_MONSTER": {
    "name": "自定义怪物",
    "moves": {
      "ATTACK": { "title": "准备攻击" },
      "BUFF": { "title": "正在蓄力" }
    }
  }
}
```

---

## 遭遇类

```csharp
public class MyEncounter : EncounterModel
{
    // 站位 ID（GenerateMonsters 里引用）
    public override IReadOnlyList<string> Slots => new List<string> { "front" };
    public override bool HasScene => false;
    public override RoomType RoomType => RoomType.Monster;
    public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel> { ModelDb.Monster<MyMonster>() };

    // protected，返回 (怪物, 站位ID) 列表
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<MyMonster>().ToMutable(), "front"),
        };
    }
}
```

### RoomType（真实枚举）

`Monster` / `Elite` / `Boss` / `Treasure` / `Shop` / `Event` / `RestSite` / `Map`（外加 `Unassigned`）

> ⚠️ 旧版写的 `BossChest`/`Ancient` 不存在。

### 自定义站位

- `Slots` 属性声明站位 ID 列表
- `GenerateMonsters()` 返回 `(MonsterModel, string?)`，第二元素为站位 ID
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

