# 自定义卡牌动态变量

> 纯原生实现。灵感来源：BaseLib `CustomCalculatedVar`。

## 概述

卡牌描述里用 `{Damage}`、`{Block}` 显示数值，这些是 `DynamicVar`。原生有以下类型：

| 原生变量 | 说明 |
|---------|------|
| `DynamicVar` | 固定值，在构造时指定 |
| `CalculatedDamageVar` | 受力量/易伤等修正，名字固定为 `Damage` |
| `CalculatedBlockVar` | 受敏捷/脆弱等修正，名字固定为 `Block` |
| `CalculatedVar` | 自定义计算逻辑，名字可指定 |

如果你想在描述里显示自定义的动态数值（如 `{Fire}`），原生 `CalculatedVar` 直接支持——不需要任何 mod 依赖。

## 基础用法（真实 API）

在 `CanonicalVars` 中返回三个变量：Base（基础值）+ Extra（倍率）+ 主变量（自动计算）。

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars
{
    get
    {
        yield return new CalculationBaseVar(6m);      // 基础值（名字固定 "CalculationBase"）
        yield return new CalculationExtraVar(1m);     // 倍率乘数（名字固定 "CalculationExtra"）
        yield return new CalculatedVar("Fire")       // 显示名 {Fire}，自动计算 = base + extra * mult
            .WithMultiplier((card, target) => GetFireMult());
    }
}
```

> ⚠️ 真实验证：`CalculatedVar` 原生只有 `CalculatedVar(string name)` 构造 + `WithMultiplier(Func<CardModel, Creature?, decimal>)`（旧版 4 参构造不存在）。计算固定为 `GetBaseVar().BaseValue + GetExtraVar().BaseValue * mult`，默认 Base/Extra 取 `DynamicVars.CalculationBase/CalculationExtra`（名字固定）。

描述中直接用 `{Fire}` 引用：

```json
{
  "FireStrike": {
    "description": "造成 {Fire} 点火焰伤害。"
  }
}
```

## 同一张卡多个自定义变量

默认 Base/Extra 只有一个（名字固定），多个计算变量需子类覆写 `GetBaseVar()`/`GetExtraVar()` 指向各自变量：

```csharp
public class FireVar : CalculatedVar
{
    public FireVar() : base("Fire") { }
    protected override DynamicVar GetBaseVar() =>
        ((CardModel)_owner).DynamicVars["FireBase"];
    protected override DynamicVar GetExtraVar() =>
        ((CardModel)_owner).DynamicVars["FireExtra"];
}

// CanonicalVars 中：
// yield return new DynamicVar("FireBase", 6);
// yield return new DynamicVar("FireExtra", 1);
// yield return new FireVar().WithMultiplier((card, target) => GetFireMult());
```

描述：`"造成 {Fire} 点火伤。"`（{Ice} 同理建 IceVar 子类）。

## 参见

- [card-advanced.md](card-advanced.md) — 链式辅助方法、注册、肖像、本地化
- [card-constructor.md](card-constructor.md) — 卡牌构造函数基础