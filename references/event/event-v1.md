# 自定义事件

> 参考：[杀戮尖塔2模组开发教程06 - 自定义事件 - 哔哩哔哩](https://www.bilibili.com/opus/1180714323922649110)（from 烟汐忆梦_YM）
> API 签名验证：[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) `Abstracts/CustomEventModel.cs`

---

## 概述

所有事件继承 `EventModel` 抽象类。事件是玩家进入问号房间后随机发生的，核心由 `GenerateInitialOptions` 方法返回选项列表。

---

## 基础事件模板

```csharp
public class MyCustomEvent : EventModel
{
    public override Task<List<EventOption>> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        options.Add(new EventOption(
            InitialOptionKey("GetCard"),
            async (context) =>
            {
                CardCmd.Obtain(context.Player, new MyCustomCard());
                SetEventFinished("MY_CUSTOM_EVENT.finish.get_card");
            }
        ));

        options.Add(new EventOption(
            InitialOptionKey("GetGold"),
            async (context) =>
            {
                context.Player.AddGold(50);
                SetEventFinished("MY_CUSTOM_EVENT.finish.get_gold");
            }
        ));

        return Task.FromResult(options);
    }
}
```

---

## 可覆盖属性（新增）

```csharp
public class MyCustomEvent : EventModel
{
    // 事件初始肖像图路径
    public override string? InitialPortraitPath =>
        "res://MyMod/images/events/my_event.png";

    // 事件背景场景路径
    public override string? BackgroundScenePath =>
        "res://MyMod/scenes/events/my_event_bg.tscn";

    // 事件特效场景路径
    public override string? VfxPath =>
        "res://MyMod/scenes/events/my_event_vfx.tscn";
}
```

### 属性说明

| 属性 | 类型 | 说明 |
|------|------|------|
| `InitialPortraitPath` | `string?` | 事件初始肖像图路径 |
| `BackgroundScenePath` | `string?` | 事件背景场景路径 |
| `VfxPath` | `string?` | 事件特效场景路径 |

---

## EventOption 构造

```csharp
new EventOption(
    LocString title,           // 标题文本
    Func<PlayerChoiceContext, Task> callback,  // 选择回调（null = 不可选）
    RelicModel relic = null    // 可选：显示遗物图标
)
```

| 参数 | 说明 |
|------|------|
| `title` | 选项标题文本 |
| `callback` | 选择回调，null 则选项不可触发 |
| `relic` | 可选，在选项旁显示遗物图标 |

### 辅助方法

| 方法 | 说明 |
|------|------|
| `L10NLookup(string key)` | 通用本地化查询 |
| `SetEventFinished(string textKey)` | 退出事件并显示结束文本 |
| `SetEventState(string stateKey)` | 打开新选项（多页切换） |
| `InitialOptionKey(string id)` | 构建初始选项的本地化键 |

---

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

键格式：`<事件ID>.<目标类型>.<项ID>.<键>`

---

## 背景图

```
事件背景纹理：res://images/events/<事件ID小写>.png
```

分辨率：3440x1613

---

## 添加事件

```csharp
[HarmonyPatch(typeof(Overgrowth), "AllEvents", MethodType.Getter)]
public static class OvergrowthAllEventsPatch
{
    public static void Postfix(ref List<EventModel> __result)
    {
        __result.Add(new MyCustomEvent());
    }
}
```

---

## 多页选项

```csharp
public override Task<List<EventOption>> GenerateInitialOptions()
{
    options.Add(new EventOption(InitialOptionKey("Enter"),
        async (context) => { SetEventState("RECHARGE"); }));
    return Task.FromResult(options);
}

public override Task<List<EventOption>> GetStateOptions(string state)
{
    if (state == "RECHARGE")
    {
        // 返回新页面的选项
    }
    return base.GetStateOptions(state);
}
```

---

## 常见问题

| 问题 | 解决 |
|------|------|
| 事件不触发 | 检查 HarmonyPatch 是否生效 |
| 选项不可点击 | 检查 callback 是否为 null |
| 肖像图不显示 | 检查 `InitialPortraitPath` 路径 |
| 本地化不显示 | 检查 events.json 键格式，必须 Publish |
| 背景图不显示 | PNG 命名与事件 ID 小写一致 |
| 多页切换不生效 | 重写 `GetStateOptions` + 本地化定义对应 state |

---

## 演进路线

- 当前：手动 HarmonyPatch 修改 `AllEvents`
- 更优：封装反射注册辅助方法
- BaseLib：`CustomEventModel` 自带 `Option()` 辅助方法