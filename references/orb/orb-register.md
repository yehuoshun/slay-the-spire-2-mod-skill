# 自定义球体：注册、本地化与自动注册

## 注册

球体需要在 ModEntry 中注册：

```csharp
// 手动注册到 ModelDb
ModelDb.Inject(typeof(MyOrb));
```

BaseLib 方式：继承 `CustomOrbModel` 自动注册。

---

## 本地化

路径：`res://<模组ID>/localization/<语言代码>/orbs.json`（locTable = `orbs`）

```json
{
  "MY_ORB": {
    "title": "自定义球体",
    "description": "被动：每回合造成 {D} 点伤害。激发：造成 {D} 点伤害。"
  }
}
```

> 键用 `title`/`description`（旧版写 `name` 是错的）。

---

## 进阶：纯原生自动注册

> 从 BaseLib 提炼，零第三方依赖。用 `[OrbModel]` 标记 + ContentRegistry 统一 `ModelDb.Inject`。
> 框架完整代码见 [serialization.md](../serialization/serialization.md)「进阶：纯原生自动注册框架」。

```csharp
[OrbModel]
public class MyOrb : OrbModel { ... }
```

> 需要进随机池时，ContentRegistry 里额外处理（或参考上方「随机池注入」Patch）。

