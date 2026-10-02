# 实战项目学习：YuWanCard

> 来源：[YuWan886/Sts2-YuWanCard](https://github.com/YuWan886/Sts2-YuWanCard)（v0.5.12，2026-10-02 学习快照）
> 定位：真实上架 Steam 创意工坊的活跃大型 mod（696 个 C# 文件 / 6.3 万行，多角色、多人、Balatro 模式）。
> 价值：**用真实代码校验本 skill 的 API 知识**，并提炼可复用的纯原生模式。

---

## 项目概览

| 项 | 内容 |
|----|------|
| 角色 | 自定义角色「猪」（Pig），含卡牌/遗物/能力/附魔/怪物/遭遇/事件/球/药水全量内容 |
| 游戏版本 | 0.107.1（含 0.110.x 适配排期） |
| 依赖 | 游戏原生 API + `Alchyr.Sts2.ModAnalyzers`（源码生成器）+ `Krafs.Publicizer` + `BSchneppe.StS2.PckPacker`（均为构建期工具） |
| 特色 | 多版本 loader 变体机制、Harmony Transpiler 模组互操作、自定义遗物稀有度、多人自定义消息、生命条预测、Balatro 模式 |
| 注意 | 项目自带大量自研基类（`YuWan*Model`），**不是游戏 API 本身**，但每个基类都直接调用原生 API，是绝佳的 API 用法样本 |

## 为什么值得学

1. **API 真实性**：所有代码在真实游戏环境编译运行，签名、命名空间、钩子名 100% 可信
2. **框架思想**：自研 ContentRegistry / ModPatcher / loader 等，可提炼为纯原生模式
3. **边界知识**：多人模式、Android AOT 兼容、版本兼容等 skill 空白领域

## 章节导航

| 文件 | 内容 |
|------|------|
| [yuwan-card-api.md](yuwan-card-api.md) | 真实 API 校验结论（签名确认表 + 与 skill 差异清单） |
| [yuwan-card-patterns-card.md](yuwan-card-patterns-card.md) | 卡牌/能力实战模式（升级机制、关键字、计算伤害、持久化） |
| [yuwan-card-patterns-relic.md](yuwan-card-patterns-relic.md) | 遗物/高级模式（自定义稀有度、生命条预测、金币保护） |
| [yuwan-card-multiplayer.md](yuwan-card-multiplayer.md) | 多人模式（约束、身份检查、自定义网络消息） |
| [yuwan-card-architecture.md](yuwan-card-architecture.md) | 工程架构与工具链（注册系统、loader、Interop、构建） |

## 使用规则

1. 写代码前先查 [api-reference](../patterns/api-reference.md)（原生签名），本模块只做补充印证
2. 本模块的「模式」均为 YuWan 自研框架的**设计思想**，落地时按本 skill 硬规则 3 转成纯原生写法
3. 引用真实代码片段时注意：片段依赖 YuWan 自研基类，**不可直接复制**，需替换为原生基类（`CardModel`/`PowerModel`/`RelicModel`…）
