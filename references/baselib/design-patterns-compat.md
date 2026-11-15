# 多版本兼容工程化与补丁韧性（BaseLib v3.4.8 提炼）

> 游戏 beta 与 main 长期并行，签名各差一个参数；一份 mod 要同时吃两套接口。BaseLib v3.4.8 的 57 个提交里约三分之一是这类工程化工作，全部可移植到纯原生。
> 转到纯原生时不需要它的任何 dll，只需要抄这套「分档 + 兜底 + 不崩」的结构。

## 一、把兼容层拆成独立程序集

最容易被打穿的是「直接实现游戏接口/继承游戏 UI 基类」的那一层：接口签名一变，整个 mod 加载期就崩。BaseLib 的做法是把这层拆成独立程序集 `BaseLibCompatibilityHelper`，游戏再变只重编这一个小 dll：

```xml
<ProjectReference Include="..\BaseLibCompatibilityHelper\BaseLibCompatibilityHelper.csproj" Private="false"/>
```

`Private="false"` = 不把它的 dll 复制进输出（两个 mod 各自部署，避免同名 dll 打架）；消费方在自己 `manifest.json` 里声明依赖，只要求存在、不锁版本：`"dependencies": [{"id": "BaseLibCompatibilityHelper", "min_version": "0.0.0"}]`；`.sln` 里同时挂两个工程一起编。

## 二、按游戏版本分档打补丁

`BetaMainCompatibility.Version` 是 `Lazy<SemanticVersion>`，取 `ReleaseInfoManager.Instance.SemVer`，异常或为空时回退 `999.999.999`（当作"太新"，走最新分支）。三段式判据：

```csharp
if (BetaMainCompatibility.Version.LessThanOrEqual(OldestVersion))     { /* 0.109.999 及更旧 */ }
else if (BetaMainCompatibility.Version.LessThanOrEqual(OldBetaVersion)) { /* 0.111.999 过渡档，标 [Obsolete] */ }
else                                                                   { /* 当前档 */ }
```

**铁律：目标方法找不到时只 `Logger.Warn($"Unable to find target method for patching … for game version {Version}")` 然后 `return`，绝不抛**——游戏小版本更新不该让玩家看到崩溃。

**何时删**：标了 `[Obsolete("No longer differs between main and beta.")]` 的兼容包装，在 beta 合入 main 后立刻删（BaseLib 这次就删掉了 `CombatStateWrapper` 整类）；按版本分档的补丁则留到 `min_game_version` 抬过那一档为止。

## 三、补丁目标的多签名兜底

显式 `TargetMethod()` 比 `nameof` 更抗改名，重载变动用 `??` 串起来：

```csharp
[HarmonyPatch]
public static class TweenPatch
{
    private static MethodBase TargetMethod() =>
        typeof(CardPileCmd).DeclaredMethod("GetTweenForCardsChangingPiles", [typeof(IEnumerable<CardPileAddResult>)])
        ?? typeof(CardPileCmd).DeclaredMethod("GetTweenForCardsChangingPiles", [typeof(IEnumerable<CardPileAddResult>), typeof(bool)]);
}
```

其它同类手法：构造器用 `typeof(NetHostGameService).GetDeclaredConstructors().Single()`；静态字段改按对象取时，用一层包装函数抹平（0.113 把 `CardSelectCmd.Selector` 改成按玩家查，包装里统一走 `CurrentCardSelector(player)`）。

**反面教材**：BaseLib 自己在 `NetClientCustomMessagePatches.TargetMethod` 里复制粘贴写成了 `NetHostGameService`，而 Postfix 首个参数是 `NetClientGameService` → 客户端那条注册静默失效、没有任何报错。目标类型与 Postfix 参数类型必须逐个对齐，抄改后一定复核。

## 四、transpiler 要「插入」不要「替换」

自定义牌堆要让「往自定义堆里加卡」也走过场动画，就得在 `CardPileCmd.Add` 的两个 transpiler 里改 `PileTypeExtensions.IsCombatPile()` 的判断结果。第一版直接替换指令的 operand：

```csharp
instruction.operand = extraCheckMethod;   // ❌ 原生调用结果被抹掉
```

后果是**别人基于该调用打的补丁全部失效**。改用 `Dup` 保留原结果再与自己的判定或起来：

```csharp
if (code[i].Calls(original))
{
    code.Insert(i++, new CodeInstruction(OpCodes.Dup));
    code.Insert(++i, new CodeInstruction(OpCodes.Call, extraCheck));
}
// [MethodImpl(MethodImplOptions.NoInlining)]
// private static bool IsCombatPileForTransition(PileType pileType, bool wasCombatPile)
//     => CustomPiles.IsCustomPile(pileType) || wasCombatPile;
```

`NoInlining` 是必须的（否则 JIT 可能内联后 IL 指纹对不上）。还要注意**安装时机**：transpiler 在 `PatchAll` 里比 prefix 先装上，原生 enum 内联检查会挡住后注册的内容，所以补丁里要显式查自己的注册表而不是依赖别人的 prefix。

## 五、反射与类型解析的回退

- `Type.GetType($"{typeName}, sts2")` 在「程序集在、类型不在」时**返回 null 而不是抛异常**，只写 `catch` 的回退分支永远进不去 → 必须 `?? AccessTools.TypeByName(typeName)`。
- 泛型互操作：`MakeGenericMethod` 之前比对泛型参数个数，`if (!target.IsGenericMethodDefinition || target.GetGenericArguments().Length != genericTypes.Length) continue;`，否则遇到同名的非泛型重载直接炸。

## 六、破坏性变更清单（v3.4.7 → v3.4.8 实测）

| 被删/被改 | 替代写法 |
|---|---|
| `CombatStateWrapper` 整类 | 直接用 `Creature.CombatState`（`ICombatState?`）；`HealthBarForecastContext.CombatState` 类型随之从 `CombatStateWrapper?` 变 `ICombatState?`，record struct 本体没变，重编即可 |
| `CommonActions.Apply<T>` / `ApplySelf<T>` 的无上下文重载 | 必须传 `PlayerChoiceContext`，手上没有就 `new ThrowingPlayerChoiceContext()`（明确暴露"这里不该有选择"） |
| `GetBaseValueForIConvertible` 不可覆写 | 自定义 var 改在 `UpdateCardPreview` 里 `BaseValue = NumericValue;` |
| `CardModifier` 没有占位模型 | 存档里已卸载 mod 的 modifier 用 `GetByIdOrNull<AbstractModel>(Id) as CardModifier` 单条丢弃，别让 `GetById<T>` 抛 `ModelNotFoundException` 毁掉整份存档读取 |

## 演进路线

- 当前：分档补丁 + 多签名兜底 + 找不到就 Warn 退场，已成为各模块 Harmony 文档的默认写法
- 下一致命场景：自定义枚举值（`PileType`/`CardKeyword`）跨版本存档稳定性仍未验证，涉及存档时优先用字符串 id
