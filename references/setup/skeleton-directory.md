# 项目骨架：标准目录结构

### 标准目录结构

```
MyMod/
├── MyMod/                    ← Godot 资源目录（与代码分离）
│   ├── images/
│   │   ├── card_portraits/   ← 卡牌肖像（big/ 大图 1000x760）
│   │   ├── potions/          ← 药水（outline/ 描边）
│   │   ├── powers/           ← 能力图标（big/ 256x256）
│   │   ├── relics/           ← 遗物（big/ + outline/ 描边）
│   │   └── charui/           ← 角色 UI（大能量图标、选择图标、地图标记）
│   ├── localization/
│   │   └── eng/
│   │       ├── cards.json
│   │       ├── relics.json
│   │       ├── powers.json
│   │       ├── potions.json
│   │       ├── ancients.json
│   │       ├── card_keywords.json
│   │       ├── characters.json
│   │       └── static_hover_tips.json
│   └── mod_image.png         ← 模组安装页展示图
├── MyModCode/                ← C# 代码目录（与资源分离）
│   ├── Cards/
│   ├── Potions/
│   ├── Powers/
│   ├── Relics/
│   ├── Character/
│   └── MainFile.cs
├── MyMod.csproj
├── MyMod.json
├── Sts2PathDiscovery.props   ← 自动检测游戏路径（见下）
├── .editorconfig             ← IDE 约定，入 Git（见下）
├── .gitignore                ← 忽略规则（见下）
└── project.godot
```

**规范要点**：
- 资源目录与代码目录**分离**（`MyMod/` vs `MyModCode/`），避免 Godot 把 .cs 当资源、路径清晰
- 本地化按文件类型拆分（cards/relics/powers/...），比单文件更清晰
- 图标路径约定：大图 `big/`、描边 `outline/`，与各模块 reference 的路径规则一致

---

## .editorconfig（模板标配，学自 ModTemplate 48151e0）

官方模板从 v2.5.4 起每个模板都自带 `.editorconfig` 并**纳入版本控制**（旧版 .gitignore 曾把 `.editorconfig` 忽略掉，PR #23 修正）。内容就 7 行：

```ini
root = true

[*.cs]

# Don't suggest removing or adding `this.` to fields/properties/methods
dotnet_diagnostic.IDE0009.severity = none
dotnet_diagnostic.IDE0003.severity = none
```

作用：关闭 Rider/VS 对 `this.` 前缀的「建议加 / 建议删」提示（IDE0009 = add/remove `this.`，IDE0003 = remove `this.`）。STS2 mod 代码里大量直接写字段/属性（Harmony 风格），这两个提示噪音很大，官方模板直接按掉。

> 注意：只关 IDE 风格建议，**不影响编译**；dotnet build 照常跑。ModAnalyzers 的校验（模型注册等硬规则）不归 editorconfig 管，仍由分析器强制执行。

## .gitignore（模板标配，学自 ModTemplate 48151e0）

三个模板（ModTemplate / ContentModTemplate / CharacterModTemplate）同步重构，按用途分组：

```gitignore
# Godot 4+
.godot/
.nomedia


# godot-spine extension
bin/

# C# project cache files
Properties/
packages/

# C# project local settins
Directory.Build.props

# Visual Studio
.vs/

# Rider
.idea/
*.sln.DotSettings.user
```

**要点**：
- `.godot/` + `.nomedia`：Godot 4+ 导入缓存与 Android 导出标记，必忽略
- `bin/`：godot-spine 扩展产物（官方注释原文，一般用不到，留着无害）
- `Properties/`、`packages/`：C# 项目缓存（NuGet 还原中间产物）
- `Directory.Build.props`：本机路径配置（Sts2PathDiscovery.props 这类），**不共享**
- `.vs/`（VS）+ `.idea/`、`*.sln.DotSettings.user`（Rider）：IDE 本地文件，`.user` 后缀是 Rider 用户级设置
- **不要忽略 `.git/`**：旧版模板曾把 `.git/` 写进 .gitignore（纯冗余坏习惯），PR #23 已移除；`publish/`、`/android/` 也一并删掉（模板已无这些产物）

