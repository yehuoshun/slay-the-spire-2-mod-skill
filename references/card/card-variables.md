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

## 基础用法

在 `CanonicalVars` 中返回三个变量：Base（基础值）+ Extra（倍率）+ 主变量（自动计算）。

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars
{
    get
    {
        yield return new DynamicVar("FireBase", 6);        // 基础值
        yield return new DynamicVar("FireExtra", 1);       // 倍率乘数
        yield return new CalculatedVar("Fire",             // 自动计算 = base + extra * mult
            b => b.BaseValue,                              // 取 FireBase.BaseValue
            e => e.BaseValue,                              // 取 FireExtra.BaseValue
            (b, e) => b + e * CountFirePowers());          // 自定义计算
    }
}
```

描述中直接用 `{Fire}` 引用：

```json
{
  "FireStrike": {
    "description": "造成 {Fire} 点火焰伤害。"
  }
}
```

## 同一张卡多个自定义变量

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars
{
    get
    {
        yield return new DynamicVar("FireBase", 6);
        yield return new DynamicVar("FireExtra", 1);
        yield return new CalculatedVar("Fire",
            b => b.BaseValue, e => e.BaseValue,
            (b, e) => b + e * GetFireMult());

        yield return new DynamicVar("IceBase", 4);
        yield return new DynamicVar("IceExtra", 1);
        yield return new CalculatedVar("Ice",
            b => b.BaseValue, e => e.BaseValue,
            (b, e) => b + e * GetIceMult());
    }
}
```

描述：`"造成 {Fire} 点火伤和 {Ice} 点冰伤。"`

## CalculatedVar 构造签名

```csharp
public CalculatedVar(
    string name,
    Func<DynamicVar, decimal> getBaseValue,
    Func<DynamicVar, decimal> getExtraValue,
    Func<decimal, decimal, decimal> calculate
)
```

- `getBaseValue` → 取对应 `{Name}Base` 变量
- `getExtraValue` → 取对应 `{Name}Extra` 变量
- `calculate(base, extra)` → 返回最终的显示值

## 参见

- [card-advanced.md](card-advanced.md) — 链式辅助方法、注册、肖像、本地化
- [card-constructor.md](card-constructor.md) — 卡牌构造函数基础