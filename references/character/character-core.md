# 自定义角色：角色类与资源路径

## 四、角色类

```csharp
public class MyCharacter : CharacterModel
{
    // 抽象必填：名字颜色 / 性别 / 血量 / 金币
    public override Color NameColor => Colors.Purple;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 70;
    public override int StartingGold => 99;

    public override float AttackAnimDelay => 0.15f;   // 攻击动画延迟
    public override float CastAnimDelay => 0.25f;     // 施法动画延迟
    public override int MaxEnergy => 3;               // 能量上限（virtual，默认 3）

    // protected，前置角色（null = 默认解锁）
    protected override CharacterModel? UnlocksAfterRunAs => null;

    // 池：用 ModelDb 引用，不是 new
    public override CardPoolModel CardPool => ModelDb.CardPool<MyCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<MyRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<MyPotionPool>();

    public override IEnumerable<CardModel> StartingDeck => [];
    public override IReadOnlyList<RelicModel> StartingRelics => [];
    public override IReadOnlyList<PotionModel> StartingPotions => [];

    public override List<string> GetArchitectAttackVfx() => [];
}
```

### CharacterGender（真实枚举）

| 值 | 说明 |
|-----|------|
| `CharacterGender.Neutral` | 中性 |
| `CharacterGender.Feminine` | 阴性/女性语法 |
| `CharacterGender.Masculine` | 阳性/男性语法 |

> ⚠️ 旧版写的 `Male/Female/Other` 不存在，且 `Name` 属性不存在（名字走本地化 `Title`）。

### 角色抽象属性（必填）

| 属性 | 类型 | 说明 |
|------|------|------|
| `NameColor` | `Color` | 名字颜色 |
| `Gender` | `CharacterGender` | 语法性别 |
| `StartingHp` | `int` | 初始生命 |
| `StartingGold` | `int` | 初始金币 |
| `AttackAnimDelay` / `CastAnimDelay` | `float` | 攻击/施法动画延迟 |
| `UnlocksAfterRunAs` | `CharacterModel?`（protected） | 前置角色 |
| `CardPool` / `RelicPool` / `PotionPool` | 池模型 | 关联三池 |
| `StartingDeck` | `IEnumerable<CardModel>` | 初始卡组 |
| `StartingRelics` | `IReadOnlyList<RelicModel>` | 初始遗物 |
| `GetArchitectAttackVfx()` | `List<string>` | 攻击建筑师动画序列 |

---

## 五、角色资源路径

> 全部资源路径表已拆到 [character-paths.md](character-paths.md)（场景/纹理/音效 + charui 通用约定）。

> 官方模板完整骨架（PlaceholderCharacterModel + 初始卡组 + 图标）→ [character-template.md](character-template.md)

