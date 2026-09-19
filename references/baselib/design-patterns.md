# 纯原生设计模式总纲（从 BaseLib 提炼）

> 模式 2「链式辅助方法」签名校正（`CardPlayState` → `CardPlay`，对齐 card）。旧版存档见 [archive](https://github.com/yehuoshun/slay-the-spire-2-mod-skill-archive/blob/main/references/baselib/design-patterns-v1.md)。

> 本 skill 主推**纯原生**开发：只靠 `0Harmony.dll` + `sts2.dll`，零第三方依赖。
> BaseLib 是优秀的设计参考，本文件把它所有便利机制**转译为纯原生实现**，作为各模块子项的通用地基。
> **基于 BaseLib v3.4.7（2026-09-11）** 提炼。

## BaseLib 更新记录

| 版本 | 日期 | 新增内容（影响纯原生设计参考） |
|------|------|------------------------------|
| v3.4.7 | 2026-09-11 | `IPlayCustomPowerSfx`（✅ → [harmony-custom-power-sfx.md](../harmony/harmony-custom-power-sfx.md)）、`HealthBarForecast` 新方向（OutwardFromCurrentHp / InwardFromMaxHp）、`ConfigSection CollapsedByDefault`（✅ → [settings-attributes.md](../settings/settings-attributes.md)）、ModInterop 扩展通用类/方法补丁、自定义资源默认视觉处理 |

> ✅ 标记的项已完成纯原生转译或文档补充。其余为 BaseLib API 自身增量，**不影响本文件的纯原生转译内容**。

## 旧版存档

- 本文件旧版：[design-patterns-v1.md](https://github.com/yehuoshun/slay-the-spire-2-mod-skill-archive/blob/main/references/baselib/design-patterns-v1.md)
- 完整旧版本仓库：[yehuoshun/slay-the-spire-2-mod-skill-archive](https://github.com/yehuoshun/slay-the-spire-2-mod-skill-archive)



## 章节导航

| 内容 | 文件 |
|------|------|
| 自动注册与链式辅助 | [design-patterns-core.md](design-patterns-core.md) |
| 便捷 override 与内联本地化 | [design-patterns-extras.md](design-patterns-extras.md) |
| 常见坑与映射 | [design-patterns-pitfalls.md](design-patterns-pitfalls.md) |

## 演进路线

- 当前：各子项手动注册 + 手写回调
- 本文件：提供纯原生自动注册框架 + 链式辅助 + 便捷 override，可直接落地
- 后续：如遇到 BaseLib 新版本新增便利机制，继续按"提炼 → 纯原生转译"流程补充到对应子项

