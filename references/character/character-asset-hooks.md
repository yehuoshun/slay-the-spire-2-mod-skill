# 资源注入总闸（AssetHooks 模式）

> 实战验证（sts2mod 俄洛伊 Illaoi v0.2.0，2026-10-07）。一个静态类集中 ~50 个 Harmony patch，批量覆盖模型资源 getter 与节点刷新，让自定义角色/卡/遗物/能力在**原版没有扩展点的地方**换上自定义资源。宏观上是「角色自定义资源的大总管」。

## 适用场景

| 需要注入的资源 | 原版是否有扩展点 | 做法 |
|---------------|----------------|------|
| 卡牌 Portrait/Description/EnergyIcon | getter 可 patch | postfix 按类型 switch 返回 |
| 遗物 Icon/IconOutline/BigIcon | getter 可 patch | prefix 短路 |
| 能力 Icon/BigIcon | getter 可 patch | postfix |
| 角色全部 Custom* 路径 | BaseLib 已覆写，但要兼容无 BaseLib 或兜底 | prefix 强制接管 |
| 卡/遗物/能力 UI 节点 | 节点方法（NCard.Reload 等） | prefix/postfix 节点级 |
| 自定义音效 | SfxCmd/NAudioManager | prefix Priority.First 拦截 |

## 核心样板

```csharp
internal static class AssetHooks
{
    private static readonly Dictionary<string, Texture2D> TextureCache = new();
    private static bool Installed;

    public static void Install(Harmony harmony)
    {
        if (Installed) return;
        Installed = true;

        // 模型资源 getter：postfix 换结果
        PatchPostfix(harmony, RequireGetter(typeof(CardModel), nameof(CardModel.Portrait)),
            nameof(CardPortraitPostfix));
        // 原版有值但想强制接管：prefix 返回 false
        PatchPrefix(harmony, RequireGetter(typeof(RelicModel), nameof(RelicModel.Icon)),
            nameof(RelicIconPrefix));
        // 节点刷新：NCard.Reload 等
        PatchPostfix(harmony, RequireMethod(typeof(NCard), "Reload",
            BindingFlags.Instance | BindingFlags.NonPublic), nameof(NCardReloadPostfix));
    }

    private static void CardPortraitPostfix(CardModel __instance, ref Texture2D __result)
    {
        __result = __instance switch
        {
            MyStrike => LoadTexture("res://MyMod/images/strike.png"),
            MyDefend => LoadTexture("res://MyMod/images/defend.png"),
            _ => __result
        };
    }
}
```

- 精确定位帮手：`RequireGetter(Type, propertyName, flags)` = `GetProperty().GetMethod ?? throw`；`RequireMethod` 同 RequireMethod 常规版。找不到直接抛 = 启动即暴露游戏版本不匹配（fail fast，比静默强）。
- getter 用 `RequireGetter`（`nameof(CardModel.Portrait)` 直接拿属性）；字符串名如 `"Reload"` 是方法。
- prefix 短路（`ref T __result` + 返回 false）适合「这类型必须用我的资源」；postfix 适合「默认值基础上替换」。

## 节点级 postfix 清单（实战）

`NRelic.Reload`（private）、`NPower.Reload`、`NCard.UpdateVisuals(PileType, CardPreviewMode)`（prefix First + postfix 双挂）、`NCreatureVisuals._Ready`、`NCharacterSelectButton.Init`、`NCharacterSelectScreen.SelectCharacter`、`NMapMarker.Initialize`、`NEnergyCounter._Ready`、`NCombatRoom.AddCreature`、`NCardLibrary._Ready`——各自注入图标/背景/布局。节点私有字段用**静态缓存的 FieldInfo**取（`RequireField(typeof(NCard), "_energyIcon")`），不每次反射。

## 自定义音效拦截（res:// 本地音频）

```csharp
private static bool SfxCmdPlayPrefix(string sfx, float volume)   // Priority.First
{
    if (!IsMyLocalAudioPath(sfx)) return true;   // 非我的路径放行
    if (ShouldSuppress(sfx)) return false;       // 要消音的：吞掉
    PlayMyAudio(sfx, volume);                    // 自建 AudioStreamPlayer 播
    return false;                                // 短路原版
}
```

- 挂 5 个入口：`SfxCmd.Play(string,float)`、`Play(string,string,float,float)`、`PlayLoop(string,bool)` + `NAudioManager.PlayOneShot` 两种重载、`PlayLoop`。
- 音频 `AudioStream` 用 `Dictionary<string, AudioStream>` 缓存，只加载一次；音量缩放（如 0.65f）。
- `[ThreadStatic] bool` 标记防重入（能量图标格式化等场景）。

## 配套：卡描述动态文本

- `EnergyIconsFormatter.TryEvaluateFormat(IFormattingInfo)` prefix：拦截 `{EnergyIcon:...}` 格式化，替换为自定义精灵图字体 `[img]res://...[/img]` 标签。
- `CardModel.Description` postfix 可整体换成新 LocString（如升级后换整段描述）。

## 注意

- 与 BaseLib 的 `CustomCharacterModel` 双轨：模型侧覆写优先，AssetHooks 兜底原版无扩展点的 getter；两套不冲突（见 character-placeholder-base.md）。
- patch 数量多时按「模型 getter / 节点 / 音效 / 格式化」分组排列，配字段名注释，便于游戏版本升级时逐组校验。