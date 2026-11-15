# 自定义药水：回调

## 回调

### OnUse — 使用药水时触发

```csharp
protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `choiceContext` | `PlayerChoiceContext` | 玩家选择上下文 |
| `target` | `Creature?` | 使用目标（TargetType 决定是否有目标；无目标时为 null） |

单目标药水先做空值检查（真实药水风格）：

```csharp
protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
{
    PotionModel.AssertValidForTargetedPotion(target);
    await CreatureCmd.Heal(target, 10);
}
```

### 其他可覆盖成员（原生真实存在）

| 成员 | 类型 | 说明 |
|------|------|------|
| `CanBeGeneratedInCombat` | `public virtual bool => true` | 是否可被随机生成（炼制药水等） |
| `PassesCustomUsabilityCheck` | `public virtual bool => true` | 自定义可用性检查 |
| `ExtraHoverTips` | `public virtual IEnumerable<IHoverTip>` | 额外悬停提示 |

```csharp
public override bool CanBeGeneratedInCombat => false;  // 阻止随机生成
```


### 动态变量 tooltip：`HoverTips` 是 lazy Concat，不是集合

给 `PotionModel.HoverTips` 打 getter Postfix 注入提示时，`__result` 通常是 `Concat` 出来的惰性序列，直接 `Add` 要么改不动要么抛。必须先物化再改：

```csharp
[HarmonyPatch(typeof(PotionModel), nameof(PotionModel.HoverTips), MethodType.Getter)]
public static class PotionHoverTipsPatch
{
    private static void Postfix(PotionModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        var tips = __result.ToList();          // 必须物化
        tips.AddRange(BuildTips(__instance));
        __result = tips;
    }
}
```

同类补丁要给卡牌、遗物、能力、药水**各打一处**：只打了前三者，就会出现「药水的数值变量没有悬停说明」这种半边生效的问题。

模组侧不要重复加：库已经自动把 `DynamicVars` 里带 tooltip 的变量挂上去，自己再加一遍就是重复显示。
