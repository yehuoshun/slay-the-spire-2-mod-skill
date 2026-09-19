# 自定义资源：UI 显示

> 资源基类与费用见 [resource-core.md](resource-core.md) 和 [resource-cost.md](resource-cost.md)。

## UI 显示

资源在战斗界面显示。最简单的做法是 Patch `ExtraCombatUi`（战斗场景的扩展 UI 层）：

```csharp
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.OnCombatSetUp))]
public static class ResourceUiPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCombatRoom __instance)
    {
        var label = new Label
        {
            Text = "Mana: 0",
            Position = new Vector2(200, 600),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        __instance.AddChild(label);
        __instance.AddChild(new NRewardHighlight()); // 可选高亮效果
    }
}
```

每帧更新资源 UI：

```csharp
[HarmonyPatch]
public static class ResourceUiRefresh
{
    private static Label? _manaLabel;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Process))]
    private static void UpdateManaDisplay(NCombatRoom __instance, double delta)
    {
        var player = __instance.Player;
        if (player == null) return;

        var mana = CustomResourceManager.Get<ManaResource>(
            player.PlayerCombatState);
        if (mana != null && _manaLabel != null)
            _manaLabel.Text = $"Mana: {mana.Amount}/{mana.MaxAmount}";
    }
}
```

> 更好的方式：用 `SpireField<NCombatRoom, Control>` 缓存 UI 节点，避免每帧 Find。

## 注册

```csharp
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        CustomResourceManager.Register<ManaResource>("Mana");
        var harmony = new Harmony("myskill");
        harmony.PatchAll();
    }
}
```

## 参见

- [resource-core.md](resource-core.md) — 资源基类与生命周期