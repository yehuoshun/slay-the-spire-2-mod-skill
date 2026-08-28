# 自定义附魔

> 参考：[杀戮尖塔2模组开发教程05 - 自定义附魔 - 哔哩哔哩](https://www.bilibili.com/opus/1180713881530531843)（from 烟汐忆梦_YM）


## 章节导航

| 内容 | 文件 |
|------|------|
| 模板、回调与案例 | [enchantment-core.md](enchantment-core.md) |
| 应用、速查与自动注册 | [enchantment-advance.md](enchantment-advance.md) |

## 概述

所有附魔效果继承 `EnchantmentModel` 抽象类。附魔附着在卡牌上（`Card` 属性），修改卡牌的行为。核心是：`CanonicalVars` 定义数值、`Enchant*Additive/Multiplicative` 改伤害/格挡、`OnPlay` 响应打出、`RecalculateValues` 让数值跟随附魔层数。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 附魔不生效 | 检查 `CanEnchant`/`CanEnchantCardType` 是否放行 |
| 伤害/格挡加成无效 | 重写 `EnchantDamageAdditive(decimal, ValueProp)` / `EnchantBlockAdditive(decimal)` |
| 数值不随层数变化 | 重写 `RecalculateValues()` 同步 `DynamicVars` |
| 图标不显示 | PNG 命名与附魔 ID 小写一致（`enchantments/<id小写>.png`） |
| 本地化不生效 | 必须 Publish 而非 Build；键用 `title`/`description` |
| 打出卡无反应 | 用 `OnPlay(PlayerChoiceContext, CardPlay?)`（没有 OnCardPlayed） |

---

## 演进路线

- 当前：手动注册（保留为基准写法）
- 纯原生自动注册（Attribute 标记 + ContentRegistry）——已并入 v3「进阶」章节
- v3（本版）：API 全量校正，弃用 v2 的错误签名（`EnchantDamageAdditive(CardModel)`、`OnEnchant(CardModel)`、`OnCardPlayed(CardModel, PlayerChoiceContext)`、`CardCmd.Enchant(card, amount)` 两参、`Amount { get; set; }`）
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，本方案零第三方依赖
