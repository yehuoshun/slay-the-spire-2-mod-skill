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
└── project.godot
```

**规范要点**：
- 资源目录与代码目录**分离**（`MyMod/` vs `MyModCode/`），避免 Godot 把 .cs 当资源、路径清晰
- 本地化按文件类型拆分（cards/relics/powers/...），比单文件更清晰
- 图标路径约定：大图 `big/`、描边 `outline/`，与各模块 reference 的路径规则一致

