# 程序化卡牌生成 & 组件 API（AutoAnthony 东尼算法）

> ⚠️⚠️ **定位红线：本模块是「生态集成参考 + 架构思想库」，不是开发主路线。**
> 写普通卡牌/遗物/角色 → **一律走 `card/`、`relic/`、`character/` 等主模块**，禁止调用本模块任何 API。
> 本模块的 API（ComponentPackageApi/ComponentRuntimeApi/CardTinkeringApi 等）是 **AutoAnthony 自有生态契约**，仅在「开发 AutoAnthony 生态集成/随机卡池/卡牌编辑器」场景才可参考；其架构思想（本地化只是输出/冻结注册/概率与数值分离）可作设计借鉴，实现一律回到 sts2.dll 原生 API（硬规则 3）。

> 来源：[mewcodex/AutoAnthony](https://github.com/mewcodex/AutoAnthony) v0.3.147（2026-10-31 学习，HEAD 78e6c9d），STS2 0.111.x，**本体纯原生**（只依赖 sts2.dll + GodotSharp + 0Harmony，LangVersion 12 / net9.0）。

---

## 是什么

把原版卡牌拆成可复用的 unit operation（组件），再按出现率+数值策略重新组装成新卡池的随机 mod（灵感来自以撒 DELETE THIS）。支持全部 5 角色 + 2 无色池，覆盖 v111 全部 567 张非多人卡拆解目录（481 张可执行）。

## 核心思想（五条，最值得抄）

1. **本地化文本只是输出，不是逻辑**——语义全在 ASCII ID + 结构化 `OperationRuntimeSpec`，描述文本随便改不影响执行
2. **出现概率与数值价值分离**——occurrence policy 管「这个效果多常见」，value policy 管「数值多大」，互不干扰
3. **冻结注册**——所有注册（组件包/执行路由/关键词）在首次使用后冻结，杜绝战斗中途/多人同步后行为漂移
4. **快照存结构化数据**——存档/多人只传 RuntimeSpec 和操作 ID，不传本地化解释；schema 迁移 append-only
5. **外部角色零侵入**——不 patch 别人的 ModelDb/卡池，只提供注册 API，外部 mod 自己管模型/存档/多人传输

## 章节导航表

| 章节 | 文件 | 内容 |
|------|------|------|
| 架构与设计原则 | [cardgen-architecture.md](cardgen-architecture.md) | ComponentAtom/RuntimeSpec/语义标记/Multiplicity/schema 迁移/单工程双用 |
| API 速查 | [cardgen-api.md](cardgen-api.md) | 5 大注册 API 签名+代码模板+冻结规则 |
| 编辑器 API | [cardgen-editor.md](cardgen-editor.md) | CardTinkeringApi v6 / AutoAnthonyEditorApi v2 卡牌编辑流程 |
| 平衡模型 | [cardgen-balance.md](cardgen-balance.md) | 效果货币/价值表/触发频率/预算包络/稀缺缩放 |

## 常见问题

- **Q：AutoAnthony 的 API 是第三方依赖吗？** A：不是。它本体零第三方 mod 依赖（同我们 skill 路线），组件 API 是它**自己暴露**的公共契约（ApiContractSmoke 独立编译验证），接入方不要求 BaseLib/RitsuLib。
- **Q：我们写普通卡牌需要学这套吗？** A：不需要。手写单卡走 card/ 模块；这套是「程序化生成 + 生态互操作」场景（随机卡池、生成器、卡牌编辑器、外部角色集成）才需要。
- **Q：闭源枚举怎么扩？** A：`CardKeyword`/`CardTag` 是封闭枚举，AutoAnthony 用 ASCII 字符串 ID + `IComponentKeywordRuntimeAdapter` 投影，绝不硬造枚举成员（见 cardgen-api.md 自定义关键词）。
- **Q：生成的卡怎么保证平衡？** A：统一效果货币（100 单位 = 1 点单目标伤害）+ 整卡预算包络 + 原生频率先验，见 cardgen-balance.md。
