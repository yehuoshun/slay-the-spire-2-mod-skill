# 附录

---

## A. 完整 API 速查表

### 玩家操作（PlayerCmd）

```csharp
await PlayerCmd.GainEnergy(player, amount);
await PlayerCmd.GainBlock(player, amount);
await PlayerCmd.GainGold(player, amount);
await PlayerCmd.Damage(player, amount, source);
await PlayerCmd.Heal(player, amount);
await PlayerCmd.ApplyPower(player, powerType, amount, source, cardSource);
await PlayerCmd.EndTurn(player);
```

### 伤害链（DamageCmd）

```csharp
// 单体伤害
await DamageCmd.Attack(6).FromCard(this).Targeting(target).Execute();

// 全体伤害
await DamageCmd.Attack(10).FromCard(this).TargetingAllOpponents(combatState).Execute();

// 随机目标
await DamageCmd.Attack(5).FromCard(this).TargetingRandomOpponents(combatState, allowRepeat: false).Execute();

// 带特效
await DamageCmd.Attack(8).FromCard(this).Targeting(target)
    .WithHitFx("animations/vfx_slash.tscn", null, null)
    .WithHitCount(3) // 攻击 3 次
    .Execute();
```

### 卡牌操作（CardCmd / CardPileCmd）

```csharp
// 丢弃
await CardCmd.Discard(card);

// 升级
card.Upgrade();

// 附魔
await CardCmd.Enchant(card, typeof(MyEnchantment), amount);

// 抽牌
await CardPileCmd.Draw(player, count);

// 添加卡牌到牌堆
await CardPileCmd.Add(card, PileType.Deck);
await CardPileCmd.Add(card, PileType.Hand);
await CardPileCmd.Add(card, PileType.Discard);
```

### 选牌（CardSelectCmd）

```csharp
// 从手牌选一张
var selected = await CardSelectCmd.FromHandGeneric(ctx, player,
    new CardSelectorPrefs("提示文本", selectCount: 1),
    filter: card => true, source: this);

// 从手牌选一张升级
var selected = await CardSelectCmd.FromHandForUpgrade(ctx, player,
    new CardSelectorPrefs("选择要升级的牌", 1),
    filter: card => card.CanUpgrade(), source: this);

// 从手牌选一张丢弃
var selected = await CardSelectCmd.FromHandForDiscard(ctx, player,
    new CardSelectorPrefs("选择要丢弃的牌", 1),
    filter: card => true, source: this);

// 从牌库选
var selected = await CardSelectCmd.FromDeckGeneric(ctx, player,
    new CardSelectorPrefs("从牌库选择", 1),
    filter: card => true, source: this);
```

### 模型数据库（ModelDb）

```csharp
// 获取 ID
ModelId id = ModelDb.GetId<MyCard>();
ModelId id = ModelDb.GetId(typeof(MyCard));

// 获取实例
var card = ModelDb.GetById<CardModel>(id);
var card = ModelDb.GetByIdOrNull<CardModel>(id);

// 所有卡牌/遗物
var allCards = ModelDb.AllCards;
var allRelics = ModelDb.AllRelics;

// 获取特定卡池
var pool = ModelDb.CardPool<ColorlessCardPool>();
```

### 生物操作（CreatureCmd）

```csharp
// 施加能力
await CreatureCmd.ApplyPower(target, typeof(VulnerablePower), amount, source, cardSource);

// 移除能力
await CreatureCmd.RemovePower(target, typeof(VulnerablePower));

// 造成伤害
await CreatureCmd.Damage(ctx, target, amount, source, cardSource);

// 获得格挡
await CreatureCmd.GainBlock(target, amount);
```

### 战斗状态

```csharp
// 当前战斗
var combat = CombatManager.Instance;
bool inCombat = combat.IsInProgress;
bool ending = combat.IsOverOrEnding;

// 战斗状态
var state = player.Creature.CombatState;
int round = state.RoundNumber;
var side = player.Creature.Side; // CombatSide.Player / CombatSide.Enemy
```

---

## B. 枚举速查

### CardType

`Attack` | `Skill` | `Power` | `Status` | `Curse` | `Token` | `Quest`

### CardRarity

`Basic` | `Common` | `Uncommon` | `Rare` | `Event` | `Token` | `Status` | `Curse` | `Ancient` | `Quest`

### TargetType

`None` | `Opponent` | `AllOpponents` | `Ally` | `AllAllies` | `AnyEnemy` | `Self` | `Osty` | `TargetedNoCreature`

### RelicRarity

