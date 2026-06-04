# 杀戮尖塔 2 Mod 开发指南

> 🦞 从第一行代码到发布 Mod——按顺序学，别跳步。

---

## 📦 开始之前

先装三样东西：

1. **[.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** — 编译必须
2. **[Megadot](https://megadot.megacrit.com)** — STS2 专用编辑器（不能用官方 Godot）
3. **Rider / VS Code** — IDE

然后安装模组模板：
```bash
dotnet new install Alchyr.Sts2.Templates
```

---

## 🎯 教程路线（按难度递增）

| 序号 | 内容 | 难度 | 预计时间 |
|------|------|------|----------|
| 1 | [环境搭建 + 创建项目](https://www.bilibili.com/opus/1179300682687053826) | ⭐ | 30 min |
| 2 | [自定义遗物](https://www.bilibili.com/opus/1179604439936270359) | ⭐⭐ | 1 h |
| 3 | [自定义卡牌](https://www.bilibili.com/opus/1179979923167641608) | ⭐⭐ | 1 h |
| 4 | [自定义药水](https://www.bilibili.com/opus/1180032536494997541) | ⭐⭐ | 30 min |
| 5 | [自定义附魔](https://www.bilibili.com/opus/1180713881530531843) | ⭐⭐ | 30 min |
| 6 | [自定义事件 + 先古之民](https://www.bilibili.com/opus/1180714323922649110) | ⭐⭐⭐ | 2 h |
| 7 | [自定义能力 Buff/Debuff](https://www.bilibili.com/opus/1181126133981118470) | ⭐⭐ | 1 h |
| 8 | [自定义角色 + Spine 动画](https://www.bilibili.com/opus/1182961747166756931) | ⭐⭐⭐⭐ | 4 h |
| 9 | [自定义怪物 + 遭遇](https://www.bilibili.com/opus/1183380755590414377) | ⭐⭐⭐ | 2 h |

> B 站教程是完整的 step-by-step，从环境到每种内容类型都有对应视频/图文。

---

## 🗂 项目结构

```
MyMod/
├── assets/
│   ├── MyMod.json              ← 模组清单
│   ├── images/
│   │   ├── cards/
│   │   ├── relics/
│   │   ├── powers/
│   │   └── events/
│   └── localization/
│       ├── zhs/                ← 简体中文
│       └── eng/                ← 英文
├── src/
│   ├── MyMod.csproj
│   ├── ModEntry.cs             ← 入口
│   ├── ModInfo.cs              ← 常量
│   ├── Cards/
│   ├── Relics/
│   ├── Powers/
│   ├── Events/
│   └── Patches/
├── tools/                      ← 构建脚本
└── dist/                       ← 构建输出（不提交 git）
```

快速创建：`dotnet new sts2mod -n MyMod` / `sts2content` / `sts2character`

---

## 🧩 核心概念速览

### Harmony 补丁

游戏代码改不了，用 Harmony 在运行时注入：

```csharp
[HarmonyPatch(typeof(CardModel), "MaxUpgradeLevel", MethodType.Getter)]
static void Postfix(ref int __result) => __result = 99;
```

### ModelDb — 模型注册

让游戏知道你的卡牌/遗物存在：

```csharp
[Pool(typeof(ColorlessCardPool))]     // 自动注册
public class MyCard : CardModel { }
```

### DynamicVars — 动态变量

所有数值显示/修改都走它：

```csharp
DynamicVars["Damage"].SetBase(8);
DynamicVars["Damage"].UpgradeValueBy(3);  // 8→11
```

### Build vs Publish

| 操作 | 做什么 | 什么时候用 |
|------|--------|-----------|
| **Build** | 编译 .dll → 复制到 mods | 纯代码改动 |
| **Publish** | 编译 + .pck + 全部资源 | 有图片/场景/本地化改动 |

---

## 🃏 Hello World：第一张卡牌

```csharp
[Pool(typeof(ColorlessCardPool))]
public class HelloWorld : CardModel
{
    public HelloWorld() : base(1, CardType.Attack, CardRarity.Common, TargetType.Opponent) {}

    public override string PortraitPath => "res://MyMod/images/cards/hello_world.png";

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(6).FromCard(this).Targeting(ctx.Targets.First()).Execute();
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Damage"].UpgradeValueBy(3);
    }
}
```

---

## 🚀 参考项目

| 项目 | 值得学什么 |
|------|-----------|
| [STS2Plus](https://github.com/StephenSHorton/STS2Plus) | 修正器架构、动态 Patch、安全检测、联机同步 |
| [sts2mod](https://github.com/s1f102500012/sts2mod) | Natsuki 12 个独立模组合集，角色/符文/无尽/难度全方位参考 |
| [YuWanCard](https://github.com/YuWan886/Sts2-YuWanCard) | 生产级角色 Mod，Builder 模式、SavedProperty 序列化 |
| [BaseLib](https://github.com/Alchyr/BaseLib-StS2) | 社区框架，CustomModel 封装、ModConfig 面板 |
| [ModTemplate](https://github.com/Alchyr/ModTemplate-StS2) | 官方模板 |

> ⚡ 建议：先看 STS2Plus 的 Patches 目录学 Harmony，再看 YuWanCard 学架构，Natsuki 合集查具体实现。

---

## 📚 进阶参考

- **[references/architecture.md](references/architecture.md)** — STS2Plus / Natsuki / YuWanCard 架构分析
- **[references/appendices.md](references/appendices.md)** — API 速查、资源路径、本地化、清单 JSON、生命周期钩子、常见坑

---

## ⚠️ 常见问题

| 问题 | 解决 |
|------|------|
| 图标不显示 | 创建 `.tres` AtlasTexture 文件，确保 PNG 路径正确 |
| 本地化不生效 | 用 **Publish** 不是 Build，本地化是资源文件 |
| Harmony 报 .NET 版本错 | 编辑 `GodotPlugins.runtimeconfig.json` → `"version": "9.0.0"` |
| 场景脚本找不到 | 初始化调 `ScriptManagerBridge.LookupScriptsInAssembly` |
| Mod 不加载 | 检查 `assets/MyMod.json` 的 `id` 和文件名是否一致 |
| 自定义属性不保存 | 检查是否调了 `SavedPropertiesTypeCache.InjectTypeIntoCache()` |
| 搜不到某个类 | Rider `Shift+Shift` 搜 STS2 源码；或 `grep sts2-res` |

---

## 🛠 AI 助手

本仓库也是 AI 开发助手的知识库。向你的 AI 助手下指令，它会按 SKILL.md 的完整工作流帮你写代码：

- "帮我做一个 2 费攻击卡，伤害 8，升级后 11"
- "帮我做一个遗物，每回合 +1 能量"
- "帮我写一个 Harmony 补丁，让所有遗物都能在商店出现"
- "帮我做一个自定义事件，三选一奖励"

---

## 鸣谢

- **[STS2Plus](https://github.com/StephenSHorton/STS2Plus)** — 修正器架构、联机同步参考
- **[sts2mod](https://github.com/s1f102500012/sts2mod)** — Natsuki 12 模组合集
- **[YuWanCard](https://github.com/YuWan886/Sts2-YuWanCard)** — 生产级角色 Mod
- **[BaseLib](https://github.com/Alchyr/BaseLib-StS2)** — 社区框架
- **[ModTemplate](https://github.com/Alchyr/ModTemplate-StS2)** — 官方模板
- **[sts2-res](https://github.com/yehuoshun/sts2-res)** — 反编译源码参考
- **[Megadot](https://megadot.megacrit.com)** — STS2 专用引擎
- **B 站教程系列** — 9 篇完整 step-by-step

---

*干就完了。* 🦞
