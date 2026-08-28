# 自定义药水：池、图标、本地化与自动注册

## 添加药水到药水池

```csharp
// 方式②：手动注册（硬规则4-②；方式① attribute 自动注册见 design-patterns 模式1）
ModHelper.AddModelToPool(typeof(SharedPotionPool), typeof(MyAoePotion));
```

### 药水池类型

| 池 | 说明 |
|-----|------|
| `SharedPotionPool` | 共享药水池，所有角色都可获取 |
| `IroncladPotionPool` | 铁甲战士专属 |
| `SilentPotionPool` | 静默猎手专属 |
| `DefectPotionPool` | 故障机器人专属 |
| `NecrobinderPotionPool` | 亡灵契约师专属 |
| `RegentPotionPool` | 摄政者专属 |

> 完整池列表见源码 `Core/Models/PotionPools/`（另有 Event/Token/Deprecated 等边界池）。

---

## 自定义药水图标

```
药水裁切纹理：     res://images/atlases/potion_atlas.sprites/<药水ID小写>.tres
药水描边裁切纹理： res://images/atlases/potion_outline_atlas.sprites/<药水ID小写>.tres
药水大图纹理：     res://images/potions/<药水ID小写>.png
```

添加描边纹理后，未发现的药水会显示描边效果。

---

## 药水本地化

路径：`res://<模组ID>/localization/<语言代码>/potions.json`

```json
{
  "MyAoePotion": {
    "name": "范围伤害药水",
    "description": "对所有敌人造成 30 点伤害。"
  }
}
```

---

## 进阶：纯原生自动注册

> 从 BaseLib 提炼，零第三方依赖。仿照 design-patterns 模式1 的 `CardPoolAttribute`/`RelicPoolAttribute`，自定义 `PotionPoolAttribute` + ContentRegistry 反射扫描，免手动注册。框架完整代码见 [serialization.md](../serialization/serialization.md)「进阶：纯原生自动注册框架」。

```csharp
[PotionPool(typeof(SharedPotionPool))]   // 标记进哪个药水池（attribute 需仿照 design-patterns 模式1 自定义）
public class MyAoePotion : PotionModel { ... }
```

