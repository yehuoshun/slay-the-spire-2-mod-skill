# 序列化与注册

> 参考：sts2-res 源码 + SKILL.md 硬规则


## 章节导航

| 内容 | 文件 |
|------|------|
| 注册时机与方式 | [serialization-register.md](serialization-register.md) |
| 纯原生自动注册框架 | [serialization-framework.md](serialization-framework.md) |
| 序列化与兼容 | [serialization-save.md](serialization-save.md) |

## 概述

STS2 的所有模型（卡牌/遗物/能力/药水等）必须注册到 `ModelDb` 才能被识别。序列化确保自定义属性在保存/加载/多人同步时数据不丢失。

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 属性没保存 | 检查 `[SavedProperty]` + `InjectTypeIntoCache` |
| 模型不注册 | 检查注册时机 |
| `AddModelToPool` 泛型报错 | 用反射重载 `AddModelToPool(poolType, modelType)` |
| ModelDb 已初始化无法注册 | 用 `ModelDb.Inject(type)` |
| 卡池缓存不刷新 | 反射重置 `_allCards` 字段 |
| ModelId 冲突 | 类名全局唯一 |

---

## 演进路线

- 当前：手动注册 + 手动 `InjectTypeIntoCache`（保留为基准写法）
- **纯原生自动注册框架**（见上方章节）——自定义 Attribute + ContentRegistry 反射扫描，免手动 `AddModelToPool`
- 灵感来源：BaseLib 的 `autoAdd` + `[Pool]` 机制，但本框架零第三方依赖
