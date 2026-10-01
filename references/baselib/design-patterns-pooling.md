# 纯原生设计模式：[Pool] 基类注解继承

> 从 design-patterns-core.md 拆出。学自 ModTemplate-StS2 官方模板。

## 基类注解继承（推荐写法）

官方模板把 `[Pool]` 标在**抽象基类**上，子类继承即自动进池，新卡零注册代码：

```csharp
// 基类标一次：本模组所有遗物自动进角色池
[Pool(typeof(CharModRelicPool))]
public abstract class CharModRelic : CustomRelicModel { ... }

// 子类无需任何标注
public class MyRelic : CharModRelic { ... }
```

> 纯原生复刻：自定义 Attribute 本身是 `AttributeUsage(Class, Inherited = true)`（默认允许继承），反射扫描时 `GetCustomAttribute` 会自动沿继承链查找，基类标注天然生效。
> 与 CardPool 的区别：池类型从子类类型反射可得，同一基类下所有子类进同一池。
