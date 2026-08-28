# 纯原生设计模式总纲（从 BaseLib 提炼）

> 模式 2「链式辅助方法」签名校正（`CardPlayState` → `CardPlay`，对齐 card）。旧版存档见 [archive](https://github.com/yehuoshun/slay-the-spire-2-mod-skill-archive/blob/main/references/baselib/design-patterns-v1.md)。

> 本 skill 主推**纯原生**开发：只靠 `0Harmony.dll` + `sts2.dll`，零第三方依赖。
> BaseLib 是优秀的设计参考，本文件把它所有便利机制**转译为纯原生实现**，作为各模块子项的通用地基。


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

