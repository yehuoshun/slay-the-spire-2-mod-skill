# 自定义遗物

> 参考：[烟汐忆梦_YM 的 B站教程](https://www.bilibili.com/opus/1179604439936270359)
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomRelicModel.cs`


## 章节导航

| 内容 | 文件 |
|------|------|
| 模板、属性与池 | [relic-core.md](relic-core.md) |
| 钩子与动态变量 | [relic-callbacks.md](relic-callbacks.md) |
| 资源、注册与自动注册 | [relic-register.md](relic-register.md) |

## 概述

所有遗物继承于 `RelicModel`（→ `AbstractModel`）。AbstractModel 实现了大量事件钩子，重写即可监听对应事件。

所有模型必须注册到 `ModelDb`，注册时生成 ModelId（用于冲突校验、本地化等）。

---

## 演进路线

- 当前：手动注册（保留为基准写法）
- 纯原生自动注册（Attribute 标记 + ContentRegistry）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`Rarity`→`RelicRarity`、`BigIconPath` public override、`AfterSideTurnStart` 2 参、`GainEnergy(state, amount)`、`EnergyVar(1m)`、编造的 `GetUpgradeReplacement`）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖
## 常见问题

| 问题 | 解决 |
|------|------|
| 图标不显示 | 检查 atlas `.tres` 命名与遗物 ID 小写一致（`IconBaseName`） |
| 描边不对 | 描边纹理大小必须与遗物纹理一致 |
| Starter 遗物在池中不出现 | Starter 不走随机池，需 Patch 初始遗物列表 |
| 本地化不生效 | 必须 Publish 而非 Build |
| `.NET 版本冲突` | 修改 `GodotPlugins.runtimeconfig.json` 为 `"version": "9.0.0"` |
| 遗物 ID 冲突 | 类名必须唯一 |
| 动态变量值不对 | `CanonicalVars` 中的变量名必须与 `DynamicVars["Name"]` 一致；注意构造参数 int/decimal |
