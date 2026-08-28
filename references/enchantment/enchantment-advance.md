# 自定义附魔：应用、速查与自动注册

## 应用附魔

```csharp
// 为指定卡牌附魔 N 层（泛型指定附魔类型）
CardCmd.Enchant<ExampleEnchantment>(card, amount);

// 或传入附魔实例
CardCmd.Enchant(ModelDb.Enchantment<ExampleEnchantment>().ToMutable(), card, amount);
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `card` | `CardModel` | 要附魔的卡牌实例 |
| `amount` | `decimal` | 附魔层数 |

---

## 附魔回调速查

| 回调 | 触发时机 | 签名 |
|------|---------|------|
| `CanEnchant(CardModel)` | 附魔前 | `bool`（先查 `CanEnchantCardType`） |
| `CanEnchantCardType(CardType)` | 附魔前 | `bool` |
| `OnEnchant()` | 附魔时 | `void`（无参） |
| `EnchantDamageAdditive(decimal, ValueProp)` | 计算伤害 | 返回伤害增量 |
| `EnchantDamageMultiplicative(decimal, ValueProp)` | 计算伤害 | 返回伤害乘算 |
| `EnchantBlockAdditive(decimal)` | 计算格挡 | 返回格挡增量 |
| `EnchantBlockMultiplicative(decimal)` | 计算格挡 | 返回格挡乘算 |
| `OnPlay(PlayerChoiceContext, CardPlay?)` | 附魔卡打出 | 响应打出 |
| `RecalculateValues()` | 层数变化 | 值重算 |

---

## 本地化

路径：`res://<模组ID>/localization/<语言代码>/enchantments.json`（locTable = `enchantments`）

```json
{
  "EXAMPLE_ENCHANTMENT": {
    "title": "示例附魔",
    "description": "增加 {Amount} 点伤害。"
  }
}
```

- `title`：自动读取 `<类名大写>.title`
- `description`：支持 `{Amount}` 与动态变量占位符
- 类名 ID 规则与卡牌一致（大驼峰 → 大写加下划线）

---

## 图标

```
附魔图标：res://images/enchantments/<附魔ID小写>.png
```

---

## 调试

按反单引号 `` ` `` 打开控制台，输入命令为手牌指定位置的卡牌附魔：

```
enchant <手牌位置> <附魔ID> <层数>
```

---

## 进阶：纯原生自动注册

> 从 BaseLib 提炼，零第三方依赖。用 `[EnchantmentModel]` 标记 + ContentRegistry 统一注册。
> 框架完整代码见 [serialization.md](../serialization/serialization.md)「进阶：纯原生自动注册框架」。

```csharp
[EnchantmentModel]
public class MyEnchantment : EnchantmentModel { ... }
```

> 附魔通过 `EnchantmentModel` 继承生效，注册方式与能力类似（`ModelDb.Inject`），用 Attribute 标记后由 ContentRegistry 统一处理。

