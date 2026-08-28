# 项目骨架（Project Skeleton）

> 生产级工程规范，从 environment-setup 拆出。学自 ModTemplate 工程化思想，纯原生，零第三方依赖。
> 基础教程见 [environment-setup.md](environment-setup.md)。

> 学自 ModTemplate 的工程化思想：资源/代码分离、目录规范、自动检测。骨架本身不依赖任何第三方，直接纯原生可用。


## 章节导航

| 内容 | 文件 |
|------|------|
| 标准目录结构 | [skeleton-directory.md](skeleton-directory.md) |
| 跨平台路径自动检测 | [skeleton-paths.md](skeleton-paths.md) |
| 构建、入口与角色资源 | [skeleton-build.md](skeleton-build.md) |

## 演进路线

- 当前：手动创建项目 + 手动配置依赖（见 environment-setup.md）
- 本文件：生产级骨架（目录规范 + props 自动检测 + 构建期打 PCK + 生产级入口）
- 终极：把纯原生骨架打包成 `dotnet new` 模板，一行生成项目（学自 ModTemplate 的 `Alchyr.Sts2.Templates` 模板机制）
- 无关 BaseLib：模板机制、目录规范、props 全是原生，BaseLib 的 Custom*Model 示例内容不采用

