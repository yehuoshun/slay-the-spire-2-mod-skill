# 创建自定义休息点选项

## 基本模板

继承 `RestSiteOption`，重写关键方法：

```csharp
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;

public class TradeOption : RestSiteOption
{
    public TradeOption(Player owner) : base(owner) { }

    // 选项名称（本地化 key）
    public override LocString Title =>
        new LocString("gameplay_ui", "MYSKILL-TRADE_TITLE");

    // 选项描述
    public override LocString Description =>
        new LocString("gameplay_ui", "MYSKILL-TRADE_DESC");

    // 图标路径
    public override string IconPath =>
        "res://myskill/images/rest/trade.png";

    // 玩家选中时的行为
    public override async Task OnOptionSelected(
        RestSiteRoom room, RestSiteContext context)
    {
        // 扣血换金币
        await CreatureCmd.Damage(Owner.Creature, 6);
        await GoldCmd.Gain(Owner.Player, 75);
        await room.RemoveOption(this);
    }
}
```

## 必重写成员

| 成员 | 类型 | 说明 |
|------|------|------|
| `Title` | `LocString` | 选项名称 |
| `Description` | `LocString` | 选项描述 |
| `IconPath` | `string` | 选项图标 |
| `OnOptionSelected(RestSiteRoom, RestSiteContext)` | `Task` | 玩家选择后的行为 |

## 可选图标

BaseLib 提供了 `CustomRestSiteOption` 子类，加了一个 `CustomIconPath` 虚属性，配合 Harmony Prefix 实现动态图标。纯原生直接 override `IconPath` 即可，无需额外操作：

```csharp
public override string IconPath =>
    SomeCondition ? "path_a.png" : "path_b.png";
```

## 添加到休息点

### Patch 方式：在进入 RestSite 时注入

```csharp
[HarmonyPatch(typeof(RestSiteRoom), nameof(RestSiteRoom._Ready))]
public static class AddCustomOptionPatch
{
    [HarmonyPostfix]
    private static void Postfix(RestSiteRoom __instance)
    {
        if (ShouldShowTrade(__instance))
        {
            __instance.AddOption(new TradeOption(__instance.Player));
        }
    }
}
```

### 使用原生 AddOption

```csharp
// 直接调用 AddOption，原生支持
[HarmonyPatch(typeof(RestSiteRoom), nameof(RestSiteRoom.OnPlayerEntered))]
public static class AddTradeOptionOnEnter
{
    [HarmonyPostfix]
    private static void Postfix(RestSiteRoom __instance)
    {
        if (!HasTradeOption(__instance))
        {
            __instance.AddOption(new TradeOption(__instance.Player));
        }
    }
}
```

## 本地化

```json
{
  "MYSKILL-TRADE_TITLE": {
    "name": "交易"
  },
  "MYSKILL-TRADE_DESC": {
    "description": "失去 6 点生命，获得 75 金币。"
  }
}
```

## 参见

- [rest-site.md](rest-site.md) — 休息点概述