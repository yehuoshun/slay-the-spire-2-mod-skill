# dotnet new 模板打包（学自 ModTemplate-StS2）

> 学自 [Alchyr/ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) 的 `Alchyr.Sts2.Templates` 模板仓库。
> 官方把「模组工程骨架」打包成 `dotnet new` 模板，一行命令生成新项目。纯原生路线可照搬这套机制打包自己的骨架。

## 章节导航

| 内容 | 文件 |
|------|------|
| 官方三套模板 + 纯原生落地 | 本文件 |
| template.json 详解（symbols/sourceName/打包使用） | [template-pack-json.md](template-pack-json.md) |

---

## 一、官方三套模板

| 模板名 | 内容 | 适用 |
|--------|------|------|
| **Slay the Spire 2 Mod** | 最简空模组：MainFile + 空目录 | 最小验证 / 纯 Patch 模组 |
| **Slay the Spire 2 Content** | 内容模组：卡牌/能力/遗物基类 + 图片工具 | 不加角色的内容 mod |
| **Slay the Spire 2 Character** | 角色模组：在 Content 基础上 + 角色类/三池 | 自创角色 |

### 模板目录结构（以 Content 为例）

```
content/ContentModTemplate/
├── .template.config/template.json   ← dotnet new 模板元数据（核心，见 template-pack-json.md）
├── ContentMod.csproj                 ← 生产级构建（含高级 Target，见 skeleton-build-targets.md）
├── Directory.Build.props             ← GodotPath / Sts2Path 手动覆盖
├── Sts2PathDiscovery.props           ← 跨平台自动探测游戏路径
├── export_presets.cfg                ← PCK 导出预设
├── project.godot                     ← Godot 项目标识
├── ContentMod.json                   ← 模组清单
├── ContentMod/                       ← 资源目录（只放资源，不编译）
│   ├── images/                       ← card_portraits/powers/relics/charui/...
│   └── localization/eng/             ← ancients/cards/powers/relics/card_keywords/characters/static_hover_tips.json
└── ContentModCode/                   ← 代码目录（MainFile + 各模型基类）
```

> ⚠️ 创建方案时必须勾选 **"Put solution and project in same directory"**，否则 Godot 找不到项目文件。

---

## 三、纯原生落地建议

- 先把纯原生骨架（`project-skeleton.md`）做成自己的模板仓库，`sourceName` 用项目代号，symbols 提供 `ModAuthor` + `NullableChecks` 两个参数即可
- `PublicizeSts` 参数对应纯原生方案 = 是否引 `Krafs.Publicizer`（见 skeleton-build-targets.md），默认 `false`（能不用就不用）
- 模板是**一次性脚手架**：生成后项目独立，模板仓库只负责"初版"；后续演进改项目，不改模板

---

## 演进路线

当前无模板打包（手动复制骨架）。做成 `dotnet new` 模板后，新模组一行命令生成，省掉复制 8+ 个配置文件的手工步骤。
