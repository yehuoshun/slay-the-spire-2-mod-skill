# 自定义 Modifier（运行规则）

> 参考：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomModifierModel.cs` + `STS2Plus` 源码

---

## 概述

Modifier 是 Custom Run 界面中可勾选的运行规则（如"敌人 HP 翻倍"、"开局 1 血"）。继承 `ModifierModel` 抽象类。

Modifier 本身只定义**元数据**（标题、描述、图标、分类），实际效果通过 Harmony Patch 实现。

---

## 基础模板

```csharp
using MegaCrit.Sts2.Core.Models;

public class MyModifier : ModifierModel
{
    public override ModifierAlignment Alignment => ModifierAlignment.Good;
    public override string? MutuallyExclusiveGroup => null;
    public override int SortOrder => 0;
}
```

---

## 可覆盖属性

```csharp
public class MyModifier : ModifierModel
{
    // 对齐类型：Good / Bad / None
    public override ModifierAlignment Alignment => ModifierAlignment.Good;

    // 互斥分组：同组内只能选一个
    public override string? MutuallyExclusiveGroup => null;

    // UI 排序顺序
    public override int SortOrder => 0;
}
```

### ModifierAlignment

| 值 | 说明 | 示例 |
|-----|------|------|
| `ModifierAlignment.Good` | 正面规则 | 攻击+2 伤害 |
| `ModifierAlignment.Bad` | 负面规则 | 玻璃大炮（1血开局） |
| `ModifierAlignment.None` | 中立 | 无尽模式 |

### 属性说明

| 属性 | 类型 | 说明 |
|------|------|------|
| `Alignment` | `ModifierAlignment` | 分类，影响 Custom Run UI 显示位置 |
| `MutuallyExclusiveGroup` | `string?` | 互斥分组，同组只能选一个 |
| `SortOrder` | `int` | 排序顺序 |
| `Title` | `LocString` | 标题（本地化） |
| `Description` | `LocString` | 描述（本地化） |
| `IconPath` | `string` | 图标路径 |

---

## 本地化

路径：`res://<模组ID>/localization/<语言代码>/modifiers.json`

```json
{
  "MY_MODIFIER": {
    "title": "示例规则",
    "description": "所有攻击牌获得 +2 点伤害。"
  }
}
```

---

## 注册到 Custom Run 界面

默认情况下，自定义 Modifier 不会出现在 Custom Run 的规则列表中。需要 Patch `NCustomRunModifiersList.GetModifiersTickedOn`：

```csharp
[HarmonyPatch(typeof(NCustomRunModifiersList), "GetModifiersTickedOn")]
public static class CustomModifierListPatch
{
    private static void Postfix(ref List<ModifierModel> __result)
    {
        __result.Add(ModelDb.Get<MyModifier>().ToMutable());
    }
}
```

---

## 序列化

Modifier 在运行中需要序列化保存。Patch `ModifierModel.FromSerializable` 处理自定义反序列化：

```csharp
[HarmonyPatch(typeof(ModifierModel), "FromSerializable")]
public static class CustomModifierSerializationPatch
{
    private static bool Prefix(SerializableModifier serializable, ref ModifierModel __result)
    {
        // 自定义反序列化逻辑
        // 返回 false 跳过原版逻辑
        return true;
    }
}
```

---

## 效果实现模式

Modifier 的效果通常通过 Harmony Patch 实现，而不是在 ModifierModel 子类中写逻辑。

### 模式 1：Patch 检查 Modifier 是否激活

```csharp
[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDamage))]
public static class AttackDamagePatch
{
    private static void Postfix(CardModel __instance, ref int __result)
    {
        if (IsModifierActive())
            __result += 2;
    }

    private static bool IsModifierActive()
    {
        // 检查当前运行中是否有该 Modifier
        // 通过 RunState 或反射
    }
}
```

### 模式 2：Patch 构造函数/创建方法

```csharp
[HarmonyPatch(typeof(CardModel), "ToMutable")]
public static class CardCreationPatch
{
    private static void Postfix(object? __result)
    {
        if (__result is CardModel card && IsModifierActive())
        {
            // 修改卡牌属性
        }
    }
}
```

### 模式 3：Patch 事件/行为

```csharp
[HarmonyPatch(typeof(EncounterModel), "GenerateMonsters")]
public static class EncounterPatch
{
    private static void Postfix(ref IEnumerable<(MonsterModel, string?)> __result)
    {
        if (IsModifierActive())
        {
            // 修改怪物 HP 等
        }
    }
}
```

---

## 完整示例：攻击增强规则

```csharp
// Modifier 模型
public class AttackBuffModifier : ModifierModel
{
    public override ModifierAlignment Alignment => ModifierAlignment.Good;
}

// 效果 Patch
[HarmonyPatchCategory("Modifiers")]
[HarmonyPatch]
public static class AttackBuffPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(CardModel), nameof(CardModel.GetDamage));
    }

    private static void Postfix(CardModel __instance, ref int __result)
    {
        if (IsActive())
            __result += 2;
    }

    private static bool IsActive()
    {
        // 检查运行状态中是否有 AttackBuffModifier
        return false;
    }
}
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| Modifier 不显示在 Custom Run | 检查 `GetModifiersTickedOn` Patch |
| Modifier 效果不生效 | 检查 Harmony Patch 是否正确触发 |
| 序列化错误 | 实现 `ModifierModel.FromSerializable` Prefix |
| 多人不同步 | 实现自定义网络消息同步规则选择 |
| Alignment 不生效 | 检查枚举值是否正确 |

---

## 演进路线

- 当前：手动 Patch 注册 + 多个 Patch 实现效果
- 更优：`CustomModifierCatalog` 工厂模式 + `PatchCategory` 分类批量加载
- BaseLib：`CustomModifierModel` 带自动注册