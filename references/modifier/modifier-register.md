# 自定义 Modifier：本地化、注册与序列化

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

Modifier 序列化由原生 `FromSerializable` 自动处理（`SaveUtil.ModifierOrDeprecated` + `SavedProperties`）。自定义属性用 `[SavedProperty]` + `InjectTypeIntoCache` 即可（见 [serialization.md](../serialization/serialization.md)），**无需 Patch `FromSerializable`**。

