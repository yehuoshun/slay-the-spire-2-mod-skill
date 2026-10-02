# references 完整文件级索引

> 📌 每个模块的 `xx.md` 为导航页（概述 + 常见问题 + **章节导航表**），正文按章节拆分在 `xx-*.md` 子文件中。读模块时先开导航页，再按需读子文件。

| 分类 | 文件 | 内容 |
|------|------|------|
| `setup/` | [environment-setup.md](setup/environment-setup.md) | 环境搭建、创建项目、PCK 打包、调试（教程） |
| `setup/` | [project-skeleton.md](setup/project-skeleton.md) | 生产级项目骨架（目录规范 + 路径检测 + 自动打 PCK） |
| `setup/` | [template-pack.md](setup/template-pack.md) | dotnet new 模板打包（三套官方模板、template.json、symbols） |
| `setup/` | [rider.md](setup/rider.md) | Rider 开发环境配置（代码检查、Live Templates） |
| `setup/` | [mod-manifest.md](setup/mod-manifest.md) | 模组清单 JSON（字段说明、纯原生方案） |
| `setup/` | [export-presets.md](setup/export-presets.md) | 导出预设 export_presets.cfg（PCK 打包必需） |
| `setup/` | [project-godot.md](setup/project-godot.md) | Godot 项目配置 project.godot（字段说明） |
| `setup/` | [ci-build.md](setup/ci-build.md) | CI 构建与打包（GitHub Actions 流水线） |
| `relic/` | [relic.md](relic/relic.md) | 自定义遗物（代码模板、稀有度、池、图标、本地化） |
| `card/` | [card.md](card/card.md) | 自定义卡牌（构造函数、API 速查、卡池、肖像、本地化） |
| `potion/` | [potion.md](potion/potion.md) | 自定义药水（属性、回调、图标、池、本地化） |
| `enchantment/` | [enchantment.md](enchantment/enchantment.md) | 自定义附魔（回调、附魔、本地化、图标） |
| `event/` | [event.md](event/event.md) | 自定义事件（选项、多页、先古之民、Patch、本地化） |
| `power/` | [power.md](power/power.md) | 自定义能力（Buff/Debuff、属性、回调、本地化） |
| `character/` | [character.md](character/character.md) | 自定义角色（卡池/遗物池/药水池、场景资源、注册） |
| `monster/` | [monster.md](monster/monster.md) | 自定义敌怪 & 遭遇（状态机、AI 行为树、注册） |
| `modifier/` | [modifier.md](modifier/modifier.md) | 自定义 Modifier（运行规则、Alignment、注册、效果实现） |
| `orb/` | [orb.md](orb/orb.md) | 自定义球体（被动/激发、图标、精灵、随机池） |
| `act/` | [act.md](act/act.md) | 自定义章节（地图背景、音乐、宝箱、房间配置） |
| `pet/` | [pet.md](pet/pet.md) | 自定义宠物（固定不行动 AI、血条、场景） |
| `resource/` | [resource.md](resource/resource.md) | 自定义资源（法力/怒气等 + 卡牌费用 + UI） |
| `badge/` | [badge.md](badge/badge.md) | 自定义模组徽章（Badge 继承、图标、注册） |
| `rest-site/` | [rest-site.md](rest-site/rest-site.md) | 自定义休息点选项（RestSiteOption 继承、图标、注入） |
| `pile/` | [pile.md](pile/pile.md) | 自定义牌堆（PileType 注入、定位、动画） |
| `reward/` | [reward.md](reward/reward.md) | 自定义奖励（RewardType 注入、序列化、示例） |
| `energy/` | [energy.md](energy/energy.md) | 自定义能量（图标、类型、视觉效果） |
| `harmony/` | [harmony.md](harmony/harmony.md) | Harmony 补丁模式（PatchCategory、安全、组织规范） |
| `harmony/` | [harmony-custom-power-sfx.md](harmony/harmony-custom-power-sfx.md) | Transpiler 实战：自定义能力音效（零第三方依赖） |
| `serialization/` | [serialization.md](serialization/serialization.md) | 序列化与注册（ModelDb、SavedProperty、InjectTypeIntoCache） |
| `settings/` | [settings.md](settings/settings.md) | 设置界面（纯原生：Attribute + ConfigFile + NSubmenu UI + 主菜单注入） |
| `baselib/` | [design-patterns.md](baselib/design-patterns.md) | 纯原生设计模式总纲（从 BaseLib 提炼，零第三方依赖） |
| `multiplayer/` | [multiplayer.md](multiplayer/multiplayer.md) | 多人模式（约束、身份检查、网络消息） |
