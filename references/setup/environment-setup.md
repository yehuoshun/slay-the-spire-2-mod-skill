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

## 生产级项目骨架

> 生产级工程规范（目录结构、路径检测、csproj、入口、角色资源）已拆分到独立文件：
> **[project-skeleton.md](project-skeleton.md)**

部署与调试见 [setup-deploy.md](setup-deploy.md)。
