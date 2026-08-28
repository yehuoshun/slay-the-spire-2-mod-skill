# 自定义卡牌

> 参考：[杀戮尖塔2模组开发教程03 - 自定义卡牌 - 哔哩哔哩](https://www.bilibili.com/opus/1179979923167641608)（from 烟汐忆梦_YM）


## 章节导航

| 内容 | 文件 |
|------|------|
| 构造函数与基础卡牌 | [card-constructor.md](card-constructor.md) |
| 核心 API 与使用条件 | [card-api.md](card-api.md) |
| 进阶写法与资源 | [card-advanced.md](card-advanced.md) |

## 概述

所有卡牌继承 `CardModel` 抽象类。卡牌通过构造函数定义基础属性，通过回调实现行为逻辑。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 卡牌不显示 | 检查 `[CardPool(typeof(池类))]` attribute 或手动 `AddModelToPool` |
| 肖像图不显示 | PNG 路径/命名与卡池和卡牌 ID 一致 |
| 本地化不生效 | 必须 Publish 而非 Build |
| 卡牌无法打出 | 检查 `IsPlayable` 回调 + `TargetType` 是否匹配 |
| 升级数值不变 | 用 `UpgradeValueBy()` 而非直接改字段 |
| OnPlay 空引用 | `ArgumentNullException.ThrowIfNull(cardPlay.Target, ...)` 空值检查 |
| 特效不显示 | Spine 资源必须放在 `res://animations/` 下 |
| 音效文件 | FMOD 使用 `.bank` 文件；调试用 `res://debug_audio/` 下的路径 |

---

## 演进路线

- 当前写法：手动 `DamageCmd.Attack().FromCard().Targeting().Execute()` 链式调用（保留为基准）
- **CardFx 链式辅助方法**（见上方章节）——`this.DealDamage(ctx, play, 6)` 一行完成攻击
- 卡池注册：v2 用**纯原生自动注册框架**（Attribute + ContentRegistry，见 [serialization.md](../serialization/serialization.md)），替代手动逐个注册
- 灵感来源：BaseLib `ConstructedCardModel` Builder，本方案零第三方依赖
