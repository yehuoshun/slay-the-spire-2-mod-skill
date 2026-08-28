# 自定义事件：本地化、背景图与添加

## 本地化

路径：`res://<模组ID>/localization/<语言代码>/events.json`

```json
{
  "MY_CUSTOM_EVENT": {
    "title": "自定义事件",
    "pages": {
      "INITIAL": {
        "description": "你遇到了一扇神秘的门...",
        "options": {
          "GetCard": { "title": "打开宝箱", "description": "获得一张卡牌" },
          "GetGold": { "title": "拿走金币", "description": "获得 50 金币" }
        }
      }
    },
    "finish": {
      "get_card": "你获得了一张卡牌！",
      "get_gold": "你获得了 50 金币！"
    }
  }
}
```

键格式：`<事件ID>.pages.<STATE>.options.<KEY>`（描述为 `pages.<STATE>.description`）

---

## 背景图

```
事件背景纹理：res://images/events/<事件ID小写>.png
```

分辨率：3440x1613

---

## 添加事件

```csharp
[HarmonyPatch(typeof(Overgrowth), nameof(Overgrowth.AllEvents), MethodType.Getter)]
public static class OvergrowthAllEventsPatch
{
    public static void Postfix(ref IEnumerable<EventModel> __result)
    {
        __result = __result.Append(new MyCustomEvent());
    }
}
```

> 注意：`AllEvents` 真实返回类型是 `IEnumerable<EventModel>`（不是 `List`），Postfix 需 `ref IEnumerable<EventModel>`。
> 也可用下方「进阶」的 attribute + 反射统一注入，避免逐事件手写 Patch。

