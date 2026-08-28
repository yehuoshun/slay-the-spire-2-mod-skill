# 自定义章节：注册、本地化与自动注册

## 注册

```csharp
// 手动注册到 ModelDb
ModelDb.Inject(typeof(MyAct));
```

BaseLib 方式：继承 `CustomActModel` 自动注册。

---

## 本地化

路径：`res://<模组ID>/localization/<语言代码>/acts.json`（locTable = `acts`）

```json
{
  "MY_ACT": {
    "title": "自定义章节"
  }
}
```

> 键用 `title`（旧版写 `name` 是错的）。

---

## 进阶：纯原生自动注册

> 从 BaseLib 提炼，零第三方依赖。用 `[ActModel]` 标记 + ContentRegistry 统一 `ModelDb.Inject`。
> 框架完整代码见 [serialization.md](../serialization/serialization.md)「进阶：纯原生自动注册框架」。

```csharp
[ActModel]
public class MyAct : ActModel { ... }
```

