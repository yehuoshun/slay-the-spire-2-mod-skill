# 公共 Interop + JSON 配置（位标志同步）

> 实战验证（sts2mod 无尽模式 EndlessMode v0.4.1，2026-10-07）。给其他 mod 提供只读查询 API + 本地 JSON 配置的两个通用模式。

## ① 公共 Interop（只读 + 全兜底）

```csharp
public static class EndlessModeInterop
{
    // 建议通过程序集名 "EndlessMode" 反射调用，避免硬依赖（调用方没装本 mod 也照常编译运行）
    public static int GetCompletedLoopCount()
    {
        try
        {
            if (RunManager.Instance?.DebugOnlyGetState() is not RunState state) return 0;
            return ModEntry.GetCompletedLoopCountForInterop(state);
        }
        catch (Exception ex)
        {
            Log.Warn($"[EndlessMode] Interop failed: {ex.Message}");
            return 0;   // 任何异常/无 run/未进入无尽 → 0，绝不抛出
        }
    }
}
```

- 契约：**无 run / 异常 / 未进入模式一律返回 0**，调用方不需要 try-catch。
- 建议消费方式：`Assembly.Load("EndlessMode")` 反射拿类型再调（返回 int/string 等基础类型，反射成本低）。
- 读状态统一走 `RunManager.Instance.DebugOnlyGetState()`（真实 API）。

## ② JSON 配置（user:// + snake_case + 防静态毒化）

```csharp
internal sealed class EndlessModeConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static EndlessModeConfig _current = LoadOrCreate();   // 静态字段初始化即加载

    [JsonPropertyName("grant_mimic_infestation")]   // snake_case 键（Godot/社区惯例）
    public bool GrantMimicInfestation { get; set; } = true;

    internal static string GetConfigPath()
        => Path.Combine(ProjectSettings.GlobalizePath("user://"), "EndlessMode", "config.json");

    private static EndlessModeConfig LoadOrCreate()
    {
        // 文件不存在 → new + Write；存在 → Deserialize + Normalize + 回写
        // 反序列化异常 → 备份默认值重写
    }

    private static void WriteConfig(string path, EndlessModeConfig config)
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, JsonSerializer.Serialize(config, JsonOptions)); }
        catch (Exception ex) { Log.Warn(...); }   // ⚠️ 静态构造路径上的 IO 异常若外泄 → TypeInitializationException 毒化整个类型
    }
}
```

- `JsonSerializerDefaults.Web`：camelCase 输出（配合 JsonPropertyName 显式 snake_case）。
- 加载在静态字段初始化（`LoadOrCreate()`）——**写操作必须 try-catch**，否则静态构造器异常会让之后每次访问配置都抛。
- 数值字段写 setter 前 `Clamp`（Normalize 兜底旧配置越界值）。

## ③ 位标志配置 + 多人同步兜底

```csharp
internal static int GetEnabledRewardFlags()
{
    int flags = 0;
    foreach (EndlessOptionalReward reward in Enum.GetValues<EndlessOptionalReward>())
        if (IsRewardEnabled(reward)) flags |= 1 << (int)reward;
    return flags;
}

internal static int GetDefaultEnabledRewardFlags()   // 多人同步不可用时的兜底值
{
    int flags = 0;   // 全部启用——只能用编译期默认！
    foreach (EndlessOptionalReward reward in Enum.GetValues<EndlessOptionalReward>())
        flags |= 1 << (int)reward;
    return flags;
}
```

- **多人兜底值两端必须一致**：主客本地配置不同会导致发放遗物分叉——同步失败时**只能回退编译期默认（全开）**，绝不能读本地配置。
- 配置经 `PlayerChoiceSynchronizer`（ReserveChoiceId + SyncLocalChoice/等待远程）在进入无尽时同步；解码失败走默认。

## 相关 API 速查

| API | 说明 |
|-----|------|
| `RunManager.Instance.DebugOnlyGetState()` | 拿 RunState（Interop 读取入口） |
| `ProjectSettings.GlobalizePath("user://")` | user 目录绝对路径（Godot） |
| `JsonSerializerDefaults.Web` | Web 默认（camelCase） |
| `Enum.GetValues<T>()` | 遍历枚举做位标志 |

> ℹ️ 无独立配置 UI 时也可复用 skill settings/ 模块的 NSubmenu 纯原生方案；EndlessMode 用的是 `EndlessModeConfigUi`（角色选择屏 postfix 挂设置窗口，工程量较大未展开）。