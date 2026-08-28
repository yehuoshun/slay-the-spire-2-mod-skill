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

