# STS2Plus 大型 Mod 架构

参考 StephenSHorton/STS2Plus 的 10 模块分层架构。

## 项目分层

```
STS2Plus/
├── STS2Plus/                  # 入口 + 状态 + 安全层
│   ├── ModEntry.cs            # 初始化入口
│   ├── MultiplayerSafety.cs   # 四人安全判断
│   ├── PatchCategories.cs     # 补丁分类常量
│   └── PlusState.cs           # 位标志状态管理
├── STS2Plus.Patches/          # ~30+ Harmony 补丁文件
├── STS2Plus.Reflection/       # 反射层：RuntimeTypeResolver
├── STS2Plus.Features/         # 纯逻辑功能模块
├── STS2Plus.Config/           # JSON 配置读写
├── STS2Plus.Localization/     # 多语言支持
├── STS2Plus.Modifiers/        # 自定义修改器系统
├── STS2Plus.Modules/          # 玩法规则模块
├── STS2Plus.Multiplayer/      # 多人网络消息
└── STS2Plus.Ui/               # UI 组件
```

## 核心设计决策

### 1. 裸模组架构

不依赖 BaseLib，直接引用 `sts2.dll` + `GodotSharp.dll` + `0Harmony.dll`：

```xml
<Reference Include="sts2">
  <HintPath>$(STS2GameDir)\data_sts2_windows_x86_64\sts2.dll</HintPath>
  <Private>false</Private>
</Reference>
```

### 2. PatchCategory 分类加载

```csharp
// PatchCategories.cs
internal static class PatchCategories
{
    public const string Core = "Core";
    public const string MoreRules = "MoreRules";
    public const string DpsMeter = "DpsMeter";
}

// 补丁类加特性
[HarmonyPatchCategory("Core")]
public static class SomePatch { ... }

// 入口按需加载，每个 category 独立 try-catch
harmony.PatchCategory(typeof(ModEntry).Assembly, "Core");
harmony.PatchCategory(typeof(ModEntry).Assembly, "MoreRules");
```

### 3. 位标志状态管理

一个 int 存 10+ 个 bool 状态：

```csharp
private const int AttackDefenseFlag = 1;   // 0b0001
private const int IronSkinFlag = 4;        // 0b0100
private const int GiantCreaturesFlag = 8;  // 0b1000

public static bool IsAttackDefenseEnabled => (state & AttackDefenseFlag) != 0;

public static void SetAttackDefense(bool enabled)
{
    if (enabled) state |= AttackDefenseFlag;
    else state &= ~AttackDefenseFlag;
}
```

### 4. 反射隔离层

不直接引用游戏类型，通过 `RuntimeTypeResolver` 动态查找：

```csharp
// 处理 Godot 类型可能被移到 GodotPlugins.dll 的情况
Type type = RuntimeTypeResolver.FindType("GodotPlugins.Game")
    ?? RuntimeTypeResolver.FindType("MegaCrit.Sts2.Core.Multiplayer.Game")
    ?? RuntimeTypeResolver.FindTypeByName("Game");
```

### 5. 四人安全层

所有 gameplay 补丁必须检查多人模式角色：

```csharp
// 权威补丁（仅 Host 或单人模式生效）
MultiplayerSafety.ShouldApplyAuthoritativeGameplayPatches()

// 本地玩家补丁（Host + 自己控制的角色）
MultiplayerSafety.ShouldApplyLocalPlayerGameplayPatches(target)
```

## 适用场景判断

| 项目规模 | 推荐架构 |
|----------|---------|
| < 5 个补丁 | 单项目，Patches 文件夹 |
| 5-20 个补丁 | 单项目 + PatchCategory 分类 |
| > 20 个补丁 | 多项目分层（参考 STS2Plus） |