`Starter` | `Common` | `Uncommon` | `Rare` | `Boss` | `Shop` | `Event` | `Ancient`

### PotionRarity

`Common` | `Uncommon` | `Rare`

### PotionUsage

`CombatOnly` | `AnyTime` | `Triggered`

### PowerType

`Buff` | `Debuff`

### PowerStackType

`Intensity` | `Duration`

### CardKeyword（常用）

`Exhaust` | `Retain` | `Innate` | `Ethereal` | `Unplayable`

### CardTag（常用）

`Strike` | `Defend`

### RoomType

`Monster` | `Elite` | `Boss` | `Event` | `Shop` | `Rest` | `Treasure`

### MonsterIntent（常用）

`Attack` | `Defend` | `Buff` | `Debuff` | `StrongAttack` | `Charge` | `Summon` | `Sleep`

### CombatSide

`Player` | `Enemy`

### PileType

`Deck` | `Hand` | `Discard` | `Exhaust`

### CharacterGender

`Male` | `Female` | `Other`

---

## C. 遗物池列表

| 池 | 说明 |
|----|------|
| `SharedRelicPool` | 公共池（宝箱+精英+商店） |
| `IroncladRelicPool` | 铁血战士专属 |
| `SilentRelicPool` | 静默猎人专属 |
| `DefectRelicPool` | 机器人专属 |
| `NecrobinderRelicPool` | 亡灵契约师专属 |
| `RegentRelicPool` | 储君专属 |
| `EventRelicPool` | 事件专属 |
| `ShopRelicPool` | 商店专属 |
| `BossRelicPool` | Boss 掉落 |
| `FallbackRelicPool` | 兜底池（Circlet） |
| `DeprecatedRelicPool` | 废弃池 |

---

## D. 药水池列表

`SharedPotionPool` / `IroncladPotionPool` / `SilentPotionPool` / `DefectPotionPool` / `NecrobinderPotionPool` / `RegentPotionPool`

---

## E. 卡池列表

`IroncladCardPool` / `SilentCardPool` / `DefectCardPool` / `NecrobinderCardPool` / `RegentCardPool` / `ColorlessCardPool` / `TokenCardPool` / `CurseCardPool` / `StatusCardPool`

---

## F. 常见坑详解

### 1. 图标不显示

**根因**：PNG 图片没有被 Megadot 识别为资源文件，因此不会被打包进 PCK。

**解决**：
- 确保 PNG 在 Megadot 项目文件系统中可见
- 重新 Publish（不是 Build）
- 检查代码中的路径与资源路径完全一致

### 2. 本地化不生效

**根因**：本地化 JSON 是资源文件，Build 只编译 DLL，不处理资源。

**解决**：必须用 Publish 或导出 PCK。

### 3. Harmony 报 .NET 版本错误

**现象**：`Could not load file or assembly 'System.Runtime, Version=8.0.0.0'`

**解决**：编辑 `GodotPlugins.runtimeconfig.json`：
```json
{
  "runtimeOptions": {
    "framework": {
      "name": "Microsoft.NETCore.App",
      "version": "9.0.0"
    }
  }
}
```

### 4. 场景脚本找不到

**现象**：`Can't find script` 或场景实例化失败

**解决**：在 `ModEntry.Initialize()` 中调用：
```csharp
ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
```

### 5. 自定义属性不保存

**现象**：`[SavedProperty]` 标注的属性在存档加载后丢失

**解决**：在初始化时注入类型缓存：
```csharp
SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MyCard));
SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MyRelic));
```

### 6. Starter 稀有度遗物不出现

**根因**：`RelicRarity.Starter` 的遗物不走随机池。

**解决**：用 Harmony Patch 修改角色的 `StartingRelics` Getter，或通过事件给予。

### 7. 重复初始化导致崩溃

**解决**：用双重检查锁防止重复初始化（见 real-code-patterns.md 模式 H）。

### 8. 卡牌在百科中不显示

**解决**：
- 检查 `shouldShowInCardLibrary` 参数（CardModel 构造第5参数）
- 确保已注册到正确的卡池
- 确保本地化 JSON 中有关键字条目

### 9. AddModelToPool 泛型报错

**解决**：使用反射重载：
```csharp
ModHelper.AddModelToPool(typeof(SharedRelicPool), typeof(MyRelic));
```

### 10. 联机时模组不生效

**解决**：
- `affects_gameplay` 设为 `true`
- 确保所有联机玩家安装了相同模组
- 联机同步相关数据需要用 `[SavedProperty]` 序列化