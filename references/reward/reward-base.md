# 自定义奖励：奖励基类

> RewardType 注入见 [reward-core.md](reward-core.md)。序列化见 [reward-serialization.md](reward-serialization.md)。

## Step 2: 继承 Reward

```csharp
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

public abstract class MyCustomReward : Reward
{
    protected MyCustomReward(Player player) : base(player) { }

    /// <summary>子类必须返回自定义的 RewardType</summary>
    protected abstract override RewardType RewardType { get; }

    /// <summary>奖励显示顺序（原生奖励索引见下方表格）</summary>
    public override int RewardsSetIndex => 9; // 原生奖励之后

    /// <summary>获取本地化字符串</summary>
    public LocString GetLoc(string key)
    {
        return new LocString("gameplay_ui", key);
    }
}
```

### 必须重写的成员

| 成员 | 类型 | 说明 |
|------|------|------|
| `RewardType` | 属性 | 返回注入的自定义枚举值 |
| `Populate()` | 方法 | 准备奖励内容（随机生成等） |
| `IsPopulated` | 属性 | 奖励是否已准备好 |
| `Description` | 属性 | 奖励面板显示的描述文本 |
| `IconPath` | 属性 | 奖励面板显示的图标路径 |
| `ToSerializable()` | 方法 | 将奖励数据转为可存 JSON |
| `RewardsSetIndex` | 属性 | 在面板中的渲染顺序 |

### 原生 RewardsSetIndex 参考

| 索引 | 奖励类型 |
|:----:|---------|
| 0 | 角色专属遗物 |
| 1 | 随机遗物 |
| 2 | 稀有遗物 |
| 3 | 遗物 |
| 4 | 药水 |
| 5 | 稀有卡 |
| 6 | 普通卡 |
| 7 | 无色卡 |
| 8 | 金币 |
| **9+** | **自定义奖励在此之后** |

### 不继承 CustomReward 的说明

BaseLib 的 `CustomReward` 额外封装了：
- `GetLoc()` 自动拼接 key 为 `ModID-ClassName`
- `DeserializeMethod` delegate + `Initialize()` 自动注册

纯原生需要手动实现这些，见 [reward-serialization.md](reward-serialization.md)。

## 参见

- [reward-core.md](reward-core.md) — RewardType 注入
- [reward-serialization.md](reward-serialization.md) — 序列化与存档
- [reward-examples.md](reward-examples.md) — 完整示例