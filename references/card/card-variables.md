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


### `GetBaseValueForIConvertible` 已不可覆写（v3.4.8）

自定义 `DynamicVar`（`DisplayVar<T>`、自定义 Calculated 变量等）原先靠覆写 `GetBaseValueForIConvertible()` 把「显示值」喂给基类。v3.4.8 起基类不再调用它——覆写还留着也能编过，但**完全不生效**，卡面显示的永远是基类算出来的值。

改成在预览更新时直接赋 `BaseValue`：

```csharp
protected override void UpdateCardPreview()
{
    BaseValue = NumericValue;   // 不是 PreviewValue
}

// NumericValue = 文本能 parse 就用 parse 值，否则回退 BaseValue
public decimal NumericValue => _owner != null && decimal.TryParse(_displayText(_owner), out var v) ? v : BaseValue;
```

判断「这个 override 还有没有用」的通用手法：升级游戏/库之后，凡是「删掉还能编过」的 `override`，多半是该抽象层已经换了实现，留着就是死代码——直接删，别留「以防万一」。
