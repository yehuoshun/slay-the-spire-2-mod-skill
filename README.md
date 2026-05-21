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

## 🗂 项目结构（推荐）

```
MyMod/
├── MyMod.csproj
├── mod_manifest.json
├── MyModCode/
│   ├── MainFile.cs            ← 入口：[ModInitializer]
│   ├── Cards/                 ← 卡牌类
│   ├── Relics/                ← 遗物类
│   ├── Potions/               ← 药水类
│   ├── Powers/                ← 能力类
│   ├── Events/                ← 事件类
│   └── Patches/               ← Harmony 补丁
├── images/                    ← 贴图资源
│   ├── cards/
│   ├── relics/
│   ├── potions/
│   └── powers/
└── localization/              ← 本地化文本
    └── zh/
        ├── cards.json
        ├── relics.json
        └── ...
```

---

## 🧩 核心概念（5 分钟速览）

### 1. Harmony 补丁

游戏代码你改不了，但可以用 Harmony 在运行时注入你的逻辑：

```csharp
[HarmonyPatch(typeof(CardModel), "MaxUpgradeLevel", MethodType.Getter)]
static void Postfix(ref int __result)
{
    __result = 99;  // 所有卡牌可以升到 99 级
}
```

### 2. ModelDb — 模型注册表

游戏的「类型数据库」。你需要让游戏知道你的卡牌/遗物存在：

```csharp
// 方法一：[Pool] 自动注册（推荐）
[Pool(typeof(ColorlessCardPool))]
public class MyCard : CardModel { }

// 方法二：手动注册
ModHelper.AddModelToPool(typeof(SharedRelicPool), typeof(MyRelic));
```

### 3. DynamicVars — 动态变量

所有数值显示/修改都走 DynamicVar 系统。升级、遗物效果、能力层数都靠它：

```csharp
DynamicVars["Damage"].SetBase(8);       // 基础伤害 8
DynamicVars["Damage"].UpgradeValueBy(3); // 升级 +3 → 11
```

### 4. Build vs Publish

| 操作 | 做什么 | 什么时候用 |
|------|--------|-----------|
| **Build** | 仅编译 .dll → 复制到 mods | 纯代码改动 |
| **Publish** | 编译 + 生成 .pck + 复制全部 | 有资源改动（图片/场景/本地化） |

---

## 🃏 第一张卡牌（Hello World）

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;

namespace MyMod.Cards;

[Pool(typeof(ColorlessCardPool))]
public class HelloWorld : CardModel
{
    public HelloWorld() : base(1, CardType.Attack, CardRarity.Common, TargetType.Opponent) {}

    public override string PortraitPath => "res://MyMod/images/cards/hello_world.png";

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(6)
            .FromCard(this)
            .Targeting(ctx.Targets.First())
            .Execute();
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Damage"].UpgradeValueBy(3);  // 6→9
    }
}
```

四步：构造 → 图路径 → 打出效果 → 升级。就这么简单。

---

## 🔍 怎么查 API？（最重要的技能）

### 不猜签名——直接查源码

```bash
# 搜索 CardModel 类的 MaxUpgradeLevel
grep -rn "MaxUpgradeLevel" ~/sts2-res/src/

# 搜索 DamageCmd 的攻击链方法
grep -rn "class DamageCmd\|Attack(" ~/sts2-res/src/

# 搜索 RelicModel 有哪些可重写方法
grep -rn "virtual.*Task\|override.*Task" ~/sts2-res/src/Models/RelicModel.cs
```

### Rider 里更快

- `Ctrl+Click` 任意 STS2 类型 → 跳到反编译源码
- `Find Usages` 看调用方式
- `Shift+Shift` 全局搜索

---

## 🚀 参考项目

| 项目 | 值得学什么 |
|------|-----------|
| [STS2Plus](https://github.com/StephenSHorton/STS2Plus) | 修正器架构、TargetMethods 动态 Patch、安全检测、联机同步 |
| [YuWanCard](https://github.com/YuWan886/Sts2-YuWanCard) | 生产级角色 Mod、Builder 链式构造、分阶段初始化、SavedProperty 序列化 |
| [BaseLib](https://github.com/Alchyr/BaseLib-StS2) | CustomModel 快捷封装、ModConfig 设置面板 |
| [ModTemplate](https://github.com/Alchyr/ModTemplate-StS2) | 官方模板（空/内容/角色三种） |

> ⚡ **建议**：先看 STS2Plus 的 Patches 目录，理解 Harmony 补丁怎么写；再看 YuWanCard 的整体架构，理解生产级 Mod 怎么组织。

---

## ⚠️ 常见问题

| 问题 | 解决 |
|------|------|
| 图标不显示 | 创建 `.tres` AtlasTexture 文件，确保引用 PNG 路径正确 |
| 本地化不生效 | 用 **Publish**（不是 Build），因为本地化是资源文件 |
| Harmony 报 .NET 版本错 | 编辑 `GodotPlugins.runtimeconfig.json` → `"version": "9.0.0"` |
| 场景脚本找不到 | 初始化调用 `ScriptManagerBridge.LookupScriptsInAssembly` |
| Build 通过但 Mod 不加载 | 检查 `mod_manifest.json` 的 `id` 和文件名是否一致 |
| 搜不到某个类 | Rider 里 `Shift+Shift` 搜 STS2 源码；或 grep sts2-res |

---

## 🛠 AI 助手使用

本仓库也是 AI 开发助手的知识库。向你的 AI 助手下指令，它会按这份指南帮你写代码：

- "帮我做一个 2 费攻击卡，伤害 8，升级后 11"
- "帮我做一个遗物，每回合 +1 能量"
- "帮我写一个 Harmony 补丁，让所有遗物都能在商店出现"
- "帮我做一个自定义事件，三选一奖励"

---

## 鸣谢

- **[STS2Plus](https://github.com/StephenSHorton/STS2Plus)** — 修正器架构、多 modifier、联机同步参考实现
- **[YuWanCard](https://github.com/YuWan886/Sts2-YuWanCard)** — 生产级角色 Mod，分阶段初始化、SavedProperty、Builder Pattern
- **[BaseLib](https://github.com/Alchyr/BaseLib-StS2)** — 社区框架，CustomModel 系列、ModConfig 面板
- **[ModTemplate](https://github.com/Alchyr/ModTemplate-StS2)** — 官方模板，dotnet new 一键创建
- **[Megadot](https://megadot.megacrit.com)** — STS2 专用 Godot 引擎
- **B 站教程系列** — 从环境到怪物，9 篇完整 step-by-step

---

*干就完了。* 🦞