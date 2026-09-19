# Transpiler 实战：自定义能力音效

> 纯原生实现。灵感来源：BaseLib v3.4.7 `IPlayCustomPowerSfx`。

## 需求 & 原理

原生能力施加时 Buff/Debuff 各播一个默认音效。拦截 `NCreature.OnPowerIncreased` 的 `ShouldPlayVfx` 判断点，插自定义音效检查。

## 章节导航

| 内容 | 文件 |
|------|------|
| 接口 + Transpiler + 注册 | [harmony-custom-power-sfx-core.md](harmony-custom-power-sfx-core.md) |
| 使用示例 + 常见问题 | [harmony-custom-power-sfx-usage.md](harmony-custom-power-sfx-usage.md) |

## 参见

- [harmony-basics.md](harmony-basics.md) — Transpiler 基础
- [power-callbacks.md](../power/power-callbacks.md) — 能力回调总表