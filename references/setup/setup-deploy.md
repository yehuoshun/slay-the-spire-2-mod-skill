# 部署：模组清单、PCK 打包、开发环境与调试

> 环境搭建见 [environment-setup.md](environment-setup.md)。

## 模组清单

模组清单是一个与模组 ID 同名的 JSON 文件，放在 `build/` 目录下。

```json
{
  "id": "MyCustomMod",
  "name": "My Custom Mod",
  "version": "1.0.0",
  "author": "Author",
  "description": "模组描述",
  "has_pck": true,
  "has_dll": true,
  "affects_gameplay": true,
  "dependencies": []
}
```

**关键字段说明：**

| 字段 | 说明 |
|------|------|
| `has_pck` | 有无数据包。纯美化包可设为 `true`，`has_dll` 设为 `false` |
| `has_dll` | 有无代码。纯资源包可不包含 DLL |
| `affects_gameplay` | 是否影响游戏玩法。**联机模式关键**：为 `false` 时不校验，为 `true` 时所有联机玩家必须安装相同模组，否则可能同步问题 |

---

## PCK 数据包

PCK 保存资源（本地化、图像、音效、Spine 动画等）。路径与游戏本体一致则会替换资源（材质包制作方法）。

**手动打包步骤：**

1. 编辑器左上角：**项目 → 导出**
2. 添加导出方案 → 选 Windows
3. 点击 **导出 PCK/ZIP**
4. 保存到 `build/` 目录，文件名 `<ModId>.pck`
5. 取消勾选「使用调试导出」和「导出为补丁」
6. 清单中 `has_pck` 设为 `true`

> 有 `$(GodotPath)` 且资源简单时，可用上方 csproj 的「构建期自动打 PCK」target 替代手动导出。

---

## C# 开发环境

### 创建 C# 解决方案

编辑器：**项目 → 工具 → C# → 创建 C# 解决方案**

### 检查 .NET 版本

打开 `.csproj` 文件，确认目标框架为 `net9.0`，语言版本 `C# 13`。

### 配置依赖库（两种方式）

**方式 A（推荐）：Sts2PathDiscovery.props 自动检测**（见上方「生产级项目骨架」），csproj 里直接引用 `$(Sts2DataDir)` 下的 DLL。

**方式 B（备选，props 不可用时）**：

- 从游戏目录 `data_sts2_<platform>/` 复制 `sts2.dll` + `0Harmony.dll` 到项目 `libs/` 目录
- IDE 中：**右键依赖项 → 添加项目引用 → 浏览 → 选择两个 DLL**

### 声明模组入口

```csharp
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace MyCustomMod;

[ModInitializer(nameof(Initialize))]
public static class MyCustomModInitializer
{
    public static void Initialize()
    {
        Log.Info("[MyCustomMod] 模组加载成功！");
    }
}
```

**规则：**
- 入口类必须是 **静态类**
- 初始化方法必须是 **无参数、无返回值、静态方法**
- 用 `[ModInitializer]` 特性指定初始化方法
- 生产级可用上方 MainFile.cs 模式（常量 + Logger + 脚本检索）

### 构建

右键项目 → **生成**（Build）。成功后 `build/` 目录会得到：
- `MyCustomMod.dll`
- `MyCustomMod.pck`（如果有资源）
- `MyCustomMod.json`（清单）

---

## 调试

将 `build/` 下的三个文件复制到游戏目录的 `mods/` 下，或 Megadot 可执行文件同目录下的 `mods/` 目录。

游戏启动后：
- 右下角提示模组载入
- 日志输出（通过 `Log.Info` 或 `GD.Print`）

---

## 演进路线

- 当前：手动创建项目 + 手动配置依赖
- **生产级骨架**（目录规范 + props 自动检测 + 构建期打 PCK + 生产级入口），学自 ModTemplate 工程化思想，纯原生
- 终极：**把纯原生骨架打包成 `dotnet new` 模板**，一行生成项目（学自 ModTemplate 的模板机制 `Alchyr.Sts2.Templates`）
- 无关 BaseLib：模板机制、目录规范、props 全是原生，BaseLib 的 Custom*Model 示例内容不采用

---

## 已知问题

| 问题 | 解决 |
|------|------|
| `.NET SDK` 版本不匹配 | 编译报 `CS1705`，检查 `global.json` 或 `.csproj` 目标框架 |
| 模组不加载 | 检查清单 `id` 与文件名一致 |
| DLL 引用报错 | 检查 `Sts2PathDiscovery.props` 路径检测是否命中，或改用手动复制 |
| 本地化不生效 | 必须 **Publish**（非 Build），本地化是资源文件 |
| 联机同步问题 | `affects_gameplay` 设置错误 |
| 自动打 PCK 失败 | `$(GodotPath)` 未配置或路径不对，改用手动导出 |