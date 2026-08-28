# 纯原生设计模式总纲（从 BaseLib 提炼）

> v2：模式 2「链式辅助方法」签名校正（`CardPlayState` → `CardPlay`，对齐 card-v3）。v1 为存档（`https://github.com/yehuoshun/slay-the-spire-2-mod-skill-archive/blob/main/references/baselib/design-patterns-v1.md`，含错误签名留档对比）。

> 本 skill 主推**纯原生**开发：只靠 `0Harmony.dll` + `sts2.dll`，零第三方依赖。
> BaseLib 是优秀的设计参考，本文件把它所有便利机制**转译为纯原生实现**，作为各模块子项的通用地基。

---

## 为什么学 BaseLib

BaseLib 的 `Custom*Model` 本质上是**原生 API 的薄封装**。研究它的源码后发现，它没有引入任何魔法——只是用：
- 自定义 Attribute（C# 标准库）
- 反射扫描（C# 标准库）
- 原生 `ModHelper.AddModelToPool` / `ModelDb.Inject`（sts2.dll）

把这些模式**用纯原生 API 复刻**，就能获得同样的开发便利，且不背第三方依赖。

**铁律：本 skill 所有写法零第三方依赖。BaseLib 只在文中作为"灵感来源"一句话标注，绝不教人使用。**

---

## 模式 1：自动注册（ContentRegistry）

### BaseLib 的做法

- 构造函数里调 `CustomContentDictionary.AddModel(GetType())` 收集类型
- `[Pool(Type)]` Attribute 标记模型属于哪个池
- 统一注册时调原生 `ModHelper.AddModelToPool(poolType, modelType)`

### 纯原生转译

```csharp
// 1. 自定义 Attribute（C# 标准库）
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class CardPoolAttribute(Type poolType) : Attribute
{
    public Type PoolType { get; } = poolType;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RelicPoolAttribute(Type poolType) : Attribute
{
    public Type PoolType { get; } = poolType;
}

// 2. ContentRegistry：反射扫描程序集，统一注册
public static class ContentRegistry
{
    public static void RegisterAll(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute<CardPoolAttribute>() is { } cardPool)
                ModHelper.AddModelToPool(cardPool.PoolType, type);
            else if (type.GetCustomAttribute<RelicPoolAttribute>() is { } relicPool)
                ModHelper.AddModelToPool(relicPool.PoolType, type);
        }
    }
}

// 3. ModEntry 里调用一次
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly());
    }
}
```

### 用法

```csharp
[CardPool(typeof(MyCardPool))]   // 自动进 MyCardPool，无需手动注册
public class MyCard : CardModel { ... }

[RelicPool(typeof(SharedRelicPool))]
public class MyRelic : RelicModel { ... }
```

### 关键点

- 注册时机必须在 `ModelDb` 初始化前（`ModEntry.Initialize` 阶段）
- 错过时机用原生 `ModelDb.Inject(type)` 补救（只注册 ID，不关联池）
- 池类型可在 Attribute 参数里动态指定，支持任意自定义池

---

## 模式 2：链式辅助方法（转译 Builder）

### BaseLib 的做法

`ConstructedCardModel` 用链式 Builder 一行定义卡牌：`.WithCost(1).WithDamage(6).WithUpgrade(...)`。

### 纯原生转译

原生 API 本身已经支持链式（`DamageCmd.Attack().FromCard().Targeting().Execute()`），可以再包一层静态辅助，把高频重复操作收敛成一行：

```csharp
public static class CardFx
{
    // 攻击：来源是当前卡牌，打指定目标
    public static Task DealDamage(this CardModel card, PlayerChoiceContext ctx,
        CardPlay play, int damage, int hits = 1, string? hitFx = null)
    {
        ArgumentNullException.ThrowIfNull(play.Target, "play.Target");
        var cmd = DamageCmd.Attack(damage).FromCard(card).Targeting(play.Target)
            .WithHitCount(hits);
        if (hitFx != null) cmd = cmd.WithHitFx(hitFx);
        return cmd.Execute(ctx);
    }

    // 攻击所有敌人
    public static Task DealDamageAll(this CardModel card, PlayerChoiceContext ctx,
        CardPlay play, int damage)
        => DamageCmd.Attack(damage).FromCard(card)
            .TargetingAllOpponents(card.CombatState).Execute(ctx);

    // 抽牌（真实签名：Draw(PlayerChoiceContext, decimal, Player, bool)）
    public static Task Draw(this CardModel card, PlayerChoiceContext ctx, int count)
        => CardPileCmd.Draw(ctx, count, card.Owner, false);
}

// 用法：一行完成攻击+抽牌
protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
{
    ArgumentNullException.ThrowIfNull(play.Target, "play.Target");
    await Task.WhenAll(
        this.DealDamage(ctx, play, 6),
        this.DealDamage(ctx, play, 3));
}
```

