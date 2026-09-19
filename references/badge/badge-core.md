# 创建自定义徽章

## 基本模板

继承 `Badge`，重写关键成员：

```csharp
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

public class FirstWinBadge : Badge
{
    // 构造函数（签名可能随版本不同）
    public FirstWinBadge(SerializableRun run, bool won, ulong playerId)
        : base(run, won, playerId, "MYSKILL-FIRST_WIN", true, false) { }

    // 徽章 ID（唯一）
    public override string Id => "MYSKILL-FIRST_WIN";

    // 是否需要胜利才获得
    public override bool RequiresWin => true;

    // 是否仅多人模式
    public override bool MultiplayerOnly => false;

    // 稀有度（决定边框颜色）
    public override BadgeRarity Rarity => BadgeRarity.Gold;

    // 是否满足获得条件
    public override bool IsObtained()
    {
        return _run.Completed; // 通关即可
    }
}
```

## 必重写成员

| 成员 | 类型 | 说明 |
|------|------|------|
| `Id` | `string` | 全局唯一 ID，建议 `ModID-NAME` 格式 |
| `RequiresWin` | `bool` | 是否必须通关才获得 |
| `MultiplayerOnly` | `bool` | 是否仅多人模式 |
| `Rarity` | `BadgeRarity` | 稀有度（`Bronze`/`Silver`/`Gold`/`Platinum`/`Special`） |
| `IsObtained()` | `bool` | 检查当前 run 是否满足条件 |

## 注册到游戏

```csharp
using MegaCrit.Sts2.Core.Models.Badges;

[HarmonyPatch(typeof(BadgePool), nameof(BadgePool.CreateAll))]
public static class RegisterCustomBadgePatch
{
    // Beta 分支（3 参）
    private static void Postfix(ref IReadOnlyCollection<Badge> __result,
        SerializableRun run, bool won, ulong playerId)
    {
        var list = __result.ToList();
        list.Add(new FirstWinBadge(run, won, playerId));
        __result = list;
    }

    // 主分支（2 参，无 won 参数）
    private static void Postfix(ref IReadOnlyCollection<Badge> __result,
        SerializableRun run, ulong playerId)
    {
        var list = __result.ToList();
        list.Add(new FirstWinBadge(run, true, playerId));
        __result = list;
    }
}
```

> 版本兼容：需要同时提供两套 Postfix 签名，或者用 `TargetMethod` 动态判断。

## 自定义图标

徽章图标默认路径由 `Id` 自动生成。如果需要自定义路径，Patch `NBadge.Create`：

```csharp
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;

[HarmonyPatch(typeof(NBadge), nameof(NBadge.Create),
    [typeof(string), typeof(BadgeRarity)])]
public static class CustomBadgeIconPatch
{
    [HarmonyPrefix]
    private static bool Prefix(string id, BadgeRarity rarity,
        ref NBadge __result)
    {
        if (id == "MYSKILL-FIRST_WIN")
        {
            __result = NBadge.Create(
                "res://myskill/images/badges/first_win.png", rarity);
            return false;
        }
        return true;
    }
}
```

## 稀有度（BadgeRarity）

| 值 | 说明 |
|----|------|
| `Bronze` | 铜（基础成就） |
| `Silver` | 银 |
| `Gold` | 金 |
| `Platinum` | 白金 |
| `Special` | 特殊 |

## 参见

- [badge.md](badge.md) — 徽章模块导航