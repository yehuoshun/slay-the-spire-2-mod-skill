# 纯原生设计模式：常见坑与映射

## 常见坑（BaseLib / 第三方灵感参考）

> 以下来自 BaseLib / YuWanCard / ShunMod 等生产级 Mod 的实战经验。纯原生定位下**不直接使用这些 API**（本 skill 零第三方依赖），仅作灵感参考——遇到同类需求按「提炼 → 纯原生转译」处理。

| 问题 | 参考方案 | 来源 |
|------|---------|------|
| 设置 UI 自己写容易出 bug | 零 Harmony + Godot 纯信号方案（自建 UI） | ShunMod |
| 先古遗物不在图鉴显示 | 自定义注册表（`AncientRelicCompendiumPatch` 思路） | 自定义 |
| 多人模式状态不同步 | 确定性随机替代 `System.Random`（纯原生自实现种子随机） | YuWanCard |
| 多人模式专属卡牌 | `IsMultiplayerOnly` 标记防单人卡死 | YuWanCard |
| 卡牌奖励序列化丢失自定义卡池 | 兼容层思路（`CardRewardSerializationCompatibility`） | BaseLib |
| 临时能力核分支兼容 | `CustomTemporaryPowerModel` + `IBetaCompatTempPower` | BaseLib |
| 外部 Mod 卡牌目标兼容 | 反射桥接层（`ExternalCardTargetingCompat` 思路） | YuWanCard |
| 角色皮肤系统 | 接口 + 持久化选择器（`IYuWanCharacterSkinProvider` + `CharacterSkinSelectionManager`） | YuWanCard |
| 配置系统 | BaseLib 3.3+ 移除配置支持，YuWanCard 改用 RitsuLib | YuWanCard |
| 自定义资源每回合不重置 | `setEachTurn` / `StartOfTurnReset`（`BasicCustomResource`） | BaseLib |

---

## 各子项映射

| 设计模式 | 应用子项 |
|---------|---------|
| 自动注册（ContentRegistry + Attribute） | `serialization`（框架）、`relic`、`potion`、`power`、`enchantment`、`orb`、`act`、`pet` |
| 链式辅助方法 | `card`（CardFx）、`patterns/code-patterns` |
| 便捷 override | `card`（GainsBlock）、`relic`（路径回退）、`power`（图标回退） |
| 内联本地化 | 不转译，保持原生 JSON |
| 自定义资源系统 | `baselib`（参考 BaseLib 3.4.5，纯原生需自建，暂留备选） |

