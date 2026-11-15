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
| 链式辅助方法 | `card`（CardFx） |
| 便捷 override | `card`（GainsBlock）、`relic`（路径回退）、`power`（图标回退） |
| 内联本地化 | 不转译，保持原生 JSON |
| 自定义资源系统 | `baselib`（参考 BaseLib 3.4.5，纯原生需自建，暂留备选） |


### 韧性写法清单（v3.4.8）

| 故障 | 写法 |
|---|---|
| 一个 mod 的监听器抛异常，中断整条 dispatch，后面的监听器全部不执行 | 逐个 `await` 包 try/catch（`OperationCanceledException` 除外，它表示游戏在拆 action），记日志后继续；布尔门 `All`/`Any` 不隔离（抛异常者没有中立真值） |
| 抛异常的监听器残留在选择上下文栈顶，之后所有玩家选择都错记到它头上 | `try { await Invoke(...); } finally { ctx.PopModel(model); }` —— pop 必须走 finally |
| 回调里改正在遍历的集合 → `InvalidOperationException` | 遍历前 `.ToList()` 快照（modifier 列表、UI 控件列表；`RemoveChildSafely` 会同步触发 `TreeExited` 改同一集合） |
| struct 用 `FirstOrDefault` 判「找不到」，结果拿到 `default` 当合法值 | 写 `FirstOrNullStruct` 返回 `TSource?` |
| 空池/权重全 0 抛裸 `IndexOutOfRangeException`，看不出谁干的 | 抛带上下文的 `InvalidOperationException`：哪个模型、哪个过滤条件把池清空了 |
| 语言目录探测在 `<lang>/` 内部找子目录，永远为空 | 在上一层 `ListDirectory("res://<id>/localization")`，用 `entry.EndsWith('/')` 认子目录再 `TrimEnd('/')` |
| 全局命名空间的 config 类前缀为空串，`prefix[..^1]` 抛 `ArgumentOutOfRangeException` 打死整个设置界面 | 先判 `prefix.Length > 0 && prefix[^1] == 分隔符`，再逐级回退 根命名空间 → 程序集名 → "Unknown mod name" |
| 存档引用了已卸载 mod 的模型，`GetById<T>` 抛异常毁掉整份存档读取 | 用 `GetByIdOrNull<T>(id) as T`，单条丢弃 + Warn；没有占位模型的类型（如 CardModifier）只能丢 |
| 泛型互操作对非泛型方法 `MakeGenericMethod`，直接炸 | 先 `!target.IsGenericMethodDefinition` 跳过，再比对泛型参数个数 |
