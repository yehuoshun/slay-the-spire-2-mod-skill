# 跟随游戏版本升级（0.113 迁移实录）

> 一个 60 文件、24 个 API 调用点的示例仓库从 0.107.1 跳到 0.113，一次编译报出 **29 个错误**（9 个 `CS0115` 覆写断点 + 20 个签名/成员缺失），全部是**签名变动**，没有语义改动。本文件给出断点清单与可复用的排查流程。

## 一、0.107.1 → 0.113 的断点清单（实测）

| 断点 | 旧（≤0.107.1） | 0.113 |
|---|---|---|
| 角色动画器 | `GenerateAnimator(MegaSprite)` | `GenerateAnimator(MegaSprite controller, Creature creature)`；`MonsterModel` 仍是单参 |
| 生命条开关 | `MonsterModel.IsHealthBarVisible`（`virtual bool`） | **整个移除**。宠物与否看 `PetOwner` 非空，可否交互看 `IsInteractable` |
| 伤害钩子 | `ModifyDamageAdditive` / `…Multiplicative` / `…MultiplicativeLate` / `…DamageCap(Creature, decimal, ValueProp, Creature?, CardModel?)` | 末尾多一个 `CardPlay? cardPlay` |
| 格挡钩子 | `ModifyBlockAdditive` / `…Multiplicative(Creature, decimal, ValueProp, CardModel?)` | 末尾多 `CardPlay?`（注意 block 系列**没有** dealer 参数） |
| 攻击命令 | `AttackCommand.FromCard(CardModel)` | `FromCard(CardModel, CardPlay)`，`cardPlay` **必填**；类型在 `MegaCrit.Sts2.Core.Commands.Builders` |
| 伤害命令 | `CreatureCmd.Damage(..., cardSource: x)` | 带 `cardSource` 的重载必须紧跟 `cardPlay`；不带 `cardSource` 的两个重载仍可用 |
| 存档属性注册 | `SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(T))` | 该类型**整个删除**（缓存并入 `ModelIdSerializationCache` 并转私有），游戏自动扫描 mod 类型 |
| 球的被动 | `OrbModel.Trigger()` | 拆成 `TriggerPassive(ctx, target)`（播音效+调 `Passive`）、`PlayPassiveSfx()`、`ActivatePassive()`；在 `Passive` 里再调 `TriggerPassive` 会递归 |
| 悬停提示 | `new HoverTip { Id = …, IsDebuff = … }` 对象初始化器 | `Id`/`IsDebuff`/`IsSmart` 变 get-only 计算属性，只能走构造器（如 `(PowerModel power, string description, bool isSmart)`） |
| 随机数 | `Rng.Counter`（`uint` 种子） | 无 `Counter`；用 `ToSerializable().counter`；种子统一 `ulong`（`PlayerRngSet.Seed` 是 `ulong`） |

## 二、排查流程（下次跳版本照抄）

1. **换 dll**：从上游 release 下 `sts2.dll` + `0Harmony.dll` 到 `deps/`（`deps/` 进 `.gitignore`，别提交游戏二进制）。
2. **先编再猜**：`dotnet build -p:Sts2DataDir=<绝对路径>/deps`。`CS0115「没有找到适合的方法来重写」`= 父类签名变了，一次就能把所有 override 断点列全；`CS7036/CS1729` = 参数增减。
3. **查签名别用 `Assembly.LoadFrom`**：缺依赖（`GodotSharp` 等）时类型加载会崩成 StackOverflow。用 `MetadataLoadContext`（NuGet `System.Reflection.MetadataLoadContext`），并且**把依赖 dll 所在目录也塞进 `PathAssemblyResolver`**——否则枚举属性/参数类型时才抛 `FileNotFoundException`，而且 catch 不住。
4. **优先读上游 mod 库源码**：像 BaseLib 这种紧跟 beta 的库，master 里已经有「新版本该怎么写」，grep 它比 dump 快十倍，还能顺带看到兼容分支。
5. **改完双验证**：`dotnet build` 0 错误 + 自有 API 白名单脚本全绿，再 push 看 CI。
6. **版本号三处对齐**：CI/构建配置里的依赖版本（如 `DEPS_VERSION`）、`manifest.json` 的 `min_game_version`、README 里的说明。

## 三、踩过的坑

- **构建失败 ≠ 没跑**：`dotnet build` 报错后仍可能执行上一次成功编译的旧二进制，把旧输出当新结果会白折腾（表现为「改了没生效」）。先确认 exe 的修改时间。
- **Windows 控制台的编码**：python 脚本 `print("✅ …")` 在 GBK 代码页抛 `UnicodeEncodeError`，加 `$env:PYTHONIOENCODING='utf-8'` 即可，不是脚本 bug。
- **`git archive | tar -x` 在 PowerShell 里不通**（tar 去读 `\\.\tape0`）：改 `--format=zip -o x.zip` 再 `Expand-Archive`。
- **别只修编译**：`IsHealthBarVisible` 这类「属性被删」往往意味着判定规则换了（宠物判定从属性变成 `PetOwner` 非空），照抄替代 API 会写出行为不对的代码。

## 演进路线

- 当前：一次跳 6 个小版本，靠 dll 编译 + 上游源码对照，半天完成 24 个调用点迁移
- 后续：把本表做成 `#if` 版本分支的模板，跳版本时只改一处符号
