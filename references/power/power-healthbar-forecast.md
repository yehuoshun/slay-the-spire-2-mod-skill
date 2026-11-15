# 能力：生命条预测（HealthBarForecast）

> 从 power-advanced.md 拆出。仅作知识记录——**原生 `sts2.dll` 无此 API**。

## 结论

> ⚠️ 反编译验证：原生 `sts2.dll` 无 `IHealthBarForecastSource` / `HealthBarForecastSegment`。这是 BaseLib v3.4.7 的方向、YuWanCard 自研接口。纯原生想实现要自研：定义接口 + Harmony Patch 生命条 UI 节点，成本高；**需要时建议直接用 BaseLib**（本 skill 唯一允许的第三方场景是设置界面，生命条预测不属于）。

## 自研接口设计（灵感 BaseLib v3.4.7）

```csharp
// 灵感 BaseLib v3.4.7：OutwardFromCurrentHp / InwardFromMaxHp
// 能力实现 IHealthBarForecastSource，返回预测段：
//   new HealthBarForecastSegment(Amount, color, HealthBarForecastDirection.FromRight, Order: 0)
//   （Amount=预测值, color=颜色, direction=方向, order=排序, material=可选毁灭条着色器）
// 毁灭条样式：ShaderUtils.CreateDoomBarShaderMaterial(
//     ShaderUtils.CreateVanillaDoomBarGradientTexture())
```


### v3.4.8 破坏性变更：`CombatStateWrapper` 已删

`HealthBarForecastContext.CombatState` 的类型从 `CombatStateWrapper?` 变成 `ICombatState?`（record struct 本体没变，只需重编译），原来的包装类整族消失：

| 旧写法 | 现在 |
|---|---|
| `new CombatStateWrapper(creature.CombatState)` / `x.WrappedCombatState(creature)` | `creature.CombatState` |
| `x.WrappedCombatState(creature)?.CurrentSide` | `creature.CombatState?.CurrentSide` |
| `new CombatStateWrapper(state).RunState` | `creature.CombatState?.RunState` |
| `RunState.IterateHookListeners.Invoke(...)` | `runState.IterateHookListeners(combatState)` |

另外 `CommonActions.Apply<T>()` / `ApplySelf<T>()` 的无上下文重载全部删除：必须传 `PlayerChoiceContext`，手上确实没有就 `new ThrowingPlayerChoiceContext()`——它会在真被调用时抛异常，比传 null 更早暴露「这里不该有选择」。
