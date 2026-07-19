# 环境搭建 & 创建项目

> 参考：[烟汐忆梦_YM 的 B站教程](https://www.bilibili.com/opus/1179300682687053826)

---

## 模组构成

一个完整的模组由三个文件组成（需同名、同目录）：

| 文件 | 说明 | 是否必须 |
|------|------|---------|
| `<ModId>.json` | 模组配置清单 | ✅ 必须 |
| `<ModId>.pck` | 数据包（资源、本地化、图像） | ❌ 可选 |
| `<ModId>.dll` | 程序集（C# 代码） | ❌ 可选 |

---

## 环境要求

| 工具 | 用途 | 下载 |
|------|------|------|
| **Megadot 编辑器** | STS2 定制的 Godot 编辑器 | [megadot.megacrit.com](https://megadot.megacrit.com) |
| **.NET 9.0 SDK** | 编译 C# 代码 | 微软官网 |
| **Rider / VS** | 写代码 | 按需 |

**推荐使用 Megadot 而非官方 Godot**，因为 STS2 的核心依赖库版本可能与官方分支不一致，用官方分支可能触发兼容性问题。

---

## 创建项目

1. 打开 Megadot → 新建项目
2. 项目名称用英文，与模组 ID 一致（影响命名空间和导出文件名）
3. 渲染器选择 **兼容渲染器**（2D 场景渲染快，不影响主程序设置）
4. 创建完成后，设置模组图像：`res://<modid>/mod_image.png`（模组安装页面的显示图）

---

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

**打包步骤：**

1. 编辑器左上角：**项目 → 导出**
2. 添加导出方案 → 选 Windows
3. 点击 **导出 PCK/ZIP**
4. 保存到 `build/` 目录，文件名 `<ModId>.pck`
5. 取消勾选「使用调试导出」和「导出为补丁」
6. 清单中 `has_pck` 设为 `true`

---

## C# 开发环境

### 创建 C# 解决方案

编辑器：**项目 → 工具 → C# → 创建 C# 解决方案**

### 检查 .NET 版本

打开 `.csproj` 文件，确认目标框架为 `net9.0`，语言版本 `C# 13`。

### 配置构建后复制 DLL

在 `.csproj` 中添加：

```xml
<Target Name="CopyDllToBuild" AfterTargets="Build">
  <Copy SourceFiles="$(OutputPath)$(AssemblyName).dll" DestinationFolder="../build/" />
</Target>
```

### 配置依赖库

需要两个 DLL：

- `sts2.dll` — 游戏本体类库
- `0Harmony.dll` — Harmony 补丁库

**获取方式**：从游戏目录 `data_sts2_<platform>/` 复制到项目 `libs/` 目录。

在 IDE 中：**右键依赖项 → 添加项目引用 → 浏览 → 选择两个 DLL**

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

## 已知问题

| 问题 | 解决 |
|------|------|
| `.NET SDK` 版本不匹配 | 编译报 `CS1705`，检查 `global.json` 或 `.csproj` 目标框架 |
| 模组不加载 | 检查清单 `id` 与文件名一致 |
| DLL 引用报错 | 检查 `libs/` 下的 `sts2.dll` 和 `0Harmony.dll` 版本是否与游戏一致 |
| 本地化不生效 | 必须 **Publish**（非 Build），本地化是资源文件 |
| 联机同步问题 | `affects_gameplay` 设置错误 |