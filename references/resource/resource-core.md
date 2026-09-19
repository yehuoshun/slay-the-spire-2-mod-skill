# 自定义资源：基类

> 注册与生命周期见 [resource-lifecycle.md](resource-lifecycle.md)。

## 资源基类

一个自定义资源就是一个继承 `AbstractModel` 的类（不是 `PowerModel`——资源不是能力，是独立的战斗状态）。

```csharp
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

public abstract class CustomResource : AbstractModel
{
    /// <summary>当前值</summary>
    protected int _amount;
    public int Amount => _amount;

    /// <summary>最大值（可选）</summary>
    public virtual int MaxAmount => 999;

    /// <summary>每回合是否重置到初始值</summary>
    public virtual bool ResetEachTurn => true;

    /// <summary>初始值</summary>
    public abstract int StartAmount { get; }

    /// <summary>准备战斗时调用</summary>
    public virtual void PrepForCombat(PlayerCombatState pcs)
    {
        _amount = StartAmount;
    }

    /// <summary>每回合开始重置</summary>
    public virtual void StartOfTurnReset(PlayerCombatState pcs)
    {
        if (ResetEachTurn) _amount = StartAmount;
    }

    /// <summary>消耗资源，返回是否成功</summary>
    public virtual bool Spend(int amount)
    {
        if (_amount < amount) return false;
        _amount -= amount;
        return true;
    }

    /// <summary>增加资源，不超过上限</summary>
    public virtual void Gain(int amount)
    {
        _amount = Math.Min(_amount + amount, MaxAmount);
    }
}
```

## 参见

- [resource-lifecycle.md](resource-lifecycle.md) — 注册、生命周期 Patch 与示例