### 关键点

- 辅助方法只是收敛重复，**不改变原生行为**
- 扩展方法命名要带模块前缀（`CardFx` / `RelicFx` / `PowerFx`），避免与其他 Mod 冲突
- 保留原生链式能力，辅助方法内部仍走 `DamageCmd` / `CardPileCmd` 等原生命令

---

## 模式 3：便捷 override（默认实现）

### BaseLib 的做法

- `CustomCardModel.GainsBlock` 自动从 `DynamicVars` 推断是否给格挡
- 图标路径带 `ResourceLoader.Exists()` 回退检查

### 纯原生转译

```csharp
// 手动 override 原生虚属性（BaseLib 只是帮你自动算，纯原生手写即可）
public override bool GainsBlock => true;   // 该卡给格挡

// 图标路径回退辅助
public static class PathFallback
{
    public static string FirstExisting(params string[] paths)
        => paths.FirstOrDefault(ResourceLoader.Exists) ?? paths.Last();
}

// 用法
public override string BigIconPath =>
    PathFallback.FirstExisting(
        "res://MyMod/images/relics/my_relic.png",
        "res://MyMod/images/relics/fallback.png");
```

### 关键点

- BaseLib 的"自动推断"本质是**帮助计算一个原生 override**，纯原生手动写即可
- 回退逻辑用 `ResourceLoader.Exists()`（Godot 原生）+ LINQ 一行实现

---

## 模式 4：内联本地化（转译 CardLoc）

### BaseLib 的做法

`Localization => new CardLoc("MyCard").WithName(...).WithDescription(...)` 在代码里内联定义本地化。

### 纯原生转译

原生用 JSON 文件本地化（`res://<ModId>/localization/<语言>/cards.json`），这是标准做法，不需要内联。

如果想让 key 管理更稳，可加一个常量辅助：

```csharp
public static class LocKeys
{
    public const string MyCard = "MyCard";
    // 集中管理所有本地化 key，避免手写字符串拼错
}
```

**结论：本地化保持原生 JSON 方案即可，BaseLib 的内联写法没有纯原生优势，不转译。**

---

## 常见坑（BaseLib / 第三方灵感参考）

> 以下来自 BaseLib / YuWanCard / ShunMod 等生产级 Mod 的实战经验。纯原生定位下**不直接使用这些 API**（本 skill 零第三方依赖），仅作灵感参考——遇到同类需求按「提炼 → 纯原生转译」处理。

| 问题 | 参考方案 | 来源 |
|------|---------|------|
| 设置 UI 自己写容易出 bug | 零 Harmony + Godot 纯信号方案（自建 UI） | ShunMod |
| 先古遗物不在图鉴显示 | 自定义注册表（`AncientRelicCompendiumPatch` 思路） | 自定义 |
| 多人模式状态不同步 | 确定性随机替代 `System.Random`（纯原生自实现种子随机） | YuWanCard |
| 多人模式专属卡牌 | `IsMultiplayerOnly` 标记防单人卡死 | YuWanCard |
| 卡牌奖励序列化丢失自定义卡池 | 兼容层思路（`CardRewardSerializationCompatibility`） | BaseLib |
| 临时能力核分支兼容 | `CustomTemporaryPowerModel` + `IBetaCompatTempPower` | BaseLib |
| 外部 Mod 卡牌目标兼容 | 反射桥接层（`ExternalCardTargetingCompat` 思路） | YuWanCard |
| 角色皮肤系统 | 接口 + 持久化选择器（`IYuWanCharacterSkinProvider` + `CharacterSkinSelectionManager`） | YuWanCard |
| 配置系统 | BaseLib 3.3+ 移除配置支持，YuWanCard 改用 RitsuLib | YuWanCard |
| 自定义资源每回合不重置 | `setEachTurn` / `StartOfTurnReset`（`BasicCustomResource`） | BaseLib |

---

## 各子项映射

| 设计模式 | 应用子项 |
|---------|---------|
| 自动注册（ContentRegistry + Attribute） | `serialization`（框架）、`relic`、`potion`、`power`、`enchantment`、`orb`、`act`、`pet` |
| 链式辅助方法 | `card`（CardFx）、`patterns/code-patterns` |
| 便捷 override | `card`（GainsBlock）、`relic`（路径回退）、`power`（图标回退） |
| 内联本地化 | 不转译，保持原生 JSON |
| 自定义资源系统 | `baselib`（参考 BaseLib 3.4.5，纯原生需自建，暂留备选） |

---

## 演进路线

- 当前：各子项手动注册 + 手写回调
- 本文件：提供纯原生自动注册框架 + 链式辅助 + 便捷 override，可直接落地
- 后续：如遇到 BaseLib 新版本新增便利机制，继续按"提炼 → 纯原生转译"流程补充到对应子项
