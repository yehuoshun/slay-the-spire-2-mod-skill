# 杀戮尖塔2 Mod 开发

帮你写 Slay the Spire 2 模组：项目搭建、Harmony 补丁、自定义卡牌/修正器、Godot UI、联机适配。

## 能做什么

- **项目搭建** — .csproj 模板、mod_manifest.json、入口点，复制即用
- **Harmony 补丁** — 固定目标、动态目标、方法体替换，三种模式覆盖所有场景
- **自定义卡牌** — 链式配置基类 + PoolAttribute 自动注册，添卡不改入口
- **自定义修正器** — SyncedModifierModel 模式 + 双路状态检查
- **Godot UI** — CanvasLayer 挂载、Overlay 阻塞检测
- **联机适配** — Host/Client 检测、交互锁定
- **本地化** — JSON 文件 + LocString 合并
- **实用模式** — 防重复处理、弱引用存储、反射+事件触发

## 快速开始

```bash
cd ~/.openclaw/workspace/skills
git clone https://github.com/yehuoshun/slay-the-spire-2-mod-skill.git
```

对话中说：

```
帮我写一个杀戮尖塔2的模组
给我一个 STS2 Mod 的项目模板
怎么给 STS2 添加卡牌
```

## 技术参考

[STS2Plus](https://github.com/StephenSHorton/STS2Plus) — 修正器、UI 组件、联机同步

## 作者

**yehuoshun** 🦞
