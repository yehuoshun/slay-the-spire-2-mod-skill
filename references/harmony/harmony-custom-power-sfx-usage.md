# 自定义能力音效：使用示例与常见问题

## 在能力中使用

```csharp
using MegaCrit.Sts2.Core.Commands;

public class MyPower : PowerModel, ICustomPowerSfx
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public bool PlayCustomSfx(int amount, bool isBuff)
    {
        if (amount >= 3)
        {
            SfxCmd.Play("res://mymod/sfx/my_power_sound.ogg", 1.0f);
            return true;
        }
        return false;
    }
}
```

## SfxCmd.Play 参数

| 参数 | 类型 | 说明 |
|------|------|------|
| `path` | `string` | 音频路径（推荐 `.ogg`） |
| `busVolume` | `float` | 音量倍率（1.0=原始，0.5=减半） |

## 常见问题

| 问题 | 解决 |
|------|------|
| Transpiler 不执行 | 检查 `PatchAll()` 是否调用；类名正确 |
| 自定义音效不触发 | 确认实现了 `ICustomPowerSfx` 且返回 `true` |
| IL 索引偏移 | 游戏更新后 `OnPowerIncreased` 变化 → 更新索引 |
| 音效不加载 | 确认音频已打包进 PCK |

## 演进路线

- 当前：手动 Transpiler
- 更优：如果依赖 BaseLib，直接实现 `IPlayCustomPowerSfx`（零代码量）

## 参见

- [harmony-custom-power-sfx-core.md](harmony-custom-power-sfx-core.md) — 接口 + Transpiler 实